<?php

namespace App\Modules\Recruitment\Services;

use App\Models\AuditLog;
use App\Models\Document;
use App\Models\Notification;
use App\Models\User;
use App\Modules\Employee\Models\Employee;
use App\Modules\Organization\Models\EmployeeAssignment;
use App\Modules\Payroll\Models\Contract;
use App\Modules\Payroll\Models\EmployeeSalary;
use App\Modules\Recruitment\Models\Application;
use App\Modules\Recruitment\Models\Candidate;
use App\Modules\Recruitment\Models\HealthCheck;
use App\Modules\Recruitment\Models\Interview;
use App\Modules\Recruitment\Models\Offer;
use App\Modules\Recruitment\Models\Requisition;
use Brick\Math\BigDecimal;
use Brick\Math\RoundingMode;
use Carbon\Carbon;
use DomainException;
use Illuminate\Http\UploadedFile;
use Illuminate\Support\Facades\Auth;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Hash;
use Illuminate\Support\Facades\Storage;
use Spatie\Permission\Models\Role;

class RecruitmentService
{
    /**
     * BƯỚC 1: Đề xuất tuyển dụng và phê duyệt 4 cấp (Trưởng phòng -> GĐ Đơn vị -> Ban TCCB -> Ban TGĐ).
     * Một cấp từ chối là toàn bộ quy trình dừng lại ngay lập tức.
     */
    public function approveRequisitionStep(Requisition $requisition, User $approver, bool $isApproved, ?string $comments = null): Requisition
    {
        return DB::transaction(function () use ($requisition, $approver, $isApproved, $comments) {
            if (!$isApproved) {
                // Một cấp từ chối là quy trình dừng lại ngay lập tức
                $requisition->update([
                    'status' => 'CANCELLED',
                ]);

                AuditLog::create([
                    'table_name'   => 'requisitions',
                    'record_id'    => $requisition->id,
                    'action'       => 'UPDATE',
                    'performed_by' => $approver->id,
                    'new_values'   => ['status' => 'CANCELLED', 'reason' => $comments],
                ]);

                return $requisition;
            }

            // Cập nhật trạng thái duyệt leo thang
            $statusFlow = [
                'DRAFT'          => 'UNIT_APPROVED',
                'UNIT_APPROVED'  => 'HR_APPROVED',
                'HR_APPROVED'    => 'APPROVED', // Ban TGĐ duyệt tối cao
            ];

            $nextStatus = $statusFlow[$requisition->status] ?? 'APPROVED';
            $requisition->update(['status' => $nextStatus]);

            AuditLog::create([
                'table_name'   => 'requisitions',
                'record_id'    => $requisition->id,
                'action'       => 'UPDATE',
                'performed_by' => $approver->id,
                'new_values'   => ['status' => $nextStatus, 'approver' => $approver->username],
            ]);

            return $requisition;
        });
    }

