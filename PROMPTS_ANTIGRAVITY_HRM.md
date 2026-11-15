# BỘ PROMPT THEO PHIÊN CHO ANTIGRAVITY – HỆ THỐNG QUẢN LÝ NHÂN LỰC (HRM) VINAMILK

> Cách dùng: Mỗi phiên dán **PROMPT NỀN (Phiên 0)** trước, sau đó dán prompt của phiên tương ứng. Chỉ chuyển phiên khi phiên trước đã build chạy được và đã commit git.
> Hiện trạng project: PHP MVC tự viết (`app/controllers`, `app/models`, `app/views`) + MySQL (XAMPP) + SPA JS mock (`index.html`, `js/modules`). Đích: **ASP.NET Core MVC (.NET 8 LTS) + EF Core + Clean Architecture + SQL Server (SSMS)**.

---

## PHIÊN 0 – PROMPT NỀN (dán đầu mọi phiên)

```
Bạn là Solution Architect + Senior Full-stack .NET + UI/UX Designer, chuyên phần mềm HRM doanh nghiệp (chuẩn tham chiếu: SAP SuccessFactors, Workday, MISA AMIS HRM, Base HRM, 1Office).

BỐI CẢNH:
- Project gốc tại thư mục hiện tại: PHP MVC tự viết + MySQL, gồm các module: Auth, Dashboard, Employee (hồ sơ, thử việc, nghỉ hưu, in mẫu 2C), Department, Attendance (chấm công, import, nghỉ phép), Payroll (bảng lương, phiếu lương, lịch sử), Recruitment, Training, KPI, Transfer (điều chuyển + in QĐ), Reward/Discipline (khen thưởng/kỷ luật + in QĐ), Reports (BM01–BM04), Roles/Permissions, ESS (Employee Self-Service), Onboarding, Analytics, AI Expert, AuditLog, Security.
- Tài liệu: README.md, BAO_CAO_HE_THONG_THONG_TIN_NHAN_LUC_VINAMILK.md, database/*.sql.
- Mục tiêu: xây dựng lại hoàn chỉnh trên ASP.NET Core MVC (.NET 8), EF Core 8, SQL Server, Clean Architecture; giữ nguyên nghiệp vụ, bổ sung chức năng còn thiếu theo chuẩn HRM doanh nghiệp.

KIẾN TRÚC BẮT BUỘC (solution `Vinamilk.HRM.sln`):
- Vinamilk.HRM.Domain: Entities, Value Objects, Enums, Domain Events, không phụ thuộc gì.
- Vinamilk.HRM.Application: CQRS (MediatR), DTO, FluentValidation, AutoMapper/Mapster, Interfaces, Pipeline Behaviors (Validation, Logging, Transaction).
- Vinamilk.HRM.Infrastructure: EF Core (DbContext, Configurations Fluent API, Migrations), Repository/UnitOfWork, Identity, Email, File Storage, Excel (ClosedXML), PDF (QuestPDF), Background Jobs (Hangfire).
- Vinamilk.HRM.Web: ASP.NET Core MVC + Razor Views + ViewComponents + Areas (Admin, ESS), Web API controllers (/api/v1) cho AJAX.
- Tests: xUnit + FluentAssertions + Testcontainers/SQL LocalDB.

QUY TẮC CHUNG:
- Code sạch SOLID, async/await, CancellationToken, không hard-code chuỗi kết nối (appsettings + User Secrets).
- Mọi bảng có: Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted (soft delete, global query filter), RowVersion (concurrency).
- Phân quyền RBAC theo Permission (policy-based), phân quyền dữ liệu theo phòng ban/đơn vị.
- Ghi AuditLog mọi thao tác thêm/sửa/xóa/duyệt.
- Giao diện, thông báo, validation bằng tiếng Việt; hỗ trợ đa ngôn ngữ (vi/en) qua Localization.
- Sau mỗi phiên: liệt kê file đã tạo/sửa, lệnh chạy (dotnet ef migrations add..., dotnet run), checklist kiểm thử, và những việc còn tồn đọng cho phiên sau.
- Không xóa project PHP cũ; tạo code mới trong thư mục `/dotnet`.
```

---

## PHIÊN 1 – PHÂN TÍCH HIỆN TRẠNG & GAP ANALYSIS

