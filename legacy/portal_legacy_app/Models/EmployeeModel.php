<?php
/**
 * =====================================================================
 * VINAMILK HRM - MODEL QUẢN LÝ HỒ SƠ NHÂN SỰ (nhansu)
 * =====================================================================
 * Tuân thủ nghiêm ngặt quy tắc bắt buộc:
 *   - 100% TRUY VẤN DANH SÁCH NHÂN SỰ BẮT BUỘC QUA HÀM scopeByUnit()
 *   - LỌC THEO CÂY ĐƠN VỊ CỦA NGƯỜI ĐĂNG NHẬP (RECURSIVE CTE)
 *   - BỘ LỌC ĐA CHIỀU & TÌM KIẾM NÂNG CAO AND/OR
 *   - QUẢN LÝ 8 TAB CHI TIẾT HỒ SƠ & TIẾP NHẬN WIZARD 4 BƯỚC
 *   - VALIDATE: CCCD 12 số, MST 10/13 số, Tuổi >= 18, Email duy nhất
 *   - 100% PREPARED STATEMENTS & GHI AUDIT_LOG
 */

require_once __DIR__ . '/../core/Model.php';

class EmployeeModel extends Model
{
    protected string $table = 'nhansu';
    protected string $primaryKey = 'MaNV';

