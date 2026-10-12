<?php

namespace App\Modules\Payroll\Models;

use App\Modules\Employee\Models\Employee;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Eloquent\SoftDeletes;

class SalaryProposal extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'salary_proposals';

    protected $fillable = [
        'proposal_code',
        'employee_id',
        'proposal_type', // PERIODIC, DEPARTMENT_RECOMMENDED, SPECIAL_EXCEPTION, MASS_STATUTORY
        'current_p1',
        'proposed_p1',
        'current_p2',
        'proposed_p2',
        'current_insurance_salary',
        'proposed_insurance_salary',
        'salary_band_id',
        'band_min',
        'band_midpoint',
        'band_max',
        'current_compa_ratio',
        'proposed_compa_ratio',
        'kpi_history_json',
        'last_salary_increase_date',
        'months_since_last_increase',
        'is_out_of_band',
        'is_special_exception',
        'is_senior_management',
        'exception_reason',
        'attachment_url',
        'warnings_json',
        'effective_from',
        'reason',
        'status', // DRAFT, SUBMITTED, PENDING_STEP, APPROVED, REJECTED, CANCELLED
        'current_step_number',
        'current_workflow_type',
        'decision_number',
        'decision_pdf_url',
        'retroactive_months',
        'retroactive_amount',
        'created_by',
        'approved_at',
        'approved_by',
    ];

    protected $casts = [
        'current_p1'                 => 'decimal:0',
        'proposed_p1'                => 'decimal:0',
        'current_p2'                 => 'decimal:0',
        'proposed_p2'                => 'decimal:0',
        'current_insurance_salary'   => 'decimal:0',
        'proposed_insurance_salary'  => 'decimal:0',
        'band_min'                   => 'decimal:0',
        'band_midpoint'              => 'decimal:0',
        'band_max'                   => 'decimal:0',
        'retroactive_amount'         => 'decimal:0',
        'current_compa_ratio'        => 'float',
        'proposed_compa_ratio'       => 'float',
        'effective_from'             => 'date',
        'last_salary_increase_date'  => 'date',
        'approved_at'                => 'datetime',
        'is_out_of_band'             => 'boolean',
        'is_special_exception'       => 'boolean',
        'is_senior_management'       => 'boolean',
        'kpi_history_json'           => 'array',
        'warnings_json'              => 'array',
        'months_since_last_increase' => 'integer',
        'current_step_number'        => 'integer',
        'retroactive_months'         => 'integer',
    ];

    public function employee(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'employee_id');
    }

    public function creator(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'created_by');
    }

    public function approver(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'approved_by');
    }

    public function steps(): HasMany
    {
        return $this->hasMany(ApprovalStep::class, 'proposal_id')->orderBy('step_number');
    }

    public function retroactiveAdjustments(): HasMany
    {
        return $this->hasMany(RetroactiveSalaryAdjustment::class, 'salary_proposal_id');
    }
}

class ApprovalStep extends Model
{
    protected $table = 'approval_steps';

    protected $fillable = [
        'proposal_id',
        'step_number',
        'approver_role',
        'approver_id',
        'action', // PENDING, APPROVED, REJECTED
        'comments',
        'action_at',
    ];

    protected $casts = [
        'step_number' => 'integer',
        'action_at'   => 'datetime',
    ];

    public function proposal(): BelongsTo
    {
        return $this->belongsTo(SalaryProposal::class, 'proposal_id');
    }

    public function approver(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'approver_id');
    }
}

class RetroactiveSalaryAdjustment extends Model
{
    protected $table = 'retroactive_salary_adjustments';

    protected $fillable = [
        'salary_proposal_id',
        'employee_id',
        'payroll_period_id',
        'source_period_id',
        'retroactive_difference',
        'status', // PENDING, PROCESSED
    ];

    protected $casts = [
        'retroactive_difference' => 'decimal:0',
    ];

    public function proposal(): BelongsTo
    {
        return $this->belongsTo(SalaryProposal::class, 'salary_proposal_id');
    }

    public function employee(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'employee_id');
    }

    public function payrollPeriod(): BelongsTo
    {
        return $this->belongsTo(PayrollPeriod::class, 'payroll_period_id');
    }

    public function sourcePeriod(): BelongsTo
    {
        return $this->belongsTo(PayrollPeriod::class, 'source_period_id');
    }
}
