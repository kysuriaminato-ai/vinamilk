using System;
using VinamilkHRM.Domain.Entities.Careers;
using VinamilkHRM.Domain.Entities.Privacy;
using VinamilkHRM.Domain.Enums;
using Xunit;

namespace VinamilkHRM.Domain.Tests.Privacy;

public class PersonalDataProtectionTests
{
    [Fact]
    public void PersonalDataAccessLog_WhenViewingOtherEmployeeCitizenId_ShouldCreateValidAuditRecord()
    {
        // ARRANGE & ACT: Người dùng (ViewerUserId: 15) mở xem CCCD của nhân viên khác (TargetEmployeeId: 88)
        int viewerId = 15;
        int targetEmployeeId = 88;
        string accessedField = "CitizenId";
        string purpose = "Kiểm tra đối soát thông tin định danh nhân sự (Nghị định 13/2023)";
        string ipAddress = "10.20.30.40";

        var log = new PersonalDataAccessLog(
            viewerId,
            targetEmployeeId,
            accessedField,
            purpose,
            ipAddress,
            isAuthorized: true);

        // ASSERT: Tiêu chí 4 - Mở xem CCCD của người khác -> có bản ghi trong nhật ký truy cập
        Assert.Equal(15, log.ViewerUserId);
        Assert.Equal(88, log.TargetEmployeeId);
        Assert.Equal("CitizenId", log.AccessedField);
        Assert.Equal(purpose, log.AccessPurpose);
        Assert.Equal("10.20.30.40", log.IpAddress);
        Assert.True(log.IsAuthorized);
        Assert.True(log.AccessTimestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void PersonalDataConsent_RecordAndWithdraw_ShouldMaintainAuditIntegrity()
    {
        // ARRANGE: Ghi nhận sự đồng ý của nhân viên khi tiếp nhận
        int employeeId = 55;
        var consent = new PersonalDataConsent(
            subjectId: employeeId,
            subjectType: "Employee",
            subjectEmail: "employee55@vinamilk.com.vn",
            consentPurpose: "PayrollAndTax",
            termsVersion: "VNM-PDP-v2026.1",
            isAgreed: true,
            ipAddress: "192.168.1.100");

        // ASSERT
        Assert.Equal(55, consent.SubjectId);
        Assert.Equal("PayrollAndTax", consent.ConsentPurpose);
        Assert.Equal("VNM-PDP-v2026.1", consent.TermsVersion);
        Assert.True(consent.IsAgreed);
        Assert.Null(consent.WithdrawnAt);

        // ACT: Chủ thể dữ liệu rút lại sự đồng ý
        var withdrawTime = DateTimeOffset.UtcNow;
        consent.Withdraw(withdrawTime);

        // ASSERT
        Assert.False(consent.IsAgreed);
        Assert.Equal(withdrawTime, consent.WithdrawnAt);
    }

    [Fact]
    public void DataRetentionPolicy_And_CandidateAnonymize_ShouldComplyWithDecree13()
    {
        // ARRANGE: Tạo hồ sơ ứng viên bị từ chối
        var candidate = new CandidateApplication(
            jobPostingId: 1,
            fullName: "Nguyễn Văn Không Trúng Tuyển",
            email: "candidate.rejected@gmail.com",
            phoneNumber: "0987654321",
            citizenId: "079099009988",
            dateOfBirth: new DateTime(1998, 4, 12),
            gender: Gender.Male,
            address: "Quận 1, TP. Hồ Chí Minh",
            resumeFileName: "CV_NguyenVanKTT.pdf",
            coverLetter: "Thư ứng tuyển",
            portfolioUrl: "https://portfolio.me");

        candidate.Reject("Không đạt vòng phỏng vấn chuyên môn");
        Assert.False(candidate.IsAnonymized);
        Assert.Null(candidate.AnonymizedAt);

        // ACT: Tác vụ ẩn danh hồ sơ quá hạn lưu trữ theo chính sách DataRetentionPolicy
        candidate.Anonymize();

        // ASSERT: Dữ liệu nhạy cảm được ẩn danh/xóa bỏ hoàn toàn
        Assert.True(candidate.IsAnonymized);
        Assert.NotNull(candidate.AnonymizedAt);
        Assert.Equal("HỒ SƠ ĐÃ ẨN DANH", candidate.FullName);
        Assert.Equal("0000000000", candidate.PhoneNumber);
        Assert.Equal("REDACTED", candidate.CitizenId);
        Assert.Equal("REDACTED", candidate.Address);
        Assert.Empty(candidate.ResumeFileName);
        Assert.Empty(candidate.CoverLetter);
        Assert.Contains("anonymized_", candidate.Email);
        Assert.Contains("Nghị định 13/2023/NĐ-CP", candidate.EvaluationNotes);
    }
}