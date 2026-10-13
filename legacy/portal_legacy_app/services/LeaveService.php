<?php
/**
 * =====================================================================
 * VINAMILK HRM - DỊCH VỤ QUẢN LÝ NGHỈ PHÉP THEO CÂY TỔ CHỨC (LeaveService)
 * =====================================================================
 * Nghiệp vụ quy chuẩn:
 *   1. Quỹ phép năm: 12 ngày cơ bản + 1 ngày mỗi 5 năm thâm niên (Điều 113, 114 BLLĐ 2019)
 *   2. Tạo đơn trên cổng ESS, tự động tìm:
 *      - Người duyệt Cấp 1: Quản lý trực tiếp (nhansu.MaNV_QuanLy)
 *      - Người duyệt Cấp 2: Trưởng đơn vị (dm_donvi.TruongDonVi)
 *   3. Nhắc nhở quá hạn 48 giờ & ủy quyền duyệt thay
 *   4. Cập nhật số dư phép realtime
 *   5. Khi đơn được duyệt đủ 2 cấp -> Tự động ghi nhận vào cham_cong_ngay
 */

declare(strict_types=1);

require_once __DIR__ . '/../core/Database.php';

class LeaveService
{
    private PDO $db;

    public function __construct(?PDO $db = null)
    {
        $this->db = $db ?? Database::getInstance()->getConnection();
    }

    /**
     * KHỞI TẠO HOẶC CẬP NHẬT SỐ DƯ PHÉP NĂM THEO THÂM NIÊN
     */
    public function initializeLeaveBalance(string $maNV, int $year): array
    {
        $stmtEmp = $this->db->prepare("SELECT MaNV, NgayVaoLam FROM nhansu WHERE MaNV = :manv LIMIT 1");
        $stmtEmp->execute([':manv' => $maNV]);
        $emp = $stmtEmp->fetch(PDO::FETCH_ASSOC);

        if (!$emp) {
            throw new InvalidArgumentException("Không tìm thấy nhân viên: {$maNV}");
        }

        $hireDate = new DateTime($emp['NgayVaoLam'] ?? date('Y-m-d'));
        $startOfYear = new DateTime("{$year}-01-01");
        $yearsOfService = (int)$hireDate->diff($startOfYear)->format('%y');
        $seniorityDays = (float)floor($yearsOfService / 5.0); // +1 ngày mỗi 5 năm

        $standardDays = 12.0;
        $totalEntitled = $standardDays + $seniorityDays;

        // Kiểm tra xem đã có bản ghi so_du_phep cho năm này chưa
        $stmtCheck = $this->db->prepare("SELECT * FROM so_du_phep WHERE ma_nv = :manv AND nam = :y LIMIT 1");
        $stmtCheck->execute([':manv' => $maNV, ':y' => $year]);
        $existing = $stmtCheck->fetch(PDO::FETCH_ASSOC);

        if ($existing) {
            $used = (float)$existing['phep_da_nghi'];
            $pending = (float)$existing['phep_cho_duyet'];
            $remaining = max(0.0, $totalEntitled - $used - $pending);

            $sqlUpd = "UPDATE so_du_phep 
                       SET phep_tieu_chuan = :std, phep_tham_nien = :sen, tong_phep_duoc_huong = :tot, phep_con_lai = :rem, updated_at = NOW()
                       WHERE id = :id";
            $this->db->prepare($sqlUpd)->execute([
                ':std' => $standardDays,
                ':sen' => $seniorityDays,
                ':tot' => $totalEntitled,
                ':rem' => $remaining,
                ':id'  => $existing['id']
            ]);

            return array_merge($existing, [
                'phep_tieu_chuan'      => $standardDays,
                'phep_tham_nien'       => $seniorityDays,
                'tong_phep_duoc_huong' => $totalEntitled,
                'phep_con_lai'         => $remaining
            ]);
        } else {
            $sqlIns = "INSERT INTO so_du_phep 
                       (ma_nv, nam, phep_tieu_chuan, phep_tham_nien, phep_chuyen_sang, tong_phep_duoc_huong, phep_da_nghi, phep_cho_duyet, phep_con_lai, ghi_chu, created_at, updated_at)
                       VALUES 
                       (:manv, :nam, :std, :sen, 0, :tot, 0, 0, :rem, 'Khởi tạo đầu năm', NOW(), NOW())";
            $this->db->prepare($sqlIns)->execute([
                ':manv' => $maNV,
                ':nam'  => $year,
                ':std'  => $standardDays,
                ':sen'  => $seniorityDays,
                ':tot'  => $totalEntitled,
                ':rem'  => $totalEntitled
            ]);

            $newId = (int)$this->db->lastInsertId();
            return [
                'id'                   => $newId,
                'ma_nv'                => $maNV,
                'nam'                  => $year,
                'phep_tieu_chuan'      => $standardDays,
                'phep_tham_nien'       => $seniorityDays,
                'phep_chuyen_sang'     => 0.0,
                'tong_phep_duoc_huong' => $totalEntitled,
                'phep_da_nghi'         => 0.0,
                'phep_cho_duyet'       => 0.0,
                'phep_con_lai'         => $totalEntitled
            ];
        }
    }

