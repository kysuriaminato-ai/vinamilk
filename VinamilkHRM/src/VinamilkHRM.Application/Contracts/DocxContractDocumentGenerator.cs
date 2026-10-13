using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using VinamilkHRM.Domain.Entities.Contracts;
using VinamilkHRM.Domain.Entities.Employees;

namespace VinamilkHRM.Application.Contracts;

public interface IContractDocumentGenerator
{
    byte[] GenerateDocxContract(LaborContract contract, Employee employee, Dictionary<string, string>? customFields = null);
    byte[] GenerateDocxAppendix(ElectronicContractAppendix appendix, LaborContract contract, Employee employee);
}

/// <summary>
/// Bộ sinh tài liệu hợp đồng và phụ lục định dạng DOCX chuẩn OpenXML với các trường trộn (Merge Fields).
/// Không phụ thuộc bất kỳ thư viện thương mại nào, đảm bảo hiệu năng cao và tuân thủ giấy phép nguồn mở.
/// </summary>
public class DocxContractDocumentGenerator : IContractDocumentGenerator
{
    public byte[] GenerateDocxContract(LaborContract contract, Employee employee, Dictionary<string, string>? customFields = null)
    {
        var mergeFields = new Dictionary<string, string>
        {
            { "ContractNumber", contract.ContractNumber },
            { "EmployeeName", employee.FullName },
            { "EmployeeCode", employee.EmployeeCode },
            { "CitizenId", employee.CitizenId },
            { "DateOfBirth", employee.DateOfBirth.ToString("dd/MM/yyyy") },
            { "Address", employee.Address },
            { "JobTitle", employee.StandardJobTitle?.JobTitleName ?? "Chuyên viên" },
            { "UnitName", employee.OrganizationalUnit?.UnitName ?? "Tập đoàn Vinamilk" },
            { "BasicSalary", $"{contract.BasicSalary:N0} VNĐ" },
            { "InsuranceSalary", $"{contract.InsuranceSalary:N0} VNĐ" },
            { "StartDate", contract.StartDate.ToString("dd/MM/yyyy") },
            { "EndDate", contract.EndDate.HasValue ? contract.EndDate.Value.ToString("dd/MM/yyyy") : "Không xác định thời hạn" },
            { "SignDate", contract.SignDate.ToString("dd/MM/yyyy") },
            { "EmployerRepresentative", "Bà Mai Kiều Liên - Tổng Giám Đốc" }
        };

        if (customFields != null)
        {
            foreach (var kvp in customFields)
            {
                mergeFields[kvp.Key] = kvp.Value;
            }
        }

        string documentXml = BuildContractXml(contract, employee, mergeFields);
        return PackDocx(documentXml);
    }

    public byte[] GenerateDocxAppendix(ElectronicContractAppendix appendix, LaborContract contract, Employee employee)
    {
        var mergeFields = new Dictionary<string, string>
        {
            { "AppendixNumber", appendix.AppendixNumber },
            { "ContractNumber", contract.ContractNumber },
            { "EmployeeName", employee.FullName },
            { "EmployeeCode", employee.EmployeeCode },
            { "CitizenId", employee.CitizenId },
            { "EffectiveDate", appendix.EffectiveDate.ToString("dd/MM/yyyy") },
            { "NewBasicSalary", $"{appendix.NewSalary:N0} VNĐ" },
            { "AdjustmentReason", appendix.AdjustmentReason },
            { "SignDate", appendix.SignDate.ToString("dd/MM/yyyy") }
        };

        string documentXml = BuildAppendixXml(appendix, contract, employee, mergeFields);
        return PackDocx(documentXml);
    }

