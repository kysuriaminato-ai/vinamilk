<?php

namespace App\Modules\Organization\Models;

use App\Modules\Employee\Models\Employee;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\SoftDeletes;

class EmployeeAssignment extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'employee_assignments';

    protected $fillable = [
        'employee_id',
        'org_unit_id',
        'position_id',
        'is_primary',
        'assignment_type',
        'decision_number',
        'effective_from',
        'effective_to',
        'is_current',
        'notes',
    ];

    protected $casts = [
        'is_primary'     => 'boolean',
        'is_current'     => 'boolean',
        'effective_from' => 'date',
        'effective_to'   => 'date',
    ];

    public function employee(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'employee_id');
    }

    public function orgUnit(): BelongsTo
    {
        return $this->belongsTo(OrgUnit::class, 'org_unit_id');
    }

    public function position(): BelongsTo
    {
        return $this->belongsTo(Position::class, 'position_id');
    }
}
