# BÁO CÁO TOÀN DIỆN KIỂM TOÁN HỆ THỐNG & ĐÁNH GIÁ AN NINH (SYSTEM AUDIT & SECURITY REPORT)
## Dự án: Vinamilk Enterprise HRM
**Tổ chức**: Công ty Cổ phần Sữa Việt Nam (Vinamilk)  
**Repository**: `kysuriaminato-ai/vinamilk`  
**Vai trò thẩm định**: Principal Software Architect & Lead Security Engineer  
**Thời điểm thực hiện**: Tháng 10/2026 (Phiên 0 - Tái cơ cấu & Vá bảo mật khẩn cấp)

---

## 1. CÂY THƯ MỤC HỆ THỐNG HIỆN TẠI (REPOSITORY DIRECTORY TREE)

Sau đợt tái cấu trúc và phân lập kiến trúc đích (Separation of Concerns), toàn bộ mã nguồn của dự án được tổ chức chuẩn mực như sau:

```
vinamilk/
├── .agents/                                # Quy ước cấu hình Agentic AI
│   └── rules/                              # Các mô-đun luật nghiệp vụ & công nghệ
│       ├── backend-dotnet.md               # Quy ước Clean Architecture .NET, CancellationToken, No Lazy Loading
│       ├── sql-server.md                   # Quy chuẩn CSDL SQL Server 2022, Vietnamese_CI_AS, Concurrency
│       ├── portal-php.md                   # Quy chuẩn Web Portal PHP 8.2+, PSR-12, XSS/CSRF
│       └── payroll-legal.md                # Quy chuẩn pháp lý tiền lương, thuế 2026
├── .github/                                # Quy trình CI/CD tự động
│   └── workflows/                          # GitHub Actions (php-l, phpunit, build)
├── docs/                                   # Tài liệu kiến trúc và kiểm toán hệ thống
│   ├── AUDIT.md                            # Báo cáo kiểm toán & an ninh hiện tại
│   ├── ARCHITECTURE.md                     # Bản thiết kế kiến trúc đích 4 tầng Clean Architecture
│   ├── BRD_VINAMILK_HRIS.md                # Tài liệu yêu cầu nghiệp vụ doanh nghiệp (BRD)
│   ├── legacy-ui/                          # SPA UI tĩnh cũ (index.html, js/, css/ dùng dữ liệu giả để tham khảo)
│   │   ├── css/
│   │   ├── js/
│   │   └── index.html
│   ├── prompts/                            # Lưu trữ prompt chỉ dẫn kỹ thuật
│   └── sessions/                           # Nhật ký các phiên làm việc
│       └── CHANGELOG.md                    # Lịch sử thay đổi hệ thống theo từng phiên
├── legacy/                                 # Lưu trữ các thành phần di sản để đối chiếu
│   └── mysql/                              # Toàn bộ mã nguồn CSDL MySQL cũ
│       ├── 001_unify.sql                   # Script hợp nhất CSDL
│       ├── 003_employee_module.sql         # Module nhân sự MySQL
│       ├── 004_attendance_module.sql       # Module chấm công MySQL
│       ├── 005_payroll_3p_service.sql      # Module tính lương 3P MySQL
│       ├── 006_fact_headcount_dashboard.sql# Bảng thực tế định biên nhân sự
│       ├── 007_ai_risk_score.sql           # Đánh giá rủi ro nhân sự AI
│       ├── 008_certificates_ppe_offboarding.sql # Chứng chỉ, BHLĐ & thôi việc
│       ├── 2026_10_12_vinamilk_hrm_full_migration.sql
│       ├── unified_vinamilk_hris_schema.sql
│       └── vinamilk_hrm.sql                # CSDL MySQL 22 bảng nguyên bản ban đầu
├── portal/                                 # Frontend Web Portal (PHP 8.2+ MVC thuần đóng vai trò giao diện gọi Web API)
│   ├── app/
│   │   ├── config/config.php               # Cấu hình hệ thống, session, kết nối
│   │   ├── controllers/                    # 22+ Bộ điều khiển (đã được vá bảo mật và kiểm tra quyền hạn)
│   │   ├── core/                           # Nhân Framework: App, Controller, Database, Model, Session, AuthMiddleware
│   │   ├── helpers/                        # Trợ giúp: Auth, Functions, Menu, Notification
│   │   ├── models/                         # Lớp dữ liệu và thực thi logic phân quyền scopeByUnit
│   │   ├── services/                       # Dịch vụ nghiệp vụ phụ trợ (Snapshot, Leave, Certificate...)
│   │   └── views/                          # Giao diện Master Layout & Views nghiệp vụ phong cách Vinamilk
│   ├── public/                             # Web root cho HTTP Server (Assets CSS, JS, Vendor)
│   │   ├── assets/
│   │   ├── .htaccess                       # Rewrite điều hướng Front Controller
│   │   └── index.php                       # File khởi động chính của Portal
│   ├── tests/                              # Bộ kiểm thử PHPUnit cho Portal
│   ├── .htaccess                           # Rewrite tự động từ /portal vào /portal/public
│   ├── composer.json                       # Định nghĩa phụ thuộc PHP
│   ├── index.php                           # Chuyển tiếp vào public/index.php
│   ├── phpunit.phar                        # Công cụ kiểm thử độc lập
│   └── phpunit.xml                         # Cấu hình kiểm thử tự động
├── tools/                                  # Công cụ và script bảo trì tiện ích
│   ├── clean.ps1                           # Script dọn dẹp thư mục tạm
│   ├── remove_emojis.js                    # Script chuẩn hóa văn bản
│   └── remove_emojis.py
├── VinamilkHRM/                            # BACKEND LÕI NGHIỆP VỤ DUY NHẤT (.NET 9 Clean Architecture)
│   ├── database/                           # Script CSDL SQL Server 2022
│   │   └── VinamilkHRMDb_Full_Schema_And_Seed.sql # CSDL SQL Server 16 bảng & Dữ liệu mẫu
│   ├── src/
│   │   ├── VinamilkHRM.Domain/             # Tầng Nghiệp vụ cốt lõi (Entities, Value Objects, Enums, Exceptions)
│   │   │   ├── Common/                     # BaseEntity, BaseAuditableEntity, DomainException, ValueObject
│   │   │   ├── Entities/                   # 26 thực thể nghiệp vụ (Assets, Attendances, Contracts, Employees...)
│   │   │   ├── Enums/                      # ContractType, KpiStatus, PaySlipStatus, UnitType, RegionScope...
│   │   │   └── Interfaces/                 # Interfaces Repository, ScopeService, PayslipPrinter
│   │   ├── VinamilkHRM.Application/        # Tầng Ứng dụng Use Case (DTOs, Services, Business Logic)
│   │   │   ├── Common/                     # Result pattern, Interfaces
│   │   │   ├── DTOs/                       # DTOs cho các phân hệ
│   │   │   ├── Interfaces/                 # IPayrollService, IKpiService, IEmployeeService...
│   │   │   └── Services/                   # PayrollService, ScopeService...
│   │   ├── VinamilkHRM.Infrastructure/     # Tầng Hạ tầng & CSDL (EF Core, Repositories, Dịch vụ in ấn)
│   │   │   ├── Data/                       # VinamilkHRMDbContext & Fluent API Configurations
│   │   │   ├── Repositories/               # UnitOfWork, EmployeeRepository, PaySlipRepository...
│   │   │   └── Services/                   # In phiếu lương đa hình, Xuất báo cáo, Phân vùng miền
│   │   └── VinamilkHRM.WebUI/              # Tầng Trình diễn / Web API (Controllers, Views, Middlewares)
│   │       ├── Controllers/                # HomeController, EmployeesController, PayrollController...
│   │       ├── Views/                      # Razor Views
│   │       ├── Program.cs                  # Khởi tạo DI Container & Middleware Pipeline
│   │       └── appsettings.json            # Cấu hình máy chủ
│   ├── tests/                              # Bộ kiểm thử Unit Test & Integration Test .NET
│   └── VinamilkHRM.sln                     # Solution .NET 9
├── .env.example                            # Bản mẫu cấu hình môi trường chuẩn (DB_DATABASE=vinamilk_hrm)
├── .gitignore                              # Bộ lọc git chuẩn hóa cho .NET, PHP, Node, Secrets
├── .htaccess                               # Tự động điều hướng từ gốc vào /portal/auth/login
├── AGENTS.md                               # Quy ước bắt buộc tối cao cho Agentic AI
├── index.php                               # File chuyển tiếp gốc sang portal/auth/login
└── README.md                               # Hướng dẫn dự án chuẩn hóa (đã xóa mật khẩu mặc định)
```

