<?php
/**
 * =====================================================================
 * VINAMILK HRM - CONTROLLER NHẬT KÝ KIỂM TOÁN (AuditLogController)
 * =====================================================================
 */

require_once __DIR__ . '/../core/Controller.php';
require_once __DIR__ . '/../models/AuditLogModel.php';

class AuditLogController extends Controller
{
    private AuditLogModel $auditLogModel;

    public function __construct()
    {
        $this->auditLogModel = new AuditLogModel();
    }

    /**
     * XEM NHẬT KÝ KIỂM TOÁN HỆ THỐNG
     */
    public function index(): void
    {
        $this->requireRole(['ADMIN', 'BOD', 'HR_TAPDOAN']);

        $action = $_GET['action'] ?? 'ALL';
        $table  = $_GET['table'] ?? 'ALL';

        $logs = $this->auditLogModel->getLogs(150, $action, $table);

        $this->view('audit/index', [
            'title'  => 'Nhật Ký Kiểm Toán & Tuân Thủ (Audit Trail) - Vinamilk HRM',
            'logs'   => $logs,
            'action' => $action,
            'table'  => $table
        ]);
    }
}
