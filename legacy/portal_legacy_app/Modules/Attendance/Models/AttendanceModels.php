<?php

namespace App\Modules\Attendance\Models;

use App\Modules\Employee\Models\Employee;
use App\Modules\Organization\Models\OrgUnit;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Eloquent\SoftDeletes;

class Shift extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'shifts';

    protected $fillable = [
        'shift_code',
        'shift_name',
        'start_time',
        'end_time',
        'break_minutes',
        'grace_period_late_minutes',
        'standard_hours',
        'is_night_shift',
        'is_active',
    ];

    protected $casts = [
        'is_night_shift'            => 'boolean',
        'is_active'                 => 'boolean',
        'standard_hours'            => 'float',
        'break_minutes'             => 'integer',
        'grace_period_late_minutes' => 'integer',
    ];

    public function schedules(): HasMany
    {
        return $this->hasMany(ShiftSchedule::class, 'shift_id');
    }
}

class ShiftSchedule extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'shift_schedules';

    protected $fillable = [
        'employee_id',
        'shift_id',
        'work_date',
        'note',
    ];

    protected $casts = [
        'work_date' => 'date',
    ];

    public function employee(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'employee_id');
    }

    public function shift(): BelongsTo
    {
        return $this->belongsTo(Shift::class, 'shift_id');
    }
}

class AttendanceLog extends Model
{
    use HasFactory;

    protected $table = 'attendance_logs';

    public $timestamps = false;

    protected $fillable = [
        'employee_id',
        'device_code',
        'check_time',
        'check_type',
        'source', // FINGERPRINT, FACE_ID, GPS_MOBILE, EXCEL
        'latitude',
        'longitude',
        'raw_data',
        'created_at',
    ];

    protected $casts = [
        'check_time' => 'datetime',
        'created_at' => 'datetime',
        'raw_data'   => 'array',
        'latitude'   => 'float',
        'longitude'  => 'float',
    ];

    public function employee(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'employee_id');
    }
}

class AttendanceDay extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'attendance_days';

    protected $fillable = [
        'employee_id',
        'work_date',
        'shift_id',
        'first_in',
        'last_out',
        'late_minutes',
        'early_minutes',
        'actual_hours',
        'standard_hours',
        'ot_normal_hours',
        'ot_weekend_hours',
        'ot_holiday_hours',
        'ot_night_hours',
        'night_work_hours',
        'status',
        'is_locked',
        'health_warning',
    ];

    protected $casts = [
        'work_date'         => 'date',
        'first_in'          => 'datetime',
        'last_out'          => 'datetime',
        'actual_hours'      => 'float',
        'standard_hours'    => 'float',
        'ot_normal_hours'   => 'float',
        'ot_weekend_hours'  => 'float',
        'ot_holiday_hours'  => 'float',
        'ot_night_hours'    => 'float',
        'night_work_hours'  => 'float',
        'late_minutes'      => 'integer',
        'early_minutes'     => 'integer',
        'is_locked'         => 'boolean',
        'health_warning'    => 'boolean',
    ];

    public function employee(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'employee_id');
    }

    public function shift(): BelongsTo
    {
        return $this->belongsTo(Shift::class, 'shift_id');
    }
}