```
NHIỆM VỤ PHIÊN 1 (chỉ phân tích, chưa viết code):
1. Đọc toàn bộ project PHP (controllers, models, views, database/*.sql, js/modules, tài liệu .md). Lập bảng: Module | Chức năng hiện có | Bảng DB liên quan | Mức hoàn thiện (%) | Lỗi/nợ kỹ thuật.
2. So sánh với chuẩn HRM doanh nghiệp, liệt kê chức năng CÒN THIẾU, phân loại Must/Should/Could (MoSCoW). Tối thiểu xem xét:
   - Core HR: cơ cấu tổ chức nhiều cấp (Công ty > Khối > Phòng > Tổ), chức danh/ngạch bậc, sơ đồ tổ chức động, lịch sử công tác, người phụ thuộc, bằng cấp/chứng chỉ, tài liệu đính kèm, quản lý hợp đồng (gia hạn, cảnh báo hết hạn, phụ lục).
   - Chấm công: ca làm việc, ca xoay, OT, đi muộn/về sớm, công tác, tích hợp máy chấm công/GPS/QR, quy trình duyệt nhiều cấp.
   - Nghỉ phép: chính sách phép năm, phép tồn, nghỉ chế độ, lịch nghỉ lễ.
   - Lương: công thức lương cấu hình được, phụ cấp, BHXH/BHYT/BHTN theo quy định hiện hành VN, thuế TNCN lũy tiến, giảm trừ gia cảnh, khóa kỳ lương, gửi phiếu lương email.
   - Tuyển dụng (ATS): yêu cầu tuyển dụng, kanban ứng viên, lịch phỏng vấn, đánh giá, offer, chuyển ứng viên → nhân viên.
   - Onboarding/Offboarding checklist, bàn giao tài sản.
   - Đào tạo: khóa học, kế hoạch, đánh giá sau đào tạo, chi phí.
   - KPI/OKR, đánh giá 360°, chu kỳ đánh giá.
   - ESS/MSS: nhân viên tự xem/cập nhật, gửi đơn; quản lý duyệt.
   - Workflow engine phê duyệt cấu hình được, thông báo realtime (SignalR), email.
   - Báo cáo động, dashboard analytics (turnover, headcount, chi phí nhân sự), export Excel/PDF.
   - Bảo mật: 2FA, khóa tài khoản, chính sách mật khẩu, nhật ký đăng nhập, mã hóa dữ liệu nhạy cảm (CCCD, STK ngân hàng), tuân thủ Nghị định 13/2023/NĐ-CP về dữ liệu cá nhân.
3. Đề xuất ERD tổng thể (Mermaid erDiagram) cho SQL Server, chuẩn hóa 3NF, quy ước đặt tên.
4. Đề xuất lộ trình các phiên tiếp theo (điều chỉnh nếu khác với lộ trình tôi đưa).
ĐẦU RA: file `docs/01_GAP_ANALYSIS.md` và `docs/02_ERD.md`.
```

---

## PHIÊN 2 – KHỞI TẠO SOLUTION CLEAN ARCHITECTURE & DATABASE SQL SERVER

```
NHIỆM VỤ PHIÊN 2:
1. Tạo solution .NET 8 theo kiến trúc ở Prompt nền, cài package: EF Core SqlServer, MediatR, FluentValidation, Mapster, Serilog (ghi file + SQL), Hangfire, ClosedXML, QuestPDF, Swashbuckle.
2. Viết Domain Entities + EF Configurations cho toàn bộ ERD ở docs/02_ERD.md (dùng decimal(18,2) cho tiền, nvarchar cho tiếng Việt, index cho cột tìm kiếm, unique cho Mã NV/Email/CCCD).
3. ApplicationDbContext: soft-delete filter, auto audit fields qua SaveChanges interceptor, RowVersion.
4. Migration đầu tiên + Seed data: roles mặc định (SuperAdmin, HR Manager, HR Staff, Line Manager, Employee, Accountant), permissions, phòng ban mẫu, 50 nhân viên mẫu (chuyển từ database/*.sql MySQL sang).
5. Script chuyển dữ liệu MySQL → SQL Server (`tools/migrate-mysql-to-mssql`), hướng dẫn chạy trên SSMS.
6. Tạo Stored Procedure/View tối ưu cho báo cáo nặng (headcount theo tháng, tổng quỹ lương).
7. appsettings.json chuỗi kết nối: `Server=.;Database=VinamilkHRM;Trusted_Connection=True;TrustServerCertificate=True`.
ĐẦU RA: solution build thành công, `dotnet ef database update` tạo DB trên SSMS.
```

