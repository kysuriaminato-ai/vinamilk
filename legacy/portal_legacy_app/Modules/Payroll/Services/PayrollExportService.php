<?php

namespace App\Modules\Payroll\Services;

use App\Modules\Payroll\Models\PayrollPeriod;
use App\Modules\Payroll\Models\Payslip;
use Brick\Math\BigDecimal;
use Brick\Math\RoundingMode;

class PayrollExportService
{
    /**
     * 1. XUẤT DỮ LIỆU PHIẾU LƯƠNG ĐIỆN TỬ (ESS PAYSLIP VIEW/PDF)
     */
    public function generateEssPayslipData(Payslip $payslip): array
    {
        $payslip->loadMissing(['employee', 'orgUnit', 'period', 'lines']);
        $employee = $payslip->employee;
        $period = $payslip->period;

        $linesGrouped = $payslip->lines->groupBy('line_type');

        return [
            'company'          => 'CÔNG TY CỔ PHẦN SỮA VIỆT NAM (VINAMILK)',
            'period_code'      => $period->period_code,
            'period_title'     => "Kỳ lương Tháng {$period->month} Năm {$period->year}",
            'employee_code'    => $employee->employee_code,
            'full_name'        => $employee->full_name,
            'position'         => $employee->current_position?->position_name ?? 'Nhân viên',
            'department'       => $payslip->orgUnit?->unit_name ?? 'Vinamilk',
            'cost_center'      => $payslip->cost_center_code,
            'bank_account'     => $employee->bank_account_number,
            'bank_name'        => $employee->bank_name,
            'standard_days'    => (float)$period->standard_working_days,
            'actual_days'      => (float)$payslip->actual_working_days,
            'paid_leave_days'  => (float)$payslip->paid_leave_days,
            'summary'          => [
                'p1_position'           => (float)$payslip->p1_position_amount,
                'p2_competency'         => (float)$payslip->p2_competency_amount,
                'p3_performance'        => (float)$payslip->p3_performance_amount,
                'ot_pay'                => (float)$payslip->ot_pay_amount,
                'total_allowances'      => (float)$payslip->total_allowance_amount,
                'retroactive_amount'    => (float)$payslip->retroactive_amount,
                'gross_income'          => (float)$payslip->gross_income,
                'social_insurance_emp'  => (float)$payslip->social_insurance_emp,
                'health_insurance_emp'  => (float)$payslip->health_insurance_emp,
                'unemployment_ins_emp'  => (float)$payslip->unemployment_insurance_emp,
                'union_fee_emp'         => (float)$payslip->union_fee_emp,
                'party_fee_emp'         => (float)$payslip->party_fee_emp,
                'personal_income_tax'   => (float)$payslip->personal_income_tax,
                'other_deductions'      => (float)$payslip->other_deductions,
                'total_deductions'      => (float)($payslip->gross_income - $payslip->net_income),
                'net_income'            => (float)$payslip->net_income,
            ],
            'detailed_lines'   => [
                'earnings'              => $linesGrouped->get('EARNING', collect())->toArray(),
                'allowances'            => $linesGrouped->get('ALLOWANCE', collect())->toArray(),
                'bonuses'               => $linesGrouped->get('BONUS', collect())->toArray(),
                'statutory_deductions'  => $linesGrouped->get('STATUTORY_DEDUCTION', collect())->toArray(),
                'tax_deductions'        => $linesGrouped->get('TAX_DEDUCTION', collect())->toArray(),
                'employer_contributions'=> $linesGrouped->get('EMPLOYER_CONTRIBUTION', collect())->toArray(),
            ],
        ];
    }

