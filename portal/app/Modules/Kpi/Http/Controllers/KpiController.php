<?php

namespace App\Modules\Kpi\Http\Controllers;

use App\Http\Controllers\Controller;
use App\Modules\Kpi\Models\KpiAssignment;
use App\Modules\Kpi\Models\KpiEmployeeSummary;
use App\Modules\Kpi\Models\KpiPeriod;
use App\Modules\Kpi\Services\KpiService;
use Carbon\Carbon;
use DomainException;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Auth;
use Illuminate\View\View;

class KpiController extends Controller
{
    protected KpiService $kpiService;

    public function __construct(KpiService $kpiService)
    {
        $this->kpiService = $kpiService;
    }

    /**
     * Màn hình Web: Giao diện Bảng Kanban dành cho Quản lý.
     */
    public function kanbanView(Request $request): View
    {
        $periodId = $request->query('period_id');
        $currentPeriod = $periodId ? KpiPeriod::findOrFail($periodId) : KpiPeriod::latest('start_date')->first();
        $periods = KpiPeriod::orderByDesc('start_date')->get();

        return view('kpi.kanban', [
            'currentPeriod' => $currentPeriod,
            'periods'       => $periods,
        ]);
    }

    /**
     * Màn hình Web: "Việc của tôi" dành cho Nhân viên (ESS).
     */
    public function myTasksView(Request $request): View
    {
        $user = Auth::user();
        $employeeId = $user->employee_id ?? 1;

        $periodId = $request->query('period_id');
        $currentPeriod = $periodId ? KpiPeriod::findOrFail($periodId) : KpiPeriod::latest('start_date')->first();
        $periods = KpiPeriod::orderByDesc('start_date')->get();

        $myTasks = KpiAssignment::where('employee_id', $employeeId)
            ->when($currentPeriod, fn($q) => $q->where('kpi_period_id', $currentPeriod->id))
            ->orderBy('due_date')
            ->get();

        $summary = KpiEmployeeSummary::where('employee_id', $employeeId)
            ->when($currentPeriod, fn($q) => $q->where('kpi_period_id', $currentPeriod->id))
            ->first();

        return view('kpi.my-tasks', [
            'currentPeriod' => $currentPeriod,
            'periods'       => $periods,
            'tasks'         => $myTasks,
            'summary'       => $summary,
        ]);
    }

    /**
     * API: Lấy dữ liệu Kanban phân theo các cột trạng thái.
     */
    public function getKanbanData(Request $request): JsonResponse
    {
        $periodId = $request->query('kpi_period_id');
        $employeeId = $request->query('employee_id');

        $query = KpiAssignment::with(['employee', 'evaluator'])
            ->when($periodId, fn($q) => $q->where('kpi_period_id', $periodId))
            ->when($employeeId, fn($q) => $q->where('employee_id', $employeeId));

        $tasks = $query->get();

        $columns = [
            'Assigned'        => $tasks->where('status', 'Assigned')->values(),
            'InProgress'      => $tasks->where('status', 'InProgress')->values(),
            'PendingApproval' => $tasks->where('status', 'PendingApproval')->values(),
            'Approved'        => $tasks->where('status', 'Approved')->values(),
            'Rejected'        => $tasks->where('status', 'Rejected')->values(),
            'NotCompleted'    => $tasks->where('status', 'NotCompleted')->values(),
        ];

        return response()->json([
            'success' => true,
            'data'    => $columns,
        ]);
    }

