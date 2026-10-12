<?php

namespace App\Modules\Organization\Models;

use App\Modules\Employee\Models\Employee;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Eloquent\SoftDeletes;

class OrgUnit extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'org_units';

    protected $fillable = [
        'unit_code',
        'unit_name',
        'unit_type',
        'parent_id',
        'path',
        'level',
        'region',
        'address',
        'tax_code',
        'cost_center_code',
        'manager_employee_id',
        'effective_from',
        'effective_to',
        'is_active',
    ];

    protected $casts = [
        'effective_from' => 'date',
        'effective_to'   => 'date',
        'is_active'      => 'boolean',
        'level'          => 'integer',
    ];

    // =========================================================================
    // QUAN HỆ CÂY PHÂN CẤP (HIERARCHY RELATIONSHIPS)
    // =========================================================================

    public function parent(): BelongsTo
    {
        return $this->belongsTo(OrgUnit::class, 'parent_id');
    }

    public function children(): HasMany
    {
        return $this->hasMany(OrgUnit::class, 'parent_id')->orderBy('unit_name');
    }

    public function manager(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'manager_employee_id');
    }

    public function assignments(): HasMany
    {
        return $this->hasMany(EmployeeAssignment::class, 'org_unit_id');
    }

    public function activeAssignments(): HasMany
    {
        return $this->hasMany(EmployeeAssignment::class, 'org_unit_id')
            ->where('is_current', true)
            ->where('is_primary', true);
    }

    public function positions(): HasMany
    {
        return $this->hasMany(Position::class, 'org_unit_id');
    }
}
