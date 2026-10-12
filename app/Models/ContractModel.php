<?php
/**
 * =====================================================================
 * VINAMILK HRM - MODEL QUẢN LÝ HỢP ĐỒNG LAO ĐỘNG (hop_dong_lao_dong)
 * =====================================================================
 * Tuân thủ Bộ luật Lao động 2019:
 *   - Phân loại hợp đồng: Thử việc, Xác định thời hạn, Không xác định thời hạn
 *   - CHẶN KÝ HĐ XÁC ĐỊNH THỜI HẠN LẦN THỨ 3 (Điều 20 BLLĐ 2019)
 *   - Cảnh báo tự động trước 60 ngày và 30 ngày hết hạn
 *   - Áp dụng phạm vi scopeByUnit() theo cây đơn vị
 */

require_once __DIR__ . '/../core/Model.php';

class ContractModel extends Model
{
    protected string $table = 'hop_dong_lao_dong';
    protected string $primaryKey = 'id';

    /**
     * Lấy danh sách hợp đồng của một nhân viên
     */
    public function getContractsByEmployee(string $maNV): array
    {
        $sql = "SELECT hd.*, 
                       DATEDIFF(hd.den_ngay, CURDATE()) as so_ngay_con_lai
                FROM hop_dong_lao_dong hd
                WHERE hd.ma_nv = :manv
                ORDER BY hd.tu_ngay DESC, hd.id DESC";

        $rows = $this->fetchAll($sql, [':manv' => $maNV]);

        // Gán nhãn cảnh báo thời hạn tự động
        foreach ($rows as &$row) {
            $row['canh_bao'] = $this->calculateWarningStatus($row['loai_hd'], $row['den_ngay'], $row['trang_thai']);
        }
        unset($row);

        return $rows;
    }

    /**
     * Lấy chi tiết hợp đồng kèm thông tin nhân sự và đơn vị để in
     */
    public function getContractById(int $id): ?array
    {
        $sql = "SELECT hd.*, 
                       ns.HoTen, ns.NgaySinh, ns.GioiTinh, ns.SoCMND_CCCD, ns.NgayCap, ns.NoiCap,
                       ns.DiaChiHienTai, ns.QueQuan, ns.SoDienThoai, ns.Email, ns.MaSoThue,
                       ns.SoTaiKhoanNH, ns.NganHang, ns.MaDV, dv.TenDV, dv.DiaChi as DiaChiDonVi,
                       cv.TenCV,
                       DATEDIFF(hd.den_ngay, CURDATE()) as so_ngay_con_lai
                FROM hop_dong_lao_dong hd
                INNER JOIN nhansu ns ON hd.ma_nv = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                WHERE hd.id = :id
                LIMIT 1";

        $row = $this->fetchOne($sql, [':id' => $id]);
        if ($row) {
            $row['canh_bao'] = $this->calculateWarningStatus($row['loai_hd'], $row['den_ngay'], $row['trang_thai']);
        }
        return $row;
    }

    /**
     * Đếm số lần nhân viên đã ký HĐ xác định thời hạn (XĐTH)
     * Quy tắc Bộ luật Lao động: Tối đa 2 lần xác định thời hạn
     */
    public function countFixedTermContracts(string $maNV): int
    {
        $sql = "SELECT COUNT(*) as cnt 
                FROM hop_dong_lao_dong 
                WHERE ma_nv = :manv 
                  AND loai_hd LIKE 'XAC_DINH_THOI_HAN%'
                  AND trang_thai != 'HUY_BO'";

        $row = $this->fetchOne($sql, [':manv' => $maNV]);
        return (int)($row['cnt'] ?? 0);
    }