---

## 2. DANH SÁCH CÁC MODULE NGHIỆP VỤ HỆ THỐNG (SYSTEM MODULES)

Hệ thống quản trị nguồn nhân lực Vinamilk HRM bao quát 10 phân hệ nghiệp vụ doanh nghiệp:

1. **Cơ cấu Tổ chức Đa điểm & Cây đơn vị 5 cấp (Organizational Units & Hierarchy)**:
   - Quản lý cây đơn vị từ Tập đoàn (Cấp 1) $\rightarrow$ Khối Văn phòng/Sản xuất/Trang trại/Kinh doanh (Cấp 2) $\rightarrow$ Nhà máy/Trang trại Green Farm/Chi nhánh/Phòng ban (Cấp 3) $\rightarrow$ Xưởng sản xuất/Khu chuồng trại/Cửa hàng Giấc Mơ Sữa Việt (Cấp 4) $\rightarrow$ Ca sản xuất (Cấp 5).
   - Cơ chế đệ quy `scopeByUnit()` phân quyền dữ liệu theo nhánh phân cấp.

2. **Hồ sơ Nhân sự & Vòng đời CBNV (Employee Lifecycle Management)**:
   - Quản lý mã nhân sự chuẩn (`VNM-xxxx`), thông tin CCCD, lý lịch 2C/TCTW, sơ yếu lý lịch, trình độ học vấn, ngạch bậc lương.
   - Theo dõi vòng đời: Tuyển dụng $\rightarrow$ Thử việc $\rightarrow$ Ký HĐLĐ $\rightarrow$ Bổ nhiệm/Điều chuyển $\rightarrow$ Khen thưởng/Kỷ luật $\rightarrow$ Thôi việc/Nghỉ hưu.

3. **Hợp đồng Lao động & Phụ lục Điện tử (Contracts & Digital Appendices)**:
   - Quản lý các loại hợp đồng: Thử việc (85% lương), Xác định thời hạn (12-36 tháng), Không xác định thời hạn.
   - Quản lý phụ lục hợp đồng điện tử điều chỉnh lương, chữ ký số Tổng Giám Đốc, đặc cách CEO override.

4. **Chấm công Thông minh & Quản lý Ca kíp (Time & Attendance Management)**:
   - Xếp ca tuần (Ca 1 Sáng, Ca 2 Chiều, Ca 3 Đêm), tích hợp máy quẹt thẻ, tính giờ công thực tế, tính giờ tăng ca OT ngày thường (150%), ngày nghỉ hàng tuần (200%), ngày lễ tết (300%) và phụ cấp làm việc ban đêm (+30%).

