<?php

namespace Database\Seeders;

use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\Artisan;

class VinamilkDemoDataSeeder extends Seeder
{
    /**
     * Run the database seeds.
     */
    public function run(): void
    {
        $this->command->info("Đang kích hoạt quy trình sinh dữ liệu mẫu Vinamilk Demo Data Seeder...");

        Artisan::call('vinamilk:seed-demo-data', [], $this->command->getOutput());
    }
}
