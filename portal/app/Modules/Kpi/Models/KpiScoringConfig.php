<?php

namespace App\Modules\Kpi\Models;

use Illuminate\Database\Eloquent\Model;

class KpiScoringConfig extends Model
{
    protected $table = 'kpi_scoring_configs';

    protected $fillable = [
        'config_key',
        'config_value',
        'description',
    ];

    protected $casts = [
        'config_value' => 'float',
    ];

    /**
     * Lấy giá trị cấu hình theo key với giá trị fallback mặc định
     */
    public static function getValue(string $key, float $default): float
    {
        $cfg = static::where('config_key', $key)->first();
        return $cfg ? (float)$cfg->config_value : $default;
    }
}
