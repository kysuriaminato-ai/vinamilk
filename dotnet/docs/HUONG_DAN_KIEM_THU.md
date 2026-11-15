# HƯỚNG DẪN KIỂM THỬ HỆ THỐNG VINAMILK HRM (.NET 8)

> Đánh dấu `[x]` vào từng ô khi đạt. Gặp lỗi: chụp màn hình + ghi lại bước đang làm, gửi để viết prompt sửa.
> Mật khẩu mẫu (môi trường Development): `Vinamilk@2026` — lần đầu đăng nhập hệ thống sẽ bắt đổi mật khẩu. Ghi lại mật khẩu mới của từng tài khoản.

## Tài khoản dùng để test

| Username | Vai trò | Dùng để test |
|---|---|---|
| `admin` | SUPER_ADMIN | Cấu hình, phân quyền, mở khóa kỳ lương, backup |
| `huyen.ntt` | HR_MANAGER | Duyệt kỳ lương, duyệt đơn |
| `dung.la` | HR_STAFF | Tính lương, trình duyệt, hồ sơ, tuyển dụng |
| `tri.dm` | ACCOUNTANT | Xuất file ngân hàng, báo cáo lương |
| `hung.nv` | LINE_MANAGER | Duyệt đơn nghỉ của nhân viên dưới quyền |
| `tung.pt` | EMPLOYEE | Cổng nhân viên (ESS): xem phiếu lương, gửi đơn |

Mẹo: dùng 2 trình duyệt (hoặc 1 cửa sổ thường + 1 cửa sổ ẩn danh) để đăng nhập 2 tài khoản cùng lúc khi test quy trình duyệt.

---

## PHẦN 0 – Chuẩn bị môi trường

- [ ] **0.1** Có SQL Server (Express/Developer) đang chạy và mở được bằng SSMS với `Server = .` (hoặc `localhost\SQLEXPRESS` — nếu vậy sửa `DefaultConnection` trong `dotnet/src/Vinamilk.HRM.Web/appsettings.Development.json`).
- [ ] **0.2** Kiểm tra .NET 8: mở PowerShell, chạy `dotnet --version` → bắt đầu bằng `8.`
- [ ] **0.3** Cài công cụ EF (chỉ 1 lần): `dotnet tool install --global dotnet-ef`
- [ ] **0.4** Chạy test tự động:
  ```
  cd C:\xampp\htdocs\vinamilk\dotnet
  dotnet test
  ```
  ✅ Đạt: `Failed: 0`.
- [ ] **0.5** Tạo database:
  ```
  dotnet ef database update --project src\Vinamilk.HRM.Infrastructure --startup-project src\Vinamilk.HRM.Web
  ```
  ✅ Đạt: dòng cuối `Done.`; trong SSMS (F5 thư mục Databases) thấy `VinamilkHRM` với các bảng `Employees`, `PayrollPeriods`, `PitBrackets`, `PayrollStatutoryParameters`, `Users`…
- [ ] **0.6** Chạy web:
  ```
  $env:ASPNETCORE_ENVIRONMENT="Development"
  dotnet run --project src\Vinamilk.HRM.Web
  ```
  ✅ Đạt: thấy dòng `Now listening on: http://localhost:xxxx`. Mở đúng địa chỉ đó. Không tắt cửa sổ PowerShell trong suốt quá trình test.
- [ ] **0.7** Trong SSMS chạy: `SELECT COUNT(*) FROM Employees; SELECT Username, MustChangePassword FROM Users;` → có khoảng 55 nhân viên, 11 user, `MustChangePassword = 1`.

---

## PHẦN 1 – Đăng nhập & bảo mật