    /**
     * LẤY SỐ DƯ PHÉP NĂM HIỆN TẠI
     */
    public function getLeaveBalance(string $maNV, int $year): array
    {
        $stmt = $this->db->prepare("SELECT * FROM so_du_phep WHERE ma_nv = :manv AND nam = :y LIMIT 1");
        $stmt->execute([':manv' => $maNV, ':y' => $year]);
        $row = $stmt->fetch(PDO::FETCH_ASSOC);

        if (!$row) {
            return $this->initializeLeaveBalance($maNV, $year);
        }

        return $row;
    }

    /**
     * TÌM NGƯỜI DUYỆT CẤP 1 VÀ CẤP 2 THEO CÂY ĐƠN VỊ dm_donvi
     */
    public function findApprovers(string $maNV): array
    {
        $stmtEmp = $this->db->prepare("SELECT MaNV, MaDV, MaNV_QuanLy FROM nhansu WHERE MaNV = :manv LIMIT 1");
        $stmtEmp->execute([':manv' => $maNV]);
        $emp = $stmtEmp->fetch(PDO::FETCH_ASSOC);

        if (!$emp) {
            return ['c1' => null, 'c2' => null];
        }

        $mgrC1 = $emp['MaNV_QuanLy'] ?: null;
        $maDV = $emp['MaDV'] ?? '';

        // Cấp 2: Trưởng đơn vị của nhân sự
        $stmtUnit = $this->db->prepare("SELECT TruongDonVi, MaDV_Cha FROM dm_donvi WHERE MaDV = :madv LIMIT 1");
        $stmtUnit->execute([':madv' => $maDV]);
        $unit = $stmtUnit->fetch(PDO::FETCH_ASSOC);

        $mgrC2 = $unit['TruongDonVi'] ?? null;

        // Nếu cấp 1 khuyết -> dùng Cấp 2 làm Cấp 1
        if (empty($mgrC1) || $mgrC1 === $maNV) {
            $mgrC1 = $mgrC2;
        }

        // Nếu nhân viên chính là Trưởng đơn vị -> Cấp 2 leo lên Trưởng đơn vị cha
        if ($mgrC2 === $maNV && !empty($unit['MaDV_Cha'])) {
            $stmtParent = $this->db->prepare("SELECT TruongDonVi FROM dm_donvi WHERE MaDV = :parent LIMIT 1");
            $stmtParent->execute([':parent' => $unit['MaDV_Cha']]);
            $mgrC2 = $stmtParent->fetchColumn() ?: $mgrC1;
        }

        return [
            'c1' => $mgrC1,
            'c2' => $mgrC2
        ];
    }