    private string BuildContractXml(LaborContract contract, Employee employee, Dictionary<string, string> fields)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.AppendLine("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">");
        sb.AppendLine("<w:body>");

        // Tiêu ngữ
        sb.AppendLine("<w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr><w:r><w:rPr><w:b/><w:sz w:val=\"28\"/></w:rPr><w:t>CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</w:t></w:r></w:p>");
        sb.AppendLine("<w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr><w:r><w:rPr><w:b/><w:sz w:val=\"24\"/></w:rPr><w:t>Độc lập - Tự do - Hạnh phúc</w:t></w:r></w:p>");
        sb.AppendLine("<w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr><w:r><w:rPr><w:b/><w:sz w:val=\"32\"/><w:color w:val=\"0033A0\"/></w:rPr><w:t>HỢP ĐỒNG LAO ĐỘNG</w:t></w:r></w:p>");
        sb.AppendLine($"<w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr><w:r><w:rPr><w:i/></w:rPr><w:t>Số hiệu: {fields["ContractNumber"]}</w:t></w:r></w:p>");

        // Nội dung hợp đồng
        sb.AppendLine("<w:p><w:r><w:rPr><w:b/></w:rPr><w:t>BÊN A: NGƯỜI SỬ DỤNG LAO ĐỘNG</w:t></w:r></w:p>");
        sb.AppendLine("<w:p><w:r><w:t>Công ty Cổ phần Sữa Việt Nam (Vinamilk) - MST: 0300588569</w:t></w:r></w:p>");
        sb.AppendLine($"<w:p><w:r><w:t>Đại diện: {fields["EmployerRepresentative"]}</w:t></w:r></w:p>");

        sb.AppendLine("<w:p><w:r><w:rPr><w:b/></w:rPr><w:t>BÊN B: NGƯỜI LAO ĐỘNG</w:t></w:r></w:p>");
        sb.AppendLine($"<w:p><w:r><w:t>Ông/Bà: {fields["EmployeeName"]} - Mã NV: {fields["EmployeeCode"]}</w:t></w:r></w:p>");
        sb.AppendLine($"<w:p><w:r><w:t>Ngày sinh: {fields["DateOfBirth"]} - Số CCCD: {fields["CitizenId"]}</w:t></w:r></w:p>");
        sb.AppendLine($"<w:p><w:r><w:t>Địa chỉ thường trú: {fields["Address"]}</w:t></w:r></w:p>");

        sb.AppendLine("<w:p><w:r><w:rPr><w:b/></w:rPr><w:t>ĐIỀU KHOẢN HỢP ĐỒNG</w:t></w:r></w:p>");
        sb.AppendLine($"<w:p><w:r><w:t>1. Vị trí công tác: {fields["JobTitle"]} tại {fields["UnitName"]}</w:t></w:r></w:p>");
        sb.AppendLine($"<w:p><w:r><w:t>2. Thời hạn hợp đồng: Từ {fields["StartDate"]} đến {fields["EndDate"]}</w:t></w:r></w:p>");
        sb.AppendLine($"<w:p><w:r><w:t>3. Mức lương cơ bản: {fields["BasicSalary"]} - Lương đóng BHXH: {fields["InsuranceSalary"]}</w:t></w:r></w:p>");
        sb.AppendLine("<w:p><w:r><w:t>4. Quyền lợi và nghĩa vụ: Tuân thủ Nội quy lao động và Thỏa ước lao động tập thể Vinamilk.</w:t></w:r></w:p>");

        sb.AppendLine($"<w:p><w:pPr><w:jc w:val=\"right\"/></w:pPr><w:r><w:t>TP. Hồ Chí Minh, ngày ký {fields["SignDate"]}</w:t></w:r></w:p>");

        sb.AppendLine("</w:body>");
        sb.AppendLine("</w:document>");
        return sb.ToString();
    }

    private string BuildAppendixXml(ElectronicContractAppendix appendix, LaborContract contract, Employee employee, Dictionary<string, string> fields)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.AppendLine("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">");
        sb.AppendLine("<w:body>");

        sb.AppendLine("<w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr><w:r><w:rPr><w:b/><w:sz w:val=\"28\"/></w:rPr><w:t>CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</w:t></w:r></w:p>");
        sb.AppendLine("<w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr><w:r><w:rPr><w:b/><w:sz w:val=\"24\"/></w:rPr><w:t>Độc lập - Tự do - Hạnh phúc</w:t></w:r></w:p>");
        sb.AppendLine("<w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr><w:r><w:rPr><w:b/><w:sz w:val=\"32\"/><w:color w:val=\"0033A0\"/></w:rPr><w:t>PHỤ LỤC HỢP ĐỒNG LAO ĐỘNG</w:t></w:r></w:p>");
        sb.AppendLine($"<w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr><w:r><w:rPr><w:i/></w:rPr><w:t>Số: {fields["AppendixNumber"]} - Kèm theo HĐLĐ số {fields["ContractNumber"]}</w:t></w:r></w:p>");

        sb.AppendLine($"<w:p><w:r><w:t>Người lao động: {fields["EmployeeName"]} (Mã: {fields["EmployeeCode"]}) - Số CCCD: {fields["CitizenId"]}</w:t></w:r></w:p>");
        sb.AppendLine($"<w:p><w:r><w:t>Ngày có hiệu lực: {fields["EffectiveDate"]}</w:t></w:r></w:p>");
        sb.AppendLine($"<w:p><w:r><w:t>Mức lương mới: {fields["NewBasicSalary"]}</w:t></w:r></w:p>");
        sb.AppendLine($"<w:p><w:r><w:t>Lý do điều chỉnh: {fields["AdjustmentReason"]}</w:t></w:r></w:p>");

        sb.AppendLine($"<w:p><w:pPr><w:jc w:val=\"right\"/></w:pPr><w:r><w:t>Ngày ký {fields["SignDate"]}</w:t></w:r></w:p>");

        sb.AppendLine("</w:body>");
        sb.AppendLine("</w:document>");
        return sb.ToString();
    }

    private byte[] PackDocx(string documentXml)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true, Encoding.UTF8))
        {
            // [Content_Types].xml
            var contentTypes = zip.CreateEntry("[Content_Types].xml");
            using (var writer = new StreamWriter(contentTypes.Open(), Encoding.UTF8))
            {
                writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                    "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                    "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                    "<Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/>" +
                    "</Types>");
            }

            // _rels/.rels
            var rels = zip.CreateEntry("_rels/.rels");
            using (var writer = new StreamWriter(rels.Open(), Encoding.UTF8))
            {
                writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/>" +
                    "</Relationships>");
            }

            // word/document.xml
            var docEntry = zip.CreateEntry("word/document.xml");
            using (var writer = new StreamWriter(docEntry.Open(), Encoding.UTF8))
            {
                writer.Write(documentXml);
            }
        }

        return ms.ToArray();
    }
}