- [ ] **1.1** Mở trang chủ khi chưa đăng nhập → tự chuyển về trang Login.
- [ ] **1.2** Đăng nhập `admin` / `Vinamilk@2026` → bị chuyển sang trang **Đổi mật khẩu**. Thử gõ thẳng `/Employees` lên thanh địa chỉ → vẫn bị đẩy về Đổi mật khẩu.
- [ ] **1.3** Đổi mật khẩu thành công → vào được Dashboard.
- [ ] **1.4** Đăng xuất, đăng nhập bằng `Vinamilk@2026!` (có dấu `!`) → **phải bị từ chối**.
- [ ] **1.5** Đăng nhập `system.job` với mật khẩu bất kỳ → **phải bị từ chối**.
- [ ] **1.6** Nhập sai mật khẩu 5 lần liên tiếp cho `dung.la` → tài khoản bị khóa tạm thời, có thông báo rõ ràng.
- [ ] **1.7** Đăng nhập `tung.pt` (EMPLOYEE), gõ `/Payroll/Index` và `/Security` → trang 403 (không có quyền), không phải lỗi 500.
- [ ] **1.8** Khi chưa đăng nhập, mở `/swagger` hoặc gọi API `/api/v1/system/backup` → bị chặn (401/chuyển Login).
- [ ] **1.9** (admin) Vào Phân quyền: bỏ quyền `Payroll.Export` của ACCOUNTANT → đăng nhập `tri.dm` không còn nút/không xuất được file ngân hàng. Bật lại quyền sau khi test.
- [ ] **1.10** (admin) Mở Nhật ký hệ thống (AuditLog) → thấy các lần đăng nhập, đổi mật khẩu, đăng nhập `system.job` bị chặn.

---

## PHẦN 2 – Tổ chức & hồ sơ nhân sự (tài khoản `dung.la`)

- [ ] **2.1** Cơ cấu tổ chức hiển thị dạng cây, sơ đồ tổ chức vẽ đúng.
- [ ] **2.2** Thêm 1 nhân viên mới, đủ CCCD, số tài khoản ngân hàng, 1 người phụ thuộc. Bỏ trống trường bắt buộc → có báo lỗi tiếng Việt từng ô.
- [ ] **2.3** Thêm nhân viên trùng Mã NV hoặc trùng CCCD → bị chặn.
- [ ] **2.4** Trong SSMS: `SELECT TOP 5 * FROM Employees ORDER BY Id DESC` → cột CCCD / số tài khoản là **chuỗi mã hóa**, không đọc được số thật. Trên giao diện vẫn hiển thị đúng.
- [ ] **2.5** Tìm kiếm, lọc theo phòng ban, xuất Excel danh sách → file mở được, tiếng Việt không lỗi font.
- [ ] **2.6** Import Excel từ file mẫu: cố tình để 1 dòng sai → báo lỗi đúng dòng, các dòng đúng vẫn được nhập.
- [ ] **2.7** Tạo hợp đồng sắp hết hạn trong 30 ngày → xuất hiện cảnh báo/thông báo.
- [ ] **2.8** Tạo điều chuyển → duyệt → in Quyết định ra PDF đúng thông tin.
- [ ] **2.9** Đăng nhập `hung.nv` (LINE_MANAGER) → chỉ thấy nhân viên đơn vị mình, không thấy phòng khác.

---

## PHẦN 3 – Chấm công & nghỉ phép

- [ ] **3.1** (`dung.la`) Tạo ca làm, xếp ca cho 1 nhóm nhân viên.
- [ ] **3.2** Import bảng chấm công Excel → dữ liệu đi muộn / OT hiển thị đúng.
- [ ] **3.3** (`tung.pt`) Gửi đơn nghỉ phép 2 ngày → trạng thái "Chờ duyệt".
- [ ] **3.4** (`tung.pt`) Gửi đơn nghỉ vượt số phép còn lại → **bị chặn**.
- [ ] **3.5** (`hung.nv`) Thấy thông báo có đơn cần duyệt, duyệt đơn → `tung.pt` nhận thông báo, số phép còn lại giảm đúng 2.
- [ ] **3.6** Từ chối 1 đơn khác có ghi lý do → người gửi thấy lý do.
- [ ] **3.7** Khóa bảng công tháng → không sửa được dữ liệu công tháng đó nữa.

---

## PHẦN 4 – Tham số lương (tài khoản `admin`)

- [ ] **4.1** Vào **Payroll → Parameters**: có 3 giai đoạn tham số. Đối chiếu:

