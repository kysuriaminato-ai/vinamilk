<?php

declare(strict_types=1);

namespace Tests\Unit;

use PHPUnit\Framework\TestCase;
use LeaveService;
use DomainException;
use Database;
use PDO;
use DateTime;
use Session;

class LeaveApprovalWorkflowTest extends TestCase
{
    private LeaveService $service;
    private PDO $db;

    protected function setUp(): void
    {
        $this->db = Database::getInstance()->getConnection();
        $this->service = new LeaveService($this->db);
        Session::set('vnm_logged_user', [
            'ID'       => 1,
            'Username' => 'admin',
            'RoleCode' => 'ADMIN',
            'MaDV'     => 'VINAMILK'
        ]);
    }

    protected function tearDown(): void
    {
        Session::remove('vnm_logged_user');
    }

    /**
     * TEST 1: Tính toán số dư phép năm theo thâm niên (+1 ngày mỗi 5 năm công tác)
     */
    public function testLeaveBalanceSeniorityCalculation(): void
    {
        $testEmp = 'VNM-0001';
        $currentYear = (int)date('Y');

        // Giả lập ngày vào làm của VNM-0001 cách đây 11 năm -> thâm niên = 11 năm -> +2 ngày
        $hireDate = (new DateTime("-11 years"))->format('Y-m-d');
        $this->db->prepare("UPDATE nhansu SET NgayVaoLam = :hire WHERE MaNV = :manv")->execute([
            ':hire' => $hireDate,
            ':manv' => $testEmp
        ]);

        $balance = $this->service->initializeLeaveBalance($testEmp, $currentYear);

        $this->assertEquals(12.0, (float)$balance['phep_tieu_chuan'], "Phép tiêu chuẩn cơ bản là 12 ngày");
        $this->assertEquals(2.0, (float)$balance['phep_tham_nien'], "Thâm niên 11 năm được cộng +2 ngày phép");
        $this->assertEquals(14.0, (float)$balance['tong_phep_duoc_huong'], "Tổng phép được hưởng là 14 ngày");
    }

    /**
     * TEST 2: Tạo đơn nghỉ phép trên ESS và tạm giữ số dư phép chờ duyệt
     */
    public function testCreateLeaveRequestAndBalanceHold(): void
    {
        $testEmp = 'VNM-0001';
        $currentYear = (int)date('Y');
        $balBefore = $this->service->getLeaveBalance($testEmp, $currentYear);

        $leaveId = $this->service->createLeaveRequest([
            'ma_nv'         => $testEmp,
            'loai_nghi'     => 'PHEP_NAM',
            'tu_ngay'       => date('Y-m-d', strtotime('+5 days')),
            'den_ngay'      => date('Y-m-d', strtotime('+6 days')),
            'so_ngay_nghi'  => 2.0,
            'ly_do'         => 'Nghỉ phép giải quyết việc gia đình'
        ]);

        $this->assertGreaterThan(0, $leaveId, "Tạo đơn nghỉ phép thành công");

        // Kiểm tra số dư phép sau khi nộp đơn: phep_cho_duyet tăng thêm 2 ngày, phep_con_lai giảm 2 ngày
        $balAfter = $this->service->getLeaveBalance($testEmp, $currentYear);
        $this->assertEquals((float)$balBefore['phep_cho_duyet'] + 2.0, (float)$balAfter['phep_cho_duyet']);
        $this->assertEquals((float)$balBefore['phep_con_lai'] - 2.0, (float)$balAfter['phep_con_lai']);
    }

    /**
     * TEST 3: Chặn nộp đơn khi số dư phép năm không đủ
     */
    public function testInsufficientLeaveBalanceException(): void
    {
        $testEmp = 'VNM-0001';
        $currentYear = (int)date('Y');
        $bal = $this->service->getLeaveBalance($testEmp, $currentYear);

        $excessiveDays = (float)$bal['phep_con_lai'] + 10.0;

        $this->expectException(DomainException::class);
        $this->expectExceptionMessageMatches('/Số dư phép năm không đủ/');

        $this->service->createLeaveRequest([
            'ma_nv'         => $testEmp,
            'loai_nghi'     => 'PHEP_NAM',
            'tu_ngay'       => date('Y-m-d', strtotime('+10 days')),
            'den_ngay'      => date('Y-m-d', strtotime('+25 days')),
            'so_ngay_nghi'  => $excessiveDays,
            'ly_do'         => 'Nghỉ quá số dư'
        ]);
    }