5. **Nghỉ phép & Quy trình Phê duyệt Đa cấp (Leave & Multi-level Approvals)**:
   - Quản lý quỹ phép năm (12 ngày chuẩn + thâm niên 5 năm thêm 1 ngày), theo dõi phép đã nghỉ, phép chờ duyệt, phép tồn.
   - Luồng duyệt đơn: Cấp 1 (Quản lý trực tiếp/Quản đốc) $\rightarrow$ Cấp 2 (Nhân sự đơn vị/Trưởng phòng HR).

6. **Đánh giá Hiệu suất KPI Doanh nghiệp (Enterprise KPI Management)**:
   - Giao chỉ tiêu KPI theo trọng số (tổng 100%), nhân viên tự đánh giá và nộp minh chứng, lãnh đạo chấm điểm và phê duyệt chính thức.
   - Tích hợp điểm KPI vào tỷ lệ thưởng lương 3P (Loại A $\ge$ 100 điểm thưởng 20%, Loại B $\ge$ 85 điểm thưởng 15%, Loại C $\ge$ 70 điểm thưởng 10%).

7. **Lương 3P, Biểu thuế & Bảo hiểm Xã hội (3P Payroll, PIT & Statutory Insurance)**:
   - Tính lương 3P: P1 (Lương chức danh/vị trí) + P2 (Lương năng lực & thâm niên) + P3 (Lương hiệu quả KPI).
   - Khấu trừ bảo hiểm bắt buộc theo luật Việt Nam: BHXH 8%, BHYT 1.5%, BHTN 1% (mức trần 20 lần lương tối thiểu vùng/lương cơ sở).
   - Biểu tính thuế TNCN lũy tiến từng phần, giảm trừ gia cảnh bản thân (11.000.000 đ) và người phụ thuộc (4.400.000 đ/người).
   - Khóa bảng lương bất biến sau khi phê duyệt và giải ngân.

8. **Tuyển dụng & Đón tiếp Nhân sự Mới (Recruitment & Onboarding)**:
   - Đăng tin tuyển dụng theo vị trí định biên, tiếp nhận hồ sơ ứng viên, ghi nhận kết quả phỏng vấn, quy trình Onboarding tự động sinh mã nhân sự và dự thảo hợp đồng thử việc.

9. **Trang thiết bị BHLĐ & Quản lý Chứng chỉ (PPE Assets & Compliance Certificates)**:
   - Cấp phát bảo hộ lao động (PPE) theo tiêu chuẩn an toàn sữa và nhà máy, cảnh báo chứng chỉ an toàn vệ sinh thực phẩm (ATTP/HACCP/ISO) sắp hết hạn trong 60 ngày.

10. **Phân tích Nhân sự & Nhật ký Kiểm toán (HR Analytics & Audit Trails)**:
    - Báo cáo định biên nhân sự, biến động headcount MoM, tỷ lệ nghỉ việc (turnover rate), tháp tuổi, thâm niên.
    - Ghi nhận nhật ký kiểm toán (Audit Log) lưu vết mọi thay đổi dữ liệu nhạy cảm (Lương, Hợp đồng, Trạng thái nhân sự).

---

## 3. DANH SÁCH BẢNG CƠ SỞ DỮ LIỆU (DATABASE SCHEMAS AUDIT)

### 3.1. Toàn bộ 22 Bảng trong CSDL MySQL (`legacy/mysql/vinamilk_hrm.sql`)
1. `dm_donvi`: Danh mục đơn vị tổ chức (cấu trúc cây phân cấp 5 cấp).
2. `dm_chucvu`: Danh mục chức danh và chức vụ chuyên môn.
3. `dm_ngach`: Danh mục ngạch và bậc lương tiêu chuẩn Vinamilk.
4. `dm_trinhdo`: Danh mục trình độ học vấn, chuyên môn, chứng chỉ.
5. `dm_ktkl`: Danh mục hình thức khen thưởng và xử lý kỷ luật.
6. `nhansu`: Bảng hồ sơ nhân sự cốt lõi (thông tin cá nhân, định danh, lương bảo hiểm).
7. `qt_congtac`: Lịch sử quá trình công tác của cán bộ nhân viên.
8. `qt_bonhiem`: Lịch sử bổ nhiệm, miễn nhiệm và giao nhiệm vụ.
9. `qt_luong`: Lịch sử nâng bậc lương, điều chỉnh lương qua các năm.
10. `qt_daotao`: Lịch sử tham gia các khóa đào tạo nội bộ và bên ngoài.
11. `qt_ktkl`: Ghi nhận các quyết định khen thưởng và kỷ luật thực tế.
12. `qt_thuyenchuyen`: Lịch sử điều chuyển nhân sự giữa các nhà máy, trang trại, chi nhánh.
13. `chamcong`: Dữ liệu quẹt thẻ chấm công ngày và tổng hợp tháng.
14. `bangluong`: Bảng thanh toán lương định kỳ hàng tháng cho nhân sự.
15. `tintuyendung`: Thông tin các vị trí tuyển dụng đang mở.
16. `hoso_ungvien`: Hồ sơ và CV của ứng viên nộp vào hệ thống.
17. `ketqua_phongvan`: Đánh giá và chấm điểm vòng phỏng vấn tuyển dụng.
18. `roles`: Danh mục vai trò bảo mật của hệ thống RBAC.
19. `users`: Tài khoản đăng nhập, mật khẩu băm bcrypt, liên kết nhân sự.
20. `permissions`: Danh mục quyền hạn chi tiết (dạng `module.action`).
21. `role_permissions`: Bảng nối phân bổ quyền hạn cho từng vai trò.
22. `menu_items`: Cấu hình danh mục menu điều hướng động theo quyền.

