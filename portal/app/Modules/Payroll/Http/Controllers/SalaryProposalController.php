<?php

namespace App\Modules\Payroll\Http\Controllers;

use App\Http\Controllers\Controller;
use App\Modules\Payroll\Models\SalaryProposal;
use App\Modules\Payroll\Services\SalaryProposalService;
use DomainException;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Auth;

class SalaryProposalController extends Controller
{
    protected SalaryProposalService $proposalService;

    public function __construct(SalaryProposalService $proposalService)
    {
        $this->proposalService = $proposalService;
    }

    /**
     * Tự động lấy dữ liệu phân tích chuẩn bị đề xuất tăng lương.
     */
    public function prepare(int $employeeId): JsonResponse
    {
        $user = Auth::user();

        try {
            $data = $this->proposalService->prepareProposalData($employeeId, $user);

            return response()->json([
                'success' => true,
                'data'    => $data,
            ]);
        } catch (DomainException $e) {
            return response()->json([
                'success' => false,
                'message' => $e->getMessage(),
            ], 403);
        }
    }

    /**
     * Lập và nộp tờ trình đề xuất tăng lương.
     */
    public function store(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'employee_id'               => 'required|exists:employees,id',
            'proposal_type'             => 'required|in:PERIODIC,DEPARTMENT_RECOMMENDED,SPECIAL_EXCEPTION,MASS_STATUTORY',
            'proposed_p1'               => 'required|numeric|min:0',
            'proposed_p2'               => 'nullable|numeric|min:0',
            'proposed_insurance_salary' => 'nullable|numeric|min:0',
            'effective_from'            => 'required|date',
            'reason'                    => 'required|string|max:2000',
            'exception_reason'          => 'nullable|string|max:2000',
            'attachment_url'            => 'nullable|string|max:500',
        ]);

        $user = Auth::user();

        try {
            $proposal = $this->proposalService->createSalaryProposal($validated, $user);

            return response()->json([
                'success' => true,
                'message' => 'Nộp tờ trình đề xuất tăng lương thành công. Hệ thống đã kích hoạt luồng phê duyệt động.',
                'data'    => $proposal->load('steps'),
            ], 201);
        } catch (DomainException $e) {
            return response()->json([
                'success' => false,
                'message' => $e->getMessage(),
            ], 422);
        }
    }

    /**
     * Phê duyệt bước hiện tại trong luồng duyệt.
     */
    public function approveStep(Request $request, int $id): JsonResponse
    {
        $validated = $request->validate([
            'comments' => 'nullable|string|max:1000',
        ]);

        $user = Auth::user();

        try {
            $proposal = $this->proposalService->approveStep($id, $user, $validated['comments'] ?? '');

            return response()->json([
                'success' => true,
                'message' => $proposal->status === 'APPROVED' 
                    ? "Tờ trình đã được phê duyệt cấp cuối cùng! Đã ban hành Quyết định số {$proposal->decision_number}."
                    : "Đã phê duyệt bước số {$proposal->current_step_number} thành công.",
                'data'    => $proposal->load('steps'),
            ]);
        } catch (DomainException $e) {
            return response()->json([
                'success' => false,
                'message' => $e->getMessage(),
            ], 403);
        }
    }

    /**
     * Từ chối tờ trình tại bước hiện tại.
     */
    public function rejectStep(Request $request, int $id): JsonResponse
    {
        $validated = $request->validate([
            'rejection_reason' => 'required|string|max:1000',
        ]);

        $user = Auth::user();

        try {
            $proposal = $this->proposalService->rejectStep($id, $user, $validated['rejection_reason']);

            return response()->json([
                'success' => true,
                'message' => 'Đã từ chối tờ trình đề xuất tăng lương.',
                'data'    => $proposal->load('steps'),
            ]);
        } catch (DomainException $e) {
            return response()->json([
                'success' => false,
                'message' => $e->getMessage(),
            ], 403);
        }
    }

    /**
     * Chi tiết tờ trình và lịch sử phê duyệt.
     */
    public function show(int $id): JsonResponse
    {
        $proposal = SalaryProposal::with(['steps.approver', 'employee', 'creator', 'retroactiveAdjustments'])->findOrFail($id);

        return response()->json([
            'success' => true,
            'data'    => $proposal,
        ]);
    }
}
