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
        // 1. Tham số pháp lý động (legal_parameters)
        if (!Schema::hasTable('legal_parameters')) {
            Schema::create('legal_parameters', function (Blueprint $table) {
                $table->id();
                $table->string('param_code', 50)->unique();
                $table->string('param_name', 150);
                $table->decimal('param_value', 15, 4);
                $table->string('unit', 20)->comment('VND, PERCENT, HOURS, RATIO');
                $table->string('description', 500)->nullable();
                $table->date('effective_from');
                $table->date('effective_to')->nullable();
                $table->boolean('is_current')->default(true);
                $table->timestamps();
                $table->softDeletes();
            });

            // Seed các tham số pháp lý chuẩn Việt Nam (Không hardcode bất kỳ số nào)
            DB::table('legal_parameters')->insert([
                ['param_code' => 'BASE_SALARY_STATUTORY', 'param_name' => 'Mức lương cơ sở nhà nước', 'param_value' => 2340000, 'unit' => 'VND', 'description' => 'Nghị định 73/2024/NĐ-CP áp dụng từ 01/07/2024', 'effective_from' => '2024-07-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['param_code' => 'PERSONAL_TAX_RELIEF', 'param_name' => 'Mức giảm trừ gia cảnh bản thân', 'param_value' => 11000000, 'unit' => 'VND', 'description' => 'Nghị quyết 954/2020/UBTVQH14', 'effective_from' => '2020-07-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['param_code' => 'DEPENDENT_TAX_RELIEF', 'param_name' => 'Mức giảm trừ mỗi người phụ thuộc', 'param_value' => 4400000, 'unit' => 'VND', 'description' => 'Nghị quyết 954/2020/UBTVQH14', 'effective_from' => '2020-07-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['param_code' => 'TAX_FREE_MEAL_CAP', 'param_name' => 'Trần phụ cấp ăn trưa miễn thuế TNCN', 'param_value' => 730000, 'unit' => 'VND', 'description' => 'Thông tư 26/2016/TT-BLĐTBXH', 'effective_from' => '2016-10-15', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['param_code' => 'SI_EMP_RATE', 'param_name' => 'Tỷ lệ đóng BHXH Người lao động', 'param_value' => 0.0800, 'unit' => 'PERCENT', 'description' => 'Luật BHXH 2014', 'effective_from' => '2016-01-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['param_code' => 'HI_EMP_RATE', 'param_name' => 'Tỷ lệ đóng BHYT Người lao động', 'param_value' => 0.0150, 'unit' => 'PERCENT', 'description' => 'Luật BHYT', 'effective_from' => '2015-01-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['param_code' => 'UI_EMP_RATE', 'param_name' => 'Tỷ lệ đóng BHTN Người lao động', 'param_value' => 0.0100, 'unit' => 'PERCENT', 'description' => 'Luật Việc làm 2013', 'effective_from' => '2015-01-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['param_code' => 'SI_COMP_RATE', 'param_name' => 'Tỷ lệ đóng BHXH Doanh nghiệp', 'param_value' => 0.1750, 'unit' => 'PERCENT', 'description' => '14% Hưu trí, 3% Ốm đau thai sản, 0.5% TNLĐ-BNN', 'effective_from' => '2021-07-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['param_code' => 'HI_COMP_RATE', 'param_name' => 'Tỷ lệ đóng BHYT Doanh nghiệp', 'param_value' => 0.0300, 'unit' => 'PERCENT', 'description' => 'Luật BHYT', 'effective_from' => '2015-01-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['param_code' => 'UI_COMP_RATE', 'param_name' => 'Tỷ lệ đóng BHTN Doanh nghiệp', 'param_value' => 0.0100, 'unit' => 'PERCENT', 'description' => 'Luật Việc làm 2013', 'effective_from' => '2015-01-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['param_code' => 'UNION_FEE_EMP_RATE', 'param_name' => 'Đoàn phí Công đoàn NLĐ (tối đa 10% lương cơ sở)', 'param_value' => 0.0100, 'unit' => 'PERCENT', 'description' => 'Điều lệ Công đoàn Việt Nam', 'effective_from' => '2020-01-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['param_code' => 'PARTY_FEE_RATE', 'param_name' => 'Đảng phí Đảng viên', 'param_value' => 0.0100, 'unit' => 'PERCENT', 'description' => 'Quyết định của Ban Bí thư TW', 'effective_from' => '2018-01-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['param_code' => 'SI_HI_CAP_MULTIPLIER', 'param_name' => 'Trần lương đóng BHXH/BHYT (20 lần lương cơ sở)', 'param_value' => 20.0, 'unit' => 'RATIO', 'description' => 'Luật BHXH 2014', 'effective_from' => '2016-01-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['param_code' => 'UI_CAP_MULTIPLIER', 'param_name' => 'Trần lương đóng BHTN (20 lần lương tối thiểu vùng)', 'param_value' => 20.0, 'unit' => 'RATIO', 'description' => 'Luật Việc làm 2013', 'effective_from' => '2015-01-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['param_code' => 'UNION_FEE_COMP_RATE', 'param_name' => 'Kinh phí Công đoàn Doanh nghiệp', 'param_value' => 0.0200, 'unit' => 'PERCENT', 'description' => 'Nghị định 191/2013/NĐ-CP (2% quỹ lương đóng BHXH)', 'effective_from' => '2014-01-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['param_code' => 'REGIONAL_MIN_WAGE_DEFAULT', 'param_name' => 'Lương tối thiểu vùng Vùng I (áp dụng cho BHTN)', 'param_value' => 4960000, 'unit' => 'VND', 'description' => 'Nghị định 74/2024/NĐ-CP áp dụng từ 01/07/2024', 'effective_from' => '2024-07-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
            ]);
        }

        // 2. Biểu thuế thu nhập cá nhân lũy tiến 7 bậc (tax_brackets)
        if (!Schema::hasTable('tax_brackets')) {
            Schema::create('tax_brackets', function (Blueprint $table) {
                $table->id();
                $table->unsignedTinyInteger('bracket_level');
                $table->decimal('min_income', 15, 0);
                $table->decimal('max_income', 15, 0)->nullable();
                $table->decimal('tax_rate', 5, 2)->comment('% thuế suất (5, 10, 15, 20, 25, 30, 35)');
                $table->decimal('quick_deduction', 15, 0)->default(0)->comment('Số trừ nhanh');
                $table->date('effective_from');
                $table->date('effective_to')->nullable();
                $table->boolean('is_current')->default(true);
                $table->timestamps();
                $table->softDeletes();
            });

            DB::table('tax_brackets')->insert([
                ['bracket_level' => 1, 'min_income' => 0, 'max_income' => 5000000, 'tax_rate' => 5.00, 'quick_deduction' => 0, 'effective_from' => '2015-01-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['bracket_level' => 2, 'min_income' => 5000000, 'max_income' => 10000000, 'tax_rate' => 10.00, 'quick_deduction' => 250000, 'effective_from' => '2015-01-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['bracket_level' => 3, 'min_income' => 10000000, 'max_income' => 18000000, 'tax_rate' => 15.00, 'quick_deduction' => 750000, 'effective_from' => '2015-01-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['bracket_level' => 4, 'min_income' => 18000000, 'max_income' => 32000000, 'tax_rate' => 20.00, 'quick_deduction' => 1650000, 'effective_from' => '2015-01-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['bracket_level' => 5, 'min_income' => 32000000, 'max_income' => 52000000, 'tax_rate' => 25.00, 'quick_deduction' => 3250000, 'effective_from' => '2015-01-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['bracket_level' => 6, 'min_income' => 52000000, 'max_income' => 80000000, 'tax_rate' => 30.00, 'quick_deduction' => 5850000, 'effective_from' => '2015-01-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['bracket_level' => 7, 'min_income' => 80000000, 'max_income' => null, 'tax_rate' => 35.00, 'quick_deduction' => 9850000, 'effective_from' => '2015-01-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
            ]);
        }

        // 3. Hệ số làm thêm giờ (ot_rates)
        if (!Schema::hasTable('ot_rates')) {
            Schema::create('ot_rates', function (Blueprint $table) {
                $table->id();
                $table->string('rate_code', 50)->unique();
                $table->string('rate_name', 150);
                $table->decimal('multiplier', 4, 2);
                $table->decimal('night_additional_multiplier', 4, 2)->default(0.30);
                $table->date('effective_from');
                $table->date('effective_to')->nullable();
                $table->boolean('is_current')->default(true);
                $table->timestamps();
            });

            DB::table('ot_rates')->insert([
                ['rate_code' => 'OT_WEEKDAY', 'rate_name' => 'Làm thêm ngày thường (150%)', 'multiplier' => 1.50, 'night_additional_multiplier' => 0.30, 'effective_from' => '2021-01-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['rate_code' => 'OT_WEEKEND', 'rate_name' => 'Làm thêm ngày nghỉ tuần (200%)', 'multiplier' => 2.00, 'night_additional_multiplier' => 0.30, 'effective_from' => '2021-01-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
                ['rate_code' => 'OT_HOLIDAY', 'rate_name' => 'Làm thêm ngày lễ, tết (300%)', 'multiplier' => 3.00, 'night_additional_multiplier' => 0.30, 'effective_from' => '2021-01-01', 'effective_to' => null, 'is_current' => true, 'created_at' => now(), 'updated_at' => now()],
            ]);
        }

        // 4. Bảng người phụ thuộc giảm trừ gia cảnh (dependents)
        if (!Schema::hasTable('dependents')) {
            Schema::create('dependents', function (Blueprint $table) {
                $table->id();
                $table->unsignedBigInteger('employee_id')->index();
                $table->string('dependent_name', 150);
                $table->string('relationship', 50)->comment('CHILD, SPOUSE, PARENT, OTHER');
                $table->string('tax_id', 50)->nullable();
                $table->date('effective_from');
                $table->date('effective_to')->nullable();
                $table->boolean('is_active')->default(true);
                $table->timestamps();
                $table->softDeletes();

                $table->foreign('employee_id')->references('id')->on('employees')->cascadeOnDelete();
            });
        }

        // 5. Bảng phiếu lương tổng hợp (payslips)
        if (!Schema::hasTable('payslips')) {
            Schema::create('payslips', function (Blueprint $table) {
                $table->id();
                $table->unsignedBigInteger('payroll_period_id')->index();
                $table->unsignedBigInteger('employee_id')->index();
                $table->unsignedBigInteger('org_unit_id')->index();
                $table->string('cost_center_code', 50)->nullable();
                $table->decimal('actual_working_days', 4, 1)->default(0.0);
                $table->decimal('paid_leave_days', 4, 1)->default(0.0);
                $table->decimal('p1_position_amount', 15, 0)->default(0);
                $table->decimal('p2_competency_amount', 15, 0)->default(0);
                $table->decimal('p3_performance_amount', 15, 0)->default(0);
                $table->decimal('ot_pay_amount', 15, 0)->default(0);
                $table->decimal('total_allowance_amount', 15, 0)->default(0);
                $table->decimal('retroactive_amount', 15, 0)->default(0)->comment('Khoản truy lĩnh');
                $table->decimal('gross_income', 15, 0)->default(0);
                $table->decimal('social_insurance_emp', 15, 0)->default(0);
                $table->decimal('health_insurance_emp', 15, 0)->default(0);
                $table->decimal('unemployment_insurance_emp', 15, 0)->default(0);
                $table->decimal('union_fee_emp', 15, 0)->default(0);
                $table->decimal('party_fee_emp', 15, 0)->default(0);
                $table->decimal('personal_income_tax', 15, 0)->default(0);
                $table->decimal('other_deductions', 15, 0)->default(0);
                $table->decimal('net_income', 15, 0)->default(0);
                $table->decimal('social_insurance_comp', 15, 0)->default(0);
                $table->decimal('health_insurance_comp', 15, 0)->default(0);
                $table->decimal('unemployment_insurance_comp', 15, 0)->default(0);
                $table->decimal('union_fee_comp', 15, 0)->default(0);
                $table->enum('status', ['DRAFT', 'CONFIRMED', 'PAID'])->default('DRAFT')->index();
                $table->dateTime('paid_at')->nullable();
                $table->string('bank_reference', 100)->nullable();
                $table->timestamps();
                $table->softDeletes();

                $table->unique(['payroll_period_id', 'employee_id']);
                $table->foreign('payroll_period_id')->references('id')->on('payroll_periods')->cascadeOnDelete();
                $table->foreign('employee_id')->references('id')->on('employees')->cascadeOnDelete();
            });
        }

        // 6. Bảng chi tiết cấu phần dòng lương (payslip_lines)
        if (!Schema::hasTable('payslip_lines')) {
            Schema::create('payslip_lines', function (Blueprint $table) {
                $table->id();
                $table->unsignedBigInteger('payslip_id')->index();
                $table->string('line_code', 50)->index();
                $table->string('line_name', 150);
                $table->enum('line_type', [
                    'EARNING',
                    'ALLOWANCE',
                    'BONUS',
                    'STATUTORY_DEDUCTION',
                    'TAX_DEDUCTION',
                    'OTHER_DEDUCTION',
                    'EMPLOYER_CONTRIBUTION'
                ])->index();
                $table->decimal('amount', 15, 0)->default(0);
                $table->string('calculation_formula', 500)->nullable();
                $table->timestamps();

                $table->foreign('payslip_id')->references('id')->on('payslips')->cascadeOnDelete();
            });
        }
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('payslip_lines');
        Schema::dropIfExists('payslips');
        Schema::dropIfExists('dependents');
        Schema::dropIfExists('ot_rates');
        Schema::dropIfExists('tax_brackets');
        Schema::dropIfExists('legal_parameters');
    }
};