---

## PHIÊN 3 – THEME, DESIGN SYSTEM & LAYOUT GIAO DIỆN

```
NHIỆM VỤ PHIÊN 3 – GIAO DIỆN:
1. Phân tích và so sánh 3–4 theme admin phù hợp HRM (ví dụ: Tabler, AdminLTE 4, CoreUI, Sneat/Materio Bootstrap 5) theo tiêu chí: giấy phép thương mại, Bootstrap 5, dark mode, RTL, độ nhẹ, component bảng/biểu đồ/form. Chọn 1 và giải thích. (Ưu tiên: Tabler – MIT, nhẹ, hiện đại.)
2. Xây Design System riêng cho Vinamilk HRM:
   - Màu chủ đạo xanh dương Vinamilk (#0213B0 / #1E3A8A) + trắng sữa, màu trạng thái success/warning/danger/info; CSS variables; Light/Dark mode lưu theo user.
   - Font: Be Vietnam Pro (hỗ trợ tiếng Việt tốt), thang chữ, spacing 4/8px, bo góc, shadow.
3. Layout Razor: `_Layout.cshtml` (sidebar thu gọn được, menu theo quyền, breadcrumb, topbar có tìm kiếm toàn cục Ctrl+K, chuông thông báo, avatar menu, chuyển ngôn ngữ, chuyển theme), `_LayoutAuth.cshtml`, `_LayoutPrint.cshtml` (in A4 quyết định/biểu mẫu).
4. Component tái sử dụng (ViewComponent/Partial/TagHelper): DataTable (server-side paging, sort, filter, export), Modal form AJAX, Toast, Confirm dialog (SweetAlert2), Select2 tìm kiếm, Date picker (Flatpickr, định dạng dd/MM/yyyy), Upload kéo-thả, Stat card, Empty state, Skeleton loading, Badge trạng thái, Timeline, Stepper.
5. HIỆU ỨNG (tinh tế, không lạm dụng, tôn trọng `prefers-reduced-motion`):
   - Động: page transition fade, skeleton shimmer khi tải, count-up số liệu dashboard, chart animate (ApexCharts), hover nâng card, ripple nút, sidebar slide, toast slide-in, modal zoom, progress bar NProgress khi AJAX, drag & drop kanban (SortableJS), scroll reveal nhẹ (AOS) cho trang giới thiệu.
   - Tĩnh: hệ thống icon nhất quán (Tabler Icons), illustration cho trang lỗi/empty, focus ring rõ ràng.
6. Responsive mobile-first (bảng chuyển dạng card trên mobile), đạt WCAG 2.1 AA (tương phản, aria-label, điều hướng bàn phím).
7. Trang: Đăng nhập (có 2FA), 403, 404, 500, bảo trì.
ĐẦU RA: layout + style guide page `/dev/styleguide` hiển thị mọi component.
```

---

## PHIÊN 4 – XÁC THỰC, PHÂN QUYỀN, BẢO MẬT, AUDIT

```
NHIỆM VỤ PHIÊN 4:
1. ASP.NET Core Identity tùy biến (User gắn Employee), đăng nhập, quên mật khẩu qua email, đổi mật khẩu, 2FA (TOTP Authenticator), khóa tài khoản sau 5 lần sai, chính sách mật khẩu, bắt đổi mật khẩu lần đầu.
2. RBAC policy-based: bảng Permission (Module.Action, ví dụ Employee.View/Create/Edit/Delete/Export/Approve), màn hình ma trận Role × Permission (checkbox), gán role cho user; Data scope (Toàn công ty / Đơn vị / Phòng / Bản thân).
3. Bảo mật: Anti-forgery, chống XSS, security headers (CSP, HSTS, X-Frame-Options), rate limiting đăng nhập, mã hóa cột nhạy cảm (Always Encrypted hoặc Data Protection API), quản lý phiên đăng nhập (xem & đăng xuất thiết bị).
4. AuditLog: ai – lúc nào – IP – thao tác – dữ liệu cũ/mới (JSON diff); màn hình tra cứu + lọc.
5. Unit test cho permission handler.
```

---

## PHIÊN 5 – CORE HR: TỔ CHỨC, NHÂN VIÊN, HỢP ĐỒNG