    /**
     * LẤY DANH SÁCH NHÂN SỰ ĐƯỢC PHÉP TRUY CẬP (QUY TẮC BẮT BUỘC: scopeByUnit)
     * Hỗ trợ tìm kiếm nâng cao AND / OR: giới tính, độ tuổi, trình độ học vấn, thâm niên
     * 
     * @param array $filters Bộ lọc tìm kiếm
     * @param int $limit Số lượng bản ghi mỗi trang
     * @param int $offset Vị trí bắt đầu
     * @return array Danh sách nhân sự kèm phân trang
     */
    public function getEmployees(array $filters = [], int $limit = 25, int $offset = 0): array
    {
        // 1. ÁP DỤNG PHÂN QUYỀN TRUY CẬP CÂY ĐƠN VỊ BẮT BUỘC
        $scope = $this->scopeByUnit('ns', 'MaDV');
        $baseWhereClauses = [$scope['where']];
        $params = $scope['params'];

        // 2. Bộ lọc cơ bản: Từ khóa tìm kiếm
        if (!empty($filters['keyword'])) {
            $kw = '%' . trim($filters['keyword']) . '%';
            $baseWhereClauses[] = "(ns.HoTen LIKE :kw_hoten OR ns.MaNV LIKE :kw_manv OR ns.SoCMND_CCCD LIKE :kw_cccd OR ns.Email LIKE :kw_email)";
            $params[':kw_hoten'] = $kw;
            $params[':kw_manv']  = $kw;
            $params[':kw_cccd']  = $kw;
            $params[':kw_email'] = $kw;
        }

        // Đơn vị (theo cây con đệ quy)
        if (!empty($filters['ma_dv']) && $filters['ma_dv'] !== 'ALL') {
            $filterUnits = $this->getDescendantUnits($filters['ma_dv']);
            $uKeys = [];
            foreach ($filterUnits as $idx => $uCode) {
                $k = ':filter_u_' . $idx;
                $uKeys[] = $k;
                $params[$k] = $uCode;
            }
            $baseWhereClauses[] = "ns.MaDV IN (" . implode(', ', $uKeys) . ")";
        }

        // Chức vụ
        if (!empty($filters['ma_cv']) && $filters['ma_cv'] !== 'ALL') {
            $baseWhereClauses[] = "ns.MaCV = :filter_macv";
            $params[':filter_macv'] = $filters['ma_cv'];
        }

        // Trạng thái (1: Đang làm, 2: Thử việc, 3: Nghỉ việc, 4: Nghỉ hưu)
        if (isset($filters['trang_thai']) && $filters['trang_thai'] !== '' && $filters['trang_thai'] !== 'ALL') {
            $baseWhereClauses[] = "ns.TrangThai = :flt_trangthai";
            $params[':flt_trangthai'] = (int)$filters['trang_thai'];
        }

        // Loại hợp đồng
        if (!empty($filters['loai_hd']) && $filters['loai_hd'] !== 'ALL') {
            $baseWhereClauses[] = "ns.LoaiHopDong LIKE :flt_loaihd";
            $params[':flt_loaihd'] = '%' . $filters['loai_hd'] . '%';
        }

        // 3. TÌM KIẾM NÂNG CAO (Hỗ trợ chế độ logic AND hoặc OR)
        $advancedClauses = [];
        $advancedLogic = strtoupper($filters['advanced_logic'] ?? 'AND');
        if (!in_array($advancedLogic, ['AND', 'OR'], true)) {
            $advancedLogic = 'AND';
        }

        // Giới tính
        if (!empty($filters['gioi_tinh']) && $filters['gioi_tinh'] !== 'ALL') {
            $advancedClauses[] = "ns.GioiTinh = :flt_gioitinh";
            $params[':flt_gioitinh'] = $filters['gioi_tinh'];
        }

        // Khoảng tuổi (tính theo TIMESTAMPDIFF so với CURDATE)
        if (!empty($filters['age_min'])) {
            $advancedClauses[] = "TIMESTAMPDIFF(YEAR, ns.NgaySinh, CURDATE()) >= :flt_agemin";
            $params[':flt_agemin'] = (int)$filters['age_min'];
        }
        if (!empty($filters['age_max'])) {
            $advancedClauses[] = "TIMESTAMPDIFF(YEAR, ns.NgaySinh, CURDATE()) <= :flt_agemax";
            $params[':flt_agemax'] = (int)$filters['age_max'];
        }

        // Trình độ học vấn
        if (!empty($filters['trinh_do']) && $filters['trinh_do'] !== 'ALL') {
            $advancedClauses[] = "ns.TrinhDoHocVan = :flt_trinhdo";
            $params[':flt_trinhdo'] = $filters['trinh_do'];
        }

        // Thâm niên công tác (tính theo số năm từ NgayVaoLam)
        if (!empty($filters['seniority']) && $filters['seniority'] !== 'ALL') {
            switch ($filters['seniority']) {
                case 'under_1':
                    $advancedClauses[] = "TIMESTAMPDIFF(YEAR, ns.NgayVaoLam, CURDATE()) < 1";
                    break;
                case '1_to_3':
                    $advancedClauses[] = "TIMESTAMPDIFF(YEAR, ns.NgayVaoLam, CURDATE()) BETWEEN 1 AND 3";
                    break;
                case '3_to_5':
                    $advancedClauses[] = "TIMESTAMPDIFF(YEAR, ns.NgayVaoLam, CURDATE()) BETWEEN 3 AND 5";
                    break;
                case 'over_5':
                    $advancedClauses[] = "TIMESTAMPDIFF(YEAR, ns.NgayVaoLam, CURDATE()) >= 5";
                    break;
            }
        }

        // Ghép mệnh đề nâng cao theo logic AND/OR
        if (!empty($advancedClauses)) {
            if ($advancedLogic === 'OR') {
                $baseWhereClauses[] = "(" . implode(' OR ', $advancedClauses) . ")";
            } else {
                foreach ($advancedClauses as $c) {
                    $baseWhereClauses[] = $c;
                }
            }
        }

        $whereSql = implode(' AND ', $baseWhereClauses);

        // 4. Đếm tổng số bản ghi
        $countSql = "SELECT COUNT(*) as total 
                     FROM nhansu ns 
                     LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                     WHERE {$whereSql}";
        $totalRow = $this->fetchOne($countSql, $params);
        $totalRecords = (int)($totalRow['total'] ?? 0);

        // 5. Truy vấn danh sách bản ghi
        $sql = "SELECT ns.*, dv.TenDV, dv.LoaiDV, dv.CapDV, cv.TenCV,
                       TIMESTAMPDIFF(YEAR, ns.NgaySinh, CURDATE()) as Tuoi,
                       TIMESTAMPDIFF(YEAR, ns.NgayVaoLam, CURDATE()) as ThamNienNam,
                       DATEDIFF(ns.NgayHetHanHD, CURDATE()) as SoNgayConLaiHD
                FROM nhansu ns
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                WHERE {$whereSql}
                ORDER BY ns.MaDV ASC, ns.HoTen ASC
                LIMIT {$limit} OFFSET {$offset}";

        $rows = $this->fetchAll($sql, $params);
        $maskedRows = array_map('mask_sensitive_employee', $rows);

        return [
            'data'         => $maskedRows,
            'total'        => $totalRecords,
            'limit'        => $limit,
            'offset'       => $offset,
            'current_page' => floor($offset / $limit) + 1,
            'total_pages'  => ceil($totalRecords / $limit)
        ];
    }

    /**
     * LẤY TOÀN BỘ DANH SÁCH ĐỂ XUẤT EXCEL (Tôn trọng scopeByUnit)
     */
    public function exportEmployees(array $filters = []): array
    {
        $res = $this->getEmployees($filters, 10000, 0);
        return $res['data'];
    }

    /**
     * LẤY CHI TIẾT 1 HỒ SƠ NHÂN SỰ CÓ KIỂM TRA PHẠM VI scopeByUnit()
     */
    public function getEmployeeById(string $maNV): ?array
    {
        $scope = $this->scopeByUnit('ns', 'MaDV');
        $sql = "SELECT ns.*, dv.TenDV, dv.LoaiDV, dv.CapDV, dv.DiaChi as DiaChiDonVi, 
                       cv.TenCV, cv.HeSoCV, cv.PhuCapCV,
                       ql.HoTen as TenQuanLy,
                       TIMESTAMPDIFF(YEAR, ns.NgaySinh, CURDATE()) as Tuoi,
                       TIMESTAMPDIFF(YEAR, ns.NgayVaoLam, CURDATE()) as ThamNienNam,
                       DATEDIFF(ns.NgayHetHanHD, CURDATE()) as SoNgayConLaiHD
                FROM nhansu ns
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                LEFT JOIN nhansu ql ON ns.MaNV_QuanLy = ql.MaNV
                WHERE ns.MaNV = :manv AND ({$scope['where']})
                LIMIT 1";
        
        $params = array_merge($scope['params'], [':manv' => $maNV]);
        $row = $this->fetchOne($sql, $params);
        return $row ? mask_sensitive_employee($row) : null;
    }

