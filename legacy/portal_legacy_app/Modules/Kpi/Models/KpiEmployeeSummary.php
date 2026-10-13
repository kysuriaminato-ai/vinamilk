<?php

namespace App\Modules\Kpi\Models;

use App\Modules\Employee\Models\Employee;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\SoftDeletes;

class KpiEmployeeSummary extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'kpi_employee_summaries';

    protected $fillable = [
        'kpi_period_id',
        'employee_id',
        'total_weight_percentage',
        'kpi_score',
        'kpi_grade',
        'p3_multiplier',
        'status', // DRAFT, APPROVED, EXPORTED_TO_PAYROLL
        'exported_at',
        'exported_by',
    ];

    protected $casts = [
        'total_weight_percentage' => 'float',
        'kpi_score'               => 'float',
        'p3_multiplier'           => 'float',
        'exported_at'             => 'datetime',
    ];

    public function period(): BelongsTo
    {
        return $this->belongsTo(KpiPeriod::class, 'kpi_period_id');
    }

    public function employee(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'employee_id');
    }
}
