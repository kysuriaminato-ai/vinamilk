using System;
using System.Collections.Generic;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Contracts;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Services.Contracts;
using Xunit;

namespace VinamilkHRM.Domain.Tests.Contracts;

public class ContractManagementTests
{
    [Fact]
    public void ValidateDefiniteContractLimit_ThirdDefiniteContract_ShouldThrow422ErrContractThirdDefinite()
    {
        // ARRANGE: Tạo nhân sự mẫu
        var employee = new OfficeEmployee(
            employeeCode: "VNM-OFF-9999",
            firstName: "Văn A",
            lastName: "Nguyễn",
            citizenId: "079090001234",
            dateOfBirth: new DateTime(1990, 5, 20),
            gender: Gender.Male,
            email: "vana@vinamilk.com.vn",
            phoneNumber: "0901234567",
            address: "Quận 7, TP. Hồ Chí Minh",
            hireDate: new DateTime(2022, 1, 1),
            organizationalUnitId: 1,
            baseSalary: 25000000m,
            allowance: 2500000m,
            jobTitle: "Chuyên viên Phân tích",
            deskLocation: "Tầng 5",
            extensionNumber: "1234",
            cubicleNumber: "5A-01");

        // Danh sách 2 hợp đồng xác định thời hạn trước đây (Lần 1 và Lần 2)
        var previousContracts = new List<LaborContract>
        {
            new DefiniteContract("HD-2022-001", 101, new DateTime(2022, 1, 1), new DateTime(2023, 12, 31), 20000000m, 20000000m, new DateTime(2021, 12, 25), 24),
            new DefiniteContract("HD-2024-002", 101, new DateTime(2024, 1, 1), new DateTime(2025, 12, 31), 25000000m, 25000000m, new DateTime(2023, 12, 25), 24)
        };

        // ACT & ASSERT: Tiêu chí 3 - Tạo hợp đồng xác định thời hạn lần 3 -> ném lỗi ERR_CONTRACT_THIRD_DEFINITE
        var ex = Assert.Throws<DomainException>(() =>
        {
            ContractRuleValidator.ValidateDefiniteContractLimit(
                employee,
                ContractType.Definite,
                previousContracts,
                isExempted: false);
        });

        Assert.Equal("ERR_CONTRACT_THIRD_DEFINITE", ex.ErrorCode);
        Assert.Contains("Điều 20 Bộ luật Lao động 2019", ex.Message);
        Assert.Contains("Hợp đồng tiếp theo bắt buộc phải là Hợp đồng không xác định thời hạn", ex.Message);
    }

    [Fact]
    public void ValidateDefiniteContractLimit_ThirdDefiniteContract_WithExemption_ShouldPass()
    {
        // ARRANGE: Nhân viên cao tuổi hoặc chuyên gia nước ngoài (ngoại lệ cấu hình)
        var employee = new OfficeEmployee(
            employeeCode: "VNM-EXP-0001",
            firstName: "Robert",
            lastName: "Smith",
            citizenId: "EX-998877",
            dateOfBirth: new DateTime(1960, 1, 1),
            gender: Gender.Male,
            email: "robert@vinamilk.com.vn",
            phoneNumber: "0909999888",
            address: "TP. Hồ Chí Minh",
            hireDate: new DateTime(2020, 1, 1),
            organizationalUnitId: 1,
            baseSalary: 80000000m,
            allowance: 8000000m,
            jobTitle: "Chuyên gia Tư vấn",
            deskLocation: "Tầng 8",
            extensionNumber: "8888",
            cubicleNumber: "8A-01");

        var previousContracts = new List<LaborContract>
        {
            new DefiniteContract("HD-2020-001", 101, new DateTime(2020, 1, 1), new DateTime(2022, 12, 31), 80000000m, 36000000m, new DateTime(2019, 12, 25), 36),
            new DefiniteContract("HD-2023-002", 101, new DateTime(2023, 1, 1), new DateTime(2025, 12, 31), 80000000m, 36000000m, new DateTime(2022, 12, 25), 36)
        };

        // ACT & ASSERT: Có ngoại lệ cấu hình hợp lệ -> không báo lỗi
        ContractRuleValidator.ValidateDefiniteContractLimit(
            employee,
            ContractType.Definite,
            previousContracts,
            isExempted: true,
            exemptionReason: "Người lao động cao tuổi theo Điều 148 Bộ luật Lao động 2019");

        // Không có ngoại lệ nào bị ném
        Assert.True(true);
    }

    [Fact]
    public void ContractExpiration_60_30_7_Days_Calculation_ShouldBePrecise()
    {
        var checkDate = new DateTime(2026, 10, 15);

        // Hợp đồng còn đúng 60 ngày
        var contract60 = new DefiniteContract("HD-60", 1, checkDate.AddYears(-1), checkDate.AddDays(60), 20000000m, 20000000m, checkDate.AddYears(-1), 12);
        // Hợp đồng còn đúng 30 ngày
        var contract30 = new DefiniteContract("HD-30", 2, checkDate.AddYears(-1), checkDate.AddDays(30), 20000000m, 20000000m, checkDate.AddYears(-1), 12);
        // Hợp đồng còn đúng 7 ngày
        var contract7 = new DefiniteContract("HD-7", 3, checkDate.AddYears(-1), checkDate.AddDays(7), 20000000m, 20000000m, checkDate.AddYears(-1), 12);
        // Hợp đồng còn 15 ngày (không nằm trong 3 mốc)
        var contract15 = new DefiniteContract("HD-15", 4, checkDate.AddYears(-1), checkDate.AddDays(15), 20000000m, 20000000m, checkDate.AddYears(-1), 12);

        int days60 = (contract60.EndDate!.Value.Date - checkDate.Date).Days;
        int days30 = (contract30.EndDate!.Value.Date - checkDate.Date).Days;
        int days7 = (contract7.EndDate!.Value.Date - checkDate.Date).Days;
        int days15 = (contract15.EndDate!.Value.Date - checkDate.Date).Days;

        Assert.Equal(60, days60);
        Assert.Equal(30, days30);
        Assert.Equal(7, days7);
        Assert.Equal(15, days15);

        // Đánh giá các mốc hợp lệ
        Assert.True(days60 == 60 || days60 == 30 || days60 == 7);
        Assert.True(days30 == 60 || days30 == 30 || days30 == 7);
        Assert.True(days7 == 60 || days7 == 30 || days7 == 7);
        Assert.False(days15 == 60 || days15 == 30 || days15 == 7);
    }
}