    /**
     * 2. XUẤT FILE CHUYỂN KHOẢN NGÂN HÀNG (BANK TRANSFER CSV VIETCOMBANK / BIDV)
     */
    public function exportBankTransferCsv(PayrollPeriod $period): string
    {
        $payslips = Payslip::with(['employee'])
            ->where('payroll_period_id', $period->id)
            ->where('net_income', '>', 0)
            ->get();

        $rows = [];
        // Unicode UTF-8 BOM để Excel hiển thị đúng tiếng Việt không bị lỗi font
        $output = "\xEF\xBB\xBF";

        // Header CSV
        $rows[] = [
            'STT',
            'MA_NV',
            'HO_VA_TEN',
            'SO_TAI_KHOAN',
            'TEN_NGAN_HANG',
            'CHI_NHANH',
            'SO_TIEN_NET_VND',
            'NOI_DUNG_THANH_TOAN'
        ];

        $stt = 1;
        $totalAmount = BigDecimal::zero();

        foreach ($payslips as $slip) {
            $emp = $slip->employee;
            $net = BigDecimal::of((string)$slip->net_income);
            $totalAmount = $totalAmount->plus($net);

            $rows[] = [
                $stt++,
                $emp->employee_code,
                $emp->full_name,
                $emp->bank_account_number ?? 'CHUA_DANG_KY',
                $emp->bank_name ?? 'Vietcombank',
                $emp->bank_branch ?? 'Hoi So Chinh',
                (string)$slip->net_income,
                "VINAMILK THANH TOAN LUONG KY {$period->month}/{$period->year} NV {$emp->employee_code}"
            ];
        }

        foreach ($rows as $row) {
            $output .= implode(',', array_map(fn ($val) => '"' . str_replace('"', '""', $val) . '"', $row)) . "\r\n";
        }

        return $output;
    }

    /**
     * 3. XUẤT BÁO CÁO BẢO HIỂM XÃ HỘI (MẪU D02-LT BHXH VIỆT NAM)
     */
    public function exportSocialInsuranceD02Lt(PayrollPeriod $period): array
    {
        $payslips = Payslip::with(['employee.currentSalary', 'employee.currentAssignment.position'])
            ->where('payroll_period_id', $period->id)
            ->get();

        $items = [];
        $totalSiHiEmp = BigDecimal::zero();
        $totalSiHiComp = BigDecimal::zero();
        $stt = 1;

        foreach ($payslips as $slip) {
            $emp = $slip->employee;
            $salary = $emp->currentSalary;
            $insSalary = (float)($salary?->insurance_salary ?? 0);

            $siEmp = (float)$slip->social_insurance_emp;
            $hiEmp = (float)$slip->health_insurance_emp;
            $uiEmp = (float)$slip->unemployment_insurance_emp;
            $totalEmp = $siEmp + $hiEmp + $uiEmp;

            $siComp = (float)$slip->social_insurance_comp;
            $hiComp = (float)$slip->health_insurance_comp;
            $uiComp = (float)$slip->unemployment_insurance_comp;
            $totalComp = $siComp + $hiComp + $uiComp;

            $grandTotal = $totalEmp + $totalComp;

            $items[] = [
                'stt'                      => $stt++,
                'employee_code'            => $emp->employee_code,
                'full_name'                => $emp->full_name,
                'social_insurance_number'  => $emp->social_insurance_number ?? 'CHUA_CAP_SO',
                'position_name'            => $emp->current_position?->position_name ?? 'Nhân viên',
                'insurance_salary'         => $insSalary,
                'si_emp'                   => $siEmp,
                'hi_emp'                   => $hiEmp,
                'ui_emp'                   => $uiEmp,
                'total_emp_deduction'      => $totalEmp,
                'si_comp'                  => $siComp,
                'hi_comp'                  => $hiComp,
                'ui_comp'                  => $uiComp,
                'total_comp_contribution'  => $totalComp,
                'grand_total_to_bhxh'      => $grandTotal,
            ];
        }

        return [
            'report_name'   => 'DANH SÁCH LAO ĐỘNG THAM GIA BHXH, BHYT, BHTN (MẪU D02-LT)',
            'company_name'  => 'CÔNG TY CỔ PHẦN SỮA VIỆT NAM (VINAMILK)',
            'period'        => "Tháng {$period->month}/{$period->year}",
            'data'          => $items,
        ];
    }