```
NHIỆM VỤ PHIÊN 5 (CQRS đầy đủ cho từng chức năng):
1. Cơ cấu tổ chức dạng cây nhiều cấp (CRUD, kéo thả sắp xếp), sơ đồ tổ chức trực quan (OrgChart.js/d3), chức danh, ngạch bậc lương.
2. Hồ sơ nhân viên dạng tab: Thông tin chung, Liên hệ, Gia đình/người phụ thuộc, Học vấn & chứng chỉ, Quá trình công tác, Hợp đồng, Lương & phụ cấp, Khen thưởng/kỷ luật, Đào tạo, Tài liệu đính kèm, Lịch sử thay đổi. Ảnh đại diện crop.
3. Danh sách nhân viên: tìm kiếm nâng cao, lọc đa điều kiện, lưu bộ lọc, chọn cột hiển thị, import Excel (có file mẫu + báo lỗi từng dòng), export Excel/PDF, in Sơ yếu lý lịch mẫu 2C.
4. Hợp đồng lao động: loại HĐ, phụ lục, gia hạn, cảnh báo hết hạn (Hangfire job hằng ngày gửi email/thông báo trước 30 ngày), in HĐ từ template.
5. Thử việc, nghỉ hưu (tính theo Bộ luật Lao động 2019 lộ trình tuổi nghỉ hưu), nghỉ việc (offboarding).
6. Điều chuyển/bổ nhiệm/thăng chức có quy trình duyệt và in Quyết định.
```

---

## PHIÊN 6 – CHẤM CÔNG, CA LÀM, NGHỈ PHÉP & WORKFLOW DUYỆT

```
NHIỆM VỤ PHIÊN 6:
1. Workflow engine dùng chung: định nghĩa quy trình nhiều bước (theo cấp quản lý/role/người cụ thể), duyệt/từ chối/ủy quyền, lịch sử duyệt, áp dụng cho mọi loại đơn.
2. Ca làm việc, xếp ca theo lịch (calendar kéo thả), ngày lễ.
3. Chấm công: import từ máy chấm công/Excel, chấm công web có GPS/QR, bảng công tháng, đi muộn/về sớm, OT, công tác, giải trình công, khóa bảng công.
4. Nghỉ phép: chính sách phép năm theo thâm niên, phép tồn chuyển năm, các loại nghỉ chế độ, đơn nghỉ qua workflow, lịch nghỉ team.
5. Thông báo realtime SignalR + email khi có đơn cần duyệt.
```

---

## PHIÊN 7 – TIỀN LƯƠNG, BẢO HIỂM, THUẾ TNCN

```
NHIỆM VỤ PHIÊN 7:
1. Cấu hình thành phần lương & công thức (lương cơ bản, phụ cấp, thưởng, OT, khấu trừ) – công thức lưu DB, tính bằng expression engine (NCalc).
2. Bảo hiểm BHXH/BHYT/BHTN và thuế TNCN: tham số tỷ lệ, mức trần, giảm trừ gia cảnh, biểu thuế lũy tiến LƯU TRONG BẢNG CẤU HÌNH theo ngày hiệu lực (không hard-code), seed theo quy định hiện hành – đánh dấu cần HR kiểm tra lại số liệu pháp lý.
3. Kỳ lương: tạo kỳ → lấy công → tính lương → kiểm tra → duyệt → khóa; tính lại; so sánh với kỳ trước.
4. Phiếu lương PDF, gửi email hàng loạt (bảo vệ bằng mật khẩu), nhân viên xem trên ESS; xuất file chuyển khoản ngân hàng, báo cáo quỹ lương.
5. Unit test đầy đủ cho phần tính lương/thuế.
```

---

## PHIÊN 8 – TUYỂN DỤNG, ONBOARDING, ĐÀO TẠO, KPI, KHEN THƯỞNG/KỶ LUẬT

```
NHIỆM VỤ PHIÊN 8:
1. Tuyển dụng (ATS): yêu cầu tuyển dụng có duyệt, tin tuyển dụng, trang Careers công khai (chuẩn SEO), nộp CV online, pipeline kanban kéo thả, lịch phỏng vấn, phiếu đánh giá, offer letter, chuyển ứng viên thành nhân viên.
2. Onboarding/Offboarding checklist theo mẫu, giao việc cho bộ phận, bàn giao tài sản.
3. Đào tạo: danh mục khóa học, kế hoạch năm, đăng ký, điểm danh, đánh giá, chứng chỉ, chi phí.
4. KPI/OKR: bộ chỉ tiêu, chu kỳ đánh giá, tự đánh giá → quản lý đánh giá → hiệu chỉnh, đánh giá 360°, xếp loại, liên kết thưởng.
5. Khen thưởng/kỷ luật: quy trình, in quyết định, báo cáo BM03.
```

