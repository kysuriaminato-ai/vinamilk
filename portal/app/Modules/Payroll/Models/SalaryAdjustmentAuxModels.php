<?php

namespace App\Modules\Payroll\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\SoftDeletes;

class SalaryBand extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'salary_bands';

    protected $fillable = [
        'band_code',
        'band_name',
        'job_level',
        'min_salary',
        'midpoint_salary',
        'max_salary',
    ];

    protected $casts = [
        'min_salary'      => 'decimal:0',
        'midpoint_salary' => 'decimal:0',
        'max_salary'      => 'decimal:0',
        'job_level'       => 'integer',
    ];
}

class RegionalMinWage extends Model
{
    protected $table = 'regional_min_wages';

    protected $fillable = [
        'region_zone',
        'min_wage_amount',
        'effective_from',
        'effective_to',
        'legal_reference',
    ];

    protected $casts = [
        'min_wage_amount' => 'decimal:0',
        'effective_from'  => 'date',
        'effective_to'    => 'date',
    ];
}

class UnitSalaryBudget extends Model
{
    protected $table = 'unit_salary_budgets';

    protected $fillable = [
        'org_unit_id',
        'year',
        'annual_budget',
        'allocated_amount',
        'used_amount',
    ];

    protected $casts = [
        'annual_budget'    => 'decimal:0',
        'allocated_amount' => 'decimal:0',
        'used_amount'      => 'decimal:0',
        'year'             => 'integer',
    ];

    public function getRemainingAmountAttribute(): float
    {
        return (float) ($this->annual_budget - $this->used_amount);
    }
}

class ApprovalRule extends Model
{
    protected $table = 'approval_rules';

    protected $fillable = [
        'rule_code',
        'rule_name',
        'workflow_type',
        'step_order',
        'approver_role',
        'description',
    ];

    protected $casts = [
        'step_order' => 'integer',
    ];
}