| Giai đoạn | Giảm trừ bản thân / NPT | Lương cơ sở | LTTV vùng I |
|---|---|---|---|
| đến 31/12/2025 | 11.000.000 / 4.400.000 | 2.340.000 | 4.960.000 |
| 01/01–30/06/2026 | 15.500.000 / 6.200.000 | 2.340.000 | 5.310.000 |
| từ 01/07/2026 | 15.500.000 / 6.200.000 | 2.530.000 | 5.310.000 |

- [ ] **4.2** Biểu thuế 2026 có **5 bậc**: 5% (≤10tr), 10% (10–30tr), 20% (30–60tr), 30% (60–100tr), 35% (>100tr). Biểu trước 2026 có 7 bậc.
- [ ] **4.3** Thử sửa tham số của giai đoạn đã có kỳ lương bị khóa → **bị chặn**.
- [ ] **4.4** Việc sửa tham số được ghi vào AuditLog.

> Kế toán/HR nên tự đối chiếu bảng trên với văn bản pháp luật hiện hành trước khi chạy lương thật.

---

## PHẦN 5 – Quy trình kỳ lương (phần quan trọng nhất)

Chọn kỳ **tháng 11/2026**.

- [ ] **5.1** (`dung.la`) Tạo kỳ lương 11/2026 → trạng thái `Draft`.
- [ ] **5.2** (`dung.la`) Bấm **Tính lương** → trạng thái `Calculated`, có danh sách nhân viên và số liệu.
- [ ] **5.3** **Tính tay đối chiếu 1 nhân viên** (chọn người lương khoảng 20–50 triệu, ghi số liệu vào bảng dưới):

| Bước | Công thức | Hệ thống | Tính tay | Khớp? |
|---|---|---|---|---|
| Tổng thu nhập (Gross) | lương + phụ cấp + OT + thưởng | | | |
| BH người lao động | 8% BHXH + 1,5% BHYT + 1% BHTN (trên lương đóng BH, có trần) | | | |
| Giảm trừ gia cảnh | 15.500.000 + 6.200.000 × số NPT | | | |
| Thu nhập tính thuế | Gross − BH − giảm trừ (nếu âm = 0) | | | |
| Thuế TNCN | theo biểu 5 bậc | | | |
| Thực lĩnh | Gross − BH − thuế − khấu trừ khác | | | |

  Ví dụ mẫu để tự kiểm tra cách tính: Gross 45.000.000, 1 NPT → BH 4.725.000 → TNTT 18.575.000 → thuế = 10tr×5% + 8,575tr×10% = **1.357.500** → thực lĩnh **38.917.500**.

- [ ] **5.4** Sửa lương 1 người → **Tính lại** cho riêng người đó → có lịch sử phiên bản, hiện cảnh báo nếu chênh > 20%.
- [ ] **5.5** (`dung.la`) **Trình duyệt** → trạng thái `Submitted`.
- [ ] **5.6** (`dung.la`) Thử tự **Duyệt** kỳ mình vừa trình → **phải bị chặn** (phân tách nhiệm vụ).
- [ ] **5.7** (`huyen.ntt`) Duyệt → `Approved`. Kiểm tra AuditLog ghi đúng người duyệt là `huyen.ntt`.
- [ ] **5.8** (`huyen.ntt`) **Khóa** kỳ → `Locked`. Thử tính lại → **bị chặn**.
- [ ] **5.9** (`dung.la`) Thử **Mở khóa** → bị chặn (không đủ quyền).
- [ ] **5.10** (`admin`) Mở khóa **không nhập lý do** → bị chặn; nhập lý do → mở được; AuditLog có lý do. Sau đó khóa lại.
- [ ] **5.11** Thử từ chối (Reject) ở 1 kỳ khác → kỳ quay về `Calculated`, người trình thấy ghi chú.

---

## PHẦN 6 – Phiếu lương, file ngân hàng, báo cáo

