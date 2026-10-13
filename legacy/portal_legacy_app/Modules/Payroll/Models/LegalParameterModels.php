<?php

namespace App\Modules\Payroll\Models;

use App\Modules\Employee\Models\Employee;
use Carbon\Carbon;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\SoftDeletes;

use Brick\Math\BigDecimal;

class LegalParameter extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'legal_parameters';

    protected $fillable = [
        'param_code',
        'param_name',
        'param_value',
        'unit',
        'description',
        'effective_from',
        'effective_to',
        'is_current',
    ];

    protected $casts = [
        'param_value'    => 'string',
        'effective_from' => 'date',
        'effective_to'   => 'date',
        'is_current'     => 'boolean',
    ];

    /**
     * Tra cứu giá trị tham số pháp lý theo ngày hiệu lực (Không hardcode) dưới dạng float
     */
    public static function getEffectiveValue(string $paramCode, Carbon $effectiveDate, float $default = 0.0): float
    {
        $dateStr = $effectiveDate->toDateString();
        $record = static::where('param_code', $paramCode)
            ->where('effective_from', '<=', $dateStr)
            ->where(function ($query) use ($dateStr) {
                $query->whereNull('effective_to')
                      ->orWhere('effective_to', '>=', $dateStr);
            })
            ->latest('effective_from')
            ->first();

        return $record ? (float)$record->param_value : $default;
    }

    /**
     * Tra cứu giá trị tham số pháp lý dưới dạng BigDecimal chính xác tuyệt đối (Không float)
     */
    public static function getEffectiveDecimal(string $paramCode, Carbon $effectiveDate, string $default = '0'): BigDecimal
    {
        $dateStr = $effectiveDate->toDateString();
        $record = static::where('param_code', $paramCode)
            ->where('effective_from', '<=', $dateStr)
            ->where(function ($query) use ($dateStr) {
                $query->whereNull('effective_to')
                      ->orWhere('effective_to', '>=', $dateStr);
            })
            ->latest('effective_from')
            ->first();

        return $record ? BigDecimal::of((string)$record->param_value) : BigDecimal::of($default);
    }
}

class TaxBracket extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'tax_brackets';

    protected $fillable = [
        'bracket_level',
        'min_income',
        'max_income',
        'tax_rate',
        'quick_deduction',
        'effective_from',
        'effective_to',
        'is_current',
    ];

    protected $casts = [
        'bracket_level'   => 'integer',
        'min_income'      => 'decimal:0',
        'max_income'      => 'decimal:0',
        'tax_rate'        => 'float',
        'quick_deduction' => 'decimal:0',
        'effective_from'  => 'date',
        'effective_to'    => 'date',
    ];

    /**
     * Lấy biểu thuế hiệu lực theo ngày
     */
    public static function getEffectiveBrackets(Carbon $effectiveDate)
    {
        $dateStr = $effectiveDate->toDateString();
        return static::where('effective_from', '<=', $dateStr)
            ->where(function ($query) use ($dateStr) {
                $query->whereNull('effective_to')
                      ->orWhere('effective_to', '>=', $dateStr);
            })
            ->orderBy('bracket_level')
            ->get();
    }
}

class OtRate extends Model
{
    use HasFactory;

    protected $table = 'ot_rates';

    protected $fillable = [
        'rate_code',
        'rate_name',
        'multiplier',
        'night_additional_multiplier',
        'effective_from',
        'effective_to',
        'is_current',
    ];

    protected $casts = [
        'multiplier'                  => 'string',
        'night_additional_multiplier' => 'string',
        'effective_from'              => 'date',
        'effective_to'                => 'date',
    ];

    public static function getRate(string $code, Carbon $effectiveDate): ?OtRate
    {
        $dateStr = $effectiveDate->toDateString();
        return static::where('rate_code', $code)
            ->where('effective_from', '<=', $dateStr)
            ->where(function ($query) use ($dateStr) {
                $query->whereNull('effective_to')
                      ->orWhere('effective_to', '>=', $dateStr);
            })
            ->latest('effective_from')
            ->first();
    }

    public function getMultiplierDecimal(): BigDecimal
    {
        return BigDecimal::of((string)$this->multiplier);
    }

    public function getNightAdditionalMultiplierDecimal(): BigDecimal
    {
        return BigDecimal::of((string)$this->night_additional_multiplier);
    }
}

class Dependent extends Model
{
    use HasFactory, SoftDeletes;

    protected $table = 'dependents';

    protected $fillable = [
        'employee_id',
        'dependent_name',
        'relationship',
        'tax_id',
        'effective_from',
        'effective_to',
        'is_active',
    ];

    protected $casts = [
        'effective_from' => 'date',
        'effective_to'   => 'date',
        'is_active'      => 'boolean',
    ];

    public function employee(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'employee_id');
    }
}
