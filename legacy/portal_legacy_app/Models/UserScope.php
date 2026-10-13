<?php

namespace App\Models;

use App\Modules\Organization\Models\OrgUnit;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class UserScope extends Model
{
    use HasFactory;

    protected $table = 'user_scopes';

    protected $fillable = [
        'user_id',
        'org_unit_id',
        'include_sub_units',
    ];

    protected $casts = [
        'include_sub_units' => 'boolean',
    ];

    public function user(): BelongsTo
    {
        return $this->belongsTo(User::class, 'user_id');
    }

    public function orgUnit(): BelongsTo
    {
        return $this->belongsTo(OrgUnit::class, 'org_unit_id');
    }
}