- [ ] **6.1** Tải phiếu lương PDF của 1 nhân viên → **đòi mật khẩu**; nhập 6 số cuối CCCD của người đó → mở được. Nhập sai → không mở được.
- [ ] **6.2** Phiếu lương có đủ: lương, phụ cấp, OT, BHXH/BHYT/BHTN, giảm trừ, TNTT, thuế từng bậc, thực lĩnh; số khớp màn hình.
- [ ] **6.3** **Gửi phiếu lương hàng loạt**: nếu chưa cấu hình SMTP, kiểm tra trạng thái gửi hiển thị `Failed` kèm lý do (không làm treo hệ thống). Nếu có SMTP test (vd. Gmail App Password hoặc smtp4dev/Mailtrap), nhận được email có đính kèm PDF.
- [ ] **6.4** (`tri.dm`) Xuất **file chuyển khoản** (CSV/Excel) → mở bằng Excel: tên không dấu viết hoa, đúng STK, **tổng tiền = tổng thực lĩnh** trên màn hình kỳ lương.
- [ ] **6.5** Xuất file ngân hàng khi kỳ mới ở `Calculated` → **bị chặn**.
- [ ] **6.6** Xuất báo cáo tổng hợp theo phòng ban → tổng quỹ lương, BH phần công ty & NLĐ, thuế khớp.
- [ ] **6.7** (`tung.pt`) Vào cổng ESS → thấy và tải được **phiếu lương của mình**. Sửa số ID trên URL tải phiếu của người khác → **403**.

---

## PHẦN 7 – Các module còn lại (kiểm nhanh)

- [ ] **7.1** Tuyển dụng: tạo yêu cầu → đăng tin → mở trang công khai `/tuyen-dung` (không cần đăng nhập) thấy tin → nộp CV → kéo thả ứng viên qua các cột kanban → chuyển thành nhân viên.
- [ ] **7.2** Onboarding: checklist tự sinh cho nhân viên mới, tick hoàn thành từng mục.
- [ ] **7.3** Đào tạo: tạo khóa, đăng ký, điểm danh, đánh giá.
- [ ] **7.4** KPI: tạo chu kỳ, nhân viên tự đánh giá, quản lý chấm, xếp loại.
- [ ] **7.5** Khen thưởng/kỷ luật: tạo quyết định, in PDF.
- [ ] **7.6** Báo cáo BM01–BM04 xuất được Excel/PDF.
- [ ] **7.7** Dashboard: số liệu hiển thị, biểu đồ chạy, lọc theo phòng ban/thời gian đổi số liệu.

---

## PHẦN 8 – Giao diện, SEO, vận hành

- [ ] **8.1** Thu nhỏ trình duyệt về cỡ điện thoại (F12 → biểu tượng điện thoại): menu thu gọn, bảng chuyển dạng thẻ, không vỡ bố cục.
- [ ] **8.2** Chuyển Dark mode / ngôn ngữ vi–en.
- [ ] **8.3** Mở `/robots.txt`, `/sitemap.xml` → có nội dung. Xem nguồn trang `/tuyen-dung/...` (Ctrl+U) có `JobPosting`. Xem nguồn trang quản trị có `noindex`.
- [ ] **8.4** Mở `/health/live` → `Healthy`.
- [ ] **8.5** Chrome DevTools → Lighthouse trên trang `/tuyen-dung` → Performance, SEO, Accessibility ≥ 90.
- [ ] **8.6** Tắt SQL Server (Services → SQL Server → Stop), tải lại trang → báo lỗi thân thiện, không hiện stack trace. Bật lại SQL Server.

---

## PHẦN 9 – Chạy song song với kế toán (trước khi dùng thật)

- [ ] **9.1** Lấy bảng lương Excel tháng gần nhất của kế toán (ít nhất 10 nhân viên, có người có NPT, có người lương cao trên trần BH).
- [ ] **9.2** Nhập cùng dữ liệu vào hệ thống, tính lương cùng kỳ.
- [ ] **9.3** So từng cột: Gross, BH, thuế, thực lĩnh. Lệch ≥ 1 đồng phải tìm ra nguyên nhân (làm tròn, khoản miễn thuế, ăn ca, phụ cấp không đóng BH…).
- [ ] **9.4** Ghi nhận mọi sai lệch → gửi để viết prompt sửa.

---

## Mẫu ghi lỗi

| # | Phần/Bước | Tài khoản | Mô tả lỗi | Kết quả mong đợi | Ảnh chụp |
|---|---|---|---|---|---|
| 1 | | | | | |
