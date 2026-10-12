<?php
/**
 * =====================================================================
 * VINAMILK HRM - CONTROLLER CƠ CẤU ĐƠN VỊ TỔ CHỨC (dm_donvi)
 * =====================================================================
 */

require_once __DIR__ . '/../core/Controller.php';
require_once __DIR__ . '/../models/OrgUnitModel.php';

class OrgUnitController extends Controller
{
    protected OrgUnitModel $orgModel;

    public function __construct()
    {
        $this->requireAuth();
        $this->orgModel = new OrgUnitModel();
    }

    /**
     * Hiển thị cây cơ cấu tổ chức 5 cấp của Vinamilk
     */
    public function index(): void
    {
        $units = $this->orgModel->getTree();
        $headcounts = $this->orgModel->getHeadcountByUnits();

        // Ghép số lượng nhân sự vào từng đơn vị
        $headcountMap = [];
        foreach ($headcounts as $hc) {
            $headcountMap[$hc['MaDV']] = (int)$hc['TongSoNhanSu'];
        }

        $this->render('org_units/index', [
            'title'        => 'Cơ cấu Tổ chức Tập đoàn Vinamilk',
            'units'        => $units,
            'headcountMap' => $headcountMap
        ]);
    }
}
