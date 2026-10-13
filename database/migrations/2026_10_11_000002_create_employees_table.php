<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    /**
     * Run the migrations.
     */
    public function up(): void
    {
        Schema::create('employees', function (Blueprint $table) {
            $table->id()->comment('Khóa chính hồ sơ nhân sự');
            $table->string('employee_code', 50)->unique()->comment('Mã nhân viên Vinamilk (VD: VNM00123)');
            $table->string('full_name', 150)->index()->comment('Họ và tên đầy đủ');
            $table->enum('gender', ['MALE', 'FEMALE', 'OTHER'])->comment('Giới tính');
            $table->date('date_of_birth')->comment('Ngày sinh');
            $table->string('national_id', 20)->unique()->comment('Số CMND/CCCD/Hộ chiếu');
            $table->date('national_id_issued_date')->nullable()->comment('Ngày cấp');
            $table->string('national_id_issued_place', 200)->nullable()->comment('Nơi cấp');
            $table->string('personal_email', 100)->nullable()->comment('Email cá nhân');
            $table->string('company_email', 100)->nullable()->unique()->comment('Email công vụ Vinamilk');
            $table->string('phone_number', 20)->comment('Số điện thoại');
            $table->string('permanent_address', 300)->nullable()->comment('Hộ khẩu thường trú');
            $table->string('current_address', 300)->nullable()->comment('Chỗ ở hiện nay');
            $table->string('tax_id', 30)->nullable()->comment('Mã số thuế TNCN');
            $table->string('bank_account_number', 50)->nullable()->comment('Số tài khoản ngân hàng');
            $table->string('bank_name', 100)->nullable()->comment('Tên ngân hàng');
            $table->string('bank_branch', 100)->nullable()->comment('Chi nhánh ngân hàng');
            $table->string('social_insurance_number', 30)->nullable()->comment('Mã số sổ BHXH');
            $table->date('hire_date')->index()->comment('Ngày bắt đầu vào làm việc');
            $table->date('probation_end_date')->nullable()->comment('Ngày kết thúc thử việc');
            $table->date('termination_date')->nullable()->comment('Ngày thôi việc');
            $table->enum('employment_status', [
                'PROBATION', 'ACTIVE', 'SUSPENDED', 'TERMINATED', 'RETIRED'
            ])->default('PROBATION')->comment('Trạng thái nhân sự');
            $table->string('avatar_url', 255)->nullable()->comment('Đường dẫn ảnh chân dung');
            $table->timestamps();
            $table->softDeletes()->comment('Xóa mềm hồ sơ');

            $table->index(['employment_status', 'deleted_at'], 'idx_employees_status');
            $table->comment('Hồ sơ nhân sự cốt lõi toàn tập đoàn Vinamilk');
        });

        // Thiết lập Foreign key tự tham chiếu cho manager_employee_id trong org_units
        Schema::table('org_units', function (Blueprint $table) {
            $table->foreign('manager_employee_id', 'fk_org_units_manager')
                  ->references('id')
                  ->on('employees')
                  ->nullOnDelete()
                  ->cascadeOnUpdate();
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::table('org_units', function (Blueprint $table) {
            $table->dropForeign('fk_org_units_manager');
        });

        Schema::dropIfExists('employees');
    }
};