### 3.2. Toàn bộ 16 Bảng trong CSDL SQL Server (`VinamilkHRM/database/VinamilkHRMDb_Full_Schema_And_Seed.sql`)
1. `dbo.OrganizationalUnits`: Đơn vị tổ chức đa miền (Headquarters, Factory, Farm, RetailStore).
2. `dbo.Employees`: Hồ sơ nhân sự đa hình (OfficeEmployee, FactoryWorker, FarmSpecialist, StoreAssociate).
3. `dbo.LaborContracts`: Hợp đồng lao động đa hình (Probation, Definite, Indefinite).
4. `dbo.ElectronicContractAppendices`: Phụ lục HĐLĐ điện tử điều chỉnh lương & chữ ký số.
5. `dbo.Shifts`: Ca làm việc sản xuất (Sáng, Chiều, Đêm).
6. `dbo.Attendances`: Chấm công chi tiết hàng ngày và giờ làm thêm OT.
7. `dbo.LeaveRequests`: Đơn xin nghỉ phép và luồng duyệt 2 cấp.
8. `dbo.KpiAssignments`: Giao chỉ tiêu KPI, tự đánh giá và điểm duyệt của lãnh đạo.
9. `dbo.SalaryProposals`: Đề xuất tăng lương định kỳ cho nhân sự.
10. `dbo.PaySlips`: Phiếu thanh toán lương chi tiết (Lương 3P, phụ cấp, giảm trừ, thuế TNCN).
11. `dbo.CompanyAssets`: Tài sản và trang thiết bị BHLĐ cấp phát cho nhân viên.
12. `dbo.AuditLogs`: Nhật ký kiểm toán lưu vết hành vi và thay đổi dữ liệu.
13. `dbo.DisciplinaryWarnings`: Cảnh báo và biên bản vi phạm kỷ luật lao động.
14. `dbo.ElectronicContractAppendices`: *(Bị trùng lặp định nghĩa bảng lần 2 tại dòng 608 trong script)*.
15. `dbo.JobPostings`: Tin tuyển dụng việc làm theo phòng ban.
16. `dbo.CandidateApplications`: Đơn ứng tuyển và hồ sơ ứng viên nộp vào vị trí tuyển dụng.

---

## 4. BẢNG ÁNH XẠ BẢNG MYSQL → ENTITY .NET (SCHEMA MAPPING & GAP ANALYSIS)

Dưới đây là bảng đối chiếu chi tiết 1-1 giữa 22 bảng CSDL MySQL và các Thực thể (Entities) trong giải pháp .NET 9 Clean Architecture, kèm theo tình trạng hiện hữu và đánh giá khoảng trống kiến trúc:

| STT | Bảng MySQL (`vinamilk_hrm`) | Entity .NET tương ứng (`VinamilkHRM.Domain`) | Bảng đích SQL Server | Trạng thái .NET | Ghi chú & Đánh giá Kiến trúc |
| :---: | :--- | :--- | :--- | :---: | :--- |
| 1 | `dm_donvi` | `OrganizationalUnit` (và 4 class con) | `dbo.OrganizationalUnits` | **Đã có** | Đầy đủ quan hệ cây phân cấp, phân vùng miền `RegionScope`. |
| 2 | `dm_chucvu` | *Chưa có Entity riêng* | *(Chưa có bảng)* | ⚠️ **CÒN THIẾU** | .NET hiện lưu cứng text `JobTitle` trong `Employee`, chưa quản lý chức danh chuẩn. |
| 3 | `dm_ngach` | *Chưa có Entity riêng* | *(Chưa có bảng)* | ⚠️ **CÒN THIẾU** | Thiếu bảng ngạch bậc lương chuẩn doanh nghiệp. |
| 4 | `dm_trinhdo` | *Chưa có Entity riêng* | *(Chưa có bảng)* | ⚠️ **CÒN THIẾU** | Thiếu quản lý bằng cấp và chứng chỉ học vấn. |
| 5 | `dm_ktkl` | *Chưa có Entity riêng* | *(Chưa có bảng)* | ⚠️ **CÒN THIẾU** | .NET mới chỉ có bảng cảnh báo kỷ luật vi phạm, thiếu danh mục khen thưởng. |
| 6 | `nhansu` | `Employee` (và 4 class con) | `dbo.Employees` | **Đã có** | Triển khai mô hình đa hình (Polymorphism) theo khối ngành Vinamilk. |
| 7 | `qt_congtac` | *Chưa có Entity riêng* | *(Chưa có bảng)* | ⚠️ **CÒN THIẾU** | Thiếu quản lý lịch sử công tác trước đây của nhân sự. |
| 8 | `qt_bonhiem` | *Chưa có Entity riêng* | *(Chưa có bảng)* | ⚠️ **CÒN THIẾU** | Thiếu quản lý quyết định bổ nhiệm, thăng chức, luân chuyển chức vụ. |
| 9 | `qt_luong` | *Chưa có Entity riêng* | *(Chưa có bảng)* | ⚠️ **CÒN THIẾU** | .NET dùng `ElectronicContractAppendix` thay thế, nhưng thiếu lịch sử tăng bậc. |
| 10 | `qt_daotao` | *Chưa có Entity riêng* | *(Chưa có bảng)* | ⚠️ **CÒN THIẾU** | Thiếu module quản lý các khóa đào tạo nâng cao năng lực CBNV. |
| 11 | `qt_ktkl` | `DisciplinaryWarning` | `dbo.DisciplinaryWarnings` | ⚠️ **MỘT PHẦN** | Chỉ ghi nhận biên bản vi phạm kỷ luật, thiếu hoàn toàn phân hệ khen thưởng. |
| 12 | `qt_thuyenchuyen` | *Chưa có Entity riêng* | *(Chưa có bảng)* | ⚠️ **CÒN THIẾU** | Thiếu quản lý quyết định điều động nhân sự giữa các đơn vị. |
| 13 | `chamcong` | `Attendance` | `dbo.Attendances` | **Đã có** | Đầy đủ dữ liệu giờ làm việc, giờ OT, liên kết ca `Shift`. |
| 14 | `bangluong` | `PaySlip` | `dbo.PaySlips` | **Đã có** | Đầy đủ các chỉ tiêu tính lương 3P, bảo hiểm và thuế TNCN lũy tiến. |
| 15 | `tintuyendung` | `JobPosting` | `dbo.JobPostings` | **Đã có** | Đầy đủ thông tin tuyển dụng, hạn nộp hồ sơ, yêu cầu kỹ năng. |
| 16 | `hoso_ungvien` | `CandidateApplication` | `dbo.CandidateApplications` | **Đã có** | Quản lý hồ sơ ứng viên nộp vào tin tuyển dụng. |
| 17 | `ketqua_phongvan` | *Chưa có Entity riêng* | *(Chưa có bảng)* | ⚠️ **CÒN THIẾU** | .NET mới dừng ở lưu đơn ứng tuyển, chưa có bảng kết quả vòng phỏng vấn. |
| 18 | `roles` | *Chưa có Entity riêng* | *(Chưa có bảng)* | ❌ **CÒN THIẾU** | **Lỗ hổng cốt lõi**: .NET chưa có hệ thống Role-Based Access Control (RBAC). |
| 19 | `users` | *Chưa có Entity riêng* | *(Chưa có bảng)* | ❌ **CÒN THIẾU** | **Lỗ hổng cốt lõi**: .NET chưa có bảng tài khoản người dùng, băm mật khẩu, JWT. |
| 20 | `permissions` | *Chưa có Entity riêng* | *(Chưa có bảng)* | ❌ **CÒN THIẾU** | Thiếu danh mục quyền hạn hạt nhân (Claim-based / Permission-based). |
| 21 | `role_permissions`| *Chưa có Entity riêng* | *(Chưa có bảng)* | ❌ **CÒN THIẾU** | Thiếu bảng phân bổ quyền hạn cho từng vai trò trong .NET. |
| 22 | `menu_items` | *Chưa có Entity riêng* | *(Chưa có bảng)* | ⚠️ **CÒN THIẾU** | Thiếu phân quyền menu điều hướng trên giao diện. |

