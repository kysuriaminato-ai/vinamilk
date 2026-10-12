<?php

namespace App\Modules\Kpi\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Eloquent\SoftDeletes;

class KpiPeriod extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'kpi_periods';

    protected $fillable = [
        'period_code',
        'period_name',
        'period_type',
        'start_date',
        'end_date',
        'status', // OPEN, EVALUATING, LOCKED, CLOSED
    ];

    protected $casts = [
        'start_date' => 'date',
        'end_date'   => 'date',
    ];

    public function assignments(): HasMany
    {
        return $this->hasMany(KpiAssignment::class, 'kpi_period_id');
    }

    public function summaries(): HasMany
    {
        return $this->hasMany(KpiEmployeeSummary::class, 'kpi_period_id');
    }
}
