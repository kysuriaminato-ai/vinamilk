<?php

namespace App\Models;

use App\Modules\Employee\Models\Employee;
use App\Modules\Organization\Models\OrgUnit;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Eloquent\SoftDeletes;
use Illuminate\Foundation\Auth\User as Authenticatable;
use Illuminate\Notifications\Notifiable;

class User extends Authenticatable
{
    use HasFactory, Notifiable, SoftDeletes;

    protected $table = 'users';

    protected $fillable = [
        'username',
        'password_hash',
        'employee_id',
        'role_id',
        'is_active',
        'two_factor_enabled',
        'last_login_at',
        'failed_login_attempts',
    ];

    protected $hidden = [
        'password_hash',
        'remember_token',
    ];

    protected $casts = [
        'is_active'             => 'boolean',
        'two_factor_enabled'    => 'boolean',
        'last_login_at'         => 'datetime',
        'failed_login_attempts' => 'integer',
    ];

    /**
     * Map password field to password_hash
     */
    public function getAuthPassword()
    {
        return $this->password_hash;
    }

    public function role(): BelongsTo
    {
        return $this->belongsTo(Role::class, 'role_id');
    }

    public function employee(): BelongsTo
    {
        return $this->belongsTo(Employee::class, 'employee_id');
    }

    public function scopes(): HasMany
    {
        return $this->hasMany(UserScope::class, 'user_id');
    }

    /**
     * Kiểm tra vai trò của người dùng
     */
    public function hasRole(string $roleCode): bool
    {
        return $this->role && $this->role->role_code === $roleCode;
    }

    /**
     * Kiểm tra quyền hạn cụ thể
     */
    public function hasPermission(string $permissionCode): bool
    {
        if ($this->hasRole('SUPER_ADMIN')) {
            return true;
        }

        if (!$this->role) {
            return false;
        }

        return $this->role->permissions()->where('permission_code', $permissionCode)->exists();
    }
}
