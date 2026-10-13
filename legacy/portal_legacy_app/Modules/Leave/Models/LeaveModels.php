<?php

namespace App\Modules\Leave\Models;

use App\Models\User;
use App\Modules\Attendance\Models\AttendanceDay;
use App\Modules\Employee\Models\Employee;
use App\Modules\Payroll\Models\Contract;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Eloquent\Relations\HasOne;
use Illuminate\Database\Eloquent\SoftDeletes;

class LeaveType extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'leave_types';

    protected $fillable = [
        'type_code',
        'type_name',
        'is_paid',
        'deduct_annual_balance',
        'social_insurance_coverage',
        'requires_attachment',
        'max_days_allowed',
    ];

    protected $casts = [
        'is_paid'                   => 'boolean',
        'deduct_annual_balance'     => 'boolean',
        'social_insurance_coverage' => 'boolean',
        'requires_attachment'       => 'boolean',
        'max_days_allowed'          => 'float',
    ];

    public function requests(): HasMany
    {
        return $this->hasMany(LeaveRequest::class, 'leave_type_id');
    }
}

class LeaveBalance extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'leave_balances';

    protected $fillable = [
        'employee_id',
        'year',
        'annual_leave_quota',
        'seniority_leave_days',
        'transferred_days',
        'used_days',
    ];

    protected $casts = [
        'annual_leave_quota'   => 'float',
        'seniority_leave_days' => 'float',
        'transferred_days'     => 'float',
        'used_days'            => 'float',
        'remaining_days'       => 'float',
    ];

    public function employee(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'employee_id');
    }

    /**
     * Tính toán số ngày phép còn khả dụng
     */
    public function getAvailableDaysAttribute(): float
    {
        return (float) ($this->annual_leave_quota + $this->seniority_leave_days + $this->transferred_days - $this->used_days);
    }
}

class LeaveRequest extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'leave_requests';

    protected $fillable = [
        'request_code',
        'employee_id',
        'leave_type_id',
        'start_date',
        'end_date',
        'leave_days',
        'reason',
        'attachment_url',
        'status', // PENDING, APPROVED, REJECTED, CANCELLED
        'approved_by',
        'approved_at',
        'rejection_reason',
    ];

    protected $casts = [
        'start_date'  => 'date',
        'end_date'    => 'date',
        'leave_days'  => 'float',
        'approved_at' => 'datetime',
    ];

    public function employee(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'employee_id');
    }

    public function leaveType(): BelongsTo
    {
        return $this->belongsTo(LeaveType::class, 'leave_type_id');
    }

    public function approver(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'approved_by');
    }
}

class ResignationRequest extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'resignation_requests';

    protected $fillable = [
        'resignation_code',
        'employee_id',
        'contract_id',
        'submitted_date',
        'required_notice_days',
        'proposed_last_date',
        'approved_last_date',
        'reason',
        'status', // SUBMITTED, MANAGER_APPROVED, HR_APPROVED, BOD_APPROVED, REJECTED, CANCELLED
        'manager_approved_by',
        'manager_approved_at',
        'hr_approved_by',
        'hr_approved_at',
        'bod_approved_by',
        'bod_approved_at',
        'handover_completed_at',
        'settlement_completed_at',
        'notes',
    ];

    protected $casts = [
        'submitted_date'          => 'date',
        'proposed_last_date'      => 'date',
        'approved_last_date'      => 'date',
        'manager_approved_at'     => 'datetime',
        'hr_approved_at'          => 'datetime',
        'bod_approved_at'         => 'datetime',
        'handover_completed_at'   => 'datetime',
        'settlement_completed_at' => 'datetime',
        'required_notice_days'    => 'integer',
    ];

    public function employee(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'employee_id');
    }

    public function contract(): BelongsTo
    {
        return $this->belongsTo(Contract::class, 'contract_id');
    }

    public function managerApprover(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'manager_approved_by');
    }

    public function handoverItems(): HasMany
    {
        return $this->hasMany(AssetHandoverItem::class, 'resignation_request_id');
    }

    public function settlement(): HasOne
    {
        return $this->hasOne(FinalSettlement::class, 'resignation_request_id');
    }
}

class AssetHandoverItem extends Model
{
    use HasFactory;

    protected $table = 'asset_handover_items';

    protected $fillable = [
        'resignation_request_id',
        'item_name',
        'item_category', // IT_LAPTOP, ACCESS_CARD, PPE_UNIFORM, TOOLS_EQUIPMENT, FINANCE_SETTLEMENT, OTHER
        'assigned_department', // IT, ADMIN, WORKSHOP, FINANCE
        'is_returned',
        'returned_at',
        'receiver_employee_id',
        'notes',
    ];

    protected $casts = [
        'is_returned' => 'boolean',
        'returned_at' => 'datetime',
    ];

    public function resignation(): BelongsTo
    {
        return $this->belongsTo(ResignationRequest::class, 'resignation_request_id');
    }

    public function receiver(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'receiver_employee_id');
    }
}

class FinalSettlement extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'final_settlements';

    protected $fillable = [
        'settlement_code',
        'resignation_request_id',
        'employee_id',
        'last_working_date',
        'actual_working_days',
        'actual_work_salary',
        'remaining_leave_days',
        'daily_leave_rate',
        'unused_leave_allowance',
        'severance_qualifying_years',
        'severance_monthly_avg_salary',
        'severance_allowance',
        'advance_deduction',
        'other_deductions',
        'net_settlement_amount',
        'payment_deadline_date',
        'payment_status', // PENDING, APPROVED, PAID, OVERDUE
        'paid_at',
        'notes',
    ];

    protected $casts = [
        'last_working_date'            => 'date',
        'payment_deadline_date'        => 'date',
        'paid_at'                      => 'datetime',
        'actual_working_days'          => 'float',
        'remaining_leave_days'         => 'float',
        'severance_qualifying_years'   => 'float',
        'actual_work_salary'           => 'decimal:0',
        'daily_leave_rate'             => 'decimal:0',
        'unused_leave_allowance'       => 'decimal:0',
        'severance_monthly_avg_salary' => 'decimal:0',
        'severance_allowance'          => 'decimal:0',
        'advance_deduction'            => 'decimal:0',
        'other_deductions'             => 'decimal:0',
        'net_settlement_amount'        => 'decimal:0',
    ];

    public function resignation(): BelongsTo
    {
        return $this->belongsTo(ResignationRequest::class, 'resignation_request_id');
    }

    public function employee(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'employee_id');
    }
}
