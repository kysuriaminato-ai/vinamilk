namespace VinamilkHRM.Application.Common.Security;

/// <summary>
/// Danh mục quyền hạn chuẩn "Module.Action" kế thừa và đồng bộ 100% với Portal PHP.
/// </summary>
public static class Permissions
{
    // Phân hệ Hồ sơ Nhân sự
    public const string NhansuView = "NHANSU.View";
    public const string NhansuAdd = "NHANSU.Add";
    public const string NhansuEdit = "NHANSU.Edit";
    public const string NhansuDelete = "NHANSU.Delete";
    public const string NhansuExport = "NHANSU.Export";
    public const string NhansuManage = "NHANSU.Edit";
    public const string NhansuViewSensitive = "NHANSU.ViewSensitive"; // Quyền xem dữ liệu nhạy cảm chưa che (CCCD, Số tài khoản ngân hàng)

    // Alias tiện dụng cho Tiền lương
    public const string TienluongView = "LUONG.View";
    public const string TienluongCalculate = "LUONG.Edit";

    // Phân hệ Chấm công & Ca kíp
    public const string ChamcongView = "CHAM_CONG.View";
    public const string ChamcongAdd = "CHAM_CONG.Add";
    public const string ChamcongEdit = "CHAM_CONG.Edit";
    public const string ChamcongManage = "CHAM_CONG.Edit";
    public const string ChamcongApprove = "CHAM_CONG.Approve";
    public const string ChamcongExport = "CHAM_CONG.Export";
    public const string ChamcongLock = "CHAM_CONG.Lock";

    // Phân hệ Tiền lương & Thuế 3P
    public const string LuongView = "LUONG.View";
    public const string LuongAdd = "LUONG.Add";
    public const string LuongEdit = "LUONG.Edit";
    public const string LuongApprove = "LUONG.Approve";
    public const string LuongExport = "LUONG.Export";

    // Phân hệ Đánh giá Hiệu suất KPI
    public const string KpiView = "KPI.View";
    public const string KpiEvaluate = "KPI.Evaluate";
    public const string KpiApprove = "KPI.Approve";

    // Phân hệ Cơ cấu Tổ chức & Đơn vị
    public const string DonviView = "DONVI.View";
    public const string DonviEdit = "DONVI.Edit";

    // Phân hệ Tuyển dụng & Onboarding
    public const string TuyendungView = "TUYEN_DUNG.View";
    public const string TuyendungAdd = "TUYEN_DUNG.Add";
    public const string TuyendungEdit = "TUYEN_DUNG.Edit";
    public const string TuyendungDelete = "TUYEN_DUNG.Delete";
    public const string TuyendungApprove = "TUYEN_DUNG.Approve";

    // Phân hệ Đào tạo
    public const string DaotaoView = "DAO_TAO.View";
    public const string DaotaoAdd = "DAO_TAO.Add";
    public const string DaotaoEdit = "DAO_TAO.Edit";

    // Phân hệ Khen thưởng - Kỷ luật
    public const string KtklView = "KTKL.View";
    public const string KtklAdd = "KTKL.Add";
    public const string KtklApprove = "KTKL.Approve";

    // Phân hệ Báo cáo & Phân tích HR Analytics
    public const string BaocaoView = "BAO_CAO.View";
    public const string BaocaoExport = "BAO_CAO.Export";

    // Phân hệ Quản trị Hệ thống
    public const string HethongView = "HE_THONG.View";
    public const string HethongEdit = "HE_THONG.Edit";

    public static readonly IReadOnlyList<string> All = new[]
    {
        NhansuView, NhansuAdd, NhansuEdit, NhansuDelete, NhansuExport, NhansuViewSensitive,
        ChamcongView, ChamcongAdd, ChamcongEdit, ChamcongApprove, ChamcongExport,
        LuongView, LuongAdd, LuongEdit, LuongApprove, LuongExport,
        KpiView, KpiEvaluate, KpiApprove,
        DonviView, DonviEdit,
        TuyendungView, TuyendungAdd, TuyendungEdit, TuyendungDelete, TuyendungApprove,
        DaotaoView, DaotaoAdd, DaotaoEdit,
        KtklView, KtklAdd, KtklApprove,
        BaocaoView, BaocaoExport,
        HethongView, HethongEdit
    };
}
