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
        // 1. Bảng kỳ đánh giá KPI (kpi_periods)
        Schema::create('kpi_periods', function (Blueprint $table) {
            $table->id();
            $table->string('period_code', 50)->unique()->comment('Mã kỳ KPI: KPI-2026-M10, KPI-2026-Q4');
            $table->string('period_name', 100)->comment('Tên kỳ đánh giá');
            $table->enum('period_type', ['MONTH', 'QUARTER', 'YEAR'])->default('MONTH');
            $table->date('start_date');
            $table->date('end_date');
            $table->enum('status', ['OPEN', 'EVALUATING', 'LOCKED', 'CLOSED'])->default('OPEN');
            $table->timestamps();
            $table->softDeletes();

            $table->index(['start_date', 'end_date']);
            $table->index('status');
        });

        // 2. Bảng cấu hình tính điểm KPI động (kpi_scoring_configs)
        Schema::create('kpi_scoring_configs', function (Blueprint $table) {
            $table->id();
            $table->string('config_key', 50)->unique();
            $table->decimal('config_value', 8, 2);
            $table->string('description', 255)->nullable();
            $table->timestamps();
        });

        // Seed cấu hình điểm số chuẩn Vinamilk
        DB::table('kpi_scoring_configs')->insert([
            ['config_key' => 'EARLY_BONUS_PER_DAY', 'config_value' => 2.00, 'description' => 'Điểm thưởng hoàn thành sớm (+2 điểm/ngày)', 'created_at' => now(), 'updated_at' => now()],
            ['config_key' => 'MAX_EARLY_BONUS', 'config_value' => 10.00, 'description' => 'Điểm thưởng sớm tối đa (+10 điểm)', 'created_at' => now(), 'updated_at' => now()],
            ['config_key' => 'LATE_PENALTY_PER_DAY', 'config_value' => 5.00, 'description' => 'Điểm phạt hoàn thành trễ (-5 điểm/ngày)', 'created_at' => now(), 'updated_at' => now()],
            ['config_key' => 'BASELINE_SCORE', 'config_value' => 100.00, 'description' => 'Điểm chuẩn ban đầu giao việc (100 điểm)', 'created_at' => now(), 'updated_at' => now()],
            ['config_key' => 'MIN_TASK_SCORE', 'config_value' => 0.00, 'description' => 'Điểm sàn nhiệm vụ (0 điểm)', 'created_at' => now(), 'updated_at' => now()],
            ['config_key' => 'MAX_TASK_SCORE', 'config_value' => 120.00, 'description' => 'Điểm trần nhiệm vụ (120 điểm)', 'created_at' => now(), 'updated_at' => now()],
            ['config_key' => 'REASON_REQUIRED_THRESHOLD', 'config_value' => 20.00, 'description' => 'Ngưỡng điều chỉnh lãnh đạo bắt buộc nhập lý do (+/- 20 điểm)', 'created_at' => now(), 'updated_at' => now()],
        ]);

        // 3. Bảng nhiệm vụ KPI cá nhân (kpi_assignments - kế thừa repo)
        Schema::create('kpi_assignments', function (Blueprint $table) {
            $table->id();
            $table->unsignedBigInteger('kpi_period_id')->index();
            $table->unsignedBigInteger('employee_id')->index()->comment('Nhân viên thực hiện');
            $table->unsignedBigInteger('evaluator_id')->index()->comment('Quản lý giao việc và thẩm định');
            $table->string('title', 255)->comment('Tiêu đề nhiệm vụ');
            $table->text('description')->nullable()->comment('Mô tả chi tiết nhiệm vụ');
            $table->string('target_metric', 255)->comment('Chỉ tiêu đo lường kết quả (Target metric)');
            $table->decimal('weight_percentage', 5, 2)->comment('Trọng số % nhiệm vụ (Tổng kỳ = 100%)');
            $table->date('due_date')->comment('Hạn hoàn thành');
            $table->decimal('baseline_score', 5, 2)->default(100.00)->comment('Điểm chuẩn giao việc');
            $table->decimal('time_score', 5, 2)->default(0.00)->comment('Điểm thời gian theo SLA (+ thưởng / - phạt)');
            $table->decimal('manager_adjustment', 5, 2)->default(0.00)->comment('Điểm điều chỉnh của lãnh đạo');
            $table->text('adjustment_reason')->nullable()->comment('Lý do điều chỉnh (bắt buộc nếu vượt +/- 20đ)');
            $table->decimal('final_score', 5, 2)->nullable()->comment('Điểm chốt sau duyệt (clamp 0 - 120)');
            $table->text('actual_result')->nullable()->comment('Báo cáo kết quả của nhân viên');
            $table->date('completed_date')->nullable()->comment('Ngày hoàn thành thực tế');
            $table->dateTime('submission_date')->nullable()->comment('Ngày giờ nộp kết quả');
            $table->dateTime('evaluation_date')->nullable()->comment('Ngày giờ thẩm định');
            $table->text('rejection_reason')->nullable()->comment('Lý do trả lại yêu cầu làm lại');
            $table->text('feedback')->nullable()->comment('Nhận xét của lãnh đạo');
            $table->enum('status', [
                'Assigned',
                'InProgress',
                'PendingApproval',
                'Approved',
                'Rejected',
                'Closed',
                'NotCompleted'
            ])->default('Assigned')->index();
            $table->timestamps();
            $table->softDeletes();

            $table->foreign('kpi_period_id')->references('id')->on('kpi_periods')->cascadeOnDelete();
            $table->foreign('employee_id')->references('id')->on('employees')->cascadeOnDelete();
            $table->foreign('evaluator_id')->references('id')->on('employees')->cascadeOnDelete();
        });

        // 4. Bảng tổng hợp điểm KPI kỳ & Xuất sang Lương P3 (kpi_employee_summaries)
        Schema::create('kpi_employee_summaries', function (Blueprint $table) {
            $table->id();
            $table->unsignedBigInteger('kpi_period_id')->index();
            $table->unsignedBigInteger('employee_id')->index();
            $table->decimal('total_weight_percentage', 5, 2)->default(0.00);
            $table->decimal('kpi_score', 5, 2)->default(0.00)->comment('Tổng điểm KPI kỳ = Sum(Điểm nhiệm vụ * Trọng số %)');
            $table->string('kpi_grade', 10)->default('B')->comment('Xếp loại: A+, A, B, C, D');
            $table->decimal('p3_multiplier', 4, 2)->default(1.00)->comment('Hệ số lương hiệu suất P3');
            $table->enum('status', ['DRAFT', 'APPROVED', 'EXPORTED_TO_PAYROLL'])->default('DRAFT')->index();
            $table->dateTime('exported_at')->nullable();
            $table->unsignedBigInteger('exported_by')->nullable();
            $table->timestamps();
            $table->softDeletes();

            $table->unique(['kpi_period_id', 'employee_id']);
            $table->foreign('kpi_period_id')->references('id')->on('kpi_periods')->cascadeOnDelete();
            $table->foreign('employee_id')->references('id')->on('employees')->cascadeOnDelete();
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('kpi_employee_summaries');
        Schema::dropIfExists('kpi_assignments');
        Schema::dropIfExists('kpi_scoring_configs');
        Schema::dropIfExists('kpi_periods');
    }
};
