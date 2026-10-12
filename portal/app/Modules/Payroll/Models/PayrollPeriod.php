<?php

namespace App\Modules\Payroll\Models;

use Carbon\Carbon;
use DomainException;
use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Eloquent\SoftDeletes;

class PayrollPeriod extends Model
{
    use HasFactory, SoftDeletes;

    // Trạng thái vòng đời kỳ lương theo quy định:
    // Mở → Đang tính → Chờ duyệt → Đã chốt → Đã chi. Chốt rồi thì khóa.
    public const STATUS_OPEN             = 'OPEN';
    public const STATUS_CALCULATING      = 'CALCULATING';
    public const STATUS_PENDING_APPROVAL = 'PENDING_APPROVAL';
    public const STATUS_LOCKED           = 'LOCKED';
    public const STATUS_PAID             = 'PAID';

    protected $table = 'payroll_periods';

    protected $fillable = [
        'period_code',
        'month',
        'year',
        'standard_working_days',
        'status',
        'calculated_at',
    ];

    protected $casts = [
        'month'                 => 'integer',
        'year'                  => 'integer',
        'standard_working_days' => 'float',
        'calculated_at'         => 'datetime',
    ];

    public function payslips(): HasMany
    {
        return $this->hasMany(Payslip::class, 'payroll_period_id');
    }

    public function retroactiveAdjustments(): HasMany
    {
        return $this->hasMany(RetroactiveSalaryAdjustment::class, 'payroll_period_id');
    }

    /**
     * Ngày cuối cùng của kỳ lương (để tra cứu tỷ lệ tham số pháp lý hiệu lực)
     */
    public function getEndDate(): Carbon
    {
        return Carbon::createFromDate($this->year, $this->month, 1)->endOfMonth();
    }

    /**
     * Ngày bắt đầu kỳ lương
     */
    public function getStartDate(): Carbon
    {
        return Carbon::createFromDate($this->year, $this->month, 1)->startOfMonth();
    }

    public function isOpen(): bool
    {
        return in_array($this->status, [self::STATUS_OPEN, 'DRAFT'], true);
    }

    public function isCalculating(): bool
    {
        return $this->status === self::STATUS_CALCULATING;
    }

    public function isPendingApproval(): bool
    {
        return in_array($this->status, [self::STATUS_PENDING_APPROVAL, 'CALCULATED', 'HR_APPROVED', 'BOD_APPROVED'], true);
    }

    public function isLocked(): bool
    {
        return $this->status === self::STATUS_LOCKED;
    }

    public function isPaid(): bool
    {
        return $this->status === self::STATUS_PAID;
    }

    /**
     * Kiểm tra kỳ lương có cho phép chỉnh sửa/tính toán lại hay không.
     * Chốt rồi thì khóa; Đã chi tuyệt đối không được sửa.
     */
    public function canRecalculate(): bool
    {
        return !$this->isLocked() && !$this->isPaid();
    }

    /**
     * Chuyển trạng thái sang ĐANG TÍNH
     */
    public function transitionToCalculating(): void
    {
        if (!$this->canRecalculate()) {
            throw new DomainException("Kỳ lương {$this->period_code} đã ở trạng thái [{$this->status}], đã khóa không thể tính toán lại.");
        }
        $this->update(['status' => self::STATUS_CALCULATING]);
    }

    /**
     * Chuyển trạng thái sang CHỜ DUYỆT sau khi tính toán xong
     */
    public function transitionToPendingApproval(): void
    {
        if ($this->isLocked() || $this->isPaid()) {
            throw new DomainException("Kỳ lương {$this->period_code} đã bị khóa hoặc đã chi.");
        }
        $this->update([
            'status' => self::STATUS_PENDING_APPROVAL,
            'calculated_at' => now(),
        ]);
    }

    /**
     * Chuyển trạng thái sang ĐÃ CHỐT (Khóa bảng lương)
     */
    public function lock(): void
    {
        if ($this->isPaid()) {
            throw new DomainException("Kỳ lương {$this->period_code} đã chi, không thể chốt lại.");
        }
        $this->update(['status' => self::STATUS_LOCKED]);
    }

    /**
     * Chuyển trạng thái sang ĐÃ CHI
     */
    public function markAsPaid(): void
    {
        if (!$this->isLocked()) {
            throw new DomainException("Kỳ lương {$this->period_code} phải được chốt (LOCKED) trước khi chuyển sang Đã chi (PAID).");
        }
        $this->update(['status' => self::STATUS_PAID]);
    }
}
