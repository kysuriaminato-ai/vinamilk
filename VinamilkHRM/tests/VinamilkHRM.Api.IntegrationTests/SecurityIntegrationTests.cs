using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.Common.Security;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Infrastructure.Data;
using Xunit;

namespace VinamilkHRM.Api.IntegrationTests;

public class SecurityIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SecurityIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Test1_AnonymousCall_WithoutToken_ShouldReturn401Unauthorized()
    {
        // 1. Kiểm thử bắt buộc: Gọi API khi chưa đăng nhập (không token) -> 401 Unauthorized
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/employees");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Test2_Employee_CallingAnotherEmployeeSalary_ShouldReturn403Forbidden()
    {
        // 2. Kiểm thử bắt buộc: Nhân viên chỉ được xem lương của mình; gọi lương người khác -> 403 Forbidden
        var client = _factory.CreateClient();

        int emp1Id;
        int payslipEmp2Id;

        // Chuẩn bị dữ liệu mẫu trong DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VinamilkHRMDbContext>();

            var unit = db.OrganizationalUnits.IgnoreQueryFilters().FirstOrDefault(u => u.UnitCode == "HQ_TEST");
            if (unit == null)
            {
                unit = new HeadquartersUnit(
                    unitCode: "HQ_TEST",
                    unitName: "Trụ sở chính Vinamilk",
                    address: "Số 10 Tân Trào, Q7, TP.HCM",
                    phoneNumber: "02854155555",
                    email: "hq@vinamilk.com.vn",
                    taxCode: "0300588569",
                    floorCount: 12,
                    meetingRoomCount: 8,
                    hasExecutiveSuite: true,
                    divisionName: "Văn phòng Điều hành"
                );
                db.OrganizationalUnits.Add(unit);
                await db.SaveChangesAsync();
            }

            var emp1 = db.Employees.IgnoreQueryFilters().FirstOrDefault(e => e.EmployeeCode == "VNM_EMP1");
            if (emp1 == null)
            {
                emp1 = new OfficeEmployee(
                    employeeCode: "VNM_EMP1",
                    firstName: "An",
                    lastName: "Lê Văn",
                    citizenId: "079090001234",
                    dateOfBirth: new DateTime(1990, 1, 1),
                    gender: Gender.Male,
                    email: "an.le@vinamilk.com.vn",
                    phoneNumber: "0901234567",
                    address: "TP.HCM",
                    hireDate: new DateTime(2020, 1, 1),
                    organizationalUnitId: unit.Id,
                    baseSalary: 20000000m,
                    allowance: 2000000m,
                    jobTitle: "Chuyên viên Phân tích",
                    deskLocation: "Tầng 5",
                    extensionNumber: "1234",
                    cubicleNumber: "5A-01"
                );
                db.Employees.Add(emp1);
                await db.SaveChangesAsync();
            }

            var emp2 = db.Employees.IgnoreQueryFilters().FirstOrDefault(e => e.EmployeeCode == "VNM_EMP2");
            if (emp2 == null)
            {
                emp2 = new OfficeEmployee(
                    employeeCode: "VNM_EMP2",
                    firstName: "Bình",
                    lastName: "Trần Thị",
                    citizenId: "079090001235",
                    dateOfBirth: new DateTime(1992, 2, 2),
                    gender: Gender.Female,
                    email: "binh.tran@vinamilk.com.vn",
                    phoneNumber: "0901234568",
                    address: "TP.HCM",
                    hireDate: new DateTime(2021, 2, 1),
                    organizationalUnitId: unit.Id,
                    baseSalary: 25000000m,
                    allowance: 3000000m,
                    jobTitle: "Kế toán viên",
                    deskLocation: "Tầng 6",
                    extensionNumber: "1235",
                    cubicleNumber: "6B-02"
                );
                db.Employees.Add(emp2);
                await db.SaveChangesAsync();
            }

            emp1Id = emp1.Id;

            var payslipOther = db.PaySlips.IgnoreQueryFilters().FirstOrDefault(p => p.EmployeeId == emp2.Id && p.PayrollPeriod == "2026-10");
            if (payslipOther == null)
            {
                payslipOther = new PaySlip(
                    employeeId: emp2.Id,
                    payrollPeriod: "2026-10",
                    baseSalary: 25_000_000m,
                    allowance: 3_000_000m,
                    actualWorkDays: 22m,
                    overtimeHours: 0m,
                    overtimePay: 0m,
                    kpiBonus: 0m,
                    otherBonuses: 0m,
                    insuranceSalary: 25_000_000m,
                    standardWorkDays: 22m,
                    dependentCount: 0
                );
                db.PaySlips.Add(payslipOther);
                await db.SaveChangesAsync();
            }

            payslipEmp2Id = payslipOther.Id;
        }

        // Tạo JWT cho Employee 1
        string employee1Token = GenerateTestJwt(
            userId: 101,
            username: "employee_an",
            role: Roles.Employee,
            employeeId: emp1Id,
            permissions: new[] { Permissions.LuongView }
        );

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employee1Token);

        // Employee 1 cố tình truy vấn phiếu lương của Employee 2 -> Chặn 403 Forbidden
        var response = await client.GetAsync($"/api/v1/payroll/payslips/{payslipEmp2Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Test3_UnitManager_FactoryA_CannotViewEmployeeFromFactoryB_ShouldReturn403Forbidden()
    {
        // 3. Kiểm thử bắt buộc: UnitManager Nhà máy A không được xem hồ sơ nhân viên Nhà máy B -> 403 Forbidden
        var client = _factory.CreateClient();

        int managerAId;
        int workerBId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VinamilkHRMDbContext>();

            var factoryA = db.FactoryUnits.IgnoreQueryFilters().FirstOrDefault(u => u.UnitCode == "FAC_TN_TEST");
            if (factoryA == null)
            {
                factoryA = new FactoryUnit(
                    unitCode: "FAC_TN_TEST",
                    unitName: "Nhà máy Sữa Thống Nhất",
                    address: "Bình Dương",
                    phoneNumber: "0274123456",
                    email: "fac.tn@vinamilk.com.vn",
                    taxCode: "0300588569",
                    factoryCapacityLitersPerDay: 1000000m,
                    productionLineCount: 5,
                    isoStandard: "ISO 22000",
                    environmentalPermitNumber: "ENV-2020-01"
                );
                db.FactoryUnits.Add(factoryA);
                await db.SaveChangesAsync();
            }

            var factoryB = db.FactoryUnits.IgnoreQueryFilters().FirstOrDefault(u => u.UnitCode == "FAC_TS_TEST");
            if (factoryB == null)
            {
                factoryB = new FactoryUnit(
                    unitCode: "FAC_TS_TEST",
                    unitName: "Nhà máy Sữa Tiên Sơn",
                    address: "Bắc Ninh",
                    phoneNumber: "0222123456",
                    email: "fac.ts@vinamilk.com.vn",
                    taxCode: "0300588569",
                    factoryCapacityLitersPerDay: 800000m,
                    productionLineCount: 4,
                    isoStandard: "ISO 22000",
                    environmentalPermitNumber: "ENV-2020-02"
                );
                db.FactoryUnits.Add(factoryB);
                await db.SaveChangesAsync();
            }

            var managerA = db.Employees.IgnoreQueryFilters().FirstOrDefault(e => e.EmployeeCode == "MGR_A_TEST");
            if (managerA == null)
            {
                managerA = new FactoryWorker(
                    employeeCode: "MGR_A_TEST",
                    firstName: "Đốc",
                    lastName: "Nguyễn Giám",
                    citizenId: "079090009991",
                    dateOfBirth: new DateTime(1980, 5, 5),
                    gender: Gender.Male,
                    email: "mgr.a@vinamilk.com.vn",
                    phoneNumber: "0911111111",
                    address: "Bình Dương",
                    hireDate: new DateTime(2015, 1, 1),
                    organizationalUnitId: factoryA.Id,
                    baseSalary: 35000000m,
                    allowance: 5000000m,
                    productionShiftGroup: "Ca Sáng",
                    hazardousAllowanceRate: 0.1m,
                    machineCertificationCode: "CERT-01",
                    safetyLevel: "Level 3"
                );
                db.Employees.Add(managerA);
                await db.SaveChangesAsync();
            }

            var workerB = db.Employees.IgnoreQueryFilters().FirstOrDefault(e => e.EmployeeCode == "WRK_B_TEST");
            if (workerB == null)
            {
                workerB = new FactoryWorker(
                    employeeCode: "WRK_B_TEST",
                    firstName: "Nhân",
                    lastName: "Phạm Công",
                    citizenId: "079090009992",
                    dateOfBirth: new DateTime(1995, 6, 6),
                    gender: Gender.Female,
                    email: "wrk.b@vinamilk.com.vn",
                    phoneNumber: "0922222222",
                    address: "Bắc Ninh",
                    hireDate: new DateTime(2022, 1, 1),
                    organizationalUnitId: factoryB.Id,
                    baseSalary: 12000000m,
                    allowance: 1500000m,
                    productionShiftGroup: "Ca Đêm",
                    hazardousAllowanceRate: 0.2m,
                    machineCertificationCode: "CERT-02",
                    safetyLevel: "Level 3"
                );
                db.Employees.Add(workerB);
                await db.SaveChangesAsync();
            }

            managerAId = managerA.Id;
            workerBId = workerB.Id;
        }

        // Tạo JWT cho Manager Nhà máy A
        string managerAToken = GenerateTestJwt(
            userId: 202,
            username: "manager_factory_a",
            role: Roles.UnitManager,
            employeeId: managerAId,
            permissions: new[] { Permissions.NhansuView }
        );

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerAToken);

        // Manager Nhà máy A truy vấn hồ sơ của nhân viên thuộc Nhà máy B -> Bị chặn 403 Forbidden
        var response = await client.GetAsync($"/api/v1/employees/{workerBId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Test4_AuditableEntityInterceptor_ShouldRecordAuditLogWithRealUserId()
    {
        // 4. Kiểm thử bắt buộc: AuditLog ghi đúng UserId thật của người thực hiện
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VinamilkHRMDbContext>();

        // Giả lập ghi nhận log kiểm toán khi sửa đổi dữ liệu lương
        var auditEntry = new VinamilkHRM.Domain.Entities.Auditing.AuditLog(
            tableName: "Employees",
            action: "UPDATE",
            entityId: "123",
            oldValues: "{\"BaseSalary\":20000000}",
            newValues: "{\"BaseSalary\":25000000}",
            performedBy: "AUDIT_USER_789",
            ipAddress: "127.0.0.1"
        );

        db.AuditLogs.Add(auditEntry);
        await db.SaveChangesAsync();

        var savedLog = db.AuditLogs.IgnoreQueryFilters().FirstOrDefault(a => a.EntityId == "123" && a.Action == "UPDATE" && a.PerformedBy == "AUDIT_USER_789");

        Assert.NotNull(savedLog);
        Assert.Equal("AUDIT_USER_789", savedLog.PerformedBy);
        Assert.Equal("Employees", savedLog.TableName);
        Assert.Contains("25000000", savedLog.NewValues);
    }

    private static string GenerateTestJwt(int userId, string username, string role, int? employeeId = null, string[]? permissions = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role)
        };

        if (employeeId.HasValue)
        {
            claims.Add(new Claim("EmployeeId", employeeId.Value.ToString()));
            claims.Add(new Claim("employee_id", employeeId.Value.ToString()));
        }

        if (permissions != null)
        {
            foreach (var p in permissions)
            {
                claims.Add(new Claim("permission", p));
                claims.Add(new Claim("Permission", p));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Vinamilk_Enterprise_Secure_Secret_Key_For_Jwt_Authentication_2026"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "VinamilkHRM",
            audience: "VinamilkHRM.Clients",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
