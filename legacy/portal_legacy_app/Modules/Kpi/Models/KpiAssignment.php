<?php

namespace App\Modules\Kpi\Models;

use App\Modules\Employee\Models\Employee;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\SoftDeletes;

class KpiAssignment extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'kpi_assignments';

    protected $fillable = [
        'kpi_period_id',
        'employee_id',
        'evaluator_id',
        'title',
        'description',
        'target_metric',
        'weight_percentage',
        'due_date',
        'baseline_score',
        'time_score',
        'manager_adjustment',
        'adjustment_reason',
        'final_score',
        'actual_result',
        'completed_date',
        'submission_date',
        'evaluation_date',
        'rejection_reason',
        'feedback',
        'status', // Assigned, InProgress, PendingApproval, Approved, Rejected, Closed, NotCompleted
    ];

    protected $casts = [
        'weight_percentage'  => 'float',
        'baseline_score'     => 'float',
        'time_score'         => 'float',
        'manager_adjustment' => 'float',
        'final_score'        => 'float',
        'due_date'           => 'date',
        'completed_date'     => 'date',
        'submission_date'    => 'datetime',
        'evaluation_date'    => 'datetime',
    ];

    public function period(): BelongsTo
    {
        return $this->belongsTo(KpiPeriod::class, 'kpi_period_id');
    }

    public function employee(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'employee_id');
    }

    public function evaluator(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'evaluator_id');
    }

    /**
     * Scope lọc theo trạng thái
     */
    public function scopeByStatus($query, string $status)
    {
        return $query->where('status', $status);
    }
}
