<?php

namespace App\Modules\Leave\Http\Controllers;

use App\Http\Controllers\Controller;
use App\Modules\Leave\Models\LeaveBalance;
use App\Modules\Leave\Models\LeaveRequest;
use App\Modules\Leave\Models\LeaveType;
use App\Modules\Leave\Services\LeaveService;
use Carbon\Carbon;
use DomainException;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Auth;

class LeaveController extends Controller
{
    protected LeaveService $leaveService;

    public function __construct(LeaveService $leaveService)
    {
        $this->leaveService = $leaveService;
    }

    /**
     * Danh sách loại nghỉ phép và cấu hình cờ.
     */
    public function getLeaveTypes(): JsonResponse
    {
        $types = LeaveType::all();
        return response()->json([
            'success' => true,
            'data'    => $types,
        ]);
    }

    /**
     * Tra cứu số dư quỹ phép năm và thâm niên của nhân viên trên ESS.
     */
    public function getMyBalance(Request $request): JsonResponse
    {
        $user = Auth::user();
        $employeeId = $user->employee_id ?? $request->query('employee_id');
        $year = (int)$request->query('year', now()->year);

        $balance = LeaveBalance::where('employee_id', $employeeId)
            ->where('year', $year)
            ->first();

        return response()->json([
            'success' => true,
            'data'    => $balance ? [
                'year'                 => $balance->year,
                'annual_leave_quota'   => $balance->annual_leave_quota,
                'seniority_leave_days' => $balance->seniority_leave_days,
                'transferred_days'     => $balance->transferred_days,
                'used_days'            => $balance->used_days,
                'available_days'       => $balance->available_days,
            ] : null,
        ]);
    }

    /**
     * Nộp đơn xin nghỉ phép trên ESS.
     */
    public function submitRequest(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'leave_type_id'  => 'required|exists:leave_types,id',
            'start_date'     => 'required|date',
            'end_date'       => 'required|date|after_or_equal:start_date',
            'leave_days'     => 'required|numeric|min:0.5',
            'reason'         => 'required|string|max:1000',
            'attachment'     => 'nullable|file|mimes:pdf,jpg,jpeg,png|max:5120',
            'attachment_url' => 'nullable|string',
        ]);

        $user = Auth::user();
        $employeeId = $user->employee_id ?? $request->input('employee_id');

        try {
            $attachment = $request->file('attachment');
            $leaveReq = $this->leaveService->submitLeaveRequest($employeeId, $validated, $attachment);

            return response()->json([
                'success' => true,
                'message' => 'Nộp đơn xin nghỉ phép thành công. Đơn đã được chuyển đến Quản lý trực tiếp trên cây tổ chức.',
                'data'    => $leaveReq,
            ], 201);
        } catch (DomainException $e) {
            return response()->json([
                'success' => false,
                'message' => $e->getMessage(),
            ], 422);
        }
    }

    /**
     * Quản lý phê duyệt đơn nghỉ phép (Tuân thủ SoD).
     */
    public function approveRequest(int $id): JsonResponse
    {
        $user = Auth::user();

        try {
            $approved = $this->leaveService->approveLeaveRequest($id, $user);

            return response()->json([
                'success' => true,
                'message' => 'Phê duyệt đơn nghỉ phép thành công. Đã cập nhật bảng công và số dư phép.',
                'data'    => $approved,
            ]);
        } catch (DomainException $e) {
            return response()->json([
                'success' => false,
                'message' => $e->getMessage(),
            ], 403);
        }
    }

    /**
     * Từ chối đơn nghỉ phép.
     */
    public function rejectRequest(Request $request, int $id): JsonResponse
    {
        $validated = $request->validate([
            'rejection_reason' => 'required|string|max:500',
        ]);

        $user = Auth::user();

        try {
            $rejected = $this->leaveService->rejectLeaveRequest($id, $user, $validated['rejection_reason']);

            return response()->json([
                'success' => true,
                'message' => 'Đã từ chối đơn xin nghỉ phép.',
                'data'    => $rejected,
            ]);
        } catch (DomainException $e) {
            return response()->json([
                'success' => false,
                'message' => $e->getMessage(),
            ], 403);
        }
    }

    /**
     * Ghi nhận vắng mặt không phép (AWOL) & Cảnh báo kỷ luật sa thải (Điều 125 Khoản 4).
     */
    public function recordAwol(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'employee_id'  => 'required|exists:employees,id',
            'absence_date' => 'required|date',
            'reason'       => 'nullable|string|max:500',
        ]);

        $absenceDate = Carbon::parse($validated['absence_date']);
        $attDay = $this->leaveService->recordUnauthorizedAbsence($validated['employee_id'], $absenceDate, $validated['reason'] ?? '');

        return response()->json([
            'success' => true,
            'message' => 'Đã ghi nhận vắng mặt không phép và kiểm tra ngưỡng cảnh báo kỷ luật.',
            'data'    => $attDay,
        ]);
    }
}
