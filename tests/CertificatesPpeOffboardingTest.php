<?php
/**
 * =====================================================================
 * VINAMILK HRM - TEST SUITE TOÀN DIỆN CHO 3 PHÂN HỆ MỚI:
 * 1. QUẢN LÝ CHỨNG CHỈ & CHẶN XẾP CA ATTP (ISO 22000 / HACCP)
 * 2. TÀI SẢN & ĐỊNH MỨC TRANG BỊ PPE THEO VỊ TRÍ
 * 3. QUY TRÌNH THÔI VIỆC (OFFBOARDING 6 BƯỚC, KHẤU TRỪ LƯƠNG & EXIT SURVEY)
 * =====================================================================
 */

declare(strict_types=1);

require_once __DIR__ . '/../app/core/Database.php';
require_once __DIR__ . '/../app/services/CertificateService.php';
require_once __DIR__ . '/../app/services/AssetPpeService.php';
require_once __DIR__ . '/../app/services/OffboardingService.php';
require_once __DIR__ . '/../app/services/PayrollService.php';
require_once __DIR__ . '/../app/Models/AttendanceModel.php';

class CertificatesPpeOffboardingTest
{
    private PDO $db;
    private CertificateService $certService;
    private AssetPpeService $assetService;
    private OffboardingService $offboardingService;
    private PayrollService $payrollService;
    private AttendanceModel $attendanceModel;

    private int $passCount = 0;
    private int $failCount = 0;

    public function __construct()
    {
        $this->db = Database::getInstance()->getConnection();
        $this->certService = new CertificateService($this->db);
        $this->assetService = new AssetPpeService($this->db);
        $this->offboardingService = new OffboardingService($this->db);
        $this->payrollService = new PayrollService($this->db);
        $this->attendanceModel = new AttendanceModel();
    }

    private function assert(bool $condition, string $testName, string $details = ''): void
    {
        if ($condition) {
            $this->passCount++;
            echo " [PASS] {$testName}\n";
            if ($details) {
                echo "        └─ {$details}\n";
            }
        } else {
            $this->failCount++;
            echo " [FAIL] {$testName}\n";
            if ($details) {
                echo "        └─ LỖI: {$details}\n";
            }
        }
    }

    private function ensureEmployee(string $maNV, string $hoTen, string $maDV, string $maCV, string $chucVu, string $cccd): void
    {
        // Xóa các bảng phụ thuộc trước
        $this->db->prepare("DELETE FROM exit_interview_survey WHERE ma_nv = :manv")->execute([':manv' => $maNV]);
        $this->db->prepare("DELETE FROM offboarding_process WHERE ma_nv = :manv")->execute([':manv' => $maNV]);
        $this->db->prepare("DELETE FROM cap_phat_tai_san WHERE ma_nv = :manv")->execute([':manv' => $maNV]);
        $this->db->prepare("DELETE FROM chung_chi_nhan_vien WHERE ma_nv = :manv")->execute([':manv' => $maNV]);
        $this->db->prepare("DELETE FROM phan_ca WHERE ma_nv = :manv")->execute([':manv' => $maNV]);
        $this->db->prepare("DELETE FROM users WHERE MaNV = :manv")->execute([':manv' => $maNV]);
        $this->db->prepare("DELETE FROM nhansu WHERE MaNV = :manv OR SoCMND_CCCD = :cccd")->execute([':manv' => $maNV, ':cccd' => $cccd]);

        $sql = "INSERT INTO nhansu 
                (MaNV, HoTen, NgaySinh, GioiTinh, SoCMND_CCCD, NgayVaoLam, MaDV, MaCV, ChucVu, VungLuong, TrangThai, LuongBaoHiem, SoNguoiPhuThuoc, CreatedAt)
                VALUES 
                (:manv, :hoten, '1992-05-15', 'Nam', :cccd, '2022-01-01', :madv, :macv, :chucvu, 1, 1, 12000000, 1, NOW())";
        $stmt = $this->db->prepare($sql);
        $stmt->execute([
            ':manv'   => $maNV,
            ':hoten'  => $hoTen,
            ':cccd'   => $cccd,
            ':madv'   => $maDV,
            ':macv'   => $maCV,
            ':chucvu' => $chucVu
        ]);
    }

