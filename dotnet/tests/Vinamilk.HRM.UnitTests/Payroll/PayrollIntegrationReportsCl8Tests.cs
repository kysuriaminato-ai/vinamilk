using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Vinamilk.HRM.Application.Analytics;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Payroll;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Identity;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.Time;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Persistence.Seed;
using Vinamilk.HRM.Infrastructure.Security;
using Vinamilk.HRM.Infrastructure.Services;
using Xunit;

namespace Vinamilk.HRM.UnitTests.Payroll;

/// <summary>
/// Kiểm thử tích hợp toàn diện PHIÊN CL8 (tính tay độc lập bằng số liệu cụ thể trong comment):
/// 1. Đối chiếu chéo 100% đến từng đồng giữa: Bảng lương chi tiết đơn vị (A3 PDF) == Báo cáo Cost Center == File Bank Transfer == Bút toán Kế toán (TK 3341 & Nợ == Có).
/// 2. Tổng hợp quỹ lương Tháng/Quý/Năm không tính trùng (double-count) đợt Tạm ứng giữa tháng & Lương cuối tháng; so sánh biến động giữa hai kỳ.
/// 3. Báo cáo BHXH D02-LT (Tăng mới / Giảm nghỉ / Điều chỉnh mức đóng), đối chiếu số phải nộp = 0 lệch, và Template Excel cấu hình động trong DB.
/// 4. Thuế TNCN: Tờ khai 05/KK-TNCN, Quyết toán năm 05/QTT-TNCN, cấp Chứng từ khấu trừ thuế PDF & xuất Excel 05-1/05-2/QTT.
/// 5. Dashboard C&B: cơ cấu thu nhập, tỷ lệ OT/Gross, chi phí BH, Top biến động kỳ.
/// 6. Cổng ESS mở rộng: bảng chấm công chi tiết từng ngày, chi tiết quỹ phép, lịch sử phiếu lương & tự tải chứng từ thuế TNCN (kèm kiểm tra quyền Self-Only).
/// </summary>
public class PayrollIntegrationReportsCl8Tests
{
    private sealed class StubCurrentUser : ICurrentUserService
    {
        public int? UserId { get; init; }
        public string Username { get; init; } = "cb.tester";
        public int? EmployeeId { get; init; }
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "xUnit-CL8";
        public bool IsAuthenticated => true;
        public bool AllowAllPermissions { get; init; } = true;
        public bool HasPermission(string permissionCode) => AllowAllPermissions;
    }