### Các thực thể mở rộng ĐÃ CÓ trong .NET (MySQL cũ chưa có hoặc nằm ở file bổ sung):
- `LaborContract` (Probation, Definite, Indefinite) $\rightarrow$ `dbo.LaborContracts`: Quản lý hợp đồng lao động chuẩn luật.
- `ElectronicContractAppendix` $\rightarrow$ `dbo.ElectronicContractAppendices`: Phụ lục hợp đồng điện tử có băm chữ ký số SHA-256.
- `LeaveRequest` $\rightarrow$ `dbo.LeaveRequests`: Quản lý đơn nghỉ phép và quy trình duyệt 2 cấp.
- `KpiAssignment` $\rightarrow$ `dbo.KpiAssignments`: Quản lý giao chỉ tiêu và đánh giá KPI.
- `SalaryProposal` $\rightarrow$ `dbo.SalaryProposals`: Thực thể đề xuất lương (*chưa có logic nghiệp vụ sử dụng*).
- `CompanyAsset` $\rightarrow$ `dbo.CompanyAssets`: Quản lý tài sản công ty và thiết bị bảo hộ lao động.
- `AuditLog` $\rightarrow$ `dbo.AuditLogs`: Nhật ký kiểm toán hệ thống.
- `Shift` $\rightarrow$ `dbo.Shifts`: Ca làm việc sản xuất tại nhà máy.

---

## 5. DANH SÁCH LỖI HỆ THỐNG XẾP THEO MỨC ĐỘ NGUY CƠ (FINDINGS & VULNERABILITIES)

Tất cả các phát hiện và lỗ hổng được phân loại theo thang đo rủi ro bảo mật chuẩn công nghiệp (CVSS v3.1 / OWASP Top 10):

```
       ┌───────────────────────────────┐
       │   CRITICAL: 6 Lỗ hổng         │
       ├───────────────────────────────┤
       │   HIGH:     5 Lỗ hổng         │
       ├───────────────────────────────┤
       │   MEDIUM:   4 Lỗ hổng         │
       ├───────────────────────────────┤
       │   LOW:      3 Vấn đề          │
       └───────────────────────────────┘
```

---

### MỨC ĐỘ 1: CRITICAL (NGUY CƠ ĐẶC BIỆT NGHIÊM TRỌNG - VÁ NGAY LẬP TỨC)

#### 🔴 Lỗi C-01: Lộ thông tin bí mật (.env, cookies.txt) và mã nhị phân trong Git Index
- **Hiện trạng phát hiện**: Tập tin `.env` chứa mật khẩu kết nối CSDL và `cookies.txt` chứa cookie phiên đăng nhập thực tế của quản trị viên từng được commit vào lịch sử Git. Đồng thời các thư mục nhị phân `bin/`, `obj/` bị commit tràn lan do thiếu tập tin `.gitignore` hoàn chỉnh.
- **Tác động**: Kẻ tấn công có mã nguồn có thể trực tiếp chiếm quyền quản trị CSDL hoặc sử dụng cookie bị lộ để mạo danh phiên đăng nhập (Session Hijacking).
- **Hành động đã xử lý khẩn**: Đã tạo `.gitignore` chuẩn cho .NET, PHP, Node; gỡ bỏ hoàn toàn trạng thái track của `.env`, `cookies.txt`, `bin/`, `obj/` khỏi Git index bằng `git rm --cached`.
- **Việc cần làm tiếp**: Tẩy sạch lịch sử commit cũ bằng `git filter-repo` (xem hướng dẫn ở Mục 6).