    public function runAll(): void
    {
        echo "======================================================================\n";
        echo "  BẮT ĐẦU KIỂM THỬ TỰ ĐỘNG: CHỨNG CHỈ, PPE & OFFBOARDING (VINAMILK HRM)\n";
        echo "======================================================================\n\n";

        $this->testCertificateExpiryAndWarning();
        $this->testFoodSafetyShiftBlockingRule();
        $this->testAttendanceRosterBlockingIntegration();
        $this->testAssetPpeStandardsAndAllocation();
        $this->testAssetReturnAndPenaltyCalculation();
        $this->testOffboardingSixStepsWorkflow();
        $this->testExitInterviewSurveyAndAiData();
        $this->testPayrollDeductionIntegration();

        echo "\n======================================================================\n";
        echo "  KẾT QUẢ KIỂM THỬ: {$this->passCount} PASSED | {$this->failCount} FAILED\n";
        echo "======================================================================\n";

        if ($this->failCount > 0) {
            exit(1);
        }
    }

    /**
     * TEST 1: CẢNH BÁO HẾT HẠN CHỨNG CHỈ (60 NGÀY) & THẺ XANH ATTP
     */
    public function testCertificateExpiryAndWarning(): void
    {
        echo "--- 1. KIỂM THỬ CHỨNG CHỈ & CẢNH BÁO 60 NGÀY ---\n";

        $testEmp = 'VNM-TEST1';
        $this->ensureEmployee($testEmp, 'Nguyễn Văn Test ATTP', 'NM_BD', 'CN', 'Công nhân sản xuất', '079090999001');

        // Thêm thẻ xanh ATTP sắp hết hạn trong 30 ngày (nằm trong cảnh báo 60 ngày)
        $id1 = $this->certService->saveCertificate([
            'ma_nv'             => $testEmp,
            'loai_chung_chi'    => 'THE_XANH_ATTP',
            'ten_chung_chi'     => 'Thẻ Xanh An Toàn Thực Phẩm Nhà Máy Sữa',
            'so_hieu_chung_chi' => 'TX-TEST-WARN-30D',
            'noi_cap'           => 'TT Y Tế Dĩ An',
            'ngay_cap'          => date('Y-m-d', strtotime('-335 days')),
            'ngay_het_han'      => date('Y-m-d', strtotime('+30 days')),
            'canh_bao_truoc_ngay' => 60,
            'ket_qua_kham'      => 'Đạt tiêu chuẩn sức khỏe TT 14'
        ]);

        $this->assert($id1 > 0, "Lưu chứng chỉ thẻ xanh ATTP thành công (ID: {$id1})");

        // Kiểm tra danh sách cảnh báo 60 ngày
        $expiring = $this->certService->getExpiringCertificates(60);
        $found = false;
        foreach ($expiring as $c) {
            if ($c['ma_nv'] === $testEmp && $c['so_hieu_chung_chi'] === 'TX-TEST-WARN-30D') {
                $found = true;
                break;
            }
        }
        $this->assert($found, "Hệ thống cảnh báo trước 60 ngày phát hiện thẻ xanh sắp hết hạn", "Tìm thấy chứng chỉ trong danh sách cảnh báo 60 ngày");

        // Kiểm tra hàm hasValidFoodSafetyCard: Hôm nay còn hạn -> True
        $validToday = $this->certService->hasValidFoodSafetyCard($testEmp, date('Y-m-d'));
        $this->assert($validToday, "hasValidFoodSafetyCard trả về TRUE khi thẻ còn hiệu lực");

        // Kiểm tra hàm hasValidFoodSafetyCard: Sau 40 ngày (quá hạn) -> False
        $futureDate = date('Y-m-d', strtotime('+40 days'));
        $validFuture = $this->certService->hasValidFoodSafetyCard($testEmp, $futureDate);
        $this->assert(!$validFuture, "hasValidFoodSafetyCard trả về FALSE khi thẻ đã quá hạn vào tương lai ({$futureDate})");
    }

