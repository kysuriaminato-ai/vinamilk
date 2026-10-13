<?php

namespace App\Modules\Leave\Http\Controllers;

use App\Http\Controllers\Controller;
use App\Modules\Employee\Models\Employee;
use App\Modules\Leave\Models\ResignationRequest;
use App\Modules\Leave\Services\OffboardingService;
use Carbon\Carbon;
use DomainException;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Auth;

class OffboardingController extends Controller
{
    protected OffboardingService $offboardingService;

    public function __construct(OffboardingService $offboardingService)
    {
        $this->offboardingService = $offboardingService;
    }

    /**
     * Tra cứu số ngày báo trước tối thiểu pháp định của nhân viên theo HĐLĐ.
     */
    public function getNoticeDays(int $employeeId): JsonResponse
    {
        $employee = Employee::findOrFail($employeeId);
        $noticeDays = $this->offboardingService->calculateStatutoryNoticeDays($employee);

        return response()->json([
            'success'     => true,
            'employee_id' => $employeeId,
            'notice_days' => $noticeDays,
            'note'        => match ($noticeDays) {
                45 => 'Hợp đồng không xác định thời hạn (Điều 35.1.a BLLĐ 2019)',
                30 => 'Hợp đồng xác định thời hạn 12-36 tháng (Điều 35.1.b BLLĐ 2019)',
                3  => 'Hợp đồng thử việc / dưới 12 tháng (Điều 35.1.c BLLĐ 2019)',
                default => 'Theo quy định hiện hành'
            },
        ]);
    }

    /**
     * Nhân viên nộp đơn xin nghỉ việc trên cổng ESS.
     */
    public function submitResignation(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'proposed_last_date'   => 'required|date',
            'submitted_date'       => 'nullable|date',
            'reason'               => 'required|string|max:1000',
            'is_mutual_agreement'  => 'nullable|boolean',
        ]);

        $user = Auth::user();
        $employeeId = $user->employee_id ?? $request->input('employee_id');

        try {
            $resignation = $this->offboardingService->submitResignationRequest($employeeId, $validated);

            return response()->json([
                'success' => true,
                'message' => 'Nộp đơn thôi việc thành công. Đã tạo checklist bàn giao tài sản và gửi thông báo đến Quản lý.',
                'data'    => $resignation->load('handoverItems'),
            ], 201);
        } catch (DomainException $e) {
            return response()->json([
                'success' => false,
                'message' => $e->getMessage(),
            ], 422);
        }
    }

    /**
     * Cấp 1: Quản lý trực tiếp phê duyệt đơn thôi việc.
     */
    public function approveByManager(int $id): JsonResponse
    {
        $user = Auth::user();

        try {
            $resignation = $this->offboardingService->approveByManager($id, $user);

            return response()->json([
                'success' => true,
                'message' => 'Quản lý trực tiếp đã xác nhận đơn thôi việc và kế hoạch bàn giao.',
                'data'    => $resignation,
            ]);
        } catch (DomainException $e) {
            return response()->json([
                'success' => false,
                'message' => $e->getMessage(),
            ], 403);
        }
    }

    /**
     * Cấp 2: Phòng Nhân sự phê duyệt hồ sơ thôi việc.
     */
    public function approveByHr(int $id): JsonResponse
    {
        $user = Auth::user();

        try {
            $resignation = $this->offboardingService->approveByHr($id, $user);

            return response()->json([
                'success' => true,
                'message' => 'Phòng Nhân sự đã phê duyệt hồ sơ pháp lý thôi việc.',
                'data'    => $resignation,
            ]);
        } catch (DomainException $e) {
            return response()->json([
                'success' => false,
                'message' => $e->getMessage(),
            ], 403);
        }
    }

    /**
     * Cấp 3: Ban Giám đốc phê duyệt Quyết định thôi việc và chốt ngày làm việc cuối.
     */
    public function approveByBod(Request $request, int $id): JsonResponse
    {
        $validated = $request->validate([
            'approved_last_date' => 'nullable|date',
        ]);

        $user = Auth::user();
        $lastDate = !empty($validated['approved_last_date']) ? Carbon::parse($validated['approved_last_date']) : null;

        try {
            $resignation = $this->offboardingService->approveByBod($id, $user, $lastDate);

            return response()->json([
                'success' => true,
                'message' => 'Ban Giám đốc đã ký phê duyệt Quyết định thôi việc.',
                'data'    => $resignation,
            ]);
        } catch (DomainException $e) {
            return response()->json([
                'success' => false,
                'message' => $e->getMessage(),
            ], 403);
        }
    }

    /**
     * Xác nhận thu hồi tài sản theo checklist bàn giao.
     */
    public function confirmAssetHandover(Request $request, int $itemId): JsonResponse
    {
        $validated = $request->validate([
            'notes' => 'nullable|string|max:500',
        ]);

        $user = Auth::user();
        $receiverId = $user->employee_id ?? 1;

        $item = $this->offboardingService->confirmHandoverItem($itemId, $receiverId, $validated['notes'] ?? null);

        return response()->json([
            'success' => true,
            'message' => "Đã xác nhận thu hồi tài sản [{$item->item_name}].",
            'data'    => $item,
        ]);
    }

    /**
     * Tính toán kỳ lương cuối và thời hạn thanh toán (Điều 46, Điều 48 BLLĐ 2019).
     */
    public function calculateSettlement(Request $request, int $id): JsonResponse
    {
        $adjustments = $request->validate([
            'actual_working_days'   => 'nullable|numeric|min:0',
            'standard_working_days' => 'nullable|numeric|min:1',
            'advance_deduction'     => 'nullable|numeric|min:0',
            'other_deductions'      => 'nullable|numeric|min:0',
        ]);

        try {
            $settlement = $this->offboardingService->calculateFinalSettlement($id, $adjustments);

            return response()->json([
                'success' => true,
                'message' => 'Tính toán kỳ lương quyết toán thôi việc thành công.',
                'data'    => $settlement,
            ]);
        } catch (DomainException $e) {
            return response()->json([
                'success' => false,
                'message' => $e->getMessage(),
            ], 422);
        }
    }
}
