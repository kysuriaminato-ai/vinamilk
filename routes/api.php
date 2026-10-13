<?php

use App\Modules\Kpi\Http\Controllers\KpiController;
use App\Modules\Leave\Http\Controllers\LeaveController;
use App\Modules\Leave\Http\Controllers\OffboardingController;
use Illuminate\Support\Facades\Route;

Route::prefix('v1')->group(function () {
    // PHÂN HỆ KPI
    Route::prefix('kpi')->name('api.kpi.')->group(function () {
        Route::get('/kanban', [KpiController::class, 'getKanbanData'])->name('kanban.data');
        Route::post('/tasks', [KpiController::class, 'storeTask'])->name('tasks.store');
        Route::post('/tasks/{id}/start', [KpiController::class, 'startTask'])->name('tasks.start');
        Route::post('/tasks/{id}/submit', [KpiController::class, 'submitTask'])->name('tasks.submit');
        Route::post('/tasks/{id}/approve', [KpiController::class, 'approveTask'])->name('tasks.approve');
        Route::post('/tasks/{id}/reject', [KpiController::class, 'rejectTask'])->name('tasks.reject');
        Route::post('/periods/{id}/export-payroll', [KpiController::class, 'exportPayroll'])->name('periods.export');
    });

    // PHÂN HỆ NGHỈ PHÉP & THÔI VIỆC
    Route::prefix('leave')->name('api.leave.')->group(function () {
        Route::get('/types', [LeaveController::class, 'getLeaveTypes'])->name('types');
        Route::get('/my-balance', [LeaveController::class, 'getMyBalance'])->name('balance');
        Route::post('/requests', [LeaveController::class, 'submitRequest'])->name('requests.submit');
        Route::post('/requests/{id}/approve', [LeaveController::class, 'approveRequest'])->name('requests.approve');
        Route::post('/requests/{id}/reject', [LeaveController::class, 'rejectRequest'])->name('requests.reject');
        Route::post('/awol', [LeaveController::class, 'recordAwol'])->name('awol');
    });

    Route::prefix('offboarding')->name('api.offboarding.')->group(function () {
        Route::get('/notice-days/{empId}', [OffboardingController::class, 'getNoticeDays'])->name('notice-days');
        Route::post('/resignation', [OffboardingController::class, 'submitResignation'])->name('resignation.submit');
        Route::post('/requests/{id}/manager-approve', [OffboardingController::class, 'approveByManager'])->name('resignation.manager-approve');
        Route::post('/requests/{id}/hr-approve', [OffboardingController::class, 'approveByHr'])->name('resignation.hr-approve');
        Route::post('/requests/{id}/bod-approve', [OffboardingController::class, 'approveByBod'])->name('resignation.bod-approve');
        Route::post('/handover-items/{id}/confirm', [OffboardingController::class, 'confirmAssetHandover'])->name('handover.confirm');
        Route::post('/requests/{id}/settlement', [OffboardingController::class, 'calculateSettlement'])->name('settlement.calc');
    });

    // PHÂN HỆ TĂNG LƯƠNG & LUỒNG DUYỆT ĐỘNG
    Route::prefix('salary-proposals')->name('api.salary-proposals.')->group(function () {
        Route::get('/prepare/{employeeId}', [\App\Modules\Payroll\Http\Controllers\SalaryProposalController::class, 'prepare'])->name('prepare');
        Route::post('/', [\App\Modules\Payroll\Http\Controllers\SalaryProposalController::class, 'store'])->name('store');
        Route::get('/{id}', [\App\Modules\Payroll\Http\Controllers\SalaryProposalController::class, 'show'])->name('show');
        Route::post('/{id}/approve-step', [\App\Modules\Payroll\Http\Controllers\SalaryProposalController::class, 'approveStep'])->name('approve-step');
        Route::post('/{id}/reject-step', [\App\Modules\Payroll\Http\Controllers\SalaryProposalController::class, 'rejectStep'])->name('reject-step');
    });

    // PHÂN HỆ TÍNH LƯƠNG (PAYROLL ENGINE)
    Route::prefix('payroll')->name('api.payroll.')->group(function () {
        Route::post('/periods', [\App\Modules\Payroll\Http\Controllers\PayrollEngineController::class, 'openPeriod'])->name('periods.open');
        Route::get('/periods', [\App\Modules\Payroll\Http\Controllers\PayrollEngineController::class, 'listPeriods'])->name('periods.list');
        Route::post('/periods/{id}/calculate-batch', [\App\Modules\Payroll\Http\Controllers\PayrollEngineController::class, 'calculateBatch'])->name('periods.calculate-batch');
        Route::get('/periods/{id}/progress', [\App\Modules\Payroll\Http\Controllers\PayrollEngineController::class, 'getProgress'])->name('periods.progress');
        Route::post('/periods/{id}/recalculate-employee', [\App\Modules\Payroll\Http\Controllers\PayrollEngineController::class, 'recalculateEmployee'])->name('periods.recalculate-employee');
        Route::post('/periods/{id}/recalculate-org-unit', [\App\Modules\Payroll\Http\Controllers\PayrollEngineController::class, 'recalculateOrgUnit'])->name('periods.recalculate-org-unit');
        Route::post('/periods/{id}/lock', [\App\Modules\Payroll\Http\Controllers\PayrollEngineController::class, 'lockPeriod'])->name('periods.lock');
        Route::post('/periods/{id}/pay', [\App\Modules\Payroll\Http\Controllers\PayrollEngineController::class, 'payPeriod'])->name('periods.pay');
        Route::get('/payslips/{id}/ess', [\App\Modules\Payroll\Http\Controllers\PayrollEngineController::class, 'getEssPayslip'])->name('payslips.ess');
        Route::get('/periods/{id}/export-bank', [\App\Modules\Payroll\Http\Controllers\PayrollEngineController::class, 'exportBankTransfer'])->name('periods.export-bank');
        Route::get('/periods/{id}/export-insurance', [\App\Modules\Payroll\Http\Controllers\PayrollEngineController::class, 'exportSocialInsurance'])->name('periods.export-insurance');
        Route::get('/periods/{id}/export-erp', [\App\Modules\Payroll\Http\Controllers\PayrollEngineController::class, 'exportErpJournal'])->name('periods.export-erp');
    });
});
