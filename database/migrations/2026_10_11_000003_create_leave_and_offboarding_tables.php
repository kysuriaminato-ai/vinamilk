<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    /**
     * Run the migrations.
     */
    public function up(): void
    {
        // 1. Danh mục loại nghỉ phép
        Schema::create('leave_types', function (Blueprint $table) {
            $table->id();
            $table->string('type_code', 50)->unique()->comment('ANNUAL, PAID_PERSONAL, SICK_SI, MATERNITY, UNPAID, UNAUTHORIZED');
            $table->string('type_name', 100)->comment('Tên loại nghỉ');
            $table->boolean('is_paid')->default(true)->comment('Có hưởng lương hay không');
            $table->boolean('deduct_annual_balance')->default(false)->comment('Trừ vào quỹ phép năm');
            $table->boolean('social_insurance_coverage')->default(false)->comment('Hưởng chế độ BHXH');
            $table->boolean('requires_attachment')->default(false)->comment('Bắt buộc nộp chứng từ y tế/giấy tờ');
            $table->decimal('max_days_allowed', 4, 1)->nullable()->comment('Số ngày tối đa cho phép nghỉ');
            $table->timestamps();
            $table->softDeletes();
        });

        // Seed danh mục loại phép theo chuẩn BLLĐ 2019
        DB::table('leave_types')->insert([
            [
                'type_code'                 => 'ANNUAL',
                'type_name'                 => 'Nghỉ phép năm',
                'is_paid'                   => true,
                'deduct_annual_balance'     => true,
                'social_insurance_coverage' => false,
                'requires_attachment'       => false,
                'max_days_allowed'          => null,
                'created_at'                => now(),
                'updated_at'                => now(),
            ],
            [
                'type_code'                 => 'PAID_PERSONAL',
                'type_name'                 => 'Việc riêng có hưởng lương (Kết hôn, tang chế)',
                'is_paid'                   => true,
                'deduct_annual_balance'     => false,
                'social_insurance_coverage' => false,
                'requires_attachment'       => true,
                'max_days_allowed'          => 3.0,
                'created_at'                => now(),
                'updated_at'                => now(),
            ],
            [
                'type_code'                 => 'SICK_SI',
                'type_name'                 => 'Nghỉ ốm đau hưởng trợ cấp BHXH',
                'is_paid'                   => false,
                'deduct_annual_balance'     => false,
                'social_insurance_coverage' => true,
                'requires_attachment'       => true,
                'max_days_allowed'          => 30.0,
                'created_at'                => now(),
                'updated_at'                => now(),
            ],
            [
                'type_code'                 => 'MATERNITY',
                'type_name'                 => 'Nghỉ thai sản hưởng trợ cấp BHXH',
                'is_paid'                   => false,
                'deduct_annual_balance'     => false,
                'social_insurance_coverage' => true,
                'requires_attachment'       => true,
                'max_days_allowed'          => 180.0,
                'created_at'                => now(),
                'updated_at'                => now(),
            ],
            [
                'type_code'                 => 'UNPAID',
                'type_name'                 => 'Nghỉ việc riêng không hưởng lương',
                'is_paid'                   => false,
                'deduct_annual_balance'     => false,
                'social_insurance_coverage' => false,
                'requires_attachment'       => false,
                'max_days_allowed'          => null,
                'created_at'                => now(),
                'updated_at'                => now(),
            ],
            [
                'type_code'                 => 'UNAUTHORIZED',
                'type_name'                 => 'Nghỉ không phép (AWOL - Trừ công, trừ chuyên cần)',
                'is_paid'                   => false,
                'deduct_annual_balance'     => false,
                'social_insurance_coverage' => false,
                'requires_attachment'       => false,
                'max_days_allowed'          => 0.0,
                'created_at'                => now(),
                'updated_at'                => now(),
            ],
        ]);

        // 2. Quản lý quỹ phép năm và thâm niên
        Schema::create('leave_balances', function (Blueprint $table) {
            $table->id();
            $table->unsignedBigInteger('employee_id')->index();
            $table->unsignedSmallInteger('year')->index();
            $table->decimal('annual_leave_quota', 4, 1)->default(12.0)->comment('Phép năm tiêu chuẩn 12 ngày');
            $table->decimal('seniority_leave_days', 4, 1)->default(0.0)->comment('Thâm niên: mỗi 5 năm + 1 ngày');
            $table->decimal('transferred_days', 4, 1)->default(0.0)->comment('Phép tồn năm trước chuyển sang');
            $table->decimal('used_days', 4, 1)->default(0.0)->comment('Số ngày đã nghỉ');
            $table->timestamps();
            $table->softDeletes();

            $table->unique(['employee_id', 'year']);
            $table->foreign('employee_id')->references('id')->on('employees')->cascadeOnDelete();
        });

        // 3. Đơn xin nghỉ phép
        Schema::create('leave_requests', function (Blueprint $table) {
            $table->id();
            $table->string('request_code', 50)->unique();
            $table->unsignedBigInteger('employee_id')->index();
            $table->unsignedBigInteger('leave_type_id')->index();
            $table->date('start_date');
            $table->date('end_date');
            $table->decimal('leave_days', 4, 1);
            $table->text('reason');
            $table->string('attachment_url', 500)->nullable();
            $table->enum('status', ['PENDING', 'APPROVED', 'REJECTED', 'CANCELLED'])->default('PENDING')->index();
            $table->unsignedBigInteger('approved_by')->nullable()->index();
            $table->dateTime('approved_at')->nullable();
            $table->string('rejection_reason', 500)->nullable();
            $table->timestamps();
            $table->softDeletes();

            $table->foreign('employee_id')->references('id')->on('employees')->cascadeOnDelete();
            $table->foreign('leave_type_id')->references('id')->on('leave_types')->cascadeOnUpdate();
            $table->foreign('approved_by')->references('id')->on('employees')->nullOnDelete();
        });

        // 4. Đơn xin thôi việc (Resignation / Offboarding)
        Schema::create('resignation_requests', function (Blueprint $table) {
            $table->id();
            $table->string('resignation_code', 50)->unique();
            $table->unsignedBigInteger('employee_id')->index();
            $table->unsignedBigInteger('contract_id')->nullable()->index();
            $table->date('submitted_date');
            $table->unsignedSmallInteger('required_notice_days')->comment('Số ngày báo trước theo luật: 45, 30 hoặc 3');
            $table->date('proposed_last_date')->comment('Ngày làm việc cuối nhân viên đề xuất');
            $table->date('approved_last_date')->nullable()->comment('Ngày làm việc cuối BGĐ chính thức duyệt');
            $table->text('reason');
            $table->enum('status', [
                'SUBMITTED',
                'MANAGER_APPROVED',
                'HR_APPROVED',
                'BOD_APPROVED',
                'REJECTED',
                'CANCELLED'
            ])->default('SUBMITTED')->index();
            $table->unsignedBigInteger('manager_approved_by')->nullable();
            $table->dateTime('manager_approved_at')->nullable();
            $table->unsignedBigInteger('hr_approved_by')->nullable();
            $table->dateTime('hr_approved_at')->nullable();
            $table->unsignedBigInteger('bod_approved_by')->nullable();
            $table->dateTime('bod_approved_at')->nullable();
            $table->dateTime('handover_completed_at')->nullable();
            $table->dateTime('settlement_completed_at')->nullable();
            $table->text('notes')->nullable();
            $table->timestamps();
            $table->softDeletes();

            $table->foreign('employee_id')->references('id')->on('employees')->cascadeOnDelete();
        });

        // 5. Checklist bàn giao tài sản (Asset Handover Checklist)
        Schema::create('asset_handover_items', function (Blueprint $table) {
            $table->id();
            $table->unsignedBigInteger('resignation_request_id')->index();
            $table->string('item_name', 255);
            $table->string('item_category', 50)->comment('IT_LAPTOP, ACCESS_CARD, PPE_UNIFORM, TOOLS_EQUIPMENT, FINANCE_SETTLEMENT, OTHER');
            $table->string('assigned_department', 50)->comment('IT, ADMIN, WORKSHOP, FINANCE');
            $table->boolean('is_returned')->default(false)->index();
            $table->dateTime('returned_at')->nullable();
            $table->unsignedBigInteger('receiver_employee_id')->nullable()->index();
            $table->text('notes')->nullable();
            $table->timestamps();

            $table->foreign('resignation_request_id')->references('id')->on('resignation_requests')->cascadeOnDelete();
            $table->foreign('receiver_employee_id')->references('id')->on('employees')->nullOnDelete();
        });

        // 6. Quyết toán kỳ lương cuối (Final Settlement)
        Schema::create('final_settlements', function (Blueprint $table) {
            $table->id();
            $table->string('settlement_code', 50)->unique();
            $table->unsignedBigInteger('resignation_request_id')->unique()->index();
            $table->unsignedBigInteger('employee_id')->index();
            $table->date('last_working_date');
            $table->decimal('actual_working_days', 4, 1)->default(0.0);
            $table->decimal('actual_work_salary', 15, 0)->default(0);
            $table->decimal('remaining_leave_days', 4, 1)->default(0.0);
            $table->decimal('daily_leave_rate', 15, 0)->default(0);
            $table->decimal('unused_leave_allowance', 15, 0)->default(0);
            $table->decimal('severance_qualifying_years', 4, 1)->default(0.0);
            $table->decimal('severance_monthly_avg_salary', 15, 0)->default(0);
            $table->decimal('severance_allowance', 15, 0)->default(0);
            $table->decimal('advance_deduction', 15, 0)->default(0);
            $table->decimal('other_deductions', 15, 0)->default(0);
            $table->decimal('net_settlement_amount', 15, 0)->default(0);
            $table->date('payment_deadline_date')->comment('Hạn thanh toán 14 ngày làm việc theo Điều 48 BLLĐ');
            $table->enum('payment_status', ['PENDING', 'APPROVED', 'PAID', 'OVERDUE'])->default('PENDING')->index();
            $table->dateTime('paid_at')->nullable();
            $table->text('notes')->nullable();
            $table->timestamps();
            $table->softDeletes();

            $table->foreign('resignation_request_id')->references('id')->on('resignation_requests')->cascadeOnDelete();
            $table->foreign('employee_id')->references('id')->on('employees')->cascadeOnDelete();
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('final_settlements');
        Schema::dropIfExists('asset_handover_items');
        Schema::dropIfExists('resignation_requests');
        Schema::dropIfExists('leave_requests');
        Schema::dropIfExists('leave_balances');
        Schema::dropIfExists('leave_types');
    }
};
