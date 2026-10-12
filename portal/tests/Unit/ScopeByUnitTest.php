<?php

declare(strict_types=1);

namespace Tests\Unit;

use PHPUnit\Framework\TestCase;
use EmployeeModel;
use Auth;
use Session;

class ScopeByUnitTest extends TestCase
{
    private EmployeeModel $model;

    protected function setUp(): void
    {
        $this->model = new EmployeeModel();
        unset($_GET['unit_scope'], $_GET['ma_dv']);
    }

    protected function tearDown(): void
    {
        Session::remove('vnm_logged_user');
        unset($_SESSION['user'], $_SESSION['role'], $_GET['unit_scope'], $_GET['ma_dv']);
    }

    /**
     * TEST 1: Người dùng chưa đăng nhập -> scopeByUnit trả về '1 = 0'
     */
    public function testUnauthenticatedUserReturnsEmptyCondition(): void
    {
        Auth::logout();
        Session::remove('vnm_logged_user');
        $scope = $this->model->scopeByUnit('ns', 'MaDV');
        $this->assertEquals('1 = 0', $scope['where']);
        $this->assertEmpty($scope['params']);
    }

    /**
     * TEST 2: ADMIN / BOD / HR_TAPDOAN không chọn bộ lọc -> xem toàn tập đoàn ('1 = 1')
     */
    public function testExecutiveRolesHaveGlobalScopeWithoutFilter(): void
    {
        Session::set('vnm_logged_user', [
            'ID'       => 1,
            'Username' => 'bod.ceo',
            'RoleCode' => 'BOD',
            'MaDV'     => 'VINAMILK'
        ]);

        $scope = $this->model->scopeByUnit('ns', 'MaDV');
        $this->assertEquals('1 = 1', $scope['where']);
        $this->assertEmpty($scope['params']);
    }

    /**
     * TEST 3: BOD chọn bộ lọc đơn vị cụ thể (NM_BD) -> Lọc theo cây đơn vị con của NM_BD
     */
    public function testExecutiveRolesFilterBySpecificUnitAndDescendants(): void
    {
        Session::set('vnm_logged_user', [
            'ID'       => 1,
            'Username' => 'admin',
            'RoleCode' => 'ADMIN',
            'MaDV'     => 'VINAMILK'
        ]);
        $_GET['unit_scope'] = 'NM_BD';

        $scope = $this->model->scopeByUnit('ns', 'MaDV');
        $this->assertStringContainsString('ns.MaDV IN (', $scope['where']);
        $this->assertContains('NM_BD', array_values($scope['params']));
    }

    /**
     * TEST 4: HR_DONVI (Nhà máy Bình Dương) chỉ được xem NM_BD và các xưởng/ca con của NM_BD
     */
    public function testUnitHrIsStrictlyScopedToOwnUnitTree(): void
    {
        Session::set('vnm_logged_user', [
            'ID'       => 4,
            'Username' => 'hr.nhamay',
            'RoleCode' => 'HR_DONVI',
            'MaDV'     => 'NM_BD'
        ]);

        $scope = $this->model->scopeByUnit('ns', 'MaDV');
        $this->assertStringContainsString('ns.MaDV IN (', $scope['where']);
        
        $scopedUnits = array_values($scope['params']);
        $this->assertContains('NM_BD', $scopedUnits);
        // Đảm bảo không chứa đơn vị ngoài phạm vi (như NM Tiên Sơn NM_TS hay Trụ sở)
        $this->assertNotContains('NM_TS', $scopedUnits);
        $this->assertNotContains('VINAMILK', $scopedUnits);
    }

    /**
     * TEST 5: HR_DONVI cố tình can thiệp URL để xem đơn vị ngoài thẩm quyền -> Trả về '1 = 0'
     */
    public function testUnitHrAccessingUnauthorizedUnitReturnsEmpty(): void
    {
        Session::set('vnm_logged_user', [
            'ID'       => 4,
            'Username' => 'hr.nhamay',
            'RoleCode' => 'HR_DONVI',
            'MaDV'     => 'NM_BD'
        ]);
        // Cố tình yêu cầu đơn vị Tiên Sơn
        $_GET['unit_scope'] = 'NM_TS';

        $scope = $this->model->scopeByUnit('ns', 'MaDV');
        $this->assertEquals('1 = 0', $scope['where'], "Cố tình truy cập đơn vị ngoài phạm vi phải bị chặn hoàn toàn (1 = 0)");
    }

    /**
     * TEST 6: NHANVIEN (ESS) chỉ được xem hồ sơ của chính mình
     */
    public function testEmployeeSelfServiceIsScopedToSelfOnly(): void
    {
        Session::set('vnm_logged_user', [
            'ID'       => 7,
            'Username' => 'nhanvien.ess',
            'RoleCode' => 'NHANVIEN',
            'MaNV'     => 'VNM-0003',
            'MaDV'     => 'NM_TS'
        ]);

        $scope = $this->model->scopeByUnit('ns', 'MaDV');
        $this->assertEquals('ns.MaNV = :ess_manv', $scope['where']);
        $this->assertEquals('VNM-0003', $scope['params'][':ess_manv']);
    }
}