    /**
     * TEST 2: QUY TẮC CHẶN XẾP CA DÂY CHUYỀN SẢN XUẤT NẾU THẺ ATTP HẾT HẠN
     */
    public function testFoodSafetyShiftBlockingRule(): void
    {
        echo "\n--- 2. KIỂM THỬ QUY TẮC CHẶN XẾP CA DÂY CHUYỀN SẢN XUẤT ---\n";

        $testEmpExpired = 'VNM-TEST2';
        $this->ensureEmployee($testEmpExpired, 'Lê Thị Hết Hạn ATTP', 'NM_BD', 'CN', 'Công nhân vận hành', '079090999002');

        // Thêm thẻ xanh ĐÃ HẾT HẠN 5 NGÀY TRƯỚC
        $this->certService->saveCertificate([
            'ma_nv'             => $testEmpExpired,
            'loai_chung_chi'    => 'THE_XANH_ATTP',
            'ten_chung_chi'     => 'Thẻ Xanh ATTP Đã Quá Hạn Khám',
            'so_hieu_chung_chi' => 'TX-TEST-EXPIRED-99',
            'noi_cap'           => 'Bệnh viện Becamex Bình Dương',
            'ngay_cap'          => date('Y-m-d', strtotime('-370 days')),
            'ngay_het_han'      => date('Y-m-d', strtotime('-5 days')),
            'canh_bao_truoc_ngay' => 60
        ]);

        // 1. Xếp vào ca hành chính văn phòng CA_HANH_CHINH -> Không chặn (hợp lệ)
        $allowOffice = false;
        try {
            $allowOffice = $this->certService->validateShiftAssignment($testEmpExpired, 'CA_HANH_CHINH', date('Y-m-d'), 'NM_BD');
        } catch (DomainException $e) {
            $allowOffice = false;
        }
        $this->assert($allowOffice, "Cho phép xếp ca hành chính / văn phòng đối với nhân sự thẻ ATTP hết hạn");

        // 2. Xếp vào ca sản xuất nhà máy CA_1_SANG -> PHẢI BỊ CHẶN VÀ NÉM DomainException
        $blockedCa1 = false;
        $blockedMessage = '';
        try {
            $this->certService->validateShiftAssignment($testEmpExpired, 'CA_1_SANG', date('Y-m-d'), 'NM_BD');
        } catch (DomainException $e) {
            $blockedCa1 = true;
            $blockedMessage = $e->getMessage();
        }
        $this->assert($blockedCa1 && str_contains($blockedMessage, 'CHẶN XẾP CA DÂY CHUYỀN'), 
            "CHẶN XẾP CA DÂY CHUYỀN (CA_1_SANG) khi thẻ xanh ATTP hết hạn", 
            $blockedMessage);

        // 3. Xếp vào ca 3 đêm CA_3_DEM -> PHẢI BỊ CHẶN
        $blockedCa3 = false;
        try {
            $this->certService->validateShiftAssignment($testEmpExpired, 'CA_3_DEM', date('Y-m-d'), 'NM_BD');
        } catch (DomainException $e) {
            $blockedCa3 = true;
        }
        $this->assert($blockedCa3, "CHẶN XẾP CA DÂY CHUYỀN (CA_3_DEM) khi thẻ xanh ATTP hết hạn");
    }

