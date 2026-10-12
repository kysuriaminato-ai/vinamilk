<?php

namespace App\Modules\Payroll\Models;

use App\Modules\Employee\Models\Employee;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\SoftDeletes;

class EmployeeSalary extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'employee_salaries';

    protected $fillable = [
        'employee_id',
        'p1_position_salary',
        'p2_competency_salary',
        'insurance_salary',
        'toxic_allowance',
        'farm_field_allowance',
        'responsibility_allowance',
        'meal_allowance',
        'decision_number',
        'effective_from',
        'effective_to',
        'is_current',
    ];

    protected $casts = [
        'p1_position_salary'       => 'decimal:0',
        'p2_competency_salary'     => 'decimal:0',
        'insurance_salary'         => 'decimal:0',
        'toxic_allowance'          => 'decimal:0',
        'farm_field_allowance'     => 'decimal:0',
        'responsibility_allowance' => 'decimal:0',
        'meal_allowance'           => 'decimal:0',
        'effective_from'           => 'date',
        'effective_to'             => 'date',
        'is_current'               => 'boolean',
    ];

    public function employee(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'employee_id');
    }
}
