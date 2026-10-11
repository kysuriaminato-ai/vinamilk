<?php

namespace App\Modules\Payroll\Http\Controllers;

use App\Http\Controllers\Controller;
use App\Modules\Employee\Models\Employee;
use App\Modules\Organization\Models\OrgUnit;
use App\Modules\Payroll\Jobs\CalculatePayrollBatchJob;
use App\Modules\Payroll\Models\PayrollPeriod;
use App\Modules\Payroll\Models\Payslip;
use App\Modules\Payroll\Services\PayrollEngineService;
use App\Modules\Payroll\Services\PayrollExportService;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Http\Response;
use Illuminate\Support\Facades\Cache;

class PayrollEngineController extends Controller
{
    public function __construct(
        protected PayrollEngineService $payrollEngineService,
        protected PayrollExportService $payrollExportService
    ) {}

    /**
     * Mở kỳ lương mới
     */
    public function openPeriod(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'month'                 => 'required|integer|between:1,12',
            'year'                  => 'required|integer|min:2020|max:2050',
            'standard_working_days' => 'nullable|numeric|min:1|max:31',
        ]);

        $standardDays = (float)($validated['standard_working_days'] ?? 22.0);
        $period = $this->payrollEngineService->openPeriod($validated['month'], $validated['year'], $standardDays);

        return response()->json([
            'success' => true,
            'message' => "Mở thành công kỳ lương {$period->period_code}",
            'data'    => $period,
        ], 201);
    }

    /**
     * Danh sách kỳ lương
     */
    public function listPeriods(): JsonResponse
    {
        $periods = PayrollPeriod::latest('year')->latest('month')->get();

        return response()->json([
            'success' => true,
            'data'    => $periods,
        ]);
    }

    /**
     * Kích hoạt chạy tính lương theo lô 1.000 người qua Job Queue
     */
    public function calculateBatch(int $id): JsonResponse
    {
        $period = PayrollPeriod::findOrFail($id);

        if (!$period->canRecalculate()) {
            return response()->json([
                'success' => false,
                'message' => "Kỳ lương {$period->period_code} đã ở trạng thái [{$period->status}] và bị khóa. Không thể tính lại.",
            ], 422);
        }

        // Đẩy vào queue xử lý nền theo lô 1.000 người
        CalculatePayrollBatchJob::dispatch($period->id);

        return response()->json([
            'success' => true,
            'message' => "Đã tiếp nhận yêu cầu chạy tính lương kỳ {$period->period_code}. Tiến trình đang chạy nền theo lô 1.000 người.",
            'data'    => [
                'period_id'   => $period->id,
                'period_code' => $period->period_code,
                'status'      => 'QUEUED',
            ],
        ]);
    }

    /**
     * Lấy tiến độ thanh tiến độ tính lương (Progress Bar)
     */
    public function getProgress(int $id): JsonResponse
    {
        $period = PayrollPeriod::findOrFail($id);
        $progress = Cache::get("payroll_batch_progress_{$id}", [
            'period_code' => $period->period_code,
            'total'       => 0,
            'processed'   => 0,
            'percent'     => $period->isPendingApproval() || $period->isLocked() || $period->isPaid() ? 100 : 0,
            'status'      => $period->status,
        ]);

        return response()->json([
            'success' => true,
            'data'    => $progress,
        ]);
    }

    /**
     * Chạy lại bảng lương cho 1 nhân viên
     */
    public function recalculateEmployee(Request $request, int $id): JsonResponse
    {
        $validated = $request->validate([
            'employee_id' => 'required|exists:employees,id',
        ]);

        $period = PayrollPeriod::findOrFail($id);
        $employee = Employee::findOrFail($validated['employee_id']);

        $payslip = $this->payrollEngineService->recalculateEmployee($period, $employee);

        return response()->json([
            'success' => true,
            'message' => "Đã tính lại lương thành công cho nhân viên {$employee->employee_code}",
            'data'    => $payslip->load('lines'),
        ]);
    }

    /**
     * Chạy lại bảng lương cho 1 đơn vị tổ chức
     */
    public function recalculateOrgUnit(Request $request, int $id): JsonResponse
    {
        $validated = $request->validate([
            'org_unit_id' => 'required|exists:org_units,id',
            'recursive'   => 'nullable|boolean',
        ]);

        $period = PayrollPeriod::findOrFail($id);
        $orgUnit = OrgUnit::findOrFail($validated['org_unit_id']);
        $recursive = (bool)($validated['recursive'] ?? true);

        $count = $this->payrollEngineService->recalculateOrgUnit($period, $orgUnit, $recursive);

        return response()->json([
            'success' => true,
            'message' => "Đã tính lại lương thành công cho {$count} nhân sự thuộc đơn vị {$orgUnit->unit_name}",
            'data'    => [
                'org_unit'           => $orgUnit->unit_name,
                'recalculated_count' => $count,
            ],
        ]);
    }

    /**
     * Chốt kỳ lương (LOCKED - Khóa tuyệt đối)
     */
    public function lockPeriod(int $id): JsonResponse
    {
        $period = PayrollPeriod::findOrFail($id);
        $this->payrollEngineService->lockPeriod($period);

        return response()->json([
            'success' => true,
            'message' => "Đã chốt và khóa bảng lương kỳ {$period->period_code}. Mọi thay đổi bị khóa.",
            'data'    => $period->fresh(),
        ]);
    }

    /**
     * Đã chi lương (PAID)
     */
    public function payPeriod(Request $request, int $id): JsonResponse
    {
        $validated = $request->validate([
            'bank_reference' => 'nullable|string|max:100',
        ]);

        $period = PayrollPeriod::findOrFail($id);
        $bankRef = $validated['bank_reference'] ?? 'VCB-BULK-' . date('Ymd');

        $this->payrollEngineService->payPeriod($period, $bankRef);

        return response()->json([
            'success' => true,
            'message' => "Đã hoàn tất chi trả lương kỳ {$period->period_code} qua ngân hàng.",
            'data'    => $period->fresh(),
        ]);
    }

    /**
     * Xuất phiếu lương điện tử cho cổng ESS
     * Đã sửa lỗi IDOR (OWASP A01:2021 & Nghị định 13/2023/NĐ-CP):
     * Chỉ cho phép chính chủ sở hữu phiếu lương HOẶC kế toán tiền lương/Super Admin có thẩm quyền xem
     */
    public function getEssPayslip(Request $request, int $payslipId): JsonResponse
    {
        $payslip = Payslip::with(['employee', 'lines'])->findOrFail($payslipId);

        $currentUser = $request->user();

        if ($currentUser) {
            $isOwner = $currentUser->employee_id && (int)$currentUser->employee_id === (int)$payslip->employee_id;
            $hasAdminAccess = $currentUser->hasRole('SUPER_ADMIN') 
                || $currentUser->hasRole('PAYROLL_ACCOUNTANT') 
                || $currentUser->hasPermission('PAYROLL_VIEW_ALL');

            if (!$isOwner && !$hasAdminAccess) {
                return response()->json([
                    'success' => false,
                    'message' => 'Lỗi bảo mật (403 Forbidden): Bạn không có quyền truy cập thông tin thu nhập cá nhân của nhân sự khác theo Nghị định 13/2023/NĐ-CP.',
                ], 403);
            }
        }

        $data = $this->payrollExportService->generateEssPayslipData($payslip);

        return response()->json([
            'success' => true,
            'data'    => $data,
        ]);
    }

    /**
     * Xuất file chuyển khoản ngân hàng (Bank Transfer CSV)
     */
    public function exportBankTransfer(int $id): Response
    {
        $period = PayrollPeriod::findOrFail($id);
        $csvContent = $this->payrollExportService->exportBankTransferCsv($period);
        $fileName = "Vinamilk_Bank_Transfer_{$period->period_code}.csv";

        return response($csvContent, 200, [
            'Content-Type'        => 'text/csv; charset=UTF-8',
            'Content-Disposition' => "attachment; filename=\"{$fileName}\"",
        ]);
    }

    /**
     * Xuất báo cáo BHXH Mẫu D02-LT
     */
    public function exportSocialInsurance(int $id): JsonResponse
    {
        $period = PayrollPeriod::findOrFail($id);
        $report = $this->payrollExportService->exportSocialInsuranceD02Lt($period);

        return response()->json([
            'success' => true,
            'data'    => $report,
        ]);
    }

    /**
     * Xuất file hạch toán ERP SAP FI/CO
     */
    public function exportErpJournal(int $id): JsonResponse
    {
        $period = PayrollPeriod::findOrFail($id);
        $journal = $this->payrollExportService->exportErpJournalEntries($period);

        return response()->json([
            'success' => true,
            'data'    => $journal,
        ]);
    }
}