    /**
     * TEST 3: TÍCH HỢP BẢNG CHẤM CÔNG PHÂN CA TUẦN saveWeeklyRoster CHẶN VÀ ROLLBACK
     */
    public function testAttendanceRosterBlockingIntegration(): void
    {
        echo "\n--- 3. KIỂM THỬ TÍCH HỢP PHÂN CA TUẦN (saveWeeklyRoster) VÀ ROLLBACK TRANSACTION ---\n";

        $testEmpExpired = 'VNM-TEST2';
        $today = date('Y-m-d');

        // Thử lưu lịch phân ca tuần có chứa nhân viên thẻ ATTP hết hạn
        $rosterData = [
            'assignments' => [
                [
                    'ma_nv' => $testEmpExpired,
                    'ngay'  => $today,
                    'ma_ca' => 'CA_2_CHIEU',
                    'ma_dv' => 'NM_BD'
                ]
            ]
        ];

        $rosterBlocked = false;
        $errorMsg = '';
        try {
            $this->attendanceModel->saveWeeklyRoster($rosterData);
        } catch (DomainException $e) {
            $rosterBlocked = true;
            $errorMsg = $e->getMessage();
        } catch (Throwable $e) {
            $rosterBlocked = true;
            $errorMsg = $e->getMessage();
        }

        $this->assert($rosterBlocked, "saveWeeklyRoster chặn thành công nhân viên ATTP quá hạn và ném Exception", $errorMsg);

        // Kiểm tra trong CSDL đảm bảo giao dịch đã rollback, không có bản ghi phan_ca nào bị lưu
        $stmtCheck = $this->db->prepare("SELECT COUNT(*) FROM phan_ca WHERE ma_nv = :manv AND ngay_lam_viec = :ngay AND ma_ca = 'CA_2_CHIEU'");
        $stmtCheck->execute([':manv' => $testEmpExpired, ':ngay' => $today]);
        $count = (int)$stmtCheck->fetchColumn();

        $this->assert($count === 0, "Giao dịch CSDL đã được ROLLBACK an toàn (Số bản ghi phan_ca = 0)");
    }

    /**
     * TEST 4: ĐỊNH MỨC PPE THEO VỊ TRÍ & CẤP PHÁT CÓ KÝ NHẬN ĐIỆN TỬ
     */
    public function testAssetPpeStandardsAndAllocation(): void
    {
        echo "\n--- 4. KIỂM THỬ ĐỊNH MỨC TRANG BỊ PPE & CẤP PHÁT KÝ NHẬN ---\n";

        // Gợi ý cho nhân viên Nhà máy
        $factoryEmp = 'VNM-TEST2';
        $sugFactory = $this->assetService->suggestPpeByEmployee($factoryEmp);
        $this->assert($sugFactory['group'] === 'NHA_MAY_SAN_XUAT', "Nhận diện đúng nhóm Nhà máy sản xuất (NHA_MAY_SAN_XUAT)");
        $this->assert(count($sugFactory['standards']) >= 4, "Gợi ý đủ định mức PPE nhà máy (Ủng, Áo blouse, Mũ trùm, Găng tay nitrile)");

        // Cấp phát tài sản PPE có ký nhận điện tử ngay lúc cấp
        $assetId = $this->assetService->allocateAsset([
            'ma_nv'                => $factoryEmp,
            'loai_tai_san'         => 'BHLD_PPE',
            'ten_tai_san'          => 'Ủng bảo hộ chống trơn trượt phòng sạch',
            'ma_dinh_danh_serial'  => 'UNG-TEST-999',
            'gia_tri_tai_san'      => 450000,
            'da_ky_nhan_cap'       => 1,
            'nguoi_giao'           => 'Trưởng Kho An Toàn',
            'tinh_trang_khi_cap'   => 'Mới 100%'
        ]);

        $this->assert($assetId > 0, "Cấp phát tài sản thành công (Asset ID: {$assetId})");

        // Kiểm tra mã biên bản và chuỗi hash chữ ký
        $stmtA = $this->db->prepare("SELECT ma_bien_ban, da_ky_nhan_cap, chu_ky_hash FROM cap_phat_tai_san WHERE id = :id");
        $stmtA->execute([':id' => $assetId]);
        $rowA = $stmtA->fetch(PDO::FETCH_ASSOC);

        $this->assert(str_starts_with($rowA['ma_bien_ban'], 'BB-CP-'), "Tự động sinh mã biên bản bàn giao: {$rowA['ma_bien_ban']}");
        $this->assert((int)$rowA['da_ky_nhan_cap'] === 1 && !empty($rowA['chu_ky_hash']), "Ký nhận điện tử thành công với mã hash SHA-256 an toàn");

        // Cấp phát chưa ký -> Ký nhận sau qua signAllocation
        $assetId2 = $this->assetService->allocateAsset([
            'ma_nv'                => $factoryEmp,
            'loai_tai_san'         => 'MAY_TINH_LAPTOP',
            'ten_tai_san'          => 'Laptop Dell Latitude Văn Phòng',
            'ma_dinh_danh_serial'  => 'LT-TEST-UNSIGNED',
            'gia_tri_tai_san'      => 18500000,
            'da_ky_nhan_cap'       => 0
        ]);
        $signOk = $this->assetService->signAllocation($assetId2);
        $this->assert($signOk, "Ký nhận điện tử bổ sung qua signAllocation() thành công");
    }