    private static (PayrollDocumentService Docs, IPiiEncryptionService Pii) CreateServices()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:PiiEncryptionKey"] = "UnitTest_SecretKey_32Chars_Alpha!",
                ["QuestPDF:LicenseType"] = "Community"
            })
            .Build();
        var encryption = new PiiEncryptionService(config);
        return (new PayrollDocumentService(encryption, config), encryption);
    }

    private static async Task<ApplicationDbContext> CreateSeededDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VnmPayrollCl8_{Guid.NewGuid():N}")
            .Options;
        var db = new ApplicationDbContext(options);

        await db.PayrollStatutoryParameters.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultStatutoryParameters());
        await db.PitBrackets.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultPitBrackets());
        await db.PayrollParameters.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultTaxExemptPayrollParameters());
        await db.PayrollAccountingAccountConfigs.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultAccountingConfigs());
        await db.SocialInsuranceReportTemplates.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultSocialInsuranceTemplates());

        var defaultGroup = ApplicationDbContextInitializer.BuildDefaultCompanyPolicyGroup();
        defaultGroup.Id = 1;
        await db.PolicyGroups.AddAsync(defaultGroup);

        db.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, UnitCode = "NM_BS", UnitName = "Nhà máy Sữa Bình Sơn", CostCenterCode = "NM_BS", WageRegion = 1, IsActive = true },
            new OrganizationUnit { Id = 2, UnitCode = "HO_FIN", UnitName = "Khối Tài chính Kế toán", CostCenterCode = "HO_FIN", WageRegion = 1, IsActive = true });

        db.Positions.AddRange(
            new Position { Id = 1, PositionCode = "SX", PositionName = "Kỹ sư Vận hành", PositionLevel = 5, IsActive = true },
            new Position { Id = 2, PositionCode = "KT", PositionName = "Chuyên viên Kế toán", PositionLevel = 5, IsActive = true });

        db.Roles.Add(new Role { Id = 1, RoleCode = "EMPLOYEE", RoleName = "Nhân viên", IsSystemRole = true });
        await db.SaveChangesAsync();
        return db;
    }

    // =========================================================================
    // TEST 01: ĐỐI CHIẾU CHÉO 100%: A3 PDF == COST CENTER == BANK TRANSFER == HẠCH TOÁN KẾ TOÁN 3341
    // =========================================================================
    [Fact]
    public async Task FullPayrollReports_ReconcileToSingleDong_BetweenUnitA3Pdf_CostCenter_BankTransfer_AndAccountingJournal334()
    {
        using var db = await CreateSeededDbContextAsync();
        var (docs, pii) = CreateServices();

        var e1 = new Employee { Id = 1, EmployeeCode = "VNM001", FullName = "Nguyễn Văn A", OrganizationUnitId = 1, PositionId = 1, WorkStatus = "Active", HireDate = new DateOnly(2023, 1, 10), BankName = "Vietcombank", BankAccountEncrypted = pii.Encrypt("0071001112223") };
        var e2 = new Employee { Id = 2, EmployeeCode = "VNM002", FullName = "Trần Thị B", OrganizationUnitId = 1, PositionId = 1, WorkStatus = "Active", HireDate = new DateOnly(2023, 5, 10), BankName = "VietinBank", BankAccountEncrypted = pii.Encrypt("1010004445556") };
        var e3 = new Employee { Id = 3, EmployeeCode = "VNM003", FullName = "Lê Minh C", OrganizationUnitId = 2, PositionId = 2, WorkStatus = "Active", HireDate = new DateOnly(2022, 3, 15), BankName = "Techcombank", BankAccountEncrypted = pii.Encrypt("1903338889990") };
        db.Employees.AddRange(e1, e2, e3);

        var period = new PayrollPeriod
        {
            Id = 10,
            PeriodCode = "BL-2026-06",
            PeriodTitle = "Bảng lương tháng 06/2026",
            Year = 2026,
            Month = 6,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 6, 30),
            DisbursementDate = new DateOnly(2026, 7, 5),
            StandardWorkdays = 26m,
            PeriodType = "Monthly",
            Status = "Locked"
        };
        db.PayrollPeriods.Add(period);

        // Tính tay:
        // E1 (NM_BS): Gross = 20,000,000 | BH_NLD = 2,100,000 (SI 1.6M, HI 300k, UI 200k) | CD_NLD = 100,000 | PIT = 500,000 | Adv = 2,000,000 | Loan = 500,000
        //    Net E1 = 20,000,000 - 2,100,000 - 100,000 - 500,000 - 2,000,000 - 500,000 = 14,800,000
        //    DN E1: SI 3,400,000 + HI 600,000 + UI 200,000 + OA 100,000 + CD 400,000 = 4,700,000 -> CompCost = 24,700,000
        // E2 (NM_BS): Gross = 15,000,000 | BH_NLD = 1,575,000 | PIT = 200,000 -> Net E2 = 13,225,000 | CompCost = 18,525,000
        // E3 (HO_FIN): Gross = 30,000,000 | BH_NLD = 3,150,000 | CD_NLD = 150,000 | PIT = 1,800,000 -> Net E3 = 24,900,000 | CompCost = 37,050,000
        // TỔNG TOÀN KỲ: Gross = 65,000,000 | Net = 52,925,000 | CompCost = 80,275,000
        db.PayrollRecords.AddRange(
            new PayrollRecord
            {
                Id = 101, PayrollPeriodId = 10, EmployeeId = 1, OrganizationUnitId = 1, PositionId = 1,
                StandardWorkdays = 26m, PayableWorkdays = 26m, ContractBaseSalary = 20_000_000m, ActualBaseSalary = 20_000_000m,
                GrossIncome = 20_000_000m, InsuranceSalaryBase = 20_000_000m, UnemploymentInsuranceBase = 20_000_000m,
                EmployeeSocialInsurance = 1_600_000m, EmployeeHealthInsurance = 300_000m, EmployeeUnemploymentInsurance = 200_000m, TotalEmployeeInsurance = 2_100_000m,
                EmployeeUnionFee = 100_000m, PersonalIncomeTax = 500_000m, SalaryAdvanceDeduction = 2_000_000m, LoanInstallmentDeduction = 500_000m,
                NetSalary = 14_800_000m,
                CompanySocialInsurance = 3_400_000m, CompanyHealthInsurance = 600_000m, CompanyUnemploymentInsurance = 200_000m, CompanyOccupationalAccidentInsurance = 100_000m, CompanyUnionFee = 400_000m,
                TotalCompanyCost = 24_700_000m
            },
            new PayrollRecord
            {
                Id = 102, PayrollPeriodId = 10, EmployeeId = 2, OrganizationUnitId = 1, PositionId = 1,
                StandardWorkdays = 26m, PayableWorkdays = 26m, ContractBaseSalary = 15_000_000m, ActualBaseSalary = 15_000_000m,
                GrossIncome = 15_000_000m, InsuranceSalaryBase = 15_000_000m, UnemploymentInsuranceBase = 15_000_000m,
                EmployeeSocialInsurance = 1_200_000m, EmployeeHealthInsurance = 225_000m, EmployeeUnemploymentInsurance = 150_000m, TotalEmployeeInsurance = 1_575_000m,
                PersonalIncomeTax = 200_000m, NetSalary = 13_225_000m,
                CompanySocialInsurance = 2_550_000m, CompanyHealthInsurance = 450_000m, CompanyUnemploymentInsurance = 150_000m, CompanyOccupationalAccidentInsurance = 75_000m, CompanyUnionFee = 300_000m,
                TotalCompanyCost = 18_525_000m
            },
            new PayrollRecord
            {
                Id = 103, PayrollPeriodId = 10, EmployeeId = 3, OrganizationUnitId = 2, PositionId = 2,
                StandardWorkdays = 26m, PayableWorkdays = 26m, ContractBaseSalary = 30_000_000m, ActualBaseSalary = 30_000_000m,
                GrossIncome = 30_000_000m, InsuranceSalaryBase = 30_000_000m, UnemploymentInsuranceBase = 30_000_000m,
                EmployeeSocialInsurance = 2_400_000m, EmployeeHealthInsurance = 450_000m, EmployeeUnemploymentInsurance = 300_000m, TotalEmployeeInsurance = 3_150_000m,
                EmployeeUnionFee = 150_000m, PersonalIncomeTax = 1_800_000m, NetSalary = 24_900_000m,
                CompanySocialInsurance = 5_100_000m, CompanyHealthInsurance = 900_000m, CompanyUnemploymentInsurance = 300_000m, CompanyOccupationalAccidentInsurance = 150_000m, CompanyUnionFee = 600_000m,
                TotalCompanyCost = 37_050_000m
            });
        await db.SaveChangesAsync();

        var unitReport = await new GetDetailedUnitPayrollReportQueryHandler(db)
            .Handle(new GetDetailedUnitPayrollReportQuery(10), CancellationToken.None);
        var costCenter = await new GetCostCenterLaborCostQueryHandler(db)
            .Handle(new GetCostCenterLaborCostQuery(10), CancellationToken.None);
        var bankExport = await new ExportBankTransferFileQueryHandler(db, pii, new IBankTransferTemplateFormatter[] { new CommonBankTransferTemplateFormatter() })
            .Handle(new ExportBankTransferFileQuery(10, "excel", "COMMON"), CancellationToken.None);
        var journal = await new GeneratePayrollAccountingEntriesQueryHandler(db)
            .Handle(new GeneratePayrollAccountingEntriesQuery(10), CancellationToken.None);
        var unitPdf = await new ExportDetailedUnitPayrollA3PdfQueryHandler(db, docs)
            .Handle(new ExportDetailedUnitPayrollA3PdfQuery(10), CancellationToken.None);
        var journalCsv = await new ExportAccountingJournalQueryHandler(db, docs)
            .Handle(new ExportAccountingJournalQuery(10, "csv"), CancellationToken.None);

        unitReport.GrandTotalGross.Should().Be(65_000_000m);
        unitReport.GrandTotalNet.Should().Be(52_925_000m);
        unitReport.GrandTotalCompanyCost.Should().Be(80_275_000m);
        unitPdf.PdfBytes.Take(4).Should().BeEquivalentTo(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // %PDF

        costCenter.GrandTotalGross.Should().Be(unitReport.GrandTotalGross);
        costCenter.GrandTotalNet.Should().Be(unitReport.GrandTotalNet);
        costCenter.GrandTotalCompanyCost.Should().Be(unitReport.GrandTotalCompanyCost);

        bankExport.TotalAmount.Should().Be(52_925_000m);

        journal.IsBalanced.Should().BeTrue();
        journal.TotalDebit.Should().Be(journal.TotalCredit);
        journal.PayableToEmployeesAccount334.Should().Be(52_925_000m);
        journal.Lines.Should().Contain(l => l.CostCenterCode == "NM_BS" && l.DebitAccountCode == "6221" && l.Amount == 35_000_000m);
        journal.Lines.Should().Contain(l => l.CostCenterCode == "HO_FIN" && l.DebitAccountCode == "6421" && l.Amount == 30_000_000m);
        journalCsv.FileBytes.Length.Should().BeGreaterThan(100);
    }

    // =========================================================================
    // TEST 02: QUỸ LƯƠNG NĂM KHÔNG TÍNH TRÙNG TẠM ỨNG GIỮA THÁNG + SO SÁNH KỲ
    // =========================================================================
    [Fact]
    public async Task PayrollFundSummary_And_PeriodComparison_PreventMidMonthAdvanceDoubleCount()
    {
        using var db = await CreateSeededDbContextAsync();
        db.Employees.Add(new Employee { Id = 1, EmployeeCode = "VNM001", FullName = "NV A", OrganizationUnitId = 1, PositionId = 1, WorkStatus = "Active", HireDate = new DateOnly(2023, 1, 1) });

        db.PayrollPeriods.AddRange(
            new PayrollPeriod { Id = 1, PeriodCode = "BL-2026-04", PeriodTitle = "T04", Year = 2026, Month = 4, PeriodType = "Monthly", Status = "Paid", StartDate = new DateOnly(2026, 4, 1), EndDate = new DateOnly(2026, 4, 30) },
            new PayrollPeriod { Id = 2, PeriodCode = "TU-2026-05", PeriodTitle = "Tạm ứng T05", Year = 2026, Month = 5, PeriodType = "MidMonthAdvance", Status = "Paid", StartDate = new DateOnly(2026, 5, 1), EndDate = new DateOnly(2026, 5, 15) },
            new PayrollPeriod { Id = 3, PeriodCode = "BL-2026-05", PeriodTitle = "Lương T05", Year = 2026, Month = 5, PeriodType = "Monthly", Status = "Approved", StartDate = new DateOnly(2026, 5, 1), EndDate = new DateOnly(2026, 5, 31) });

        db.PayrollRecords.AddRange(
            new PayrollRecord { Id = 1, PayrollPeriodId = 1, EmployeeId = 1, OrganizationUnitId = 1, PositionId = 1, GrossIncome = 40_000_000m, NetSalary = 35_000_000m, TotalCompanyCost = 48_000_000m },
            // Tạm ứng giữa tháng: thực chi 10M (Gross tạm ghi 10M ở batch tạm ứng nhưng không cộng trùng vào Gross tháng)
            new PayrollRecord { Id = 2, PayrollPeriodId = 2, EmployeeId = 1, OrganizationUnitId = 1, PositionId = 1, GrossIncome = 10_000_000m, NetSalary = 10_000_000m, TotalCompanyCost = 10_000_000m },
            // Lương chính T05: Gross 45M, đã trừ tạm ứng 10M nên Net cuối tháng còn 29M
            new PayrollRecord { Id = 3, PayrollPeriodId = 3, EmployeeId = 1, OrganizationUnitId = 1, PositionId = 1, GrossIncome = 45_000_000m, SalaryAdvanceDeduction = 10_000_000m, NetSalary = 29_000_000m, TotalCompanyCost = 54_000_000m });
        await db.SaveChangesAsync();

        var fund = await new GetPayrollFundSummaryQueryHandler(db)
            .Handle(new GetPayrollFundSummaryQuery(2026), CancellationToken.None);

        var may = fund.MonthlyBuckets.First(b => b.Month == 5);
        may.TotalGross.Should().Be(45_000_000m);               // Không bị 55M!
        may.TotalAdvanceDisbursedInMonth.Should().Be(10_000_000m);
        may.TotalNetSalaryDisbursed.Should().Be(39_000_000m);   // 10M đợt 1 + 29M đợt 2 = 39M
        may.TotalCompanyCost.Should().Be(54_000_000m);

        fund.YearlyTotalGross.Should().Be(85_000_000m);
        fund.YearlyTotalNetSalaryDisbursed.Should().Be(74_000_000m);

        var cmp = await new ComparePayrollPeriodsQueryHandler(db)
            .Handle(new ComparePayrollPeriodsQuery(3, 1), CancellationToken.None);
        cmp.GrossVariance.Should().Be(5_000_000m);
        cmp.GrossVariancePercent.Should().Be(12.5m);
    }

    // =========================================================================
    // TEST 03: BHXH D02-LT (TĂNG/GIẢM/ĐIỀU CHỈNH), ĐỐI CHIẾU = 0 & TEMPLATE ĐỘNG
    // =========================================================================
    [Fact]
    public async Task SocialInsuranceReport_DetectsMovements_ReconcilesToZero_AndSupportsDynamicColumnTemplate()
    {
        using var db = await CreateSeededDbContextAsync();
        var (docs, _) = CreateServices();
        var user = new StubCurrentUser { UserId = 9, Username = "hr.si" };

        db.Employees.AddRange(
            new Employee { Id = 1, EmployeeCode = "NV01", FullName = "Không Đổi", SocialInsuranceNumber = "7910000001", OrganizationUnitId = 1, PositionId = 1, WorkStatus = "Active", HireDate = new DateOnly(2023, 1, 1) },
            new Employee { Id = 2, EmployeeCode = "NV02", FullName = "Tăng Lương", SocialInsuranceNumber = "7910000002", OrganizationUnitId = 1, PositionId = 1, WorkStatus = "Active", HireDate = new DateOnly(2023, 1, 1) },
            new Employee { Id = 3, EmployeeCode = "NV03", FullName = "Nghỉ Việc", SocialInsuranceNumber = "7910000003", OrganizationUnitId = 1, PositionId = 1, WorkStatus = "Terminated", HireDate = new DateOnly(2023, 1, 1) },
            new Employee { Id = 4, EmployeeCode = "NV04", FullName = "Tuyển Mới", SocialInsuranceNumber = "7910000004", OrganizationUnitId = 2, PositionId = 2, WorkStatus = "Active", HireDate = new DateOnly(2026, 6, 1) });

        db.PayrollPeriods.AddRange(
            new PayrollPeriod { Id = 1, PeriodCode = "BL-2026-05", Year = 2026, Month = 5, PeriodType = "Monthly", Status = "Paid", StartDate = new DateOnly(2026, 5, 1), EndDate = new DateOnly(2026, 5, 31) },
            new PayrollPeriod { Id = 2, PeriodCode = "BL-2026-06", Year = 2026, Month = 6, PeriodType = "Monthly", Status = "Approved", StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 6, 30) });

        // Tháng 5: NV01 (20M), NV02 (15M), NV03 (10M)
        db.PayrollRecords.AddRange(
            new PayrollRecord { Id = 1, PayrollPeriodId = 1, EmployeeId = 1, OrganizationUnitId = 1, PositionId = 1, InsuranceSalaryBase = 20_000_000m, UnemploymentInsuranceBase = 20_000_000m },
            new PayrollRecord { Id = 2, PayrollPeriodId = 1, EmployeeId = 2, OrganizationUnitId = 1, PositionId = 1, InsuranceSalaryBase = 15_000_000m, UnemploymentInsuranceBase = 15_000_000m },
            new PayrollRecord { Id = 3, PayrollPeriodId = 1, EmployeeId = 3, OrganizationUnitId = 1, PositionId = 1, InsuranceSalaryBase = 10_000_000m, UnemploymentInsuranceBase = 10_000_000m });

        // Tháng 6: NV01 (20M - giữ nguyên), NV02 (22M - tăng 7M), NV04 (18M - tăng mới)
        // Tổng quỹ BH T6 = 60,000,000 -> NLĐ 10.5% = 6,300,000; DN (17% + 0.5% TNLĐ + 3% + 1% = 21.5%) = 12,900,000 -> Tổng 19,200,000
        static PayrollRecord MakeJuneSiRecord(long id, int empId, int unitId, int posId, decimal siBase)
        {
            decimal eSi = Math.Round(siBase * 0.08m, 0);
            decimal eHi = Math.Round(siBase * 0.015m, 0);
            decimal eUi = Math.Round(siBase * 0.01m, 0);
            decimal cSi = Math.Round(siBase * 0.17m, 0);
            decimal cHi = Math.Round(siBase * 0.03m, 0);
            decimal cUi = Math.Round(siBase * 0.01m, 0);
            decimal cOa = Math.Round(siBase * 0.005m, 0);
            return new PayrollRecord
            {
                Id = id, PayrollPeriodId = 2, EmployeeId = empId, OrganizationUnitId = unitId, PositionId = posId,
                InsuranceSalaryBase = siBase, UnemploymentInsuranceBase = siBase,
                EmployeeSocialInsurance = eSi, EmployeeHealthInsurance = eHi, EmployeeUnemploymentInsurance = eUi,
                TotalEmployeeInsurance = eSi + eHi + eUi,
                CompanySocialInsurance = cSi, CompanyHealthInsurance = cHi, CompanyUnemploymentInsurance = cUi,
                CompanyOccupationalAccidentInsurance = cOa
            };
        }

        db.PayrollRecords.AddRange(
            MakeJuneSiRecord(11, 1, 1, 1, 20_000_000m),
            MakeJuneSiRecord(12, 2, 1, 1, 22_000_000m),
            MakeJuneSiRecord(13, 4, 2, 2, 18_000_000m));
        await db.SaveChangesAsync();

        var report = await new GetSocialInsuranceMonthlyReportQueryHandler(db)
            .Handle(new GetSocialInsuranceMonthlyReportQuery(2026, 6, 2), CancellationToken.None);

        report.IncreaseCount.Should().Be(1);
        report.DecreaseCount.Should().Be(1);
        report.SalaryAdjustmentCount.Should().Be(1);
        report.UnchangedCount.Should().Be(1);

        report.Movements.Should().Contain(m => m.EmployeeCode == "NV04" && m.ChangeCode == "TANG_MOI");
        report.Movements.Should().Contain(m => m.EmployeeCode == "NV03" && m.ChangeCode == "GIAM_NGHI_VIEC");
        report.Movements.Should().Contain(m => m.EmployeeCode == "NV02" && m.ChangeCode == "DIEU_CHINH_TANG" && m.SalaryBaseDelta == 7_000_000m);

        report.Reconciliation.TotalExpectedPayableToAgency.Should().Be(19_200_000m);
        report.Reconciliation.TotalActualFromPayrollRecords.Should().Be(19_200_000m);
        report.Reconciliation.ReconciliationDifference.Should().Be(0m);

        // Đổi cấu trúc cột trong DB mà không sửa code
        var tpl = await db.SocialInsuranceReportTemplates.FirstAsync(t => t.TemplateCode == "D02-LT");
        string customJson = "[{\"ColumnIndex\":1,\"HeaderText\":\"MA_NV_CUSTOM\",\"FieldKey\":\"EmployeeCode\",\"Width\":15},{\"ColumnIndex\":2,\"HeaderText\":\"SO_BHXH_CUSTOM\",\"FieldKey\":\"SocialInsuranceNumber\",\"Width\":18}]";
        await new SaveSocialInsuranceTemplateCommandHandler(db, user)
            .Handle(new SaveSocialInsuranceTemplateCommand(tpl.Id, customJson, "eBHXH-New", "QĐ 490"), CancellationToken.None);

        var excel = await new ExportSocialInsuranceExcelQueryHandler(db, docs)
            .Handle(new ExportSocialInsuranceExcelQuery(2026, 6, "D02-LT", 2), CancellationToken.None);
        using var ms = new MemoryStream(excel.FileBytes);
        using var wb = new XLWorkbook(ms);
        wb.Worksheet("BienDong_D02LT").Cell(4, 1).GetString().Should().Be("MA_NV_CUSTOM");
        wb.Worksheet("BienDong_D02LT").Cell(4, 2).GetString().Should().Be("SO_BHXH_CUSTOM");
    }

    // =========================================================================
    // TEST 04: THUẾ TNCN: 05/KK-TNCN, QUYẾT TOÁN 05/QTT-TNCN, CHỨNG TỪ PDF & EXCEL
    // =========================================================================
    [Fact]
    public async Task PitDeclaration05KK_AnnualFinalization05QTT_AndCertificatePdf_ComputeAccurately()
    {
        using var db = await CreateSeededDbContextAsync();
        var (docs, pii) = CreateServices();
        var user = new StubCurrentUser { UserId = 1, Username = "cb.tax" };

        var resident = new Employee
        {
            Id = 1, EmployeeCode = "VNM_RES", FullName = "Nguyễn Cư Trú", OrganizationUnitId = 2, PositionId = 2,
            TaxCodeEncrypted = pii.Encrypt("8012345678"), CitizenIdEncrypted = pii.Encrypt("079090001234"),
            HireDate = new DateOnly(2022, 1, 1)
        };
        var nonResident = new Employee
        {
            Id = 2, EmployeeCode = "VNM_EXP", FullName = "John Expert", OrganizationUnitId = 2, PositionId = 2,
            TaxCodeEncrypted = pii.Encrypt("8099998888"), CitizenIdEncrypted = pii.Encrypt("PP998877"),
            HireDate = new DateOnly(2026, 1, 1)
        };
        db.Employees.AddRange(resident, nonResident);

        // Tính tay quyết toán lũy tiến năm 2026 (bản thân 15.5M/tháng, bậc 1 0..10M 5%, bậc 2 10..30M 10%):
        // - Resident hoạt động 2 tháng (T1, T2):
        //   + T1: Gross 5,500,000, SelfDeduction 15,500,000 => Assessable T1 = 0 => Tạm khấu trừ T1 = 0
        //   + T2: Gross 55,500,000, SelfDeduction 15,500,000 => Assessable T2 = 40,000,000 => Tạm khấu trừ T2 = 4,500,000
        //   => Tổng cả năm: Taxable = 61,000,000, FamilyDeduction = 31,000,000 => Assessable năm = 30,000,000 / 2 tháng = 15,000,000/tháng
        //   => Nghĩa vụ thuế mỗi tháng bình quân = 10M*5% + 5M*10% = 1,000,000 => Quyết toán năm = 2,000,000
        //   => Hoàn thuế = 4,500,000 - 2,000,000 = 2,500,000 đ!
        // - NonResident (20%): Gross 20,000,000 => Thuế = 4,000,000 đ.
        var p1 = new PayrollPeriod { Id = 1, PeriodCode = "BL-2026-01", Year = 2026, Month = 1, PeriodType = "Monthly", Status = "Paid", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 1, 31) };
        var p2 = new PayrollPeriod { Id = 2, PeriodCode = "BL-2026-02", Year = 2026, Month = 2, PeriodType = "Monthly", Status = "Paid", StartDate = new DateOnly(2026, 2, 1), EndDate = new DateOnly(2026, 2, 28) };
        db.PayrollPeriods.AddRange(p1, p2);

        db.PayrollRecords.AddRange(
            new PayrollRecord { Id = 1, PayrollPeriodId = 1, EmployeeId = 1, OrganizationUnitId = 2, PositionId = 2, TaxResidencyMode = "ResidentStandard", GrossIncome = 5_500_000m, TotalEmployeeInsurance = 0m, SelfDeductionAmount = 15_500_000m, DependentDeductionAmount = 0m, TaxableIncome = 5_500_000m, AssessableIncome = 0m, PersonalIncomeTax = 0m, NetSalary = 5_500_000m },
            new PayrollRecord { Id = 2, PayrollPeriodId = 2, EmployeeId = 1, OrganizationUnitId = 2, PositionId = 2, TaxResidencyMode = "ResidentStandard", GrossIncome = 55_500_000m, TotalEmployeeInsurance = 0m, SelfDeductionAmount = 15_500_000m, DependentDeductionAmount = 0m, TaxableIncome = 55_500_000m, AssessableIncome = 40_000_000m, PersonalIncomeTax = 4_500_000m, NetSalary = 51_000_000m },
            new PayrollRecord { Id = 3, PayrollPeriodId = 1, EmployeeId = 2, OrganizationUnitId = 2, PositionId = 2, TaxResidencyMode = "NonResident20Percent", GrossIncome = 20_000_000m, TaxableIncome = 20_000_000m, AssessableIncome = 20_000_000m, PersonalIncomeTax = 4_000_000m, NetSalary = 16_000_000m });
        await db.SaveChangesAsync();

        var q1Decl = await new GetPitDeclarationSummaryQueryHandler(db)
            .Handle(new GetPitDeclarationSummaryQuery(2026, Quarter: 1), CancellationToken.None);
        q1Decl.TotalEmployeesTaxed.Should().Be(2);
        q1Decl.GrandTotalPitWithheld.Should().Be(8_500_000m); // 4,500k + 4,000k

        var annual = await new GetAnnualPitFinalizationQueryHandler(db, pii)
            .Handle(new GetAnnualPitFinalizationQuery(2026), CancellationToken.None);
        var resRow = annual.Employees.First(e => e.EmployeeCode == "VNM_RES");
        resRow.TotalPitWithheld.Should().Be(4_500_000m);
        resRow.AnnualFinalTaxLiability.Should().Be(2_000_000m);
        resRow.RefundableTax.Should().Be(2_500_000m);

        var certPdf = await new IssueAndGetPitCertificatePdfQueryHandler(db, pii, docs, user)
            .Handle(new IssueAndGetPitCertificatePdfQuery(1, 2026, PersistCertificate: true), CancellationToken.None);
        certPdf.PdfBytes.Take(4).Should().BeEquivalentTo(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        (await db.PitWithholdingCertificates.CountAsync(c => c.EmployeeId == 1 && c.TaxYear == 2026)).Should().Be(1);

        var excel = await new ExportPitSettlementExcelQueryHandler(db, pii, docs)
            .Handle(new ExportPitSettlementExcelQuery(2026), CancellationToken.None);
        using var ms = new MemoryStream(excel.FileBytes);
        using var wb = new XLWorkbook(ms);
        wb.Worksheets.Select(s => s.Name).Should().Contain(new[] { "05_1_BK_QTT", "05_2_BK_VangLai", "TongHop_05QTT" });
    }

    // =========================================================================
    // TEST 05: DASHBOARD C&B (CƠ CẤU THU NHẬP, TỶ LỆ OT, TOP BIẾN ĐỘNG)
    // =========================================================================
    [Fact]
    public async Task CompensationDashboard_ComputesIncomeBreakdown_OtShare_AndTopVariances()
    {
        using var db = await CreateSeededDbContextAsync();
        db.Employees.AddRange(
            new Employee { Id = 1, EmployeeCode = "NV01", FullName = "Biến Động Lớn", OrganizationUnitId = 1, PositionId = 1, HireDate = new DateOnly(2023, 1, 1) },
            new Employee { Id = 2, EmployeeCode = "NV02", FullName = "Ổn Định", OrganizationUnitId = 1, PositionId = 1, HireDate = new DateOnly(2023, 1, 1) });

        db.PayrollPeriods.Add(new PayrollPeriod { Id = 20, PeriodCode = "BL-2026-07", Year = 2026, Month = 7, PeriodType = "Monthly", Status = "Approved", StartDate = new DateOnly(2026, 7, 1), EndDate = new DateOnly(2026, 7, 31) });
        db.PayrollRecords.AddRange(
            new PayrollRecord { Id = 1, PayrollPeriodId = 20, EmployeeId = 1, OrganizationUnitId = 1, PositionId = 1, ActualBaseSalary = 20_000_000m, OvertimePay = 10_000_000m, GrossIncome = 30_000_000m, PreviousPeriodNetSalary = 18_000_000m, NetSalary = 26_000_000m, TotalCompanyCost = 34_000_000m },
            new PayrollRecord { Id = 2, PayrollPeriodId = 20, EmployeeId = 2, OrganizationUnitId = 1, PositionId = 1, ActualBaseSalary = 20_000_000m, OvertimePay = 0m, GrossIncome = 20_000_000m, PreviousPeriodNetSalary = 17_000_000m, NetSalary = 17_500_000m, TotalCompanyCost = 22_000_000m });
        await db.SaveChangesAsync();

        var dash = await new GetCompensationDashboardQueryHandler(db)
            .Handle(new GetCompensationDashboardQuery(2026, 20), CancellationToken.None);

        dash.FocusTotalGross.Should().Be(50_000_000m);
        dash.FocusOvertimePay.Should().Be(10_000_000m);
        dash.FocusOtSharePercent.Should().Be(20.00m); // 10M / 50M = 20%
        dash.TopVariances.First().EmployeeCode.Should().Be("NV01");
        dash.TopVariances.First().VarianceAmount.Should().Be(8_000_000m);
    }

    // =========================================================================
    // TEST 06: CỔNG ESS MỞ RỘNG (CÔNG HÀNG NGÀY, QUỸ PHÉP CHI TIẾT & CHỨNG TỪ THUẾ SELF-ONLY)
    // =========================================================================
    [Fact]
    public async Task EssPortalCl8_ReturnsDailyTimesheet_LeaveBalanceDetails_AndEnforcesSelfOnlyPitCert()
    {
        using var db = await CreateSeededDbContextAsync();
        var (docs, pii) = CreateServices();

        var shift = new WorkShift { Id = 1, ShiftCode = "CA_HC", ShiftName = "Ca Hành chính", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(17, 0) };
        db.WorkShifts.Add(shift);
        var alType = new LeaveType { Id = 1, LeaveTypeCode = "AL", LeaveTypeName = "Phép năm luật định", DisplayOrder = 1 };
        db.LeaveTypes.Add(alType);

        var e1 = new Employee { Id = 1, EmployeeCode = "NV01", FullName = "Trần Tự Phục Vụ", OrganizationUnitId = 1, PositionId = 1, HireDate = new DateOnly(2024, 1, 1) };
        var e2 = new Employee { Id = 2, EmployeeCode = "NV02", FullName = "Người Khác", OrganizationUnitId = 1, PositionId = 1, HireDate = new DateOnly(2024, 1, 1) };
        db.Employees.AddRange(e1, e2);

        db.DailyAttendances.AddRange(
            new DailyAttendance { Id = 1, EmployeeId = 1, WorkShiftId = 1, AttendanceDate = new DateOnly(2026, 8, 3), AttendanceSymbol = "X", CheckInTime = new DateTime(2026, 8, 3, 7, 55, 0), CheckOutTime = new DateTime(2026, 8, 3, 17, 5, 0), WorkingHours = 8m, WorkdayCount = 1m },
            new DailyAttendance { Id = 2, EmployeeId = 1, WorkShiftId = 1, AttendanceDate = new DateOnly(2026, 8, 4), AttendanceSymbol = "X+", CheckInTime = new DateTime(2026, 8, 4, 8, 10, 0), CheckOutTime = new DateTime(2026, 8, 4, 19, 10, 0), WorkingHours = 10m, WorkdayCount = 1m, OvertimeHours150 = 2m, LateMinutes = 10 });

        db.LeaveBalances.Add(new LeaveBalance
        {
            Id = 1, EmployeeId = 1, LeaveTypeId = 1, Year = 2026,
            StandardEntitlement = 12m, SeniorityExtraDays = 1m, CarriedForwardDays = 2m, UsedDays = 3m, RemainingDays = 12m, CompensatoryHoursBalance = 4m
        });

        var p8 = new PayrollPeriod { Id = 8, PeriodCode = "BL-2026-08", Year = 2026, Month = 8, PeriodType = "Monthly", Status = "Paid", StartDate = new DateOnly(2026, 8, 1), EndDate = new DateOnly(2026, 8, 31) };
        db.PayrollPeriods.Add(p8);
        db.PayrollRecords.Add(new PayrollRecord
        {
            Id = 801, PayrollPeriodId = 8, EmployeeId = 1, OrganizationUnitId = 1, PositionId = 1,
            PayableWorkdays = 26m, GrossIncome = 22_000_000m, TotalEmployeeInsurance = 2_310_000m, PersonalIncomeTax = 250_000m, NetSalary = 19_440_000m
        });
        await db.SaveChangesAsync();

        var selfUser = new StubCurrentUser { UserId = 101, Username = "nv01", EmployeeId = 1, AllowAllPermissions = false };
        var portal = await new GetEssMssPortalQueryHandler(db, selfUser)
            .Handle(new GetEssMssPortalQuery(EmployeeId: 1, TimesheetYear: 2026, TimesheetMonth: 8, PitYear: 2026), CancellationToken.None);

        portal.MonthlyTimesheet.Should().HaveCount(2);
        portal.MonthlyTimesheet![1].OvertimeHours.Should().Be(2m);
        portal.LeaveBalanceDetails.Should().HaveCount(1);
        portal.LeaveBalanceDetails![0].RemainingDays.Should().Be(12m);
        portal.AnnualPitSummary!.TotalPitWithheld.Should().Be(250_000m);

        var certHandler = new IssueAndGetPitCertificatePdfQueryHandler(db, pii, docs, selfUser);
        var ownPdf = await certHandler.Handle(new IssueAndGetPitCertificatePdfQuery(1, 2026, PersistCertificate: true, EnforceSelfOnly: true), CancellationToken.None);
        ownPdf.PdfBytes.Length.Should().BeGreaterThan(500);

        Func<Task> otherDownload = () => certHandler.Handle(new IssueAndGetPitCertificatePdfQuery(2, 2026, PersistCertificate: false, EnforceSelfOnly: true), CancellationToken.None);
        await otherDownload.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