    /**
     * BƯỚC 2: Ứng viên nộp hồ sơ công khai (Upload CV PDF <= 5MB) hoặc HR Local nhập tay.
     * Đã sửa lỗi: Kiểm tra Server-Side Magic Byte, ngăn chặn Polyglot/RCE, lưu trữ Private Disk bảo vệ dữ liệu NĐ 13/2023
     */
    public function submitApplication(int $requisitionId, array $candidateData, ?UploadedFile $cvFile = null): Application
    {
        return DB::transaction(function () use ($requisitionId, $candidateData, $cvFile) {
            $cvUrl = null;
            if ($cvFile) {
                // 1. Kiểm tra dung lượng tối đa 5MB
                if ($cvFile->getSize() > 5 * 1024 * 1024) {
                    throw new DomainException('Dung lượng tệp CV không được vượt quá 5MB.');
                }

                // 2. Kiểm tra Extension hợp lệ (chỉ chấp nhận duy nhất .pdf)
                $extension = strtolower($cvFile->getClientOriginalExtension());
                if ($extension !== 'pdf') {
                    throw new DomainException('Phần mở rộng tệp không hợp lệ. Chỉ chấp nhận tệp có đuôi .pdf.');
                }

                // 3. Kiểm tra MIME type thực tế từ FileInfo (Server-side inspection, không tin cậy Client Header)
                $realMimeType = $cvFile->getMimeType();
                if ($realMimeType !== 'application/pdf') {
                    throw new DomainException('Định dạng tệp không hợp lệ. Hệ thống chỉ chấp nhận tệp tài liệu PDF chuẩn.');
                }

                // 4. Kiểm tra Magic Bytes (%PDF-) ở đầu tệp
                $filePath = $cvFile->getRealPath();
                $handle = fopen($filePath, 'rb');
                $header = fread($handle, 5);
                fclose($handle);
                if ($header !== '%PDF-') {
                    throw new DomainException('Cấu trúc nhị phân của tệp không phải PDF chuẩn (Nghi ngờ tệp độc hại giả mạo đuôi file).');
                }

                // 5. Sinh tên tệp ngẫu nhiên an toàn bằng UUIDv4 để tránh Path Traversal & Ghi đè
                $safeFileName = \Illuminate\Support\Str::uuid() . '.pdf';
                // Lưu vào Private Disk (local private disk) thay vì public để bảo vệ dữ liệu cá nhân theo NĐ 13/2023
                $cvUrl = $cvFile->storeAs('recruitment/cvs', $safeFileName, 'local');
            }

            // Tạo hoặc cập nhật thông tin ứng viên
            $candidate = Candidate::firstOrCreate(
                ['email' => $candidateData['email']],
                array_merge($candidateData, [
                    'candidate_code' => 'CAN-' . date('Ym') . '-' . rand(1000, 9999),
                    'cv_file_url'    => $cvUrl,
                ])
            );

            // Tạo đơn ứng tuyển ở cột nhận hồ sơ (NEW)
            return Application::create([
                'candidate_id'   => $candidate->id,
                'requisition_id' => $requisitionId,
                'source'         => $candidateData['source'] ?? 'PORTAL',
                'applied_date'   => now()->toDateString(),
                'status'         => 'NEW',
                'notes'          => $candidateData['notes'] ?? null,
            ]);
        });
    }

    /**
     * BƯỚC 4: Hội đồng chấm phỏng vấn theo Rubric tiêu chí.
     */
    public function evaluateInterview(int $applicationId, int $interviewerId, float $score, string $feedback, string $result): Interview
    {
        return DB::transaction(function () use ($applicationId, $interviewerId, $score, $feedback, $result) {
            $interview = Interview::create([
                'application_id' => $applicationId,
                'round_number'   => 1,
                'interview_date' => now(),
                'interviewer_id' => $interviewerId,
                'score'          => $score,
                'feedback'       => $feedback,
                'result'         => $result,
            ]);

            $application = Application::findOrFail($applicationId);
            if ($result === 'PASS') {
                $application->update(['status' => 'HEALTH_CHECK']); // Chuyển sang khám sức khỏe
            } elseif ($result === 'FAIL') {
                $application->update(['status' => 'REJECTED']);
            }

            return $interview;
        });
    }

    /**
     * BƯỚC 5: KHÁM SỨC KHỎE KHÔNG ĐẠT THÌ CHẶN TẠO OFFER (KIỂM TRA TẠI TẦNG SERVICE).
     */
    public function createOffer(int $applicationId, array $offerData): Offer
    {
        $application = Application::with('candidate')->findOrFail($applicationId);

        // Kiểm tra kết quả khám sức khỏe mới nhất của ứng viên
        $healthCheck = HealthCheck::where('candidate_id', $application->candidate_id)
            ->latest('check_date')
            ->first();

        // NGUYÊN TẮC AN TOÀN SINH HỌC & VỆ SINH SỮA: Chưa khám hoặc Không đạt -> CHẶN TUYỆT ĐỐI
        if (!$healthCheck || !$healthCheck->is_passed) {
            throw new DomainException(
                'Lỗi nghiệp vụ ATS: Không thể tạo Thư mời nhận việc (Offer). Ứng viên chưa có kết quả khám sức khỏe hoặc kết quả khám KHÔNG ĐẠT tiêu chuẩn vệ sinh an toàn thực phẩm.'
            );
        }

        return DB::transaction(function () use ($application, $offerData) {
            $offer = Offer::create([
                'application_id'      => $application->id,
                'position_id'         => $offerData['position_id'],
                'offered_p1'          => $offerData['offered_p1'],
                'offered_p2'          => $offerData['offered_p2'],
                'probation_rate'      => $offerData['probation_rate'] ?? 85.00,
                'expected_start_date' => $offerData['expected_start_date'],
                'status'              => 'APPROVED',
                'approved_by'         => Auth::id() ?? 1,
            ]);

            $application->update(['status' => 'OFFERED']);

            return $offer;
        });
    }

