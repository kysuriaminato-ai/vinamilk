<?php

namespace App\Modules\Recruitment\Http\Controllers;

use App\Http\Controllers\Controller;
use App\Modules\Recruitment\Models\Application;
use App\Modules\Recruitment\Models\Requisition;
use App\Modules\Recruitment\Services\RecruitmentService;
use DomainException;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\RedirectResponse;
use Illuminate\Http\Request;
use Illuminate\View\View;

class RecruitmentController extends Controller
{
    public function __construct(
        protected RecruitmentService $recruitmentService
    ) {}

    /**
     * BẢNG KANBAN THEO DÕI QUY TRÌNH ỨNG VIÊN (ATS PIPELINE)
     */
    public function kanban(Request $request): View
    {
        $requisitionId = $request->query('requisition_id');

        $query = Application::with(['candidate', 'requisition.position', 'healthCheck', 'offer']);
        if ($requisitionId) {
            $query->where('requisition_id', $requisitionId);
        }

        $applications = $query->latest()->get();

        // Gom nhóm hồ sơ theo 7 cột Kanban
        $columns = [
            'NEW'          => $applications->where('status', 'NEW'),
            'SCREENING'    => $applications->where('status', 'SCREENING'),
            'INTERVIEWING' => $applications->where('status', 'INTERVIEWING'),
            'HEALTH_CHECK' => $applications->where('status', 'HEALTH_CHECK'),
            'OFFERED'      => $applications->where('status', 'OFFERED'),
            'HIRED'        => $applications->where('status', 'HIRED'),
            'REJECTED'     => $applications->where('status', 'REJECTED'),
        ];

        $requisitions = Requisition::where('status', 'APPROVED')->get();

        return view('recruitment.kanban', compact('columns', 'requisitions', 'requisitionId'));
    }

    /**
     * Tạo Thư mời nhận việc (Có kiểm tra bắt buộc Khám sức khỏe Đạt).
     */
    public function storeOffer(Request $request, int $applicationId): RedirectResponse
    {
        $validated = $request->validate([
            'position_id'         => 'required|exists:positions,id',
            'offered_p1'          => 'required|numeric|min:0',
            'offered_p2'          => 'required|numeric|min:0',
            'probation_rate'      => 'required|numeric|min:85|max:100',
            'expected_start_date' => 'required|date|after:today',
        ]);

        try {
            $this->recruitmentService->createOffer($applicationId, $validated);
            return redirect()->back()->with('success', 'Đã khởi tạo Thư mời nhận việc (Offer) thành công.');
        } catch (DomainException $e) {
            return redirect()->back()->with('error', $e->getMessage());
        }
    }

    /**
     * Ứng viên đồng ý Offer -> Kích hoạt Onboarding tự động.
     */
    public function acceptOffer(int $offerId): RedirectResponse
    {
        try {
            $employee = $this->recruitmentService->acceptOffer($offerId);
            return redirect()->route('recruitment.kanban')
                ->with('success', "Tiếp nhận nhân sự thành công! Đã cấp Mã nhân viên {$employee->employee_code}, ký hợp đồng thử việc 2 tháng và kích hoạt tài khoản ESS.");
        } catch (\Exception $e) {
            return redirect()->back()->with('error', 'Lỗi tiếp nhận hồ sơ: ' . $e->getMessage());
        }
    }

    /**
     * Ứng viên nộp CV qua cổng tuyển dụng công khai.
     */
    public function submitPublicApplication(Request $request, int $requisitionId): JsonResponse
    {
        $validated = $request->validate([
            'full_name'    => 'required|string|max:150',
            'email'        => 'required|email|max:100',
            'phone_number' => 'required|string|max:20',
            'cv_file'      => 'required|file|mimes:pdf|max:5120', // PDF <= 5MB
        ]);

        try {
            $application = $this->recruitmentService->submitApplication(
                $requisitionId,
                $validated,
                $request->file('cv_file')
            );

            return response()->json([
                'success' => true,
                'message' => 'Hồ sơ ứng tuyển của bạn đã được tiếp nhận thành công.',
                'application_id' => $application->id,
            ]);
        } catch (DomainException $e) {
            return response()->json(['success' => false, 'message' => $e->getMessage()], 422);
        }
    }
}
