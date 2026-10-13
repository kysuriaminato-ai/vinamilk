<?php

namespace App\Modules\Payroll\Models;

use App\Modules\Employee\Models\Employee;
use App\Modules\Organization\Models\OrgUnit;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Eloquent\SoftDeletes;

class Payslip extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'payslips';

    protected $fillable = [
        'payroll_period_id',
        'employee_id',
        'org_unit_id',
        'cost_center_code',
        'actual_working_days',
        'paid_leave_days',
        'p1_position_amount',
        'p2_competency_amount',
        'p3_performance_amount',
        'ot_pay_amount',
        'total_allowance_amount',
        'retroactive_amount',
        'gross_income',
        'social_insurance_emp',
        'health_insurance_emp',
        'unemployment_insurance_emp',
        'union_fee_emp',
        'party_fee_emp',
        'personal_income_tax',
        'other_deductions',
        'net_income',
        'social_insurance_comp',
        'health_insurance_comp',
        'unemployment_insurance_comp',
        'union_fee_comp',
        'status', // DRAFT, CONFIRMED, PAID
        'paid_at',
        'bank_reference',
    ];

    protected $casts = [
        'actual_working_days'         => 'float',
        'paid_leave_days'             => 'float',
        'p1_position_amount'          => 'decimal:0',
        'p2_competency_amount'        => 'decimal:0',
        'p3_performance_amount'       => 'decimal:0',
        'ot_pay_amount'               => 'decimal:0',
        'total_allowance_amount'      => 'decimal:0',
        'retroactive_amount'          => 'decimal:0',
        'gross_income'                => 'decimal:0',
        'social_insurance_emp'        => 'decimal:0',
        'health_insurance_emp'        => 'decimal:0',
        'unemployment_insurance_emp'  => 'decimal:0',
        'union_fee_emp'               => 'decimal:0',
        'party_fee_emp'               => 'decimal:0',
        'personal_income_tax'         => 'decimal:0',
        'other_deductions'            => 'decimal:0',
        'net_income'                  => 'decimal:0',
        'social_insurance_comp'       => 'decimal:0',
        'health_insurance_comp'       => 'decimal:0',
        'unemployment_insurance_comp' => 'decimal:0',
        'union_fee_comp'              => 'decimal:0',
        'paid_at'                     => 'datetime',
    ];

    public function period(): BelongsTo
    {
        return $this->belongsTo(PayrollPeriod::class, 'payroll_period_id');
    }

    public function employee(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'employee_id');
    }

    public function orgUnit(): BelongsTo
    {
        return $this->belongsTo(OrgUnit::class, 'org_unit_id');
    }

    public function lines(): HasMany
    {
        return $this->hasMany(PayslipLine::class, 'payslip_id');
    }
}

class PayslipLine extends Model
{
    use HasFactory;

    protected $table = 'payslip_lines';

    protected $fillable = [
        'payslip_id',
        'line_code',
        'line_name',
        'line_type',
        'amount',
        'calculation_formula',
    ];

    protected $casts = [
        'amount' => 'decimal:0',
    ];

    public function payslip(): BelongsTo
    {
        return $this->belongsTo(Payslip::class, 'payslip_id');
    }
}
