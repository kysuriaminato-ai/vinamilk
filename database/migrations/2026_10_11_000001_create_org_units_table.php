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
        Schema::create('org_units', function (Blueprint $table) {
            $table->id()->comment('Khóa chính định danh đơn vị');
            $table->string('unit_code', 50)->unique()->comment('Mã đơn vị duy nhất (VD: VNM_HQ, NM_TIEN_SON)');
            $table->string('unit_name', 255)->comment('Tên đơn vị tổ chức');
            $table->enum('unit_type', [
                'HEADQUARTERS', 'DIVISION', 'FACTORY', 'DAIRY_FARM',
                'BRANCH', 'LOGISTICS_CENTER', 'DEPARTMENT', 'WORKSHOP', 'TEAM'
            ])->comment('Loại hình đơn vị');
            $table->foreignId('parent_id')->nullable()->constrained('org_units')->restrictOnDelete()->cascadeOnUpdate()->comment('Tham chiếu đơn vị cấp cha');
            $table->string('path', 1000)->index()->comment('Đường dẫn phân cấp vật lý (Materialized Path, VD: /1/4/15/)');
            $table->unsignedTinyInteger('level')->default(1)->comment('Cấp bậc phân tầng cây');
            $table->enum('region', ['NORTH', 'CENTRAL', 'SOUTH'])->default('SOUTH')->comment('Phân vùng địa lý');
            $table->string('address', 500)->nullable()->comment('Địa chỉ trụ sở/nhà máy');
            $table->string('tax_code', 50)->nullable()->comment('Mã số thuế chi nhánh');
            $table->string('cost_center_code', 50)->nullable()->comment('Mã Cost Center SAP ERP');
            $table->unsignedBigInteger('manager_employee_id')->nullable()->comment('ID của Giám đốc/Trưởng đơn vị');
            $table->date('effective_from')->comment('Ngày bắt đầu có hiệu lực vận hành');
            $table->date('effective_to')->nullable()->comment('Ngày chấm dứt hiệu lực (NULL = Đang hoạt động)');
            $table->boolean('is_active')->default(true)->comment('Cờ trạng thái hoạt động');
            $table->timestamps();
            $table->softDeletes()->comment('Thời điểm xóa mềm');

            $table->index(['is_active', 'deleted_at'], 'idx_org_units_active');
            $table->comment('Cơ cấu tổ chức hình cây đa tầng kết hợp Materialized Path');
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('org_units');
    }
};