#### 🔴 Lỗi C-02: Bảng tài khoản Demo trong README công khai mật khẩu mặc định
- **Hiện trạng phát hiện**: Trong `README.md`, bảng tài khoản hệ thống demo công khai rõ ràng mật khẩu mặc định `Vinamilk@2026` cho cả 11 tài khoản đặc quyền (`admin`, `bod.ceo`, `hr.tapdoan`, `ketoan.luong`...) và 300 nhân sự.
- **Tác động**: Bất kỳ người nào truy cập kho mã nguồn đều có thể đăng nhập vào môi trường triển khai thực tế bằng tài khoản quản trị tối cao nếu mật khẩu chưa được đổi.
- **Hành động đã xử lý khẩn**: Đã loại bỏ hoàn toàn mật khẩu văn bản thô khỏi `README.md`, chuyển sang cơ chế thông báo kích hoạt cờ bảo mật bắt buộc đổi mật khẩu `MustChangePassword = 1` trong lần đăng nhập đầu tiên.

#### 🔴 Lỗi C-03: Các Controller PHP thiếu kiểm tra xác thực và phân quyền (Broken Access Control)
- **Hiện trạng phát hiện**: Các controller `KpiController`, `DepartmentController`, `ApiAnalyticsController`, `ApiEmployeeController`, `ApiEssController`, `ApiOnboardingController`, `ApiSecurityController` không hề gọi phương thức kiểm tra đăng nhập (`$this->requireAuth()`) hay kiểm tra quyền hạn (`$this->requirePermission()`). Đồng thời `ApiEssController` còn có đoạn code fallback tự động gán mã nhân sự `VNM-0001` (Tổng Giám Đốc) cho khách vãng lai chưa đăng nhập!
- **Tác động**: Bất kỳ người dùng nặc danh nào ngoài Internet đều có thể xem toàn bộ hồ sơ nhân sự, bảng lương, báo cáo phân tích, chỉnh sửa cơ cấu tổ chức và phê duyệt điểm KPI.
- **Hành động đã xử lý khẩn**: Đã bổ sung đầy đủ `$this->requirePermission(...)` và `$this->requireAuth()` vào 100% action của cả 7 controller; loại bỏ hoàn toàn fallback `VNM-0001`.

#### 🔴 Lỗi C-04: Cấu hình CORS Wildcard toàn quyền (`Access-Control-Allow-Origin: *`)
- **Hiện trạng phát hiện**: Các API controller của PHP trả header `Access-Control-Allow-Origin: *` cho các yêu cầu phản hồi JSON.
- **Tác động**: Cho phép các trang web độc hại của bên thứ ba gửi yêu cầu âm thầm từ trình duyệt của nạn nhân để đánh cắp dữ liệu nhân sự, tiền lương hoặc thực hiện tấn công CSRF xuyên miền.
- **Hành động đã xử lý khẩn**: Đã gỡ bỏ toàn bộ header `Access-Control-Allow-Origin: *` khỏi tất cả API controller.

#### 🔴 Lỗi C-05: Trả thông báo lỗi ngoại lệ PDO trực tiếp ra Client (Information Disclosure)
- **Hiện trạng phát hiện**: Trong `Database.php` dòng 38, khi kết nối CSDL MySQL thất bại, khối catch trả trực tiếp chuỗi `$e->getMessage()` ra giao diện HTML bằng lệnh `die()`.
- **Tác động**: Làm lộ địa chỉ IP máy chủ CSDL nội bộ, cổng kết nối, tên người dùng CSDL, cấu trúc bảng và vết ngăn xếp lỗi hệ thống.
- **Hành động đã xử lý khẩn**: Đã sửa `Database.php` để ghi chi tiết lỗi vào `error_log()` trên máy chủ, chỉ trả về thông báo lỗi chung thân thiện cho người dùng cuối.

#### 🔴 Lỗi C-06: .NET Backend hoàn toàn thiếu cơ chế Authentication, [Authorize] và Antiforgery
- **Hiện trạng phát hiện**: Trong `VinamilkHRM.WebUI/Program.cs`, dịch vụ `AddAuthentication()` không được đăng ký cấu hình schema xác thực nào. Tất cả Controllers trong `VinamilkHRM.WebUI` không có thuộc tính `[Authorize]`. Thuộc tính chống giả mạo request `[ValidateAntiForgeryToken]` bị thiếu ở hầu hết các endpoint tiếp nhận POST.
- **Tác động**: Toàn bộ các API và màn hình quản trị của lõi .NET có thể bị truy cập nặc danh không cần xác thực.
- **Kế hoạch xử lý**: Triển khai JWT Bearer Authentication / Cookie Auth và áp dụng Policy Authorization trên toàn bộ Web API trong các phiên kế tiếp.

---

### MỨC ĐỘ 2: HIGH (NGUY CƠ CAO - SAI LỆCH NGHIỆP VỤ & TÍNH TOÀN VẸN)

#### 🟠 Lỗi H-01: PayrollService tự ý gán 22 công chuẩn khi thiếu dữ liệu chấm công
- **Hiện trạng phát hiện**: Tại dòng 81 của `VinamilkHRM.Application/Services/PayrollService.cs`:
  ```csharp
  if (actualWorkDays <= 0) actualWorkDays = 22m; // Mặc định đủ công chuẩn nếu quẹt thẻ chưa đẩy về
  ```