    /**
     * TAB 3: QUÁ TRÌNH CÔNG TÁC (su_kien_nhan_su)
     */
    public function getCareerHistory(string $maNV): array
    {
        $sql = "SELECT sk.*, 
                       dv_cu.TenDV as TenDonViCu, dv_moi.TenDV as TenDonViMoi,
                       cv_cu.TenCV as TenChucVuCu, cv_moi.TenCV as TenChucVuMoi
                FROM su_kien_nhan_su sk
                LEFT JOIN dm_donvi dv_cu ON sk.don_vi_cu = dv_cu.MaDV
                LEFT JOIN dm_donvi dv_moi ON sk.don_vi_moi = dv_moi.MaDV
                LEFT JOIN dm_chucvu cv_cu ON sk.chuc_vu_cu = cv_cu.MaCV
                LEFT JOIN dm_chucvu cv_moi ON sk.chuc_vu_moi = cv_moi.MaCV
                WHERE sk.ma_nv = :manv
                ORDER BY sk.ngay_hieu_luc DESC, sk.id DESC";

        return $this->fetchAll($sql, [':manv' => $maNV]);
    }

    /**
     * TAB 4: LỊCH SỬ LƯƠNG & ĐIỀU CHỈNH LƯƠNG
     */
    public function getSalaryHistory(string $maNV): array
    {
        $sql = "SELECT sk.*
                FROM su_kien_nhan_su sk
                WHERE sk.ma_nv = :manv AND sk.loai_su_kien IN ('TANG_LUONG', 'TUYEN_MOI', 'KY_HOP_DONG')
                ORDER BY sk.ngay_hieu_luc DESC, sk.id DESC";

        $rows = $this->fetchAll($sql, [':manv' => $maNV]);
        
        // Che số tiền lương nếu không có quyền luong.sensitive
        if (!Auth::canViewSensitive('luong') && Auth::maNV() !== $maNV) {
            foreach ($rows as &$r) {
                if (isset($r['luong_cu'])) $r['luong_cu'] = 0;
                if (isset($r['luong_moi'])) $r['luong_moi'] = 0;
            }
            unset($r);
        }
        return $rows;
    }

    /**
     * TAB 5: ĐÀO TẠO & CHỨNG CHỈ (chung_chi_nhan_vien)
     * Kèm cảnh báo 60 ngày & cờ chặn xếp ca dây chuyền nếu thẻ ATTP hết hạn
     */
    public function getCertificates(string $maNV): array
    {
        $sql = "SELECT cc.*, 
                       DATEDIFF(cc.ngay_het_han, CURDATE()) as so_ngay_con_lai,
                       CASE WHEN cc.ngay_het_han < CURDATE() THEN 1 ELSE 0 END as da_het_han,
                       CASE WHEN cc.ngay_het_han >= CURDATE() AND cc.ngay_het_han <= DATE_ADD(CURDATE(), INTERVAL 60 DAY) THEN 1 ELSE 0 END as sap_het_han_60ngay,
                       CASE WHEN (cc.loai_chung_chi = 'THE_XANH_ATTP' OR cc.la_the_xanh_attp = 1) AND cc.ngay_het_han < CURDATE() THEN 1 ELSE 0 END as co_nguy_co_chan_ca
                FROM chung_chi_nhan_vien cc
                WHERE cc.ma_nv = :manv
                ORDER BY cc.ngay_het_han ASC, cc.id DESC";

        return $this->fetchAll($sql, [':manv' => $maNV]);
    }

    /**
     * TAB 6: KHEN THƯỞNG & KỶ LUẬT (khen_thuong_ky_luat)
     */
    public function getRewardsAndDisciplines(string $maNV): array
    {
        $sql = "SELECT kt.* 
                FROM khen_thuong_ky_luat kt
                WHERE kt.ma_nv = :manv
                ORDER BY kt.ngay_quyet_dinh DESC, kt.id DESC";

        return $this->fetchAll($sql, [':manv' => $maNV]);
    }

    /**
     * Thêm mới quyết định Khen thưởng / Kỷ luật
     */
    public function addRewardDiscipline(array $data): int
    {
        $sql = "INSERT INTO khen_thuong_ky_luat 
                (ma_nv, loai, hinh_thuc, so_quyet_dinh, ngay_quyet_dinh, ngay_hieu_luc, co_quan_ban_hanh, nguoi_ky, ly_do, so_tien, ghi_chu)
                VALUES 
                (:ma_nv, :loai, :hinh_thuc, :so_quyet_dinh, :ngay_quyet_dinh, :ngay_hieu_luc, :co_quan_ban_hanh, :nguoi_ky, :ly_do, :so_tien, :ghi_chu)";

        $this->query($sql, [
            ':ma_nv'            => $data['ma_nv'],
            ':loai'             => $data['loai'],
            ':hinh_thuc'        => $data['hinh_thuc'],
            ':so_quyet_dinh'    => $data['so_quyet_dinh'] ?? '',
            ':ngay_quyet_dinh'  => $data['ngay_quyet_dinh'],
            ':ngay_hieu_luc'    => $data['ngay_hieu_luc'] ?? $data['ngay_quyet_dinh'],
            ':co_quan_ban_hanh' => $data['co_quan_ban_hanh'] ?? 'Công ty CP Sữa Việt Nam',
            ':nguoi_ky'         => $data['nguoi_ky'] ?? '',
            ':ly_do'            => $data['ly_do'],
            ':so_tien'          => (float)($data['so_tien'] ?? 0),
            ':ghi_chu'          => $data['ghi_chu'] ?? ''
        ]);

        $id = (int)$this->lastInsertId();

        // Đồng bộ sự kiện nhân sự
        $this->query(
            "INSERT INTO su_kien_nhan_su (ma_nv, loai_su_kien, ngay_hieu_luc, so_quyet_dinh, nguoi_ky, ly_do_bien_dong, nguoi_ghi_nhan, created_at)
             VALUES (:ma_nv, :loai_sk, :ngay_hl, :so_qd, :nguoi_ky, :ly_do, :nguoi_ghi, NOW())",
            [
                ':ma_nv'     => $data['ma_nv'],
                ':loai_sk'   => $data['loai'] === 'KHEN_THUONG' ? 'KHEN_THUONG' : 'KY_LUAT',
                ':ngay_hl'   => $data['ngay_quyet_dinh'],
                ':so_qd'     => $data['so_quyet_dinh'] ?? '',
                ':nguoi_ky'  => $data['nguoi_ky'] ?? '',
                ':ly_do'     => $data['hinh_thuc'] . ': ' . $data['ly_do'],
                ':nguoi_ghi' => Auth::username() ?? 'SYSTEM'
            ]
        );

        $this->logAudit('CREATE', 'khen_thuong_ky_luat', (string)$id, null, $data);
        return $id;
    }

    /**
     * TAB 7: TÀI SẢN CẤP PHÁT & PPE (cap_phat_tai_san)
     * Quản lý giá trị, ký nhận điện tử và tình trạng thu hồi / khấu trừ lương
     */
    public function getAssets(string $maNV): array
    {
        $sql = "SELECT ts.* 
                FROM cap_phat_tai_san ts
                WHERE ts.ma_nv = :manv
                ORDER BY ts.ngay_cap_phat DESC, ts.id DESC";

        return $this->fetchAll($sql, [':manv' => $maNV]);
    }

    /**
     * Cấp phát tài sản mới cho nhân viên (có ký nhận và biên bản bàn giao)
     */
    public function addAsset(array $data): int
    {
        $maBienBan = 'BB-CP-' . date('Y') . '-' . strtoupper(substr(uniqid(), -5));
        $isSigned = !empty($data['da_ky_nhan_cap']) ? 1 : 0;
        $hashSignature = $isSigned ? hash('sha256', ($data['ma_nv'] ?? '') . '|' . ($data['ten_tai_san'] ?? '') . '|' . time()) : null;

        $sql = "INSERT INTO cap_phat_tai_san 
                (ma_nv, loai_tai_san, ten_tai_san, ma_dinh_danh_serial, so_luong, don_vi_tinh, gia_tri_tai_san, gia_tri_khau_tru, ngay_cap_phat, tinh_trang_khi_cap, nguoi_giao, nguoi_nhan_ky, da_ky_nhan_cap, ngay_ky_nhan_cap, ma_bien_ban, chu_ky_hash, trang_thai, ghi_chu)
                VALUES 
                (:ma_nv, :loai_tai_san, :ten_tai_san, :ma_dinh_danh_serial, :so_luong, :don_vi_tinh, :gia_tri_tai_san, :gia_tri_khau_tru, :ngay_cap_phat, :tinh_trang_khi_cap, :nguoi_giao, :nguoi_nhan_ky, :da_ky_nhan_cap, :ngay_ky_nhan_cap, :ma_bien_ban, :chu_ky_hash, 'DANG_SU_DUNG', :ghi_chu)";

        $this->query($sql, [
            ':ma_nv'               => $data['ma_nv'],
            ':loai_tai_san'        => $data['loai_tai_san'],
            ':ten_tai_san'         => $data['ten_tai_san'],
            ':ma_dinh_danh_serial' => $data['ma_dinh_danh_serial'] ?? '',
            ':so_luong'            => (int)($data['so_luong'] ?? 1),
            ':don_vi_tinh'         => $data['don_vi_tinh'] ?? 'Chiếc',
            ':gia_tri_tai_san'     => (float)($data['gia_tri_tai_san'] ?? 0),
            ':gia_tri_khau_tru'    => (float)($data['gia_tri_khau_tru'] ?? 0),
            ':ngay_cap_phat'       => $data['ngay_cap_phat'] ?? date('Y-m-d'),
            ':tinh_trang_khi_cap'  => $data['tinh_trang_khi_cap'] ?? 'Mới',
            ':nguoi_giao'          => $data['nguoi_giao'] ?? (Auth::username() ?? 'Cán bộ QLTS'),
            ':nguoi_nhan_ky'       => $data['ma_nv'],
            ':da_ky_nhan_cap'      => $isSigned,
            ':ngay_ky_nhan_cap'    => $isSigned ? date('Y-m-d H:i:s') : null,
            ':ma_bien_ban'         => $maBienBan,
            ':chu_ky_hash'         => $hashSignature,
            ':ghi_chu'             => $data['ghi_chu'] ?? ''
        ]);

        $id = (int)$this->lastInsertId();
        $this->logAudit('CREATE', 'cap_phat_tai_san', (string)$id, null, $data);
        return $id;
    }

    /**
     * TAB 9: QUY TRÌNH THÔI VIỆC (offboarding_process)
     */
    public function getOffboarding(string $maNV): ?array
    {
        $sql = "SELECT op.*,
                       (op.buoc_1_ban_giao_cong_viec + op.buoc_2_thu_hoi_tai_san + op.buoc_3_khoa_tai_khoan + 
                        op.buoc_4_chot_cong + op.buoc_5_quyet_toan_luong + op.buoc_6_chot_bhxh) AS so_buoc_hoan_thanh
                FROM offboarding_process op
                WHERE op.ma_nv = :manv
                ORDER BY op.id DESC LIMIT 1";

        return $this->fetchOne($sql, [':manv' => $maNV]);
    }

    /**
     * KHẢO SÁT LÝ DO NGHỈ VIỆC CỦA NHÂN VIÊN
     */
    public function getExitSurvey(string $maNV): ?array
    {
        $sql = "SELECT * FROM exit_interview_survey WHERE ma_nv = :manv ORDER BY id DESC LIMIT 1";
        return $this->fetchOne($sql, [':manv' => $maNV]);
    }

    /**
     * =================================================================
     * VALIDATION NGUYÊN TẮC: CCCD 12 số, MST 10/13 số, Tuổi >= 18, Email duy nhất
     * =================================================================
     * 
     * @param array $data Dữ liệu cần validate
     * @param string|null $currentMaNV Mã NV nếu đang cập nhật (để loại trừ kiểm tra email trùng)
     * @return array Danh sách thông báo lỗi (trống nếu hợp lệ)
     */
    public function validateEmployeeData(array $data, ?string $currentMaNV = null): array
    {
        $errors = [];

        // 1. Kiểm tra Họ và tên
        if (empty(trim($data['HoTen'] ?? ''))) {
            $errors['HoTen'] = 'Họ và tên không được để trống.';
        }

        // 2. Validate CCCD 12 số (chính xác 12 chữ số)
        $cccd = trim($data['SoCMND_CCCD'] ?? '');
        if (empty($cccd)) {
            $errors['SoCMND_CCCD'] = 'Số CCCD không được để trống.';
        } elseif (!preg_match('/^[0-9]{12}$/', $cccd)) {
            $errors['SoCMND_CCCD'] = 'Số CCCD không hợp lệ (Phải đúng chuẩn 12 chữ số).';
        }

        // 3. Validate MST cá nhân: 10 hoặc 13 chữ số
        if (!empty($data['MaSoThue'])) {
            $mst = str_replace('-', '', trim($data['MaSoThue']));
            if (!preg_match('/^[0-9]{10}$|^[0-9]{13}$/', $mst)) {
                $errors['MaSoThue'] = 'Mã số thuế cá nhân không hợp lệ (Phải gồm đúng 10 hoặc 13 chữ số).';
            }
        }

        // 4. Validate Tuổi >= 18 tuổi
        if (empty($data['NgaySinh'])) {
            $errors['NgaySinh'] = 'Ngày sinh không được để trống.';
        } else {
            try {
                $dob = new DateTime($data['NgaySinh']);
                $today = new DateTime('today');
                $age = $dob->diff($today)->y;
                if ($age < 18) {
                    $errors['NgaySinh'] = "Người lao động chưa đủ tuổi lao động (Hiện tại {$age} tuổi, yêu cầu tối thiểu 18 tuổi).";
                }
            } catch (Exception $e) {
                $errors['NgaySinh'] = 'Định dạng ngày sinh không hợp lệ.';
            }
        }

        // 5. Validate Email hợp lệ & DUY NHẤT
        $email = trim($data['Email'] ?? '');
        if (empty($email)) {
            $errors['Email'] = 'Email không được để trống.';
        } elseif (!filter_var($email, FILTER_VALIDATE_EMAIL)) {
            $errors['Email'] = 'Địa chỉ Email không đúng định dạng.';
        } else {
            $checkSql = "SELECT MaNV FROM nhansu WHERE Email = :email";
            $params = [':email' => $email];
            if (!empty($currentMaNV)) {
                $checkSql .= " AND MaNV != :current_manv";
                $params[':current_manv'] = $currentMaNV;
            }
            $existing = $this->fetchOne($checkSql, $params);
            if ($existing) {
                $errors['Email'] = "Email [{$email}] đã tồn tại trong hệ thống (nhân sự {$existing['MaNV']}).";
            }
        }

        // 6. Kiểm tra đơn vị
        if (empty($data['MaDV'])) {
            $errors['MaDV'] = 'Vui lòng chọn đơn vị công tác.';
        }

        return $errors;
    }

    /**
     * =================================================================
     * WIZARD TIẾP NHẬN NHÂN VIÊN MỚI 4 BƯỚC
     * =================================================================
     * Bước 1: Thông tin cá nhân
     * Bước 2: Đơn vị / Chức vụ
     * Bước 3: Hợp đồng / Lương
     * Bước 4: Cấp tài khoản & Tài sản
     * 
     * @param array $wizardData
     * @return string MaNV vừa được tạo
     * @throws Exception Nếu validation thất bại hoặc lỗi transaction
     */
    public function createFullEmployeeWithWizard(array $wizardData): string
    {
        // 1. Kiểm tra validation
        $errors = $this->validateEmployeeData($wizardData);
        if (!empty($errors)) {
            throw new InvalidArgumentException(implode(' | ', $errors));
        }

        $this->beginTransaction();
        try {
            // Tự động sinh mã nhân sự kế tiếp
            $maNV = $this->generateNextEmployeeCode();

            // 2. Chèn dữ liệu vào bảng nhansu
            $nhansuFields = [
                'MaNV'             => $maNV,
                'HoTen'            => trim($wizardData['HoTen']),
                'TenThuongGoi'     => trim($wizardData['TenThuongGoi'] ?? ''),
                'NgaySinh'         => $wizardData['NgaySinh'],
                'GioiTinh'         => $wizardData['GioiTinh'] ?? 'Nam',
                'SoCMND_CCCD'      => trim($wizardData['SoCMND_CCCD']),
                'NgayCap'          => !empty($wizardData['NgayCap']) ? $wizardData['NgayCap'] : null,
                'NoiCap'           => trim($wizardData['NoiCap'] ?? ''),
                'QueQuan'          => trim($wizardData['QueQuan'] ?? ''),
                'DiaChiHienTai'    => trim($wizardData['DiaChiHienTai'] ?? ''),
                'Email'            => trim($wizardData['Email']),
                'SoDienThoai'      => trim($wizardData['SoDienThoai'] ?? ''),
                'MaSoThue'         => !empty($wizardData['MaSoThue']) ? trim($wizardData['MaSoThue']) : null,
                'TrinhDoHocVan'    => trim($wizardData['TrinhDoHocVan'] ?? 'Đại học'),
                'ChuyenNganh'      => trim($wizardData['ChuyenNganh'] ?? ''),
                'TruongDaoTao'     => trim($wizardData['TruongDaoTao'] ?? ''),
                'NgayVaoLam'       => $wizardData['NgayVaoLam'] ?? date('Y-m-d'),
                'MaDV'             => $wizardData['MaDV'],
                'MaCV'             => $wizardData['MaCV'] ?? 'NV',
                'VungLuong'        => (int)($wizardData['VungLuong'] ?? 1),
                'MaNV_QuanLy'      => !empty($wizardData['MaNV_QuanLy']) ? $wizardData['MaNV_QuanLy'] : null,
                'HeSoLuongHienTai' => (float)($wizardData['HeSoLuongHienTai'] ?? 1.0),
                'BacLuongHienTai'  => (int)($wizardData['BacLuongHienTai'] ?? 1),
                'LuongBaoHiem'     => (float)($wizardData['LuongBaoHiem'] ?? 0),
                'SoTaiKhoanNH'     => trim($wizardData['SoTaiKhoanNH'] ?? ''),
                'NganHang'         => trim($wizardData['NganHang'] ?? ''),
                'SoNguoiPhuThuoc'  => (int)($wizardData['SoNguoiPhuThuoc'] ?? 0),
                'LoaiHopDong'      => $wizardData['loai_hd'] ?? 'THU_VIEC',
                'NgayHetHanHD'     => !empty($wizardData['den_ngay']) ? $wizardData['den_ngay'] : null,
                'TrangThai'        => 1, // Đang làm việc
                'CreatedAt'        => date('Y-m-d H:i:s')
            ];

            $cols = implode('`, `', array_keys($nhansuFields));
            $params = [];
            foreach ($nhansuFields as $k => $v) {
                $params[':' . $k] = $v;
            }
            $ph = implode(', ', array_keys($params));

            $this->query("INSERT INTO nhansu (`{$cols}`) VALUES ({$ph})", $params);

            // 3. Khởi tạo Hợp đồng ban đầu (Bước 3)
            $contractModel = new ContractModel();
            $contractData = [
                'ma_nv'               => $maNV,
                'loai_hd'             => $wizardData['loai_hd'] ?? 'THU_VIEC',
                'so_hd'               => $wizardData['so_hd'] ?? '',
                'ngay_ky'             => $wizardData['ngay_ky'] ?? date('Y-m-d'),
                'tu_ngay'             => $wizardData['tu_ngay'] ?? ($wizardData['NgayVaoLam'] ?? date('Y-m-d')),
                'den_ngay'            => !empty($wizardData['den_ngay']) ? $wizardData['den_ngay'] : null,
                'chuc_danh'           => $wizardData['chuc_danh'] ?? 'Nhân viên',
                'dia_diem_lam_viec'   => $wizardData['dia_diem_lam_viec'] ?? 'Tại đơn vị công tác',
                'luong_can_ban'       => (float)($wizardData['luong_can_ban'] ?? 10000000),
                'luong_dong_bh'       => (float)($wizardData['luong_dong_bh'] ?? $wizardData['luong_can_ban'] ?? 10000000),
                'phu_cap_trach_nhiem' => (float)($wizardData['phu_cap_trach_nhiem'] ?? 0),
                'phu_cap_an_ca'       => (float)($wizardData['phu_cap_an_ca'] ?? 730000),
                'ty_le_huong_luong'   => (float)($wizardData['ty_le_huong_luong'] ?? 85.00),
                'nguoi_dai_dien_ky'   => $wizardData['nguoi_dai_dien_ky'] ?? 'Bùi Thị Hương',
                'chuc_vu_nguoi_ky'    => $wizardData['chuc_vu_nguoi_ky'] ?? 'Giám đốc Điều hành Nhân sự',
                'trang_thai'          => 'HIEU_LUC'
            ];
            $contractModel->createContract($contractData);

            // 4. Ghi nhận sự kiện Tuyển mới
            $this->query(
                "INSERT INTO su_kien_nhan_su (ma_nv, loai_su_kien, ngay_hieu_luc, don_vi_moi, chuc_vu_moi, luong_moi, he_so_moi, ly_do_bien_dong, nguoi_ghi_nhan, created_at)
                 VALUES (:ma_nv, 'TUYEN_MOI', :ngay_hl, :dv_moi, :cv_moi, :luong, :he_so, 'Tiếp nhận nhân sự mới qua Onboarding Wizard', :nguoi_ghi, NOW())",
                [
                    ':ma_nv'     => $maNV,
                    ':ngay_hl'   => $wizardData['NgayVaoLam'] ?? date('Y-m-d'),
                    ':dv_moi'    => $wizardData['MaDV'],
                    ':cv_moi'    => $wizardData['MaCV'] ?? 'NV',
                    ':luong'     => (float)($wizardData['luong_can_ban'] ?? 0),
                    ':he_so'     => (float)($wizardData['HeSoLuongHienTai'] ?? 1.0),
                    ':nguoi_ghi' => Auth::username() ?? 'HR_ONBOARD'
                ]
            );

            // 5. Cấp tài khoản người dùng nếu được chọn (Bước 4)
            if (!empty($wizardData['create_account']) && $wizardData['create_account'] == '1') {
                $username = !empty($wizardData['account_username']) ? trim($wizardData['account_username']) : strtolower(str_replace('-', '', $maNV));
                $rawPass = !empty($wizardData['account_password']) ? $wizardData['account_password'] : 'Vinamilk@2026';
                $hash = password_hash($rawPass, PASSWORD_BCRYPT);
                $roleId = (int)($wizardData['RoleID'] ?? 7); // Mặc định NHANVIEN (RoleID 7)

                // Kiểm tra nếu username đã tồn tại
                $existUser = $this->fetchOne("SELECT ID FROM users WHERE Username = :u", [':u' => $username]);
                $roleRow = $this->fetchOne("SELECT RoleCode FROM roles WHERE RoleID = :rid", [':rid' => $roleId]);
                $roleCode = $roleRow['RoleCode'] ?? 'NHANVIEN';

                if (!$existUser) {
                    $this->query(
                        "INSERT INTO users (Username, Password, MaNV, MaDV, RoleID, RoleCode, IsActive, MustChangePassword, CreatedAt)
                         VALUES (:u, :p, :manv, :madv, :role, :rcode, 1, 1, NOW())",
                        [
                            ':u'     => $username,
                            ':p'     => $hash,
                            ':manv'  => $maNV,
                            ':madv'  => $wizardData['MaDV'],
                            ':role'  => $roleId,
                            ':rcode' => $roleCode
                        ]
                    );
                }
            }

            // 6. Cấp phát tài sản ban đầu nếu có (Bước 4)
            if (!empty($wizardData['asset_card'])) {
                $this->addAsset([
                    'ma_nv'               => $maNV,
                    'loai_tai_san'        => 'THE_TU_RA_VAO',
                    'ten_tai_san'         => 'Thẻ từ nhân viên Vinamilk',
                    'ma_dinh_danh_serial' => $wizardData['card_serial'] ?? ('CARD-' . $maNV),
                    'so_luong'            => 1,
                    'don_vi_tinh'         => 'Thẻ',
                    'ngay_cap_phat'       => $wizardData['NgayVaoLam'] ?? date('Y-m-d'),
                    'tinh_trang_khi_cap'  => 'Mới',
                    'ghi_chu'             => 'Cấp phát thẻ từ khi tiếp nhận'
                ]);
            }

            if (!empty($wizardData['asset_laptop'])) {
                $this->addAsset([
                    'ma_nv'               => $maNV,
                    'loai_tai_san'        => 'MAY_TINH_LAPTOP',
                    'ten_tai_san'         => $wizardData['laptop_name'] ?? 'Laptop làm việc văn phòng',
                    'ma_dinh_danh_serial' => $wizardData['laptop_serial'] ?? '',
                    'so_luong'            => 1,
                    'don_vi_tinh'         => 'Chiếc',
                    'ngay_cap_phat'       => $wizardData['NgayVaoLam'] ?? date('Y-m-d'),
                    'tinh_trang_khi_cap'  => 'Mới 100%',
                    'ghi_chu'             => 'Cấp máy tính làm việc ban đầu'
                ]);
            }

            if (!empty($wizardData['asset_ppe'])) {
                $this->addAsset([
                    'ma_nv'               => $maNV,
                    'loai_tai_san'        => 'BHLD_PPE',
                    'ten_tai_san'         => 'Bộ đồ bảo hộ lao động & Mũ phòng sạch',
                    'ma_dinh_danh_serial' => 'PPE-' . $maNV,
                    'so_luong'            => 2,
                    'don_vi_tinh'         => 'Bộ',
                    'ngay_cap_phat'       => $wizardData['NgayVaoLam'] ?? date('Y-m-d'),
                    'tinh_trang_khi_cap'  => 'Mới',
                    'ghi_chu'             => 'Trang bị BHLD ca sản xuất'
                ]);
            }

            // Ghi audit log
            $this->logAudit('ONBOARD_WIZARD', 'nhansu', $maNV, null, ['HoTen' => $wizardData['HoTen'], 'MaDV' => $wizardData['MaDV']]);

            $this->commit();
            return $maNV;
        } catch (Throwable $e) {
            $this->rollBack();
            throw $e;
        }
    }

    /**
     * Tự động sinh mã nhân sự mới dạng VNM-XXXX
     */
    public function generateNextEmployeeCode(): string
    {
        $sql = "SELECT CAST(SUBSTRING(MaNV, 5) AS UNSIGNED) as num 
                FROM nhansu 
                WHERE MaNV REGEXP '^VNM-[0-9]+$' 
                ORDER BY num DESC LIMIT 1";
        $row = $this->fetchOne($sql);
        if ($row && !empty($row['num'])) {
            $next = (int)$row['num'] + 1;
            return sprintf('VNM-%04d', $next);
        }
        return 'VNM-0001';
    }

    /**
     * TÌM KIẾM NHANH NHÂN VIÊN THEO MÃ NV, HỌ TÊN HOẶC 4 SỐ CUỐI CCCD
     */
    public function quickSearch(string $keyword, int $limit = 8): array
    {
        $scope = $this->scopeByUnit('ns', 'MaDV');
        $kw = '%' . trim($keyword) . '%';
        $sql = "SELECT ns.MaNV, ns.HoTen, ns.MaDV, dv.TenDV, ns.ChucVu, cv.TenCV, 
                       RIGHT(ns.SoCMND_CCCD, 4) as Last4CCCD
                FROM nhansu ns
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                WHERE ({$scope['where']})
                  AND (
                    ns.MaNV LIKE :kw1 
                    OR ns.HoTen LIKE :kw2 
                    OR ns.SoCMND_CCCD LIKE :kw3
                    OR RIGHT(ns.SoCMND_CCCD, 4) = :kw4
                  )
                ORDER BY ns.HoTen ASC
                LIMIT {$limit}";

        $params = array_merge($scope['params'], [
            ':kw1' => $kw,
            ':kw2' => $kw,
            ':kw3' => $kw,
            ':kw4' => trim($keyword)
        ]);

        return $this->fetchAll($sql, $params);
    }
}
