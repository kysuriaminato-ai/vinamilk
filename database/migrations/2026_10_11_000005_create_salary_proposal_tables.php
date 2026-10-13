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
        // 1. Khung lương chức danh (salary_bands)
        if (!Schema::hasTable('salary_bands')) {
            Schema::create('salary_bands', function (Blueprint $table) {
                $table->id();
                $table->string('band_code', 50)->unique();
                $table->string('band_name', 100);
                $table->unsignedInteger('job_level')->default(1)->comment('Cấp bậc: 1 Nhân viên, 2 Chuyên viên, 3 Trưởng phòng, 4 Giám đốc');
                $table->decimal('min_salary', 15, 0)->comment('Mức sàn khung lương');
                $table->decimal('midpoint_salary', 15, 0)->comment('Mức trung vị (Midpoint) dùng tính Compa-Ratio');
                $table->decimal('max_salary', 15, 0)->comment('Mức trần khung lương');
                $table->timestamps();
                $table->softDeletes();
            });

            // Seed dữ liệu mẫu khung lương Vinamilk
            DB::table('salary_bands')->insert([
                ['band_code' => 'BAND-OP-1', 'band_name' => 'Công nhân Vận hành / Sản xuất', 'job_level' => 1, 'min_salary' => 8000000, 'midpoint_salary' => 11000000, 'max_salary' => 15000000, 'created_at' => now(), 'updated_at' => now()],
                ['band_code' => 'BAND-ENG-2', 'band_name' => 'Kỹ sư / Chuyên viên Kỹ thuật', 'job_level' => 2, 'min_salary' => 15000000, 'midpoint_salary' => 22000000, 'max_salary' => 30000000, 'created_at' => now(), 'updated_at' => now()],
                ['band_code' => 'BAND-MGR-3', 'band_name' => 'Trưởng phòng / Quản đốc', 'job_level' => 3, 'min_salary' => 28000000, 'midpoint_salary' => 40000000, 'max_salary' => 55000000, 'created_at' => now(), 'updated_at' => now()],
                ['band_code' => 'BAND-DIR-4', 'band_name' => 'Giám đốc Khối / Giám đốc Nhà máy', 'job_level' => 4, 'min_salary' => 50000000, 'midpoint_salary' => 75000000, 'max_salary' => 110000000, 'created_at' => now(), 'updated_at' => now()],
            ]);
        }

        // 2. Lương tối thiểu vùng (regional_min_wages)
        if (!Schema::hasTable('regional_min_wages')) {
            Schema::create('regional_min_wages', function (Blueprint $table) {
                $table->id();
                $table->string('region_zone', 20)->unique()->comment('REGION_1, REGION_2, REGION_3, REGION_4');
                $table->decimal('min_wage_amount', 15, 0);
                $table->date('effective_from');
                $table->date('effective_to')->nullable();
                $table->string('legal_reference', 255)->comment('Nghị định quy định');
                $table->timestamps();
            });

            // Seed theo Nghị định 74/2024/NĐ-CP
            DB::table('regional_min_wages')->insert([
                ['region_zone' => 'REGION_1', 'min_wage_amount' => 4960000, 'effective_from' => '2024-07-01', 'effective_to' => null, 'legal_reference' => 'Nghị định 74/2024/NĐ-CP (TP.HCM, Bình Dương, Hà Nội)', 'created_at' => now(), 'updated_at' => now()],
                ['region_zone' => 'REGION_2', 'min_wage_amount' => 4410000, 'effective_from' => '2024-07-01', 'effective_to' => null, 'legal_reference' => 'Nghị định 74/2024/NĐ-CP', 'created_at' => now(), 'updated_at' => now()],
                ['region_zone' => 'REGION_3', 'min_wage_amount' => 3860000, 'effective_from' => '2024-07-01', 'effective_to' => null, 'legal_reference' => 'Nghị định 74/2024/NĐ-CP', 'created_at' => now(), 'updated_at' => now()],
                ['region_zone' => 'REGION_4', 'min_wage_amount' => 3450000, 'effective_from' => '2024-07-01', 'effective_to' => null, 'legal_reference' => 'Nghị định 74/2024/NĐ-CP', 'created_at' => now(), 'updated_at' => now()],
            ]);
        }

        // 3. Ngân sách tăng lương đơn vị (unit_salary_budgets)
        if (!Schema::hasTable('unit_salary_budgets')) {
            Schema::create('unit_salary_budgets', function (Blueprint $table) {
                $table->id();
                $table->unsignedBigInteger('org_unit_id')->index();
                $table->unsignedSmallInteger('year')->index();
                $table->decimal('annual_budget', 15, 0)->comment('Tổng ngân sách tăng lương được duyệt trong năm');
                $table->decimal('allocated_amount', 15, 0)->default(0);
                $table->decimal('used_amount', 15, 0)->default(0);
                $table->timestamps();

                $table->unique(['org_unit_id', 'year']);
                $table->foreign('org_unit_id')->references('id')->on('org_units')->cascadeOnDelete();
            });
        }

        // 4. Biểu lương nhân viên SCD Type 2 (employee_salaries)
        if (!Schema::hasTable('employee_salaries')) {
            Schema::create('employee_salaries', function (Blueprint $table) {
                $table->id();
                $table->unsignedBigInteger('employee_id')->index();
                $table->decimal('p1_position_salary', 15, 0)->default(0);
                $table->decimal('p2_competency_salary', 15, 0)->default(0);
                $table->decimal('insurance_salary', 15, 0)->default(0);
                $table->decimal('toxic_allowance', 15, 0)->default(0);
                $table->decimal('farm_field_allowance', 15, 0)->default(0);
                $table->decimal('responsibility_allowance', 15, 0)->default(0);
                $table->decimal('meal_allowance', 15, 0)->default(730000);
                $table->string('decision_number', 100)->nullable();
                $table->date('effective_from');
                $table->date('effective_to')->nullable();
                $table->boolean('is_current')->default(true)->index();
                $table->timestamps();
                $table->softDeletes();

                $table->foreign('employee_id')->references('id')->on('employees')->cascadeOnDelete();
            });
        }

        // 5. Bảng kỳ lương (payroll_periods)
        if (!Schema::hasTable('payroll_periods')) {
            Schema::create('payroll_periods', function (Blueprint $table) {
                $table->id();
                $table->string('period_code', 50)->unique();
                $table->unsignedTinyInteger('month');
                $table->unsignedSmallInteger('year');
                $table->decimal('standard_working_days', 4, 1)->default(22.0);
                $table->string('status', 30)->default('OPEN')->index()->comment('OPEN, CALCULATING, PENDING_APPROVAL, LOCKED, PAID');
                $table->dateTime('calculated_at')->nullable();
                $table->timestamps();
                $table->softDeletes();

                $table->unique(['month', 'year']);
            });
        }

        // 6. Bảng quy tắc duyệt động (approval_rules)
        if (!Schema::hasTable('approval_rules')) {
            Schema::create('approval_rules', function (Blueprint $table) {
                $table->id();
                $table->string('rule_code', 50);
                $table->string('rule_name', 100);
                $table->enum('workflow_type', ['DEFAULT', 'OUT_OF_BAND_OR_EXCEPTION', 'SENIOR_MANAGEMENT'])->index();
                $table->unsignedTinyInteger('step_order');
                $table->string('approver_role', 50)->comment('DIVISION_DIRECTOR, CB_COMMITTEE, HR_DIRECTOR, CEO, REMUNERATION_COMMITTEE');
                $table->string('description', 255)->nullable();
                $table->timestamps();
            });

            // Seed luồng duyệt động:
            DB::table('approval_rules')->insert([
                // Luồng mặc định (2 cấp): Giám đốc Khối -> Giám đốc Nhân sự
                ['rule_code' => 'DEF_1', 'rule_name' => 'Duyệt Giám đốc Khối', 'workflow_type' => 'DEFAULT', 'step_order' => 1, 'approver_role' => 'DIVISION_DIRECTOR', 'description' => 'Xác nhận của Giám đốc khối/đơn vị', 'created_at' => now(), 'updated_at' => now()],
                ['rule_code' => 'DEF_2', 'rule_name' => 'Duyệt Giám đốc Nhân sự', 'workflow_type' => 'DEFAULT', 'step_order' => 2, 'approver_role' => 'HR_DIRECTOR', 'description' => 'Phê duyệt của Giám đốc Nhân sự Tập đoàn', 'created_at' => now(), 'updated_at' => now()],

                // Luồng Vượt khung / Đặc cách (4 cấp): Khối -> Ban C&B thẩm định -> HR Director -> Tổng Giám đốc
                ['rule_code' => 'EXC_1', 'rule_name' => 'Đề xuất Giám đốc Khối', 'workflow_type' => 'OUT_OF_BAND_OR_EXCEPTION', 'step_order' => 1, 'approver_role' => 'DIVISION_DIRECTOR', 'description' => 'Giám đốc khối đề xuất', 'created_at' => now(), 'updated_at' => now()],
                ['rule_code' => 'EXC_2', 'rule_name' => 'Thẩm định Ban C&B', 'workflow_type' => 'OUT_OF_BAND_OR_EXCEPTION', 'step_order' => 2, 'approver_role' => 'CB_COMMITTEE', 'description' => 'Ban Đãi ngộ thẩm định thị trường & ngân sách', 'created_at' => now(), 'updated_at' => now()],
                ['rule_code' => 'EXC_3', 'rule_name' => 'Ý kiến Giám đốc Nhân sự', 'workflow_type' => 'OUT_OF_BAND_OR_EXCEPTION', 'step_order' => 3, 'approver_role' => 'HR_DIRECTOR', 'description' => 'Giám đốc Nhân sự xem xét', 'created_at' => now(), 'updated_at' => now()],
                ['rule_code' => 'EXC_4', 'rule_name' => 'Phê duyệt Tổng Giám đốc', 'workflow_type' => 'OUT_OF_BAND_OR_EXCEPTION', 'step_order' => 4, 'approver_role' => 'CEO', 'description' => 'Tổng Giám đốc Vinamilk ký duyệt quyết định', 'created_at' => now(), 'updated_at' => now()],

                // Luồng Quản lý cấp cao (5 cấp): thêm Ủy ban Lương thưởng HĐQT
                ['rule_code' => 'SNR_1', 'rule_name' => 'Đề xuất Giám đốc Khối', 'workflow_type' => 'SENIOR_MANAGEMENT', 'step_order' => 1, 'approver_role' => 'DIVISION_DIRECTOR', 'description' => 'Giám đốc khối', 'created_at' => now(), 'updated_at' => now()],
                ['rule_code' => 'SNR_2', 'rule_name' => 'Thẩm định Ban C&B', 'workflow_type' => 'SENIOR_MANAGEMENT', 'step_order' => 2, 'approver_role' => 'CB_COMMITTEE', 'description' => 'Ban C&B', 'created_at' => now(), 'updated_at' => now()],
                ['rule_code' => 'SNR_3', 'rule_name' => 'Ý kiến Giám đốc Nhân sự', 'workflow_type' => 'SENIOR_MANAGEMENT', 'step_order' => 3, 'approver_role' => 'HR_DIRECTOR', 'description' => 'Giám đốc Nhân sự', 'created_at' => now(), 'updated_at' => now()],
                ['rule_code' => 'SNR_4', 'rule_name' => 'Tổng Giám đốc thông qua', 'workflow_type' => 'SENIOR_MANAGEMENT', 'step_order' => 4, 'approver_role' => 'CEO', 'description' => 'CEO thông qua', 'created_at' => now(), 'updated_at' => now()],
                ['rule_code' => 'SNR_5', 'rule_name' => 'Ủy ban Lương thưởng HĐQT', 'workflow_type' => 'SENIOR_MANAGEMENT', 'step_order' => 5, 'approver_role' => 'REMUNERATION_COMMITTEE', 'description' => 'Ủy ban Nhân sự & Lương thưởng HĐQT phê duyệt', 'created_at' => now(), 'updated_at' => now()],
            ]);
        }

        // 7. Bảng tờ trình đề xuất tăng lương (salary_proposals)
        if (!Schema::hasTable('salary_proposals')) {
            Schema::create('salary_proposals', function (Blueprint $table) {
                $table->id();
                $table->string('proposal_code', 50)->unique();
                $table->unsignedBigInteger('employee_id')->index();
                $table->enum('proposal_type', [
                    'PERIODIC',                // Định kỳ hàng năm
                    'DEPARTMENT_RECOMMENDED', // Phòng ban đề xuất
                    'SPECIAL_EXCEPTION',      // Đặc cách
                    'MASS_STATUTORY'          // Hàng loạt (pháp lý đổi)
                ])->index();

                // Lương hiện tại & Đề xuất
                $table->decimal('current_p1', 15, 0);
                $table->decimal('proposed_p1', 15, 0);
                $table->decimal('current_p2', 15, 0)->default(0);
                $table->decimal('proposed_p2', 15, 0)->default(0);
                $table->decimal('current_insurance_salary', 15, 0);
                $table->decimal('proposed_insurance_salary', 15, 0);

                // Khung lương & Compa-ratio
                $table->unsignedBigInteger('salary_band_id')->nullable();
                $table->decimal('band_min', 15, 0)->nullable();
                $table->decimal('band_midpoint', 15, 0)->nullable();
                $table->decimal('band_max', 15, 0)->nullable();
                $table->decimal('current_compa_ratio', 6, 2)->nullable()->comment('% compa-ratio hiện tại');
                $table->decimal('proposed_compa_ratio', 6, 2)->nullable()->comment('% compa-ratio sau tăng');

                // Lịch sử KPI & Lần tăng gần nhất
                $table->json('kpi_history_json')->nullable()->comment('Điểm & Xếp loại 2 kỳ KPI gần nhất');
                $table->date('last_salary_increase_date')->nullable();
                $table->unsignedSmallInteger('months_since_last_increase')->default(0);

                // Cờ kiểm soát & Đặc cách
                $table->boolean('is_out_of_band')->default(false);
                $table->boolean('is_special_exception')->default(false);
                $table->boolean('is_senior_management')->default(false);
                $table->text('exception_reason')->nullable();
                $table->string('attachment_url', 500)->nullable();
                $table->json('warnings_json')->nullable()->comment('Danh sách cảnh báo: thời gian, vượt khung, so sánh');

                // Hiệu lực & Trạng thái
                $table->date('effective_from');
                $table->text('reason');
                $table->enum('status', [
                    'DRAFT',
                    'SUBMITTED',
                    'PENDING_STEP',
                    'APPROVED',
                    'REJECTED',
                    'CANCELLED'
                ])->default('SUBMITTED')->index();
                $table->unsignedTinyInteger('current_step_number')->default(1);
                $table->string('current_workflow_type', 50)->default('DEFAULT');

                // Quyết định ban hành & Truy lĩnh
                $table->string('decision_number', 100)->nullable();
                $table->string('decision_pdf_url', 500)->nullable();
                $table->unsignedSmallInteger('retroactive_months')->default(0);
                $table->decimal('retroactive_amount', 15, 0)->default(0);

                // Nhật ký
                $table->unsignedBigInteger('created_by')->index();
                $table->dateTime('approved_at')->nullable();
                $table->unsignedBigInteger('approved_by')->nullable();
                $table->timestamps();
                $table->softDeletes();

                $table->foreign('employee_id')->references('id')->on('employees')->cascadeOnDelete();
                $table->foreign('created_by')->references('id')->on('employees')->cascadeOnDelete();
            });
        }

        // 8. Bảng các bước phê duyệt động (approval_steps)
        if (!Schema::hasTable('approval_steps')) {
            Schema::create('approval_steps', function (Blueprint $table) {
                $table->id();
                $table->unsignedBigInteger('proposal_id')->index();
                $table->unsignedTinyInteger('step_number');
                $table->string('approver_role', 50);
                $table->unsignedBigInteger('approver_id')->nullable()->index();
                $table->enum('action', ['PENDING', 'APPROVED', 'REJECTED'])->default('PENDING')->index();
                $table->text('comments')->nullable();
                $table->dateTime('action_at')->nullable();
                $table->timestamps();

                $table->foreign('proposal_id')->references('id')->on('salary_proposals')->cascadeOnDelete();
            });
        }

        // 9. Khoản truy lĩnh lương (retroactive_salary_adjustments)
        if (!Schema::hasTable('retroactive_salary_adjustments')) {
            Schema::create('retroactive_salary_adjustments', function (Blueprint $table) {
                $table->id();
                $table->unsignedBigInteger('salary_proposal_id')->index();
                $table->unsignedBigInteger('employee_id')->index();
                $table->unsignedBigInteger('payroll_period_id')->index()->comment('Kỳ nhận tiền truy lĩnh');
                $table->unsignedBigInteger('source_period_id')->index()->comment('Kỳ quá khứ bị truy lĩnh');
                $table->decimal('retroactive_difference', 15, 0)->comment('Chênh lệch lương 1 tháng');
                $table->enum('status', ['PENDING', 'PROCESSED'])->default('PENDING');
                $table->timestamps();

                $table->foreign('salary_proposal_id')->references('id')->on('salary_proposals')->cascadeOnDelete();
                $table->foreign('employee_id')->references('id')->on('employees')->cascadeOnDelete();
            });
        }
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('retroactive_salary_adjustments');
        Schema::dropIfExists('approval_steps');
        Schema::dropIfExists('salary_proposals');
        Schema::dropIfExists('approval_rules');
        Schema::dropIfExists('payroll_periods');
        Schema::dropIfExists('employee_salaries');
        Schema::dropIfExists('unit_salary_budgets');
        Schema::dropIfExists('regional_min_wages');
        Schema::dropIfExists('salary_bands');
    }
};