    /**
     * API: Lãnh đạo giao nhiệm vụ mới.
     */
    public function storeTask(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'kpi_period_id'     => 'required|exists:kpi_periods,id',
            'employee_id'       => 'required|exists:employees,id',
            'title'             => 'required|string|max:255',
            'description'       => 'nullable|string',
            'target_metric'     => 'required|string|max:255',
            'weight_percentage' => 'required|numeric|min:1|max:100',
            'due_date'          => 'required|date',
            'baseline_score'    => 'nullable|numeric|min:0',
        ]);

        $evaluator = Auth::user();

        try {
            $task = $this->kpiService->createTask($validated, $evaluator);

            return response()->json([
                'success' => true,
                'message' => 'Giao nhiệm vụ KPI thành công.',
                'data'    => $task,
            ], 201);
        } catch (DomainException $e) {
            return response()->json([
                'success' => false,
                'message' => $e->getMessage(),
            ], 422);
        }
    }

    /**
     * API: Nhân viên bắt đầu làm nhiệm vụ.
     */
    public function startTask(int $id): JsonResponse
    {
        $user = Auth::user();

        try {
            $task = $this->kpiService->startTask($id, $user);

            return response()->json([
                'success' => true,
                'message' => 'Nhiệm vụ đã chuyển sang trạng thái Đang thực hiện.',
                'data'    => $task,
            ]);
        } catch (DomainException $e) {
            return response()->json([
                'success' => false,
                'message' => $e->getMessage(),
            ], 403);
        }
    }

    /**
     * API: Nhân viên nộp kết quả thực hiện.
     */
    public function submitTask(Request $request, int $id): JsonResponse
    {
        $validated = $request->validate([
            'actual_result'  => 'required|string|max:2000',
            'completed_date' => 'nullable|date',
        ]);

        $user = Auth::user();
        $completedDate = !empty($validated['completed_date']) ? Carbon::parse($validated['completed_date']) : now();

        try {
            $task = $this->kpiService->submitTask($id, $validated['actual_result'], $completedDate, $user);

            return response()->json([
                'success' => true,
                'message' => 'Nộp báo cáo hoàn thành nhiệm vụ thành công. Đã tự động tính điểm thời gian SLA và gửi quản lý duyệt.',
                'data'    => $task,
            ]);
        } catch (DomainException $e) {
            return response()->json([
                'success' => false,
                'message' => $e->getMessage(),
            ], 422);
        }
    }

    /**
     * API: Lãnh đạo thẩm định và phê duyệt nhiệm vụ.
     */
    public function approveTask(Request $request, int $id): JsonResponse
    {
        $validated = $request->validate([
            'manager_adjustment' => 'nullable|numeric|between:-100,100',
            'adjustment_reason'  => 'nullable|string|max:1000',
            'feedback'           => 'nullable|string|max:1000',
        ]);

        $evaluator = Auth::user();
        $adjustment = (float)($validated['manager_adjustment'] ?? 0.0);

        try {
            $task = $this->kpiService->approveTask(
                $id,
                $evaluator,
                $adjustment,
                $validated['adjustment_reason'] ?? null,
                $validated['feedback'] ?? null
            );

            return response()->json([
                'success' => true,
                'message' => 'Phê duyệt nhiệm vụ KPI thành công. Đã chốt điểm và cập nhật tổng kết kỳ.',
                'data'    => $task,
            ]);
        } catch (DomainException $e) {
            return response()->json([
                'success' => false,
                'message' => $e->getMessage(),
            ], 422);
        }
    }

    /**
     * API: Lãnh đạo từ chối và trả lại nhiệm vụ yêu cầu làm lại.
     */
    public function rejectTask(Request $request, int $id): JsonResponse
    {
        $validated = $request->validate([
            'rejection_reason' => 'required|string|max:1000',
        ]);

        $evaluator = Auth::user();

        try {
            $task = $this->kpiService->rejectTask($id, $evaluator, $validated['rejection_reason']);

            return response()->json([
                'success' => true,
                'message' => 'Đã trả lại nhiệm vụ yêu cầu nhân viên hoàn thiện và nộp lại.',
                'data'    => $task,
            ]);
        } catch (DomainException $e) {
            return response()->json([
                'success' => false,
                'message' => $e->getMessage(),
            ], 422);
        }
    }

    /**
     * API: Xuất điểm KPI đã duyệt sang Payroll (Lương P3).
     */
    public function exportPayroll(int $periodId): JsonResponse
    {
        $user = Auth::user();

        try {
            $result = $this->kpiService->exportPeriodKpiToPayroll($periodId, $user);

            return response()->json([
                'success' => true,
                'message' => "Đã xuất điểm KPI của {$result['exported_count']} nhân sự sang phân hệ Tính lương P3.",
                'data'    => $result,
            ]);
        } catch (DomainException $e) {
            return response()->json([
                'success' => false,
                'message' => $e->getMessage(),
            ], 422);
        }
    }
}