    /**
     * TẠO ĐƠN NGHỈ PHÉP TRÊN ESS
     */
    public function createLeaveRequest(array $data): int
    {
        $maNV        = trim($data['ma_nv'] ?? '');
        $loaiNghi    = trim($data['loai_nghi'] ?? 'PHEP_NAM');
        $tuNgay      = $data['tu_ngay'] ?? date('Y-m-d');
        $denNgay     = $data['den_ngay'] ?? $tuNgay;
        $soNgayNghi  = (float)($data['so_ngay_nghi'] ?? 1.0);
        $lyDo        = trim($data['ly_do'] ?? 'Nghỉ giải quyết việc cá nhân');
        $nguoiBanGiao= trim($data['nguoi_ban_giao'] ?? '');

        if (empty($maNV)) {
            throw new InvalidArgumentException("Mã nhân viên không được để trống.");
        }

        if ($denNgay < $tuNgay) {
            throw new InvalidArgumentException("Ngày kết thúc nghỉ không được trước ngày bắt đầu.");
        }

        $year = (int)date('Y', strtotime($tuNgay));

        // Nếu là phép năm -> Kiểm tra số dư phép
        if ($loaiNghi === 'PHEP_NAM') {
            $bal = $this->getLeaveBalance($maNV, $year);
            if ((float)$bal['phep_con_lai'] < $soNgayNghi) {
                throw new DomainException(
                    "Số dư phép năm không đủ. Còn lại: {$bal['phep_con_lai']} ngày, yêu cầu nghỉ: {$soNgayNghi} ngày."
                );
            }
        }

        $approvers = $this->findApprovers($maNV);
        $approverC1 = $data['nguoi_duyet_c1'] ?? $approvers['c1'];
        $approverC2 = $data['nguoi_duyet_c2'] ?? $approvers['c2'];

        $maDon = 'LEAVE-' . date('Y') . '-' . strtoupper(substr(uniqid(), -5));

        $this->db->beginTransaction();
        try {
            $sqlIns = "INSERT INTO don_nghi_phep 
                       (ma_don, ma_nv, loai_nghi, tu_ngay, den_ngay, so_ngay_nghi, ly_do, nguoi_ban_giao,
                        trang_thai, nguoi_duyet_c1, trang_thai_c1, thoi_gian_gui_c1,
                        nguoi_duyet_c2, trang_thai_c2, created_at, updated_at)
                       VALUES 
                       (:madon, :manv, :loai, :tu, :den, :songay, :lydo, :bangiao,
                        'CHO_DUYET_C1', :c1, 'CHO_DUYET', NOW(),
                        :c2, 'CHUA_DEN_LUOT', NOW(), NOW())";

            $stmt = $this->db->prepare($sqlIns);
            $stmt->execute([
                ':madon'   => $maDon,
                ':manv'    => $maNV,
                ':loai'    => $loaiNghi,
                ':tu'      => $tuNgay,
                ':den'     => $denNgay,
                ':songay'  => $soNgayNghi,
                ':lydo'    => $lyDo,
                ':bangiao' => $nguoiBanGiao,
                ':c1'      => $approverC1,
                ':c2'      => $approverC2
            ]);

            $leaveId = (int)$this->db->lastInsertId();

            // Tăng số ngày chờ duyệt trong so_du_phep nếu là phép năm
            if ($loaiNghi === 'PHEP_NAM') {
                $this->db->prepare("UPDATE so_du_phep 
                                    SET phep_cho_duyet = phep_cho_duyet + :days1,
                                        phep_con_lai = phep_con_lai - :days2,
                                        updated_at = NOW()
                                    WHERE ma_nv = :manv AND nam = :y")
                         ->execute([':days1' => $soNgayNghi, ':days2' => $soNgayNghi, ':manv' => $maNV, ':y' => $year]);
            }

            $this->db->commit();
            return $leaveId;
        } catch (Throwable $e) {
            $this->db->rollBack();
            throw $e;
        }
    }

    /**
     * PHÊ DUYỆT CẤP 1 (QUẢN LÝ TRỰC TIẾP)
     */
    public function approveLevel1(int $leaveId, string $approverMaNV, ?string $comments = null): bool
    {
        $stmt = $this->db->prepare("SELECT * FROM don_nghi_phep WHERE id = :id LIMIT 1");
        $stmt->execute([':id' => $leaveId]);
        $leave = $stmt->fetch(PDO::FETCH_ASSOC);

        if (!$leave) {
            throw new InvalidArgumentException("Không tìm thấy đơn nghỉ phép #{$leaveId}");
        }

        if ($leave['trang_thai'] !== 'CHO_DUYET_C1' && $leave['trang_thai'] !== 'CHO_DUYET') {
            throw new DomainException("Đơn không ở trạng thái chờ duyệt cấp 1.");
        }

        // Chuyển sang chờ duyệt Cấp 2
        $sql = "UPDATE don_nghi_phep 
                SET trang_thai = 'CHO_DUYET_C2',
                    trang_thai_c1 = 'DA_DUYET',
                    ngay_duyet_c1 = NOW(),
                    y_kien_c1 = :comments,
                    trang_thai_c2 = 'CHO_DUYET',
                    thoi_gian_gui_c2 = NOW(),
                    updated_at = NOW()
                WHERE id = :id";

        $res = $this->db->prepare($sql)->execute([
            ':comments' => $comments ?? 'Quản lý trực tiếp đồng ý phê duyệt',
            ':id'       => $leaveId
        ]);

        return $res;
    }

    /**
     * PHÊ DUYỆT CẤP 2 (TRƯỞNG ĐƠN VỊ) -> CẬP NHẬT QUỸ PHÉP & TỰ ĐỘNG GHI CHẤM CÔNG NGÀY
     */
    public function approveLevel2(int $leaveId, string $approverMaNV, ?string $comments = null): bool
    {
        $stmt = $this->db->prepare("SELECT * FROM don_nghi_phep WHERE id = :id LIMIT 1");
        $stmt->execute([':id' => $leaveId]);
        $leave = $stmt->fetch(PDO::FETCH_ASSOC);

        if (!$leave) {
            throw new InvalidArgumentException("Không tìm thấy đơn nghỉ phép #{$leaveId}");
        }

        if ($leave['trang_thai'] !== 'CHO_DUYET_C2') {
            throw new DomainException("Đơn chưa được Cấp 1 duyệt hoặc không ở trạng thái chờ duyệt Cấp 2.");
        }

        $this->db->beginTransaction();
        try {
            // 1. Cập nhật trạng thái đơn thành DA_DUYET hoàn toàn
            $sql = "UPDATE don_nghi_phep 
                    SET trang_thai = 'DA_DUYET',
                        trang_thai_c2 = 'DA_DUYET',
                        ngay_duyet_c2 = NOW(),
                        y_kien_c2 = :comments,
                        nguoi_duyet = :approver,
                        ngay_duyet = NOW(),
                        updated_at = NOW()
                    WHERE id = :id";

            $this->db->prepare($sql)->execute([
                ':comments' => $comments ?? 'Trưởng đơn vị chấp thuận phê duyệt nghỉ phép',
                ':approver' => $approverMaNV,
                ':id'       => $leaveId
            ]);

            $maNV = $leave['ma_nv'];
            $loaiNghi = $leave['loai_nghi'];
            $soNgay = (float)$leave['so_ngay_nghi'];
            $year = (int)date('Y', strtotime($leave['tu_ngay']));

            // 2. Chuyển từ phep_cho_duyet sang phep_da_nghi
            if ($loaiNghi === 'PHEP_NAM') {
                $this->db->prepare("UPDATE so_du_phep 
                                    SET phep_cho_duyet = GREATEST(0, phep_cho_duyet - :days1),
                                        phep_da_nghi = phep_da_nghi + :days2,
                                        updated_at = NOW()
                                    WHERE ma_nv = :manv AND nam = :y")
                         ->execute([':days1' => $soNgay, ':days2' => $soNgay, ':manv' => $maNV, ':y' => $year]);
            }

            // 3. TỰ ĐỘNG GHI NHẬN VÀO BẢNG cham_cong_ngay
            $attendanceCode = match ($loaiNghi) {
                'PHEP_NAM'              => 'NGHI_PHEP_NAM',
                'NGHI_OM'               => 'NGHI_OM_BHXH',
                'THAI_SAN'              => 'NGHI_THAI_SAN',
                'VIEC_RIENG_CO_LUONG'   => 'NGHI_PHEP_NAM',
                'KHONG_LUONG'           => 'NGHI_KHONG_LUONG',
                'NGHI_BU'               => 'NGHI_PHEP_NAM',
                default                 => 'NGHI_PHEP_NAM'
            };

            // Sinh các ngày từ tu_ngay đến den_ngay
            $currentDate = new DateTime($leave['tu_ngay']);
            $endDate = new DateTime($leave['den_ngay']);

            $stmtAtt = $this->db->prepare("INSERT INTO cham_cong_ngay 
                        (ma_nv, ngay, gio_vao, gio_ra, cong_chuan, cong_thuc_te, loai_cong, trang_thai_duyet, ghi_chu, created_at)
                        VALUES 
                        (:manv, :ngay, NULL, NULL, 1.0, :congthucte, :loai, 'HOP_LE', :ghichu, NOW())
                        ON DUPLICATE KEY UPDATE 
                        cong_thuc_te = VALUES(cong_thuc_te), loai_cong = VALUES(loai_cong), trang_thai_duyet = 'HOP_LE', ghi_chu = VALUES(ghi_chu)");

            $isPaidLeave = in_array($loaiNghi, ['PHEP_NAM', 'VIEC_RIENG_CO_LUONG'], true) ? 1.0 : 0.0;

            while ($currentDate <= $endDate) {
                $dayStr = $currentDate->format('Y-m-d');
                $stmtAtt->execute([
                    ':manv'        => $maNV,
                    ':ngay'        => $dayStr,
                    ':congthucte'  => $isPaidLeave,
                    ':loai'        => $attendanceCode,
                    ':ghichu'      => "Nghỉ phép duyệt theo đơn {$leave['ma_don']} ({$loaiNghi})"
                ]);
                $currentDate->modify('+1 day');
            }

            $this->db->commit();
            return true;
        } catch (Throwable $e) {
            $this->db->rollBack();
            throw $e;
        }
    }

    /**
     * TỪ CHỐI ĐƠN NGHỈ PHÉP (HOÀN TRẢ SỐ DƯ PHÉP CHỜ DUYỆT)
     */
    public function rejectLeaveRequest(int $leaveId, string $rejecterMaNV, ?string $reason = null): bool
    {
        $stmt = $this->db->prepare("SELECT * FROM don_nghi_phep WHERE id = :id LIMIT 1");
        $stmt->execute([':id' => $leaveId]);
        $leave = $stmt->fetch(PDO::FETCH_ASSOC);

        if (!$leave) {
            throw new InvalidArgumentException("Không tìm thấy đơn #{$leaveId}");
        }

        $this->db->beginTransaction();
        try {
            $sql = "UPDATE don_nghi_phep 
                    SET trang_thai = 'TU_CHOI',
                        nguoi_duyet = :rejecter,
                        ngay_duyet = NOW(),
                        y_kien_duyet = :reason,
                        updated_at = NOW()
                    WHERE id = :id";
            $this->db->prepare($sql)->execute([
                ':rejecter' => $rejecterMaNV,
                ':reason'   => $reason ?? 'Từ chối đơn xin nghỉ phép',
                ':id'       => $leaveId
            ]);

            // Hoàn lại phep_con_lai nếu trước đó đã tạm giữ trong phep_cho_duyet
            if ($leave['loai_nghi'] === 'PHEP_NAM') {
                $year = (int)date('Y', strtotime($leave['tu_ngay']));
                $soNgay = (float)$leave['so_ngay_nghi'];

                $this->db->prepare("UPDATE so_du_phep 
                                    SET phep_cho_duyet = GREATEST(0, phep_cho_duyet - :days1),
                                        phep_con_lai = phep_con_lai + :days2,
                                        updated_at = NOW()
                                    WHERE ma_nv = :manv AND nam = :y")
                         ->execute([':days1' => $soNgay, ':days2' => $soNgay, ':manv' => $leave['ma_nv'], ':y' => $year]);
            }

            $this->db->commit();
            return true;
        } catch (Throwable $e) {
            $this->db->rollBack();
            throw $e;
        }
    }

    /**
     * NHẮC NHỞ QUÁ HẠN 48 GIỜ & CHO PHÉP ỦY QUYỀN DUYỆT THAY
     */
    public function checkAndRemindOverdue48h(int $leaveId): bool
    {
        $stmt = $this->db->prepare("SELECT * FROM don_nghi_phep WHERE id = :id LIMIT 1");
        $stmt->execute([':id' => $leaveId]);
        $leave = $stmt->fetch(PDO::FETCH_ASSOC);

        if (!$leave || !in_array($leave['trang_thai'], ['CHO_DUYET_C1', 'CHO_DUYET_C2'], true)) {
            return false;
        }

        // Đánh dấu quá hạn và tăng số lần nhắc
        $sql = "UPDATE don_nghi_phep 
                SET qua_han_48h = 1,
                    so_lan_nhac = so_lan_nhac + 1,
                    thoi_gian_nhac_cuoi = NOW()
                WHERE id = :id";

        return $this->db->prepare($sql)->execute([':id' => $leaveId]);
    }

    /**
     * THIẾT LẬP ỦY QUYỀN DUYỆT THAY KHI QUÁ HẠN 48 GIỜ
     */
    public function delegateApproval(int $leaveId, string $delegateMaNV, string $reason): bool
    {
        $sql = "UPDATE don_nghi_phep 
                SET nguoi_duyet_thay = :delegate,
                    ly_do_uy_quyen = :reason,
                    duyet_uy_quyen = 1,
                    updated_at = NOW()
                WHERE id = :id";

        return $this->db->prepare($sql)->execute([
            ':delegate' => $delegateMaNV,
            ':reason'   => $reason,
            ':id'       => $leaveId
        ]);
    }
}