    /**
     * BƯỚC 6: ỨNG VIÊN ĐỒNG Ý OFFER -> KÍCH HOẠT ONBOARDING TRONG 1 ATOMIC TRANSACTION.
     * Tạo Employee (VNM-000123), Assignment, Salary (Thử việc 85%), Contract (2 tháng), User ESS, chuyển Document.
     */
    public function acceptOffer(int $offerId): Employee
    {
        return DB::transaction(function () use ($offerId) {
            $offer = Offer::with(['application.candidate', 'application.requisition'])->findOrFail($offerId);
            $candidate = $offer->application->candidate;
            $requisition = $offer->application->requisition;

            // 1. Sinh Mã nhân viên tự động chuẩn định dạng: VNM-000123
            $nextSequence = $this->generateNextEmployeeCode();

            $startDate = Carbon::parse($offer->expected_start_date);
            $probationEndDate = $startDate->copy()->addMonths(2)->subDay();

            // 2. Tạo bản ghi Hồ sơ nhân sự (employees)
            $employee = Employee::create([
                'employee_code'       => $nextSequence,
                'full_name'           => $candidate->full_name,
                'gender'              => $candidate->gender ?? 'OTHER',
                'date_of_birth'       => $candidate->date_of_birth ?? '1995-01-01',
                'national_id'         => $candidate->national_id ?? ('CCCD' . rand(10000000, 99999999)),
                'personal_email'      => $candidate->email,
                'company_email'       => strtolower(str_replace(' ', '', $candidate->full_name)) . '@vinamilk.com.vn',
                'phone_number'        => $candidate->phone_number,
                'hire_date'           => $startDate->toDateString(),
                'probation_end_date'  => $probationEndDate->toDateString(),
                'employment_status'   => 'PROBATION',
            ]);

            // 3. Tạo phân công công tác ban đầu (employee_assignments)
            EmployeeAssignment::create([
                'employee_id'     => $employee->id,
                'org_unit_id'     => $requisition->org_unit_id,
                'position_id'     => $offer->position_id,
                'is_primary'      => true,
                'assignment_type' => 'INITIAL',
                'effective_from'  => $startDate->toDateString(),
                'effective_to'    => null,
                'is_current'      => true,
                'notes'           => 'Tiếp nhận thử việc từ tuyển dụng đợt ' . $requisition->requisition_code,
            ]);

            // 4. Tạo mức lương thử việc 85% (employee_salaries) bằng brick/math
            $probationRatio = BigDecimal::of($offer->probation_rate)->dividedBy(100, 4, RoundingMode::HALF_UP);
            $p1Probation = BigDecimal::of($offer->offered_p1)->multipliedBy($probationRatio)->toScale(0, RoundingMode::HALF_UP);
            $p2Probation = BigDecimal::of($offer->offered_p2)->multipliedBy($probationRatio)->toScale(0, RoundingMode::HALF_UP);

            EmployeeSalary::create([
                'employee_id'          => $employee->id,
                'p1_position_salary'   => $p1Probation->toBigInteger(),
                'p2_competency_salary' => $p2Probation->toBigInteger(),
                'insurance_salary'     => $p1Probation->toBigInteger(),
                'effective_from'       => $startDate->toDateString(),
                'effective_to'         => null,
                'is_current'           => true,
            ]);

            // 5. Tạo Hợp đồng thử việc 2 tháng (contracts)
            Contract::create([
                'contract_code'    => 'HDTV-' . date('Y') . '/' . $nextSequence,
                'employee_id'      => $employee->id,
                'contract_type'    => 'PROBATION',
                'contract_status'  => 'ACTIVE',
                'signed_date'      => now()->toDateString(),
                'start_date'       => $startDate->toDateString(),
                'end_date'         => $probationEndDate->toDateString(),
                'base_salary'      => $p1Probation->toBigInteger(),
                'working_location' => $requisition->orgUnit?->unit_name ?? 'Vinamilk HQ',
                'effective_from'   => $startDate->toDateString(),
                'effective_to'     => $probationEndDate->toDateString(),
                'is_current'       => true,
            ]);

            // 6. Tạo tài khoản đăng nhập ESS (users) có cờ bắt buộc đổi mật khẩu lần đầu
            $roleEmployee = Role::firstOrCreate(['name' => 'EMPLOYEE', 'guard_name' => 'web']);
            $user = User::create([
                'username'             => strtolower($nextSequence),
                'password'             => Hash::make('Vinamilk@' . date('Y')),
                'employee_id'          => $employee->id,
                'role_id'              => $roleEmployee->id,
                'is_active'            => true,
                'must_change_password' => true,
            ]);
            $user->assignRole($roleEmployee);

            // 7. Chép hồ sơ CV đính kèm sang bảng tài liệu lưu trữ (documents)
            if ($candidate->cv_file_url) {
                Document::create([
                    'employee_id'   => $employee->id,
                    'document_name' => 'Hồ sơ CV Ứng viên - ' . $candidate->full_name,
                    'document_type' => 'OTHER',
                    'file_path'     => $candidate->cv_file_url,
                ]);
            }

            // 8. Cập nhật trạng thái hoàn tất ATS
            $offer->update(['status' => 'ACCEPTED']);
            $offer->application->update(['status' => 'HIRED']);

            AuditLog::create([
                'table_name'   => 'employees',
                'record_id'    => $employee->id,
                'action'       => 'INSERT',
                'performed_by' => Auth::id() ?? 1,
                'new_values'   => [
                    'employee_code' => $nextSequence,
                    'action'        => 'ONBOARDING_OFFER_ACCEPTED'
                ],
            ]);

            return $employee;
        });
    }

