# Vinamilk Enterprise HRM (.NET 8 Clean Architecture)

Hệ thống Quản trị Nguồn nhân lực Doanh nghiệp được xây dựng lại hoàn chỉnh trên **ASP.NET Core MVC (.NET 8)**, **EF Core 8**, **SQL Server**, chuẩn tham chiếu **SAP SuccessFactors / Workday / MISA AMIS HRM**.

## Kiến trúc Solution (`Vinamilk.HRM.sln`)
- `Vinamilk.HRM.Domain`: Entities, Value Objects, Enums, Domain Events (0 phụ thuộc).
- `Vinamilk.HRM.Application`: CQRS (MediatR), FluentValidation, DTOs, Interfaces, Pipeline Behaviors.
- `Vinamilk.HRM.Infrastructure`: EF Core 8 (`ApplicationDbContext`, `SlowQueryLoggingInterceptor`), Repository/UoW, AES-256 PII Encryption, ClosedXML, QuestPDF, Email, `DatabaseBackupService`, `LocalRagAiHrmExpertService`.
- `Vinamilk.HRM.Web`: ASP.NET Core MVC, Razor Views, Areas (`ESS`), Web API (`/api/v1/*`), Swagger (`/swagger`), Health Checks (`/health`).

## Chạy nhanh trên Local
```powershell
cd c:\xampp\htdocs\vinamilk\dotnet
dotnet build Vinamilk.HRM.sln
dotnet test Vinamilk.HRM.sln
dotnet run --project src\Vinamilk.HRM.Web\Vinamilk.HRM.Web.csproj
```

## Tài liệu API & Endpoints quan trọng
- **Swagger UI**: `/swagger` (môi trường Development)
- **Public SEO Careers**: `/gioi-thieu`, `/tuyen-dung`, `/tuyen-dung/{slug}-{id}`, `/sitemap.xml`, `/robots.txt`
- **Public API**: `GET /api/v1/careers`, `POST /api/v1/system/backup`
- **Health Checks**: `/health`, `/health/ready`, `/health/live`
- **Cổng ESS/MSS PWA**: `/ESS`
- **AI HR Expert**: `/AiExpert`