    /**
     * 4. XUẤT FILE HẠCH TOÁN CHO ERP KẾ TOÁN (SAP FI/CO JOURNAL ENTRIES)
     */
    public function exportErpJournalEntries(PayrollPeriod $period): array
    {
        $payslips = Payslip::with(['orgUnit'])
            ->where('payroll_period_id', $period->id)
            ->get();

        // Gom nhóm bút toán theo từng Cost Center của đơn vị tổ chức
        $entries = [];
        $costCenterGroups = $payslips->groupBy(fn ($item) => $item->cost_center_code ?? 'CC-GENERAL');

        foreach ($costCenterGroups as $costCenter => $slips) {
            $totalGross = $slips->sum('gross_income');
            $totalSiEmp = $slips->sum('social_insurance_emp');
            $totalHiEmp = $slips->sum('health_insurance_emp');
            $totalUiEmp = $slips->sum('unemployment_insurance_emp');
            $totalUnionEmp = $slips->sum('union_fee_emp');
            $totalPit = $slips->sum('personal_income_tax');
            $totalNet = $slips->sum('net_income');

            $totalSiComp = $slips->sum('social_insurance_comp');
            $totalHiComp = $slips->sum('health_insurance_comp');
            $totalUiComp = $slips->sum('unemployment_insurance_comp');
            $totalUnionComp = $slips->sum('union_fee_comp');

            // Xác định tài khoản chi phí theo loại hình đơn vị:
            // 622: Chi phí nhân công trực tiếp (Nhà máy, Trang trại)
            // 6271: Chi phí SX chung (Tổ, Xưởng)
            // 6411: Chi phí bán hàng (Chi nhánh, Điểm bán)
            // 6421: Chi phí quản lý (Trụ sở, Khối)
            $expenseAccount = '6421';
            if (str_contains($costCenter, 'FACTORY') || str_contains($costCenter, 'PLANT')) {
                $expenseAccount = '6221';
            } elseif (str_contains($costCenter, 'FARM')) {
                $expenseAccount = '6222';
            } elseif (str_contains($costCenter, 'SALES')) {
                $expenseAccount = '6411';
            }

            // 1. Ghi nhận Chi phí lương phải trả NLĐ: Nợ 622/627/641/642 | Có 3341
            $entries[] = [
                'document_type' => 'SA', // G/L Account Document (SAP)
                'cost_center'   => $costCenter,
                'debit_account' => $expenseAccount,
                'credit_account'=> '3341',
                'amount'        => (float)$totalGross,
                'text'          => "Hạch toán chi phí tiền lương kỳ {$period->period_code} CC: {$costCenter}",
            ];

            // 2. Khấu trừ BHXH NLĐ (8%): Nợ 3341 | Có 3383
            if ($totalSiEmp > 0) {
                $entries[] = [
                    'document_type' => 'SA',
                    'cost_center'   => $costCenter,
                    'debit_account' => '3341',
                    'credit_account'=> '3383',
                    'amount'        => (float)$totalSiEmp,
                    'text'          => "Khấu trừ BHXH NLĐ (8%) kỳ {$period->period_code}",
                ];
            }

            // 3. Khấu trừ BHYT NLĐ (1.5%): Nợ 3341 | Có 3384
            if ($totalHiEmp > 0) {
                $entries[] = [
                    'document_type' => 'SA',
                    'cost_center'   => $costCenter,
                    'debit_account' => '3341',
                    'credit_account'=> '3384',
                    'amount'        => (float)$totalHiEmp,
                    'text'          => "Khấu trừ BHYT NLĐ (1.5%) kỳ {$period->period_code}",
                ];
            }

            // 4. Khấu trừ BHTN NLĐ (1%): Nợ 3341 | Có 3386
            if ($totalUiEmp > 0) {
                $entries[] = [
                    'document_type' => 'SA',
                    'cost_center'   => $costCenter,
                    'debit_account' => '3341',
                    'credit_account'=> '3386',
                    'amount'        => (float)$totalUiEmp,
                    'text'          => "Khấu trừ BHTN NLĐ (1%) kỳ {$period->period_code}",
                ];
            }

            // 5. Khấu trừ Đoàn phí Công đoàn NLĐ (1%): Nợ 3341 | Có 3382
            if ($totalUnionEmp > 0) {
                $entries[] = [
                    'document_type' => 'SA',
                    'cost_center'   => $costCenter,
                    'debit_account' => '3341',
                    'credit_account'=> '3382',
                    'amount'        => (float)$totalUnionEmp,
                    'text'          => "Khấu trừ Đoàn phí Công đoàn kỳ {$period->period_code}",
                ];
            }

            // 6. Khấu trừ Thuế TNCN: Nợ 3341 | Có 3335
            if ($totalPit > 0) {
                $entries[] = [
                    'document_type' => 'SA',
                    'cost_center'   => $costCenter,
                    'debit_account' => '3341',
                    'credit_account'=> '3335',
                    'amount'        => (float)$totalPit,
                    'text'          => "Khấu trừ thuế TNCN kỳ {$period->period_code}",
                ];
            }

            // 7. Chi phí trích nộp theo lương DN chịu (BHXH 17.5%, BHYT 3%, BHTN 1%, KPCĐ 2%):
            // Nợ 622/641/642 | Có 3383, 3384, 3386, 3382
            if ($totalSiComp > 0) {
                $entries[] = [
                    'document_type' => 'SA',
                    'cost_center'   => $costCenter,
                    'debit_account' => $expenseAccount,
                    'credit_account'=> '3383',
                    'amount'        => (float)$totalSiComp,
                    'text'          => "Chi phí BHXH phần DN (17.5%) kỳ {$period->period_code}",
                ];
            }

            if ($totalHiComp > 0) {
                $entries[] = [
                    'document_type' => 'SA',
                    'cost_center'   => $costCenter,
                    'debit_account' => $expenseAccount,
                    'credit_account'=> '3384',
                    'amount'        => (float)$totalHiComp,
                    'text'          => "Chi phí BHYT phần DN (3%) kỳ {$period->period_code}",
                ];
            }

            if ($totalUiComp > 0) {
                $entries[] = [
                    'document_type' => 'SA',
                    'cost_center'   => $costCenter,
                    'debit_account' => $expenseAccount,
                    'credit_account'=> '3386',
                    'amount'        => (float)$totalUiComp,
                    'text'          => "Chi phí BHTN phần DN (1%) kỳ {$period->period_code}",
                ];
            }

            if ($totalUnionComp > 0) {
                $entries[] = [
                    'document_type' => 'SA',
                    'cost_center'   => $costCenter,
                    'debit_account' => $expenseAccount,
                    'credit_account'=> '3382',
                    'amount'        => (float)$totalUnionComp,
                    'text'          => "Kinh phí Công đoàn DN nộp (2%) kỳ {$period->period_code}",
                ];
            }

            // 8. Bút toán thanh toán lương thực lĩnh: Nợ 3341 | Có 1121 (Ngân hàng)
            if ($totalNet > 0) {
                $entries[] = [
                    'document_type' => 'KZ', // Vendor/Payroll Payment (SAP)
                    'cost_center'   => $costCenter,
                    'debit_account' => '3341',
                    'credit_account'=> '1121',
                    'amount'        => (float)$totalNet,
                    'text'          => "Chi lương thực lĩnh chuyển khoản kỳ {$period->period_code}",
                ];
            }
        }

        return [
            'erp_system'      => 'SAP S/4HANA FI/CO Integration',
            'journal_entries' => $entries,
            'total_debit'     => collect($entries)->sum('amount'),
            'total_credit'    => collect($entries)->sum('amount'),
        ];
    }
}