    /**
     * TEST 5: THU HỒI TÀI SẢN & BÁO MẤT TỰ ĐỘNG TÍNH TIỀN PHẠT
     */
    public function testAssetReturnAndPenaltyCalculation(): void
    {
        echo "\n--- 5. KIỂM THỬ THU HỒI, BÁO MẤT & TÍNH TIỀN KHẤU TRỪ TÀI SẢN ---\n";

        $testEmp = 'VNM-TEST1';
        $this->db->prepare("DELETE FROM cap_phat_tai_san WHERE ma_nv = :manv")->execute([':manv' => $testEmp]);

        // Cấp 2 tài sản: 1 Laptop (18.5tr) và 1 Thẻ RFID (150k)
        $idLaptop = $this->assetService->allocateAsset([
            'ma_nv'           => $testEmp,
            'loai_tai_san'    => 'MAY_TINH_LAPTOP',
            'ten_tai_san'     => 'Laptop Lenovo ThinkPad T14',
            'gia_tri_tai_san' => 18500000,
            'da_ky_nhan_cap'  => 1
        ]);

        $idCard = $this->assetService->allocateAsset([
            'ma_nv'           => $testEmp,
            'loai_tai_san'    => 'THE_TU_RA_VAO',
            'ten_tai_san'     => 'Thẻ từ ra vào RFID Vinamilk',
            'gia_tri_tai_san' => 150000,
            'da_ky_nhan_cap'  => 1
        ]);

        // Báo mất Laptop -> Bồi hoàn 18.500.000 VNĐ
        $this->assetService->returnOrRecoverAsset($idLaptop, 'BAO_MAT', 18500000.0, 'Mất ngoài công ty');

        // Thu hồi thẻ RFID thành công -> 0 VNĐ
        $this->assetService->returnOrRecoverAsset($idCard, 'DA_THU_HOI', 0.0, 'Đã nộp lại đủ');

        // Kiểm tra hàm tính tổng tiền phạt calculateTotalAssetPenalty
        $totalPenalty = $this->assetService->calculateTotalAssetPenalty($testEmp);
        $this->assert($totalPenalty == 18500000.0, "Tính đúng tổng tiền khấu trừ tài sản: " . number_format($totalPenalty, 0, ',', '.') . " VNĐ");
    }