    /**
     * Kiểm tra hợp đồng sắp hết hạn (Cảnh báo 60 ngày & 30 ngày)
     * 
     * @param int $days Số ngày tối đa cảnh báo (mặc định 60 ngày)
     * @param string|null $maDV Đơn vị lọc (nếu có)
     * @return array
     */
    public function getExpiringContracts(int $days = 60, ?string $maDV = null): array
    {
        $scope = $this->scopeByUnit('ns', 'MaDV');
        $whereClauses = [
            $scope['where'],
            "hd.trang_thai = 'HIEU_LUC'",
            "hd.den_ngay IS NOT NULL",
            "DATEDIFF(hd.den_ngay, CURDATE()) BETWEEN 0 AND :days"
        ];
        $params = array_merge($scope['params'], [':days' => $days]);

        if (!empty($maDV) && $maDV !== 'ALL') {
            $whereClauses[] = "ns.MaDV = :filter_dv";
            $params[':filter_dv'] = $maDV;
        }

        $whereSql = implode(' AND ', $whereClauses);

        $sql = "SELECT hd.*, ns.HoTen, ns.MaDV, dv.TenDV, cv.TenCV,
                       DATEDIFF(hd.den_ngay, CURDATE()) as so_ngay_con_lai
                FROM hop_dong_lao_dong hd
                INNER JOIN nhansu ns ON hd.ma_nv = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                WHERE {$whereSql}
                ORDER BY hd.den_ngay ASC";

        $rows = $this->fetchAll($sql, $params);
        foreach ($rows as &$row) {
            $row['canh_bao'] = $this->calculateWarningStatus($row['loai_hd'], $row['den_ngay'], $row['trang_thai']);
        }
        unset($row);

        return $rows;
    }

    /**
     * Tạo hợp đồng lao động mới có kiểm tra quy tắc BLLĐ 2019
     * 
     * @param array $data Thông tin hợp đồng
     * @throws Exception Khi vi phạm quy tắc chặn ký lần 3 HĐ xác định thời hạn
     * @return int ID hợp đồng vừa tạo
     */
    public function createContract(array $data): int
    {
        $maNV   = trim($data['ma_nv'] ?? '');
        $loaiHD = trim($data['loai_hd'] ?? '');

        if (empty($maNV) || empty($loaiHD)) {
            throw new InvalidArgumentException("Mã nhân viên và Loại hợp đồng không được để trống.");
        }

        // =================================================================
        // QUY TẮC BẮT BUỘC: CHẶN KÝ HĐ XÁC ĐỊNH THỜI HẠN LẦN THỨ 3
        // =================================================================
        if (str_starts_with($loaiHD, 'XAC_DINH_THOI_HAN')) {
            $existingCount = $this->countFixedTermContracts($maNV);
            if ($existingCount >= 2) {
                throw new DomainException(
                    "Theo Điều 20 Bộ luật Lao động 2019, nhân sự [{$maNV}] đã ký {$existingCount} lần " .
                    "Hợp đồng lao động xác định thời hạn. Lần thứ 3 BẮT BUỘC phải chuyển sang " .
                    "Hợp đồng KHÔNG XÁC ĐỊNH THỜI HẠN."
                );
            }
        }

        // Tự động sinh số hợp đồng nếu chưa có
        if (empty($data['so_hd'])) {
            $data['so_hd'] = $this->generateContractNumber($maNV);
        }

        $fields = [
            'ma_nv', 'so_hd', 'loai_hd', 'ngay_ky', 'tu_ngay', 'den_ngay',
            'chuc_danh', 'dia_diem_lam_viec', 'luong_can_ban', 'luong_dong_bh',
            'phu_cap_trach_nhiem', 'phu_cap_an_ca', 'phu_cap_khac', 'ty_le_huong_luong',
            'nguoi_dai_dien_ky', 'chuc_vu_nguoi_ky', 'trang_thai', 'ghi_chu'
        ];

        $insertData = [];
        foreach ($fields as $f) {
            if (isset($data[$f])) {
                $insertData[$f] = $data[$f];
            }
        }
        $insertData['created_at'] = date('Y-m-d H:i:s');

        // Bắt đầu transaction
        $this->beginTransaction();
        try {
            // Đóng trạng thái các HĐ cũ đang hiệu lực nếu ký HĐ mới
            if (($data['trang_thai'] ?? 'HIEU_LUC') === 'HIEU_LUC') {
                $this->query(
                    "UPDATE hop_dong_lao_dong SET trang_thai = 'HET_HAN' 
                     WHERE ma_nv = :manv AND trang_thai = 'HIEU_LUC'",
                    [':manv' => $maNV]
                );
            }

            $cols = implode('`, `', array_keys($insertData));
            $params = [];
            foreach ($insertData as $k => $v) {
                $params[':' . $k] = $v;
            }
            $ph = implode(', ', array_keys($params));

            $sql = "INSERT INTO hop_dong_lao_dong (`{$cols}`) VALUES ({$ph})";
            $this->query($sql, $params);
            $contractId = (int)$this->lastInsertId();

            // Cập nhật thông tin hợp đồng hiện tại vào bảng nhansu
            $this->query(
                "UPDATE nhansu SET 
                    LoaiHopDong = :loai_hd, 
                    NgayHetHanHD = :den_ngay 
                 WHERE MaNV = :manv",
                [
                    ':loai_hd'  => $loaiHD,
                    ':den_ngay' => !empty($data['den_ngay']) ? $data['den_ngay'] : null,
                    ':manv'     => $maNV
                ]
            );

            // Ghi nhận sự kiện nhân sự
            $this->query(
                "INSERT INTO su_kien_nhan_su (ma_nv, loai_su_kien, ngay_hieu_luc, so_quyet_dinh, ly_do_bien_dong, nguoi_ghi_nhan, created_at)
                 VALUES (:manv, 'KY_HOP_DONG', :ngay_hl, :so_qd, :ly_do, :nguoi_ghi, NOW())",
                [
                    ':manv'      => $maNV,
                    ':ngay_hl'   => $data['tu_ngay'] ?? date('Y-m-d'),
                    ':so_qd'     => $data['so_hd'],
                    ':ly_do'     => "Ký kết hợp đồng lao động: {$loaiHD}",
                    ':nguoi_ghi' => Auth::username() ?? 'SYSTEM'
                ]
            );

            // Ghi Audit log
            $this->logAudit('CREATE', 'hop_dong_lao_dong', (string)$contractId, null, $insertData);

            $this->commit();
            return $contractId;
        } catch (Throwable $e) {
            $this->rollBack();
            throw $e;
        }
    }

