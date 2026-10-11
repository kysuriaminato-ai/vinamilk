<?php

use App\Modules\Kpi\Http\Controllers\KpiController;
use App\Modules\Leave\Http\Controllers\LeaveController;
use App\Modules\Leave\Http\Controllers\OffboardingController;
use Illuminate\Support\Facades\Route;

// Trang chủ & Dashboard Doanh nghiệp
Route::get('/', fn() => redirect()->route('dashboard'));
Route::get('/dashboard', fn() => view('dashboard'))->name('dashboard');

// Hộp thư phê duyệt tập trung
Route::get('/approvals/inbox', fn() => view('approvals.inbox'))->name('approvals.inbox');

// Xem chi tiết hồ sơ nhân sự dạng Tab
Route::get('/employees/{id}', function ($id) {
    $employee = \App\Modules\Employee\Models\Employee::with(['currentAssignment.orgUnit', 'currentAssignment.position', 'currentSalary'])->find($id) 
        ?? new \App\Modules\Employee\Models\Employee([
            'employee_code'     => 'VNM-CN001',
            'full_name'         => 'Nguyễn Văn Công',
            'employment_status' => 'ACTIVE',
            'hire_date'         => '2023-01-01',
            'social_insurance_number' => '7912345678',
        ]);
    return view('employees.show', compact('employee'));
})->name('employees.show');

// Giao diện Web KPI
Route::middleware(['web', 'auth'])->prefix('kpi')->name('kpi.')->group(function () {
    Route::get('/kanban', [KpiController::class, 'kanbanView'])->name('kanban');
    Route::get('/my-tasks', [KpiController::class, 'myTasksView'])->name('my-tasks');
});

// PHÂN HỆ BÁO CÁO THỐNG KÊ & BIỂU MẪU BM01 - BM04
Route::middleware(['web', 'auth'])->prefix('reports')->name('reports.')->group(function () {
    Route::get('/', [\App\Modules\Report\Http\Controllers\ReportController::class, 'index'])->name('index');
    Route::get('/bm01', [\App\Modules\Report\Http\Controllers\ReportController::class, 'bm01'])->name('bm01');
    Route::get('/bm02', [\App\Modules\Report\Http\Controllers\ReportController::class, 'bm02'])->name('bm02');
    Route::get('/bm03', [\App\Modules\Report\Http\Controllers\ReportController::class, 'bm03'])->name('bm03');
    Route::get('/bm04', [\App\Modules\Report\Http\Controllers\ReportController::class, 'bm04'])->name('bm04');
    Route::get('/export-excel/{type}', [\App\Modules\Report\Http\Controllers\ReportController::class, 'exportExcel'])->name('export-excel');
    Route::get('/print/{type}', [\App\Modules\Report\Http\Controllers\ReportController::class, 'printReport'])->name('print-report');
});
