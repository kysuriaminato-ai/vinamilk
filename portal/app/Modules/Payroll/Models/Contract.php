<?php

namespace App\Modules\Payroll\Models;

use App\Modules\Employee\Models\Employee;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\SoftDeletes;

class Contract extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'contracts';

    protected $fillable = [
        'contract_code',
        'employee_id',
        'contract_type', // PROBATION, DEFINITE_TERM_1Y, DEFINITE_TERM_2Y, DEFINITE_TERM_3Y, INDEFINITE_TERM, SEASONAL
        'contract_status', // DRAFT, ACTIVE, EXPIRED, TERMINATED
        'signed_date',
        'start_date',
        'end_date',
        'base_salary',
        'working_location',
        'file_url',
        'effective_from',
        'effective_to',
        'is_current',
    ];

    protected $casts = [
        'signed_date'    => 'date',
        'start_date'     => 'date',
        'end_date'       => 'date',
        'effective_from' => 'date',
        'effective_to'   => 'date',
        'base_salary'    => 'decimal:0',
        'is_current'     => 'boolean',
    ];

    public function employee(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'employee_id');
    }
}