    /**
     * TEST 4: Luồng duyệt 2 cấp (Cấp 1 quản lý -> Cấp 2 trưởng đơn vị) & Tự ghi cham_cong_ngay
     */
    public function testTwoTierApprovalAndTimesheetSync(): void
    {
        $testEmp = 'VNM-0003';
        $currentYear = (int)date('Y');
        $this->service->initializeLeaveBalance($testEmp, $currentYear);

        $tuNgay = date('Y-m-d', strtotime('+2 days'));
        $denNgay = date('Y-m-d', strtotime('+3 days'));

        $leaveId = $this->service->createLeaveRequest([
            'ma_nv'         => $testEmp,
            'loai_nghi'     => 'PHEP_NAM',
            'tu_ngay'       => $tuNgay,
            'den_ngay'      => $denNgay,
            'so_ngay_nghi'  => 2.0,
            'ly_do'         => 'Nghỉ phép thường niên'
        ]);

        // CẤP 1 DUYỆT
        $okC1 = $this->service->approveLevel1($leaveId, 'VNM-0002', 'Đồng ý duyệt Cấp 1');
        $this->assertTrue($okC1);

        $stmt1 = $this->db->prepare("SELECT trang_thai FROM don_nghi_phep WHERE id = :id");
        $stmt1->execute([':id' => $leaveId]);
        $status1 = $stmt1->fetchColumn();
        $this->assertEquals('CHO_DUYET_C2', $status1, "Sau khi C1 duyệt, đơn chuyển sang CHO_DUYET_C2");

        // CẤP 2 DUYỆT
        $okC2 = $this->service->approveLevel2($leaveId, 'VNM-0001', 'Trưởng đơn vị phê duyệt Cấp 2');
        $this->assertTrue($okC2);

        $stmt2 = $this->db->prepare("SELECT trang_thai FROM don_nghi_phep WHERE id = :id");
        $stmt2->execute([':id' => $leaveId]);
        $status2 = $stmt2->fetchColumn();
        $this->assertEquals('DA_DUYET', $status2, "Sau khi C2 duyệt, đơn chuyển sang DA_DUYET hoàn toàn");

        // KIỂM TRA TỰ ĐỘNG GHI NHẬN VÀO BẢNG cham_cong_ngay
        $stmtAtt = $this->db->prepare("SELECT * FROM cham_cong_ngay WHERE ma_nv = :manv AND ngay = :ngay");
        $stmtAtt->execute([':manv' => $testEmp, ':ngay' => $tuNgay]);
        $attRow = $stmtAtt->fetch(PDO::FETCH_ASSOC);

        $this->assertNotEmpty($attRow, "Hệ thống tự động ghi nhận vào cham_cong_ngay");
        $this->assertEquals('NGHI_PHEP_NAM', $attRow['loai_cong'], "Mã loại công là 'NGHI_PHEP_NAM' (Phép năm)");
        $this->assertEquals(1.0, (float)$attRow['cong_thuc_te'], "Số công thực tế hưởng lương là 1.0");
        $this->assertEquals('HOP_LE', $attRow['trang_thai_duyet']);
    }

    /**
     * TEST 5: Nhắc nhở quá hạn 48 giờ và Ủy quyền duyệt thay
     */
    public function testOverdue48hReminderAndDelegation(): void
    {
        $testEmp = 'VNM-0002';
        $leaveId = $this->service->createLeaveRequest([
            'ma_nv'         => $testEmp,
            'loai_nghi'     => 'NGHI_OM',
            'tu_ngay'       => date('Y-m-d', strtotime('+1 day')),
            'den_ngay'      => date('Y-m-d', strtotime('+1 day')),
            'so_ngay_nghi'  => 1.0,
            'ly_do'         => 'Cảm sốt điều trị tại nhà'
        ]);

        // Kích hoạt nhắc nhở 48h
        $remindOk = $this->service->checkAndRemindOverdue48h($leaveId);
        $this->assertTrue($remindOk);

        $stmt = $this->db->prepare("SELECT qua_han_48h, so_lan_nhac FROM don_nghi_phep WHERE id = :id");
        $stmt->execute([':id' => $leaveId]);
        $row = $stmt->fetch(PDO::FETCH_ASSOC);
        $this->assertEquals(1, (int)$row['qua_han_48h']);
        $this->assertGreaterThan(0, (int)$row['so_lan_nhac']);

        // Ủy quyền duyệt thay
        $delegateOk = $this->service->delegateApproval($leaveId, 'VNM-0004', 'Quản lý đi công tác nước ngoài');
        $this->assertTrue($delegateOk);

        $stmtD = $this->db->prepare("SELECT nguoi_duyet_thay, duyet_uy_quyen FROM don_nghi_phep WHERE id = :id");
        $stmtD->execute([':id' => $leaveId]);
        $rowD = $stmtD->fetch(PDO::FETCH_ASSOC);
        $this->assertEquals('VNM-0004', $rowD['nguoi_duyet_thay']);
        $this->assertEquals(1, (int)$rowD['duyet_uy_quyen']);
    }

    /**
     * TEST 6: Từ chối đơn nghỉ phép -> Hoàn trả số dư phép năm chờ duyệt
     */
    public function testRejectLeaveRequestReleasesBalance(): void
    {
        $testEmp = 'VNM-0001';
        $currentYear = (int)date('Y');
        $this->service->initializeLeaveBalance($testEmp, $currentYear);

        $balBefore = $this->service->getLeaveBalance($testEmp, $currentYear);

        $leaveId = $this->service->createLeaveRequest([
            'ma_nv'         => $testEmp,
            'loai_nghi'     => 'PHEP_NAM',
            'tu_ngay'       => date('Y-m-d', strtotime('+7 days')),
            'den_ngay'      => date('Y-m-d', strtotime('+7 days')),
            'so_ngay_nghi'  => 1.0,
            'ly_do'         => 'Nghỉ cá nhân'
        ]);

        $this->service->rejectLeaveRequest($leaveId, 'VNM-0002', 'Đợt cao điểm sản xuất, không thể duyệt nghỉ');

        $balAfter = $this->service->getLeaveBalance($testEmp, $currentYear);
        $this->assertEquals((float)$balBefore['phep_con_lai'], (float)$balAfter['phep_con_lai'], "Số dư phép được hoàn trả lại sau khi từ chối");
    }
}