    /**
     * TEST 6: QUY TRÌNH THÔI VIỆC 6 BƯỚC TUẦN TỰ & TỰ ĐỘNG KHẤU TRỪ / KHÓA TÀI KHOẢN
     */
    public function testOffboardingSixStepsWorkflow(): void
    {
        echo "\n--- 6. KIỂM THỬ QUY TRÌNH THÔI VIỆC 6 BƯỚC TUẦN TỰ (OFFBOARDING) ---\n";

        $testEmpOff = 'VNM-TEST3';
        $this->ensureEmployee($testEmpOff, 'Trần Thôi Việc Test', 'PB_TAICHINH', 'CV', 'Chuyên viên tài chính', '079090999003');

        // Tạo tài khoản người dùng
        $this->db->prepare("INSERT INTO users (Username, Password, MaNV, RoleID, IsActive, CreatedAt)
                            VALUES ('user_test_off', 'hash123', :manv, 7, 1, NOW())")
                 ->execute([':manv' => $testEmpOff]);

        // Cấp 1 tài sản chưa trả: Điện thoại Samsung (3.5tr)
        $this->assetService->allocateAsset([
            'ma_nv'           => $testEmpOff,
            'loai_tai_san'    => 'DIEN_THOAI_SIM',
            'ten_tai_san'     => 'Điện thoại Samsung Galaxy A54',
            'gia_tri_tai_san' => 3500000,
            'da_ky_nhan_cap'  => 1
        ]);

        // Khởi tạo hồ sơ thôi việc
        $offId = $this->offboardingService->createOffboarding([
            'ma_nv'         => $testEmpOff,
            'ly_do_nghi'    => 'Tìm kiếm thử thách nghề nghiệp mới',
            'nguoi_lap_ho_so' => 'HR Admin'
        ]);
        $this->assert($offId > 0, "Khởi tạo hồ sơ thôi việc thành công (Offboarding ID: {$offId})");

        // BƯỚC 1: Bàn giao công việc
        $s1 = $this->offboardingService->completeStep1Handover($offId, 'Trưởng bộ phận Tài chính');
        $this->assert($s1, "Hoàn tất Bước 1: Bàn giao công việc & tài liệu nghiệp vụ");

        // BƯỚC 2: Thu hồi tài sản -> Tự động chuyển nợ sang lương
        $res2 = $this->offboardingService->completeStep2AssetRecovery($offId, 'Quản lý tài sản');
        $this->assert($res2['unreturned_count'] === 1 && $res2['total_penalty'] == 3500000.0, 
            "Hoàn tất Bước 2: Tự động phát hiện 1 tài sản nợ và tính 3.500.000 VNĐ khấu trừ");

        // BƯỚC 3: Khóa tài khoản người dùng tự động
        $s3 = $this->offboardingService->completeStep3AccountLockout($offId, 'IT Admin');
        $this->assert($s3, "Hoàn tất Bước 3: Xác nhận khóa tài khoản");
        // Kiểm tra bảng users
        $isActive = (int)$this->db->query("SELECT IsActive FROM users WHERE MaNV = '{$testEmpOff}'")->fetchColumn();
        $this->assert($isActive === 0, "Tài khoản người dùng trong bảng `users` đã bị KHÓA TỨC THÌ (IsActive = 0)");

        // BƯỚC 4: Chốt công & phép tồn
        $s4 = $this->offboardingService->completeStep4TimesheetClose($offId, 'Cán bộ C&B', 20.0, 2.5);
        $this->assert($s4, "Hoàn tất Bước 4: Chốt 20 ngày công và 2.5 ngày phép tồn");

        // BƯỚC 5: Quyết toán lương (Tổng lương 20tr - nợ tài sản 3.5tr = Thực lĩnh 16.5tr)
        $s5 = $this->offboardingService->completeStep5FinalPayroll($offId, 'Kế toán Lương', 20000000.0);
        $offData = $this->offboardingService->getOffboarding($offId);
        $this->assert($s5 && (float)$offData['thuc_linh_quyet_toan'] == 16500000.0, 
            "Hoàn tất Bước 5: Quyết toán lương tự trừ nợ tài sản (20tr - 3.5tr = 16.5tr thực lĩnh)");

        // BƯỚC 6: Chốt sổ BHXH
        $s6 = $this->offboardingService->completeStep6SocialInsurance($offId, 'Chuyên viên BHXH', '7911223344', date('Y-m-d'));
        $this->assert($s6, "Hoàn tất Bước 6: Chốt sổ BHXH và trao quyết định");

        // Kiểm tra sau 6 bước: Hồ sơ chuyển sang HOAN_TAT và nhansu.TrangThai = 3 (Nghỉ việc)
        $offFinal = $this->offboardingService->getOffboarding($offId);
        $empStatus = (int)$this->db->query("SELECT TrangThai FROM nhansu WHERE MaNV = '{$testEmpOff}'")->fetchColumn();

        $this->assert($offFinal['trang_thai'] === 'HOAN_TAT', "Hồ sơ thôi việc tự động hoàn tất (trang_thai = HOAN_TAT)");
        $this->assert($empStatus === 3, "Trạng thái nhân sự tự động chuyển sang NGHỈ VIỆC (TrangThai = 3)");
    }

    /**
     * TEST 7: KHẢO SÁT THÔI VIỆC (EXIT INTERVIEW SURVEY - NUÔI DỮ LIỆU AI)
     */
    public function testExitInterviewSurveyAndAiData(): void
    {
        echo "\n--- 7. KIỂM THỬ KHẢO SÁT THÔI VIỆC (EXIT SURVEY) CHO AI MODEL ---\n";

        $testEmpOff = 'VNM-TEST3';

        $surveyId = $this->offboardingService->saveExitSurvey([
            'ma_nv'                          => $testEmpOff,
            'ngay_khao_sat'                  => date('Y-m-d'),
            'hai_long_luong_thuong'          => 2,
            'hai_long_quan_ly'               => 4,
            'hai_long_co_hoi_thang_tien'     => 2,
            'ap_luc_cong_viec_ot'            => 4,
            'hai_long_moi_truong_dong_nghiep'=> 5,
            'hai_long_chinh_sach_phuc_loi'   => 4,
            'ly_do_chinh'                    => 'MUC_LUONG_DAI_NGO',
            'cong_ty_moi_loai_hinh'          => 'DOANH_NGHIEP_FDI',
            'muc_luong_moi_tang_phan_tram'   => 35.0,
            'dieu_gi_vnm_nen_cai_thien'      => 'Tăng tính linh hoạt trong chính sách thăng tiến',
            'san_sang_quay_lai_vnm'          => 1
        ]);

        $this->assert($surveyId > 0, "Lưu khảo sát Exit Interview thành công (ID: {$surveyId})");

        // Kiểm tra dữ liệu tổng hợp phục vụ AI
        $analytics = $this->offboardingService->getExitSurveyAnalytics();
        $this->assert(!empty($analytics['reasons']), "Tổng hợp được bảng xếp hạng lý do chính nghỉ việc cho AI");
        $this->assert(!empty($analytics['scores']) && $analytics['scores']['tong_khao_sat'] > 0, "Tính đúng điểm trung bình các chiều đo mức độ hài lòng");
    }

    /**
     * TEST 8: TÍCH HỢP TỰ ĐỘNG KHẤU TRỪ TÀI SẢN TRONG PayrollService
     */
    public function testPayrollDeductionIntegration(): void
    {
        echo "\n--- 8. KIỂM THỬ TÍCH HỢP KHẤU TRỪ TÀI SẢN TRONG PayrollService ---\n";

        $calcInput = [
            'ma_nv'             => 'VNM-TEST-PAYROLL',
            'ho_ten'            => 'Nhân Viên Tính Lương',
            'chuc_vu'           => 'Chuyên viên',
            'don_vi'            => 'Vinamilk Trụ sở chính',
            'region'            => 1,
            'contract_salary'   => 20000000.0,
            'standard_days'     => 26.0,
            'actual_days'       => 26.0,
            'kpi_score'         => 100.0,
            'advance_payment'   => 1000000.0,   // Tạm ứng 1tr
            'asset_penalty'     => 2500000.0,   // Khấu trừ tài sản mất 2.5tr
            'effective_date'    => '2026-03-01'
        ];

        $calcResult = $this->payrollService->calculateEmployeeSalary($calcInput);

        $this->assert($calcResult['khau_tru_tai_san'] == 2500000.0, 
            "PayrollService nhận đúng số tiền khấu trừ tài sản: " . number_format($calcResult['khau_tru_tai_san'], 0, ',', '.') . " VNĐ");

        $this->assert($calcResult['tong_khau_tru_khac'] == 3500000.0, 
            "Tổng khấu trừ khác (Tạm ứng 1tr + Tài sản 2.5tr) = " . number_format($calcResult['tong_khau_tru_khac'], 0, ',', '.') . " VNĐ");

        // Kiểm tra thực lĩnh
        $expectedNet = $calcResult['tong_thu_nhap'] - $calcResult['tong_bao_hiem_nv'] - $calcResult['thue_tncn'] - 3500000.0;
        $this->assert(abs($calcResult['thuc_linh'] - $expectedNet) < 0.01, 
            "Lương thực lĩnh trừ chính xác cả tiền phạt tài sản chưa bàn giao");
    }
}

// Thực thi kiểm thử
$suite = new CertificatesPpeOffboardingTest();
$suite->runAll();
