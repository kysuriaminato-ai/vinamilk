using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using VinamilkHRM.Application.Contracts;
using VinamilkHRM.Domain.Entities.Contracts;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;
using Xunit;

namespace VinamilkHRM.Application.Tests.Contracts;

public class DocxContractDocumentGeneratorTests
{
    [Fact]
    public void DocxContractDocumentGenerator_ShouldGenerateValidDocxWithMergedFields()
    {
        // ARRANGE
        var generator = new DocxContractDocumentGenerator();
        var employee = new OfficeEmployee(
            employeeCode: "VNM-OFF-1234",
            firstName: "Thị B",
            lastName: "Trần",
            citizenId: "079195009876",
            dateOfBirth: new DateTime(1995, 8, 15),
            gender: Gender.Female,
            email: "tranb@vinamilk.com.vn",
            phoneNumber: "0912345678",
            address: "Số 10 Tân Trào, Quận 7, TP. Hồ Chí Minh",
            hireDate: new DateTime(2026, 1, 1),
            organizationalUnitId: 1,
            baseSalary: 30000000m,
            allowance: 3000000m,
            jobTitle: "Chuyên viên Nhân sự",
            deskLocation: "Tầng 6",
            extensionNumber: "6789",
            cubicleNumber: "6B-02");

        var contract = new DefiniteContract(
            contractNumber: "HDLD-2026-VNM-001",
            employeeId: 101,
            startDate: new DateTime(2026, 1, 1),
            endDate: new DateTime(2027, 12, 31),
            basicSalary: 30000000m,
            insuranceSalary: 30000000m,
            signDate: new DateTime(2025, 12, 28),
            durationMonths: 24);

        // ACT: Sinh file DOCX
        byte[] docxBytes = generator.GenerateDocxContract(contract, employee);

        // ASSERT: Định dạng DOCX hợp lệ (Header Zip PK\x03\x04)
        Assert.NotNull(docxBytes);
        Assert.True(docxBytes.Length > 500, "File DOCX phải có dung lượng lớn hơn 500 bytes.");
        Assert.Equal((byte)0x50, docxBytes[0]); // 'P'
        Assert.Equal((byte)0x4B, docxBytes[1]); // 'K'

        // Giải nén kiểm tra nội dung word/document.xml đã trộn trường
        using var ms = new MemoryStream(docxBytes);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        var docEntry = zip.GetEntry("word/document.xml");
        Assert.NotNull(docEntry);

        using var reader = new StreamReader(docEntry.Open(), Encoding.UTF8);
        string xmlContent = reader.ReadToEnd();

        Assert.Contains("HDLD-2026-VNM-001", xmlContent);
        Assert.Contains("Trần Thị B", xmlContent);
        Assert.Contains("079195009876", xmlContent);
        Assert.Contains("30,000,000 VNĐ", xmlContent);
        Assert.Contains("Mai Kiều Liên", xmlContent);
    }
}