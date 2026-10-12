<?php

namespace App\Modules\Employee\Models;

use App\Models\User;
use App\Modules\Organization\Models\EmployeeAssignment;
use App\Modules\Organization\Models\OrgUnit;
use App\Modules\Organization\Models\Position;
use App\Modules\Payroll\Models\EmployeeSalary;
use App\Modules\Payroll\Models\Payslip;
use App\Scopes\OrgUnitScope;
use Illuminate\Database\Eloquent\Builder;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Eloquent\Relations\HasOne;
use Illuminate\Database\Eloquent\SoftDeletes;

class Employee extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'employees';

    protected $fillable = [
        'employee_code',
        'full_name',
        'gender',
        'date_of_birth',
        'national_id',
        'national_id_issued_date',
        'national_id_issued_place',
        'personal_email',
        'company_email',
        'phone_number',
        'permanent_address',
        'current_address',
        'tax_id',
        'bank_account_number',
        'bank_name',
        'bank_branch',
        'social_insurance_number',
        'hire_date',
        'probation_end_date',
        'termination_date',
        'employment_status',
        'avatar_url',
    ];

    protected $casts = [
        'date_of_birth'            => 'date',
        'hire_date'                => 'date',
        'probation_end_date'       => 'date',
        'termination_date'         => 'date',
        'national_id_issued_date'  => 'date',
        // Mã hóa cột nhạy cảm tại tầng CSDL (Encryption at Rest theo NĐ 13/2023)
        'national_id'              => 'encrypted',
        'bank_account_number'      => 'encrypted',
        'social_insurance_number'  => 'encrypted',
        'tax_id'                   => 'encrypted',
    ];

    /**
     * MẶT NẠ DỮ LIỆU (DATA MASKING) THEO NGHỊ ĐỊNH 13/2023/NĐ-CP
     */

    /**
     * Che số CCCD: Chỉ hiện 3 số đầu và 4 số cuối (VD: 079*****1234)
     */
    public function getMaskedNationalIdAttribute(): string
    {
        $id = (string)($this->national_id ?? '');
        if (strlen($id) <= 6) {
            return str_repeat('*', strlen($id));
        }
        return substr($id, 0, 3) . str_repeat('*', max(3, strlen($id) - 7)) . substr($id, -4);
    }

    /**
     * Che số tài khoản ngân hàng: Chỉ hiện 4 số cuối (VD: *******5678)
     */
    public function getMaskedBankAccountAttribute(): string
    {
        $acc = (string)($this->bank_account_number ?? '');
        if (strlen($acc) <= 4) {
            return str_repeat('*', strlen($acc));
        }
        return str_repeat('*', strlen($acc) - 4) . substr($acc, -4);
    }

    /**
     * Che số điện thoại cá nhân: Giữ 3 số đầu và 3 số cuối (VD: 090****456)
     */
    public function getMaskedPhoneAttribute(): string
    {
        $phone = (string)($this->phone_number ?? '');
        if (strlen($phone) <= 6) {
            return str_repeat('*', strlen($phone));
        }
        return substr($phone, 0, 3) . str_repeat('*', strlen($phone) - 6) . substr($phone, -3);
    }

    /**
     * Che mã số thuế cá nhân: Giữ 3 số đầu và 3 số cuối (VD: 801****678)
     */
    public function getMaskedTaxIdAttribute(): string
    {
        $tax = (string)($this->tax_id ?? '');
        if (strlen($tax) <= 6) {
            return str_repeat('*', strlen($tax));
        }
        return substr($tax, 0, 3) . str_repeat('*', strlen($tax) - 6) . substr($tax, -3);
    }

    /**
     * Tự động đăng ký Global Scope lọc theo cây tổ chức khi khởi tạo Model.
     */
    protected static function booted(): void
    {
        static::addGlobalScope(new OrgUnitScope());
    }

    // =========================================================================
    // QUAN HỆ THỰC THỂ (RELATIONSHIPS)
    // =========================================================================

    /**
     * Tất cả lịch sử bổ nhiệm / điều chuyển công tác (SCD Type 2).
     */
    public function assignments(): HasMany
    {
        return $this->hasMany(EmployeeAssignment::class, 'employee_id');
    }

    /**
     * Vị trí công tác chính thức hiện hành.
     */
    public function currentAssignment(): HasOne
    {
        return $this->hasOne(EmployeeAssignment::class, 'employee_id')
            ->where('is_primary', true)
            ->where('is_current', true);
    }

    /**
     * Toàn bộ lịch sử diễn biến lương 3P.
     */
    public function salaries(): HasMany
    {
        return $this->hasMany(EmployeeSalary::class, 'employee_id');
    }

    /**
     * Biểu lương 3P và phụ cấp đang hiện hành.
     */
    public function currentSalary(): HasOne
    {
        return $this->hasOne(EmployeeSalary::class, 'employee_id')
            ->where('is_current', true);
    }

    /**
     * Tài khoản đăng nhập hệ thống của nhân viên.
     */
    public function user(): HasOne
    {
        return $this->hasOne(User::class, 'employee_id');
    }

    /**
     * Danh sách phiếu lương hàng tháng.
     */
    public function payslips(): HasMany
    {
        return $this->hasMany(Payslip::class, 'employee_id');
    }

    // =========================================================================
    // LOCAL SCOPES & TIỆN ÍCH NGHIỆP VỤ
    // =========================================================================

    /**
     * Lọc nhân sự đang làm việc (ACTIVE hoặc PROBATION).
     */
    public function scopeActive(Builder $query): Builder
    {
        return $query->whereIn('employment_status', ['ACTIVE', 'PROBATION']);
    }

    /**
     * Lọc nhân sự thuộc cây con chỉ định (bỏ qua Global Scope nếu cần).
     */
    public function scopeInSubtree(Builder $query, string $pathPrefix): Builder
    {
        return $query->whereHas('currentAssignment.orgUnit', function (Builder $q) use ($pathPrefix) {
            $q->where('path', 'LIKE', $pathPrefix . '%');
        });
    }

    /**
     * Lấy đơn vị hiện tại của nhân viên.
     */
    public function getCurrentOrgUnitAttribute(): ?OrgUnit
    {
        return $this->currentAssignment?->orgUnit;
    }

    /**
     * Lấy chức danh vị trí hiện tại của nhân viên.
     */
    public function getCurrentPositionAttribute(): ?Position
    {
        return $this->currentAssignment?->position;
    }
}
