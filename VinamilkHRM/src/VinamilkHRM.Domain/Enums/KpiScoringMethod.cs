namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Phương pháp tính điểm hoàn thành chỉ tiêu KPI.
/// </summary>
public enum KpiScoringMethod
{
    HigherIsBetter = 1,  // Càng cao càng tốt: TL = Thực hiện / Mục tiêu * 100
    LowerIsBetter = 2,   // Càng thấp càng tốt: TL = Mục tiêu / Thực hiện * 100 (Thực hiện = 0 thì 120)
    Binary = 3,          // Nhị phân: Đạt 100, Không đạt 0
    ThresholdTable = 4,  // Tra bảng ngưỡng (ThresholdTable)
    Task = 5             // Công việc/nhiệm vụ theo hạn hoàn thành (DueDate, SLA)
}
