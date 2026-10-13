<?php

declare(strict_types=1);

namespace Tests\Unit;

use PHPUnit\Framework\TestCase;
use ContractModel;
use DomainException;
use Database;
use PDO;
use DateTime;
use Session;

class ContractWarningTest extends TestCase
{
    private ContractModel $model;
    private PDO $db;

    protected function setUp(): void
    {
        $this->db = Database::getInstance()->getConnection();
        $this->model = new ContractModel();
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
     * TEST 1: Cảnh báo thời hạn hợp đồng (Hết hạn trong 60 ngày vs 30 ngày vs Quá hạn)
     */
    public function testCalculateWarningStatusTransitions(): void
    {
        // 1. HĐ không xác định thời hạn -> Vô thời hạn
        $warnIndef = $this->model->calculateWarningStatus('KHONG_XAC_DINH_THOI_HAN', null, 'HIEU_LUC');
        $this->assertEquals('VO_THOI_HAN', $warnIndef['code']);
        $this->assertEquals('success', $warnIndef['badge']);

        // 2. HĐ sắp hết hạn trong 45 ngày -> Cảnh báo WARNING_60
        $date45 = (new DateTime('+45 days'))->format('Y-m-d');
        $warn45 = $this->model->calculateWarningStatus('XAC_DINH_THOI_HAN_12T', $date45, 'HIEU_LUC');
        $this->assertEquals('WARNING_60', $warn45['code']);
        $this->assertEquals('warning', $warn45['badge']);

        // 3. HĐ sắp hết hạn trong 15 ngày -> Cảnh báo khẩn CRITICAL_30
        $date15 = (new DateTime('+15 days'))->format('Y-m-d');
        $warn15 = $this->model->calculateWarningStatus('XAC_DINH_THOI_HAN_12T', $date15, 'HIEU_LUC');
        $this->assertEquals('CRITICAL_30', $warn15['code']);
        $this->assertEquals('danger', $warn15['badge']);

        // 4. HĐ đã quá hạn 5 ngày trước -> EXPIRED
        $datePast = (new DateTime('-5 days'))->format('Y-m-d');
        $warnPast = $this->model->calculateWarningStatus('XAC_DINH_THOI_HAN_12T', $datePast, 'HIEU_LUC');
        $this->assertEquals('EXPIRED', $warnPast['code']);
    }

    /**
     * TEST 2: Chặn ký hợp đồng xác định thời hạn lần thứ 3 (Điều 20 BLLĐ 2019)
     */
    public function testBlockThirdFixedTermContractUnderLaborCode(): void
    {
        $testEmp = 'VNM-0002';

        // Đảm bảo nhân viên đã có 2 hợp đồng xác định thời hạn
        $this->db->prepare("DELETE FROM hop_dong_lao_dong WHERE ma_nv = :manv AND so_hd LIKE 'HD-MOCK-TEST%'")->execute([':manv' => $testEmp]);

        // Giả lập đã ký 2 lần HĐ XĐTH
        $this->db->prepare("INSERT INTO hop_dong_lao_dong (so_hd, ma_nv, loai_hd, tu_ngay, den_ngay, luong_can_ban, trang_thai, created_at) 
                            VALUES ('HD-MOCK-TEST-1', :manv1, 'XAC_DINH_THOI_HAN_12T', '2022-01-01', '2022-12-31', 15000000, 'HET_HAN', NOW()),
                                   ('HD-MOCK-TEST-2', :manv2, 'XAC_DINH_THOI_HAN_24T', '2023-01-01', '2024-12-31', 18000000, 'HET_HAN', NOW())")
                 ->execute([':manv1' => $testEmp, ':manv2' => $testEmp]);

        $count = $this->model->countFixedTermContracts($testEmp);
        $this->assertGreaterThanOrEqual(2, $count, "Nhân viên đã ký từ 2 lần HĐ xác định thời hạn trở lên");

        // Cố tình tạo hợp đồng XĐTH lần thứ 3 -> BẮT BUỘC PHẢI BỊ CHẶN VÀ NÉM DomainException
        $this->expectException(DomainException::class);
        $this->expectExceptionMessageMatches('/Điều 20 Bộ luật Lao động 2019/');

        $this->model->createContract([
            'ma_nv'          => $testEmp,
            'loai_hd'        => 'XAC_DINH_THOI_HAN_12T',
            'tu_ngay'        => '2025-01-01',
            'den_ngay'       => '2025-12-31',
            'luong_can_ban'  => 20000000
        ]);
    }

    /**
     * TEST 3: Lấy danh sách hợp đồng hết hạn trong 60 ngày
     */
    public function testGetExpiringContractsWithin60Days(): void
    {
        $_SESSION['user'] = [
            'ID'       => 1,
            'Username' => 'admin',
            'RoleCode' => 'ADMIN',
            'MaDV'     => 'VINAMILK'
        ];

        // Tạo 1 HĐ sắp hết hạn trong 20 ngày
        $this->db->prepare("DELETE FROM hop_dong_lao_dong WHERE so_hd = 'HD-WARN-TEST-60'")->execute();
        $this->db->prepare("INSERT INTO hop_dong_lao_dong (so_hd, ma_nv, loai_hd, tu_ngay, den_ngay, luong_can_ban, trang_thai, created_at)
                            VALUES ('HD-WARN-TEST-60', 'VNM-0001', 'XAC_DINH_THOI_HAN_12T', DATE_SUB(CURDATE(), INTERVAL 345 DAY), DATE_ADD(CURDATE(), INTERVAL 20 DAY), 25000000, 'HIEU_LUC', NOW())")
                 ->execute();

        $expiring = $this->model->getExpiringContracts(60);
        $this->assertNotEmpty($expiring, "Tìm thấy danh sách hợp đồng cảnh báo trong 60 ngày");

        $found = false;
        foreach ($expiring as $c) {
            if ($c['so_hd'] === 'HD-WARN-TEST-60') {
                $found = true;
                $this->assertLessThanOrEqual(60, (int)$c['so_ngay_con_lai']);
                $this->assertEquals('CRITICAL_30', $c['canh_bao']['code']);
                break;
            }
        }
        $this->assertTrue($found, "Hợp đồng test HD-WARN-TEST-60 xuất hiện trong danh sách cảnh báo");
    }
}
