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
        // 1. Bảng chiều Thời gian (dim_date)
        if (!Schema::hasTable('dim_date')) {
            Schema::create('dim_date', function (Blueprint $table) {
                $table->integer('date_key')->primary()->comment('YYYYMMDD');
                $table->date('full_date')->unique();
                $table->unsignedTinyInteger('day_of_month');
                $table->unsignedTinyInteger('month');
                $table->string('month_name', 20);
                $table->unsignedTinyInteger('quarter');
                $table->unsignedSmallInteger('year');
                $table->unsignedTinyInteger('day_of_week')->comment('1 = Sunday, 7 = Saturday');
                $table->string('day_name', 20);
                $table->boolean('is_weekend');
                $table->boolean('is_holiday')->default(false);
                $table->index(['year', 'month']);
            });

            // Nạp dữ liệu dim_date cho giai đoạn 2020 - 2030 (Script khởi tạo)
            $startDate = new DateTime('2024-01-01');
            $endDate = new DateTime('2026-12-31');
            $interval = new DateInterval('P1D');
            $period = new DatePeriod($startDate, $interval, $endDate->modify('+1 day'));

            $dateRows = [];
            foreach ($period as $dt) {
                $dayOfWeek = (int)$dt->format('w') + 1;
                $dateRows[] = [
                    'date_key'     => (int)$dt->format('Ymd'),
                    'full_date'    => $dt->format('Y-m-d'),
                    'day_of_month' => (int)$dt->format('d'),
                    'month'        => (int)$dt->format('m'),
                    'month_name'   => 'Tháng ' . $dt->format('m'),
                    'quarter'      => (int)ceil((int)$dt->format('m') / 3),
                    'year'         => (int)$dt->format('Y'),
                    'day_of_week'  => $dayOfWeek,
                    'day_name'     => $dayOfWeek === 1 ? 'Chủ nhật' : 'Thứ ' . $dayOfWeek,
                    'is_weekend'   => in_array($dayOfWeek, [1, 7]),
                    'is_holiday'   => false,
                ];

                if (count($dateRows) >= 500) {
                    DB::table('dim_date')->insert($dateRows);
                    $dateRows = [];
                }
            }
            if (!empty($dateRows)) {
                DB::table('dim_date')->insert($dateRows);
            }
        }

        // 2. Bảng chiều Đơn vị tổ chức (dim_org_unit)
        if (!Schema::hasTable('dim_org_unit')) {
            Schema::create('dim_org_unit', function (Blueprint $table) {
                $table->id('org_unit_key');
                $table->unsignedBigInteger('org_unit_id')->index();
                $table->string('unit_code', 50);
                $table->string('unit_name', 150);
                $table->string('unit_type', 50);
                $table->string('path', 255)->index();
                $table->unsignedTinyInteger('level');
                $table->string('cost_center_code', 50)->nullable();
                $table->string('region_name', 100)->nullable();
                $table->timestamps();
            });
        }

        // 3. Bảng chiều Chức danh & Khung lương (dim_position)
        if (!Schema::hasTable('dim_position')) {
            Schema::create('dim_position', function (Blueprint $table) {
                $table->id('position_key');
                $table->unsignedBigInteger('position_id')->index();
                $table->string('position_code', 50);
                $table->string('position_name', 150);
                $table->unsignedTinyInteger('level');
                $table->string('job_grade_code', 50)->nullable();
                $table->string('salary_band_code', 50)->nullable();
                $table->decimal('midpoint_salary', 15, 0)->default(0);
                $table->timestamps();
            });
        }

        // 4. Bảng chiều Nhân sự SCD Type 2 (dim_employee)
        if (!Schema::hasTable('dim_employee')) {
            Schema::create('dim_employee', function (Blueprint $table) {
                $table->id('employee_key');
                $table->unsignedBigInteger('employee_id')->index();
                $table->string('employee_code', 50)->index();
                $table->string('full_name', 150);
                $table->string('gender', 10);
                $table->date('date_of_birth');
                $table->string('national_id', 30)->nullable();
                $table->date('hire_date');
                $table->date('termination_date')->nullable();
                $table->string('employment_status', 30);
                $table->unsignedBigInteger('org_unit_id');
                $table->unsignedBigInteger('position_id');
                $table->decimal('p1_position_salary', 15, 0)->default(0);
                $table->decimal('p2_competency_salary', 15, 0)->default(0);
                $table->decimal('insurance_salary', 15, 0)->default(0);
                // SCD Type 2 tracking columns
                $table->date('valid_from')->index();
                $table->date('valid_to')->nullable()->index();
                $table->boolean('is_current')->default(true)->index();
                $table->timestamps();
            });
        }

        // 5. Fact Định biên nhân sự hằng tháng (fact_headcount_monthly)
        if (!Schema::hasTable('fact_headcount_monthly')) {
            Schema::create('fact_headcount_monthly', function (Blueprint $table) {
                $table->id();
                $table->integer('month_key')->index()->comment('YYYYMM');
                $table->unsignedBigInteger('org_unit_key')->index();
                $table->unsignedBigInteger('position_key')->index();
                $table->unsignedInteger('headcount_bom')->default(0);
                $table->unsignedInteger('headcount_eom')->default(0);
                $table->decimal('headcount_avg', 8, 2)->default(0.00);
                $table->unsignedInteger('hires_count')->default(0);
                $table->unsignedInteger('voluntary_terminations')->default(0);
                $table->unsignedInteger('involuntary_terminations')->default(0);
                $table->decimal('total_gross_payroll', 18, 0)->default(0);
                $table->timestamps();

                $table->unique(['month_key', 'org_unit_key', 'position_key'], 'uq_hc_monthly');
            });
        }

        // 6. Fact Chấm công hằng ngày (fact_attendance_day)
        if (!Schema::hasTable('fact_attendance_day')) {
            Schema::create('fact_attendance_day', function (Blueprint $table) {
                $table->id();
                $table->integer('date_key')->index();
                $table->unsignedBigInteger('employee_key')->index();
                $table->unsignedBigInteger('org_unit_key')->index();
                $table->decimal('standard_hours', 4, 1)->default(8.0);
                $table->decimal('actual_hours', 4, 1)->default(0.0);
                $table->unsignedSmallInteger('late_minutes')->default(0);
                $table->unsignedSmallInteger('early_minutes')->default(0);
                $table->decimal('ot_normal_hours', 4, 1)->default(0.0);
                $table->decimal('ot_weekend_hours', 4, 1)->default(0.0);
                $table->decimal('ot_holiday_hours', 4, 1)->default(0.0);
                $table->decimal('night_work_hours', 4, 1)->default(0.0);
                $table->boolean('is_absent')->default(false);
                $table->string('attendance_status', 30);
                $table->timestamps();

                $table->unique(['date_key', 'employee_key'], 'uq_att_day_emp');
            });
        }

        // 7. Fact Phiếu lương & Chi phí nhân sự (fact_payslip)
        if (!Schema::hasTable('fact_payslip')) {
            Schema::create('fact_payslip', function (Blueprint $table) {
                $table->id();
                $table->integer('period_end_date_key')->index();
                $table->unsignedBigInteger('payroll_period_id')->index();
                $table->unsignedBigInteger('employee_key')->index();
                $table->unsignedBigInteger('org_unit_key')->index();
                $table->decimal('actual_working_days', 4, 1)->default(0);
                $table->decimal('p1_amount', 15, 0)->default(0);
                $table->decimal('p2_amount', 15, 0)->default(0);
                $table->decimal('p3_amount', 15, 0)->default(0);
                $table->decimal('ot_pay_amount', 15, 0)->default(0);
                $table->decimal('allowance_amount', 15, 0)->default(0);
                $table->decimal('retroactive_amount', 15, 0)->default(0);
                $table->decimal('gross_income', 15, 0)->default(0);
                $table->decimal('statutory_deductions_emp', 15, 0)->default(0);
                $table->decimal('union_fee_emp', 15, 0)->default(0);
                $table->decimal('pit_tax', 15, 0)->default(0);
                $table->decimal('net_income', 15, 0)->default(0);
                $table->decimal('employer_contributions', 15, 0)->default(0);
                $table->timestamps();

                $table->unique(['payroll_period_id', 'employee_key'], 'uq_fact_payslip');
            });
        }

        // 8. Fact KPI & Hiệu suất công việc (fact_kpi)
        if (!Schema::hasTable('fact_kpi')) {
            Schema::create('fact_kpi', function (Blueprint $table) {
                $table->id();
                $table->integer('period_date_key')->index();
                $table->unsignedBigInteger('kpi_period_id')->index();
                $table->unsignedBigInteger('employee_key')->index();
                $table->unsignedBigInteger('org_unit_key')->index();
                $table->decimal('kpi_score', 5, 2)->default(0);
                $table->string('kpi_grade', 10)->default('B');
                $table->decimal('p3_multiplier', 4, 2)->default(1.0);
                $table->unsignedSmallInteger('total_tasks')->default(0);
                $table->unsignedSmallInteger('completed_tasks')->default(0);
                $table->unsignedSmallInteger('overdue_tasks')->default(0);
                $table->timestamps();

                $table->unique(['kpi_period_id', 'employee_key'], 'uq_fact_kpi');
            });
        }
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('fact_kpi');
        Schema::dropIfExists('fact_payslip');
        Schema::dropIfExists('fact_attendance_day');
        Schema::dropIfExists('fact_headcount_monthly');
        Schema::dropIfExists('dim_employee');
        Schema::dropIfExists('dim_position');
        Schema::dropIfExists('dim_org_unit');
        Schema::dropIfExists('dim_date');
    }
};
