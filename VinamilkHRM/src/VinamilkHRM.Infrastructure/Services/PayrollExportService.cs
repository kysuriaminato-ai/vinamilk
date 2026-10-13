#pragma warning disable CS8602
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Infrastructure.Data;

namespace VinamilkHRM.Infrastructure.Services;

public class PayrollExportService : IPayrollExportService
{
    private readonly VinamilkHRMDbContext _db;
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    public PayrollExportService(VinamilkHRMDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<byte[]> ExportBankPaymentCsvAsync(int payrollRunId, string bankFormat, CancellationToken cancellationToken = default)
    {
        var run = await _db.Set<Domain.Entities.Salaries.PayrollRun>()
            .Include(r => r.PaySlips)
                .ThenInclude(p => p.Employee)
            .FirstOrDefaultAsync(r => r.Id == payrollRunId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy đợt tính lương Id={payrollRunId}.", "ERR_PAY_RUN_NOT_FOUND");

        var sb = new StringBuilder();

        if (string.Equals(bankFormat, "BIDV", StringComparison.OrdinalIgnoreCase))
        {
            // Định dạng BIDV iBank
            sb.AppendLine("STT,MaNV,TenNhanVien,SoTaiKhoan,SoTien,NoiDung");
            int stt = 1;
            foreach (var slip in run.PaySlips.OrderBy(p => p.EmployeeId))
            {
                var emp = slip.Employee;
                string acc = emp?.BankAccountNumber ?? "CHUA_CO_STK";
                string name = emp?.FullName ?? "N/A";
                sb.AppendLine($"{stt++},{emp?.EmployeeCode ?? ""},{name},{acc},{slip.NetSalary:0},Thanh toan luong {run.Period}");
            }
        }
        else
        {
            // Mặc định: Vietcombank VCB-DigiBiz
            sb.AppendLine("STT,SoTaiKhoan,TenNguoiThuHuong,SoTien,NgayHieuLuc,NoiDung");
            int stt = 1;
            string dateStr = DateTime.UtcNow.ToString("dd/MM/yyyy");
            foreach (var slip in run.PaySlips.OrderBy(p => p.EmployeeId))
            {
                var emp = slip.Employee;
                string acc = emp?.BankAccountNumber ?? "CHUA_CO_STK";
                string name = emp?.FullName ?? "N/A";
                sb.AppendLine($"{stt++},{acc},{name},{slip.NetSalary:0},{dateStr},VINAMILK CHI LUONG THANG {run.Period}");
            }
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public async Task<IReadOnlyList<CostCenterJournalEntry>> GenerateCostCenterJournalEntriesAsync(int payrollRunId, CancellationToken cancellationToken = default)
    {
        var run = await _db.Set<Domain.Entities.Salaries.PayrollRun>()
            .Include(r => r.PaySlips)
                .ThenInclude(p => p.Employee)
                    .ThenInclude(e => e.OrganizationalUnit)
            .FirstOrDefaultAsync(r => r.Id == payrollRunId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy đợt tính lương Id={payrollRunId}.", "ERR_PAY_RUN_NOT_FOUND");

        var entries = new List<CostCenterJournalEntry>();

        var grouped = run.PaySlips
            .Where(p => p.Employee != null)
            .GroupBy(p => p.Employee!.OrganizationalUnit)
            .ToList();

        foreach (var group in grouped)
        {
            var unit = group.Key;
            string unitCode = unit?.UnitCode ?? "HQ";
            string unitName = unit?.UnitName ?? "Khối Văn phòng Tập đoàn";

            decimal totalGross = group.Sum(p => p.GrossSalary);
            decimal totalNet = group.Sum(p => p.NetSalary);
            decimal totalEmpIns = group.Sum(p => p.SocialInsuranceDeduction + p.HealthInsuranceDeduction + p.UnemploymentInsuranceDeduction);
            decimal totalPit = group.Sum(p => p.PersonalIncomeTax);

            // Xác định tài khoản chi phí theo loại hình đơn vị:
            // NM / Xưởng / Trang trại: 622 (Nhân công trực tiếp)
            // Bán lẻ / Cửa hàng: 641 (Chi phí bán hàng)
            // Văn phòng / Hành chính: 642 (Chi phí quản lý doanh nghiệp)
            string debitAccount = "642";
            if (unitCode.StartsWith("NM", StringComparison.OrdinalIgnoreCase) || unitCode.StartsWith("FARM", StringComparison.OrdinalIgnoreCase))
            {
                debitAccount = "622";
            }
            else if (unitCode.StartsWith("CH", StringComparison.OrdinalIgnoreCase) || unitCode.StartsWith("STORE", StringComparison.OrdinalIgnoreCase))
            {
                debitAccount = "641";
            }

            // Bút toán 1: Chi phí lương phải trả người lao động (Nợ 622/641/642, Có 334)
            entries.Add(new CostCenterJournalEntry(
                unitCode, unitName, debitAccount, "334", totalGross,
                $"Chi phí lương kỳ {run.Period} - {unitName}"));

            // Bút toán 2: Trích bảo hiểm xã hội, y tế, thất nghiệp trừ lương NLĐ (Nợ 334, Có 3383/3384/3386)
            decimal totalBhxh = group.Sum(p => p.SocialInsuranceDeduction);
            decimal totalBhyt = group.Sum(p => p.HealthInsuranceDeduction);
            decimal totalBhtn = group.Sum(p => p.UnemploymentInsuranceDeduction);

            if (totalBhxh > 0) entries.Add(new CostCenterJournalEntry(unitCode, unitName, "334", "3383", totalBhxh, $"Trích BHXH 8% trừ lương NLĐ {unitName}"));
            if (totalBhyt > 0) entries.Add(new CostCenterJournalEntry(unitCode, unitName, "334", "3384", totalBhyt, $"Trích BHYT 1.5% trừ lương NLĐ {unitName}"));
            if (totalBhtn > 0) entries.Add(new CostCenterJournalEntry(unitCode, unitName, "334", "3386", totalBhtn, $"Trích BHTN 1% trừ lương NLĐ {unitName}"));

            // Bút toán 3: Thuế TNCN khấu trừ tại nguồn (Nợ 334, Có 3335)
            if (totalPit > 0)
            {
                entries.Add(new CostCenterJournalEntry(unitCode, unitName, "334", "3335", totalPit, $"Thuế TNCN khấu trừ kỳ {run.Period} - {unitName}"));
            }

            // Bút toán 4: Chi phí bảo hiểm và kinh phí công đoàn doanh nghiệp chịu (Nợ 622/641/642, Có 3382/3383/3384/3386)
            decimal totalEmployerCost = group.Sum(p => p.EmployerCost);
            decimal employerInsurance = Math.Max(0m, totalEmployerCost - totalGross);
            if (employerInsurance > 0)
            {
                entries.Add(new CostCenterJournalEntry(
                    unitCode, unitName, debitAccount, "338", employerInsurance,
                    $"Trích nộp BHXH/BHYT/BHTN/KPCĐ (23.5%) NSDLĐ gánh chịu - {unitName}"));
            }
        }

        return entries;
    }

    public async Task<IReadOnlyList<UnitSalaryFundItem>> GenerateUnitSalaryFundReportAsync(string period, CancellationToken cancellationToken = default)
    {
        var slips = await _db.PaySlips
            .AsNoTracking()
            .Include(p => p.Employee)
                .ThenInclude(e => e.OrganizationalUnit)
            .Where(p => p.PayrollPeriod == period)
            .ToListAsync(cancellationToken);

        var report = slips
            .GroupBy(p => p.Employee?.OrganizationalUnit)
            .Select(g =>
            {
                var unit = g.Key;
                return new UnitSalaryFundItem(
                    unit?.Id ?? 0,
                    unit?.UnitCode ?? "UNKNOWN",
                    unit?.UnitName ?? "Chưa phân bổ",
                    g.Count(),
                    g.Sum(p => p.GrossSalary),
                    g.Sum(p => p.NetSalary),
                    g.Sum(p => p.EmployerCost > 0 ? p.EmployerCost : p.GrossSalary * 1.235m),
                    g.Sum(p => p.PersonalIncomeTax));
            })
            .OrderBy(u => u.UnitCode)
            .ToList();

        return report;
    }

    public async Task<IReadOnlyList<SocialInsuranceD02LtItem>> GenerateSocialInsuranceD02LtAsync(string period, CancellationToken cancellationToken = default)
    {
        var slips = await _db.PaySlips
            .AsNoTracking()
            .Include(p => p.Employee)
            .Where(p => p.PayrollPeriod == period)
            .ToListAsync(cancellationToken);

        var list = new List<SocialInsuranceD02LtItem>();
        int stt = 1;

        foreach (var slip in slips)
        {
            var emp = slip.Employee;
            if (emp == null) continue;

            // Kiểm tra tăng mới hoặc thay đổi
            list.Add(new SocialInsuranceD02LtItem(
                stt++,
                emp.EmployeeCode,
                emp.FullName,
                emp.CitizenId, // dùng mã số BHXH / CCCD
                "DIEU_CHINH_DANG_DONG",
                emp.InsuranceSalary,
                slip.BaseSalary + slip.Allowance,
                $"{period}-01",
                "Kỳ tính bảo hiểm định kỳ"));
        }

        return list;
    }

    public async Task<IReadOnlyList<AnnualPitFinalization05QttItem>> GenerateAnnualPitFinalizationAsync(int year, CancellationToken cancellationToken = default)
    {
        string yearPrefix = $"{year}-";
        var yearSlips = await _db.PaySlips
            .AsNoTracking()
            .Include(p => p.Employee)
            .Where(p => p.PayrollPeriod.StartsWith(yearPrefix))
            .ToListAsync(cancellationToken);

        var report = yearSlips
            .Where(p => p.Employee != null).GroupBy(p => p.Employee!)
            .Select((g, index) =>
            {
                var emp = g.Key;
                decimal totalGross = g.Sum(p => p.GrossSalary);
                decimal totalInsurance = g.Sum(p => p.SocialInsuranceDeduction + p.HealthInsuranceDeduction + p.UnemploymentInsuranceDeduction);
                decimal totalSelfRelief = g.Sum(p => p.PersonalDeduction);
                decimal totalDepRelief = g.Sum(p => p.DependentDeduction);
                decimal totalTaxable = Math.Max(0m, totalGross - totalInsurance - totalSelfRelief - totalDepRelief);
                decimal totalTaxDeducted = g.Sum(p => p.PersonalIncomeTax);

                return new AnnualPitFinalization05QttItem(
                    index + 1,
                    emp?.EmployeeCode ?? "N/A",
                    emp?.FullName ?? "N/A",
                    emp?.TaxCode ?? "CHUA_CO",
                    totalGross,
                    0m,
                    totalInsurance,
                    totalSelfRelief,
                    totalDepRelief,
                    totalTaxable,
                    totalTaxDeducted,
                    0m);
            })
            .ToList();

        return report;
    }
}