- **Tác động**: Nếu nhân viên nghỉ việc không phép toàn bộ tháng hoặc máy chấm công bị lỗi chưa đồng bộ dữ liệu, hệ thống tự động trả nguyên lương 100% (22 công chuẩn), gây thất thoát tài chính doanh nghiệp nghiêm trọng.
- **Khuyến nghị kiến trúc**: Phải giữ nguyên `actualWorkDays = 0`, đánh dấu cảnh báo kỳ công chưa chốt và yêu cầu bộ phận C&B xác nhận giải trình chấm công trước khi tính lương.

#### 🟠 Lỗi H-02: PayrollService xóa mất phiếu lương đã duyệt (Vi phạm tính bất biến)
- **Hiện trạng phát hiện**: Tại dòng 146-150 của `PayrollService.cs`:
  ```csharp
  var existing = await _paySlipRepository.GetByEmployeeAndPeriodAsync(employeeId, period, cancellationToken);
  if (existing != null)
  {
      _paySlipRepository.Remove(existing);
  }
  ```
- **Tác động**: Khi tính lại bảng lương, hệ thống xóa thẳng phiếu lương cũ mà không kiểm tra trạng thái (`Status == PaySlipStatus.Approved` hoặc `PaySlipStatus.Paid`). Nếu phiếu lương đã được ký duyệt hoặc đã giải ngân, hành động này phá hủy chứng từ kế toán và vi phạm nguyên tắc kiểm toán tài chính doanh nghiệp.
- **Khuyến nghị kiến trúc**: Ném ngoại lệ `DomainException("ERR_PAYSLIP_ALREADY_APPROVED", "Không thể xóa hoặc tính lại phiếu lương đã được phê duyệt hoặc thanh toán")`.

#### 🟠 Lỗi H-03: KpiStatus bị trùng giá trị định danh (`Submitted = 4` và `PendingApproval = 4`)
- **Hiện trạng phát hiện**: Trong `VinamilkHRM.Domain/Enums/KpiStatus.cs`:
  ```csharp
  Submitted = 4,
  PendingApproval = 4, // Chờ Lãnh đạo thẩm định và duyệt điểm
  ```
- **Tác động**: Trong C#, khi hai thành viên enum có cùng giá trị số nguyên, việc ép kiểu chuỗi, tuần tự hóa JSON và so sánh logic trạng thái sẽ bị nhập nhằng, gây sai lệch luồng duyệt trạng thái KPI.
- **Khuyến nghị kiến trúc**: Tách biệt rõ ràng: `Submitted = 4`, `PendingApproval = 5`, `Approved = 6`, `Rejected = 7`.

#### 🟠 Lỗi H-04: Nhật ký kiểm toán (Audit Trail) bị gán cứng Actor `"SYSTEM"`
- **Hiện trạng phát hiện**: Trong `AuditLog.cs` và `VinamilkHRMDbContext.cs` (dòng 96, 100):
  ```csharp
  entry.Entity.SetAuditCreated("SYSTEM", utcNow);
  entry.Entity.SetAuditUpdated("SYSTEM", utcNow);
  ```
- **Tác động**: Mọi hành vi tạo mới hoặc sửa đổi dữ liệu nhạy cảm (lương, hợp đồng, nhân sự) đều mang tên người thực hiện là `"SYSTEM"`, làm vô hiệu hóa khả năng truy vết trách nhiệm cá nhân (Non-repudiation) khi xảy ra gian lận nội bộ.
- **Khuyến nghị kiến trúc**: Tiêm dịch vụ `ICurrentUserService` để lấy `UserId` thực tế của người dùng đang đăng nhập thông qua `ClaimsPrincipal`.

#### 🟠 Lỗi H-05: Tồn tại tập tin rác nguy hiểm `old_db.php`
- **Hiện trạng phát hiện**: Tập tin `legacy/old_db.php` chứa đoạn mã kết nối CSDL cũ với cấu hình tài khoản và câu truy vấn SQL thô không được kiểm soát.
- **Tác động**: Nguy cơ backdoor và rò rỉ thông tin hạ tầng kết nối.
- **Hành động đã xử lý khẩn**: Đã xóa vĩnh viễn tập tin `old_db.php`.

---

### MỨC ĐỘ 3: MEDIUM (NGUY CƠ TRUNG BÌNH - THIẾU SÓT THIẾT KẾ & TÍNH NHẤT QUÁN)

#### 🟡 Lỗi M-01: Không nhất quán tên CSDL (`vinamilk_hrm` vs `vinamilk_hris`)
- **Hiện trạng phát hiện**: Trong `.env.example` cũ đặt cấu hình `DB_DATABASE=vinamilk_hris`, trong khi CSDL MySQL thực tế và cấu hình trong `config.php`, `Database.php`, các script SQL đều mang tên `vinamilk_hrm`.
- **Tác động**: Gây lỗi kết nối ngay khi triển khai trên môi trường mới qua biến môi trường.
- **Hành động đã xử lý khẩn**: Thống nhất toàn bộ tên CSDL là `vinamilk_hrm`.

#### 🟡 Lỗi M-02: .NET Backend hoàn toàn thiếu EF Core Migrations và ConnectionStrings cấu hình
- **Hiện trạng phát hiện**: Thư mục `VinamilkHRM` không có thư mục `Migrations/`. File `appsettings.json` không có chuỗi kết nối `ConnectionStrings`. Mã nguồn trong `DependencyInjection.cs` phải tự fallback về chuỗi kết nối hard-coded `Server=(localdb)\mssqllocaldb;Database=VinamilkHRMDb...`.
- **Tác động**: Không thể tự động triển khai hoặc kiểm soát lịch sử tiến hóa schema CSDL trên môi trường Production.
- **Khuyến nghị kiến trúc**: Bổ sung `ConnectionStrings:DefaultConnection` vào `appsettings.json` và khởi tạo Initial Migration bằng `dotnet ef migrations add InitialCreate`.

