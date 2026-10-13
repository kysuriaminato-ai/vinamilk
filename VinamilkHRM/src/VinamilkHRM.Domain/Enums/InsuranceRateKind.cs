namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Các quỹ bảo hiểm bắt buộc và kinh phí công đoàn theo luật Việt Nam.
/// </summary>
public enum InsuranceRateKind
{
    BHXH_HuuTri = 1, // Bảo hiểm Xã hội - Quỹ Hưu trí & Tử tuất (NLĐ 8%, NSDLĐ 14%)
    BHXH_OmDau = 2,  // Bảo hiểm Xã hội - Quỹ Ốm đau & Thai sản (NLĐ 0%, NSDLĐ 3%)
    TNLD_BNN = 3,    // Bảo hiểm Tai nạn lao động - Bệnh nghề nghiệp (NLĐ 0%, NSDLĐ 0.5%)
    BHYT = 4,        // Bảo hiểm Y tế (NLĐ 1.5%, NSDLĐ 3%)
    BHTN = 5,        // Bảo hiểm Thất nghiệp (NLĐ 1%, NSDLĐ 1%)
    KPCD = 6         // Kinh phí Công đoàn (NSDLĐ 2%)
}