    /**
     * Tính toán trạng thái cảnh báo thời hạn hợp đồng
     */
    public function calculateWarningStatus(string $loaiHD, ?string $denNgay, string $trangThai): array
    {
        if ($loaiHD === 'KHONG_XAC_DINH_THOI_HAN' || empty($denNgay)) {
            return [
                'code'    => 'VO_THOI_HAN',
                'badge'   => 'success',
                'message' => 'Vô thời hạn',
                'days'    => null
            ];
        }

        if ($trangThai !== 'HIEU_LUC') {
            return [
                'code'    => 'DA_DONG',
                'badge'   => 'secondary',
                'message' => 'Đã kết thúc',
                'days'    => null
            ];
        }

        $now = new DateTime('today');
        $expiry = new DateTime($denNgay);
        $diff = (int)$now->diff($expiry)->format('%r%a'); // Số ngày còn lại có dấu

        if ($diff < 0) {
            return [
                'code'    => 'EXPIRED',
                'badge'   => 'dark',
                'message' => 'Đã quá hạn (' . abs($diff) . ' ngày)',
                'days'    => $diff
            ];
        } elseif ($diff <= 30) {
            return [
                'code'    => 'CRITICAL_30',
                'badge'   => 'danger',
                'message' => 'Hết hạn trong ' . $diff . ' ngày (Cảnh báo 30 ngày - Gia hạn gấp)',
                'days'    => $diff
            ];
        } elseif ($diff <= 60) {
            return [
                'code'    => 'WARNING_60',
                'badge'   => 'warning',
                'message' => 'Hết hạn trong ' . $diff . ' ngày (Cảnh báo 60 ngày)',
                'days'    => $diff
            ];
        } else {
            return [
                'code'    => 'NORMAL',
                'badge'   => 'info',
                'message' => 'Còn hiệu lực (' . $diff . ' ngày)',
                'days'    => $diff
            ];
        }
    }

    /**
     * Sinh số hợp đồng tự động chuẩn Vinamilk
     */
    protected function generateContractNumber(string $maNV): string
    {
        $year = date('Y');
        $sql = "SELECT COUNT(*) as cnt FROM hop_dong_lao_dong WHERE YEAR(created_at) = :year";
        $row = $this->fetchOne($sql, [':year' => $year]);
        $seq = (int)($row['cnt'] ?? 0) + 1;
        return sprintf('%03d/HĐLĐ-VNM/%s', $seq, $year);
    }
}