#### 🟡 Lỗi M-03: Thực thể `SalaryProposal` không được sử dụng (Dead Code)
- **Hiện trạng phát hiện**: `SalaryProposal` tồn tại ở tầng Domain (`SalaryProposal.cs`), Application (`SalaryProposalDto.cs`), Infrastructure (`SalaryProposalConfiguration.cs`), nhưng không có Repository, Service hay Controller nào sử dụng.
- **Tác động**: Làm phình to mã nguồn và gây nhầm lẫn về phạm vi chức năng thực tế của phân hệ tiền lương.

#### 🟡 Lỗi M-04: Script SQL Server tạo trùng lặp bảng `ElectronicContractAppendices` 2 lần
- **Hiện trạng phát hiện**: Trong file `VinamilkHRMDb_Full_Schema_And_Seed.sql`, lệnh `CREATE TABLE dbo.ElectronicContractAppendices` xuất hiện ở cả dòng 246 và dòng 608.
- **Tác động**: Khi chạy script trên SQL Server Management Studio (SSMS), tiến trình nạp schema sẽ bị gián đoạn và báo lỗi "There is already an object named 'ElectronicContractAppendices' in the database".

---

### MỨC ĐỘ 4: LOW (NGUY CƠ THẤP - QUẢN LÝ MÃ NGUỒN & CÔNG CỤ PHỤ TRỢ)

#### 🔵 Lỗi L-01: Các script bảo trì (`clean.ps1`, `remove_emojis.*`) nằm rải rác ngoài thư mục gốc
- **Hiện trạng phát hiện**: Các script tiện ích bảo trì và dọn dẹp văn bản nằm lẫn trong thư mục mã nguồn chính.
- **Hành động đã xử lý khẩn**: Đã gom toàn bộ các file này vào thư mục `tools/`.

#### 🔵 Lỗi L-02: Chưa có cơ chế bảo vệ CSRF trên một số Controller PHP dạng GET
- **Hiện trạng phát hiện**: Mặc dù POST đã có `validateCsrf()`, một số thao tác chuyển trạng thái (như kích hoạt/vô hiệu hóa) cần đảm bảo chuyển sang phương thức POST thay vì link thẻ `<a>` GET.

#### 🔵 Lỗi L-03: Thiếu cấu hình `appsettings.*.local.json` trong quản lý môi trường .NET
- **Hiện trạng phát hiện**: Lập trình viên dễ vô tình commit chuỗi kết nối CSDL cá nhân khi sửa trực tiếp file `appsettings.Development.json`.
- **Hành động đã xử lý khẩn**: Đã đưa `appsettings.*.local.json` vào `.gitignore`.

---

## 6. HƯỚNG DẪN XÓA LỊCH SỬ NHẠY CẢM BẰNG `git filter-repo` (TÙY CHỌN DÀNH CHO USER)

> [!WARNING]
> **LƯU Ý QUAN TRỌNG VỀ AN TOÀN**:
> Theo nguyên tắc bảo mật và chỉ đạo của dự án, trong phiên này **AGENT KHÔNG TỰ Ý VIẾT LẠI LỊCH SỬ GIT VÀ KHÔNG THỰC HIỆN FORCE-PUSH**. 
> Việc viết lại lịch sử commit sẽ làm thay đổi mã SHA của toàn bộ commit trong kho và ảnh hưởng đến các thành viên khác trong nhóm phát triển. Dưới đây là hướng dẫn chi tiết từng bước để Quản trị viên/Chủ sở hữu repo tự xem xét và thực hiện khi sẵn sàng:

### Bước 1: Sao lưu toàn bộ thư mục repo hiện tại
Trước khi thao tác viết lại lịch sử, hãy tạo bản sao lưu vật lý dự phòng:
```powershell
Copy-Item -Path "c:\Users\MAi THU\OneDrive\HTTT QLNL\vinamilk" -Destination "c:\Users\MAi THU\OneDrive\HTTT QLNL\vinamilk_backup_pre_filter" -Recurse
```

### Bước 2: Cài đặt công cụ `git-filter-repo`
Công cụ chính thức được khuyến nghị bởi Git Core Team (thay thế cho `git filter-branch` cũ vốn dễ gây lỗi):
```bash
pip install git-filter-repo
```

### Bước 3: Thực thi loại bỏ vĩnh viễn các file nhạy cảm khỏi toàn bộ lịch sử commit
Chạy lệnh phân tích và xóa triệt để vết của các file nhạy cảm trong toàn bộ các nhánh:
```bash
git filter-repo --invert-paths \
  --path .env \
  --path cookies.txt \
  --path legacy/old_db.php \
  --path-glob "**/bin/*" \
  --path-glob "**/obj/*"
```

### Bước 4: Kiểm tra lại lịch sử sau khi lọc
Kiểm tra đảm bảo các chuỗi hoặc file nhạy cảm không còn xuất hiện trong bất kỳ commit nào:
```bash
git log --all --full-history -- "**/cookies.txt"
git log --all --full-history -- "**/.env"
```

### Bước 5: Cập nhật lại Remote Origin và Đẩy lên máy chủ (Nếu quyết định áp dụng)
Sau khi lọc bằng `git filter-repo`, Git sẽ tự động gỡ remote origin để phòng ngừa vô tình đẩy đè. Nếu bạn xác nhận muốn cập nhật kho GitHub:
```bash
git remote add origin https://github.com/kysuriaminato-ai/vinamilk.git
git push origin --force --all
git push origin --force --tags
```
*(Nếu làm việc nhóm, cần thông báo cho toàn bộ thành viên clone lại repo mới sau khi force-push).*