---

## PHIÊN 9 – DASHBOARD, BÁO CÁO, ESS/MSS, AI

```
NHIỆM VỤ PHIÊN 9:
1. Dashboard theo vai trò (Ban giám đốc, HR, Quản lý, Nhân viên): headcount, biến động, tỷ lệ nghỉ việc, cơ cấu tuổi/giới tính/trình độ, chi phí nhân sự, HĐ sắp hết hạn, sinh nhật – dùng ApexCharts, lọc theo thời gian/đơn vị, cache bằng IMemoryCache/Redis.
2. Báo cáo: chuyển đổi BM01–BM04 hiện có, thêm báo cáo động (chọn cột, lọc, nhóm), lên lịch gửi báo cáo định kỳ qua email.
3. ESS/MSS (Area riêng, tối ưu mobile, có thể PWA): xem hồ sơ, phiếu lương, bảng công, gửi đơn, xem trạng thái; quản lý duyệt nhanh.
4. AI Expert: chatbot hỏi đáp chính sách nhân sự (RAG trên tài liệu nội bộ), gợi ý ứng viên phù hợp, dự báo nguy cơ nghỉ việc – thiết kế qua interface để thay nhà cung cấp AI.
```

---

## PHIÊN 10 – SEO, HIỆU NĂNG, KIỂM THỬ, TRIỂN KHAI

```
NHIỆM VỤ PHIÊN 10:
1. SEO (cho phần công khai: trang giới thiệu, Careers, tin tuyển dụng):
   - URL thân thiện có slug tiếng Việt không dấu, thẻ title/meta description động, canonical, Open Graph/Twitter Card, JSON-LD schema `JobPosting` & `Organization`, sitemap.xml & robots.txt tự sinh, breadcrumb schema, heading chuẩn, alt ảnh.
   - Phần quản trị nội bộ: `noindex, nofollow`.
2. Hiệu năng (Core Web Vitals: LCP < 2.5s, CLS < 0.1, INP < 200ms): bundling & minify (WebOptimizer), nén Brotli/Gzip, response caching, lazy-load ảnh, WebP, preload font, CDN cho thư viện, phân trang server-side, AsNoTracking, index SQL, kiểm tra query chậm.
3. Kiểm thử: unit test Application, integration test API, test UI cơ bản (Playwright), mục tiêu coverage ≥ 70% phần nghiệp vụ.
4. Health checks, Serilog + Seq, xử lý lỗi toàn cục (ProblemDetails), backup DB định kỳ.
5. Triển khai: Dockerfile + docker-compose (web + SQL Server), hướng dẫn IIS, CI/CD GitHub Actions (build, test, publish).
6. Tài liệu: README, hướng dẫn sử dụng theo vai trò, tài liệu API Swagger.
ĐẦU RA: checklist go-live và danh sách cải tiến tương lai.
```

---

## PROMPT TIỆN ÍCH (dùng bất kỳ lúc nào)

**Review & sửa lỗi:**
```
Rà soát toàn bộ code đã viết ở phiên trước: lỗi build, lỗi logic, bảo mật (OWASP Top 10), N+1 query, thiếu validation, thiếu phân quyền, vi phạm Clean Architecture. Liệt kê theo mức độ nghiêm trọng rồi sửa.
```

**Tiếp tục khi bị ngắt:**
```
Tiếp tục đúng phiên [số] từ chỗ dừng. Trước tiên liệt kê những gì đã xong và còn thiếu theo yêu cầu phiên, sau đó làm tiếp, không viết lại phần đã hoàn thành.
```

**Thêm chức năng mới:**
```
Thêm chức năng [tên] vào module [tên] theo đúng kiến trúc hiện tại: Entity → Configuration → Migration → Command/Query + Validator → API/Controller → Razor View dùng component chung → Permission → AuditLog → Test. Trình bày phân tích nghiệp vụ ngắn trước khi code.
```
