<?php

namespace App\Modules\Recruitment\Models;

use App\Models\Document;
use App\Models\User;
use App\Modules\Employee\Models\Employee;
use App\Modules\Organization\Models\EmployeeAssignment;
use App\Modules\Organization\Models\OrgUnit;
use App\Modules\Organization\Models\Position;
use App\Modules\Payroll\Models\EmployeeSalary;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Eloquent\Relations\HasOne;
use Illuminate\Database\Eloquent\SoftDeletes;

class Application extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'applications';

    protected $fillable = [
        'candidate_id',
        'requisition_id',
        'source',
        'applied_date',
        'status',
        'notes',
    ];

    protected $casts = [
        'applied_date' => 'date',
    ];

    public function candidate(): BelongsTo
    {
        return $this->belongsTo(Candidate::class, 'candidate_id');
    }

    public function requisition(): BelongsTo
    {
        return $this->belongsTo(Requisition::class, 'requisition_id');
    }

    public function interviews(): HasMany
    {
        return $this->hasMany(Interview::class, 'application_id')->orderBy('round_number');
    }

    public function offer(): HasOne
    {
        return $this->hasOne(Offer::class, 'application_id');
    }

    public function healthCheck(): HasOne
    {
        return $this->hasOne(HealthCheck::class, 'candidate_id', 'candidate_id')->latestOfMany('check_date');
    }
}

class Candidate extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'candidates';

    protected $fillable = [
        'candidate_code',
        'full_name',
        'email',
        'phone_number',
        'national_id',
        'gender',
        'education_level',
        'cv_file_url',
    ];

    public function applications(): HasMany
    {
        return $this->hasMany(Application::class, 'candidate_id');
    }

    public function healthChecks(): HasMany
    {
        return $this->hasMany(HealthCheck::class, 'candidate_id');
    }
}

class Requisition extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'requisitions';

    protected $fillable = [
        'requisition_code',
        'title',
        'org_unit_id',
        'position_id',
        'headcount',
        'job_description',
        'requirements',
        'budget_salary_from',
        'budget_salary_to',
        'deadline',
        'status',
        'created_by',
    ];

    protected $casts = [
        'deadline' => 'date',
    ];

    public function orgUnit(): BelongsTo
    {
        return $this->belongsTo(OrgUnit::class, 'org_unit_id');
    }

    public function position(): BelongsTo
    {
        return $this->belongsTo(Position::class, 'position_id');
    }

    public function creator(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'created_by');
    }

    public function applications(): HasMany
    {
        return $this->hasMany(Application::class, 'requisition_id');
    }
}

class Interview extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'interviews';

    protected $fillable = [
        'application_id',
        'round_number',
        'interview_date',
        'interviewer_id',
        'score',
        'feedback',
        'result',
    ];

    protected $casts = [
        'interview_date' => 'datetime',
        'score'          => 'float',
    ];

    public function application(): BelongsTo
    {
        return $this->belongsTo(Application::class, 'application_id');
    }

    public function interviewer(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'interviewer_id');
    }
}

class HealthCheck extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'health_checks';

    protected $fillable = [
        'candidate_id',
        'employee_id',
        'check_date',
        'medical_facility',
        'health_type',
        'health_class',
        'is_passed',
        'expiry_date',
        'certificate_url',
    ];

    protected $casts = [
        'check_date'  => 'date',
        'expiry_date' => 'date',
        'is_passed'   => 'boolean',
    ];
}

class Offer extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'offers';

    protected $fillable = [
        'application_id',
        'position_id',
        'offered_p1',
        'offered_p2',
        'probation_rate',
        'expected_start_date',
        'status',
        'approved_by',
    ];

    protected $casts = [
        'expected_start_date' => 'date',
        'offered_p1'          => 'float',
        'offered_p2'          => 'float',
        'probation_rate'      => 'float',
    ];

    public function application(): BelongsTo
    {
        return $this->belongsTo(Application::class, 'application_id');
    }

    public function position(): BelongsTo
    {
        return $this->belongsTo(Position::class, 'position_id');
    }
}