    /**
     * BƯỚC 7: Cảnh báo trước khi hết hạn thử việc 10 ngày để quản lý đánh giá.
     */
    public function checkProbationExpiryAlerts(): int
    {
        $alertDate = now()->addDays(10)->toDateString();

        $expiringEmployees = Employee::where('employment_status', 'PROBATION')
            ->whereDate('probation_end_date', $alertDate)
            ->with(['currentAssignment.orgUnit.manager.user'])
            ->get();

        foreach ($expiringEmployees as $emp) {
            $managerUser = $emp->currentAssignment?->orgUnit?->manager?->user;
            if ($managerUser) {
                Notification::create([
                    'user_id'  => $managerUser->id,
                    'title'    => 'Nhắc việc: Đánh giá hết hạn thử việc nhân viên ' . $emp->full_name,
                    'content'  => "Nhân viên {$emp->full_name} ({$emp->employee_code}) sẽ kết thúc thời gian thử việc 2 tháng vào ngày {$emp->probation_end_date}. Vui lòng thực hiện đánh giá để chuyển hợp đồng chính thức.",
                    'type'     => 'APPROVAL_REQUEST',
                    'link_url' => "/employees/{$emp->id}/probation-review",
                ]);
            }
        }

        return $expiringEmployees->count();
    }

    /**
     * Thuật toán sinh mã nhân viên tự động: VNM-000123 (6 chữ số có bù số 0)
     */
    private function generateNextEmployeeCode(): string
    {
        // Sử dụng lock for update để tránh đụng độ khi tuyển dụng hàng loạt cùng giây
        $latest = Employee::where('employee_code', 'LIKE', 'VNM-%')
            ->lockForUpdate()
            ->orderBy('id', 'desc')
            ->first();

        if (!$latest) {
            return 'VNM-000001';
        }

        $numberPart = (int)str_replace('VNM-', '', $latest->employee_code);
        $nextNumber = $numberPart + 1;

        return 'VNM-' . str_pad((string)$nextNumber, 6, '0', STR_PAD_LEFT);
    }
}
