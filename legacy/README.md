# HỆ THỐNG QUẢN LÝ NGUỒN NHÂN LỰC CÔNG TY CỔ PHẦN SỮA VIỆT NAM (VINAMILK HRM)

> **Dự án**: Xây dựng Hệ thống Quản trị Nguồn nhân lực Doanh nghiệp Tập đoàn Vinamilk (Vinamilk HRM - Enterprise Edition)  
> **Kiến trúc**: Mô hình MVC Thuần (Native PHP 8.x, PDO Singleton, MySQL)  
> **Quy chuẩn Báo cáo**: Bộ chuẩn Thống kê Nhân sự HUHA HRM  

---

## 📋 GIỚI THIỆU TỔNG QUAN

**Vinamilk HRM** là giải pháp phần mềm quản trị nguồn nhân lực toàn diện dành cho **Công ty Cổ phần Sữa Việt Nam (Vinamilk - Mã CK: VNM)**. Hệ thống được thiết kế theo kiến trúc chuẩn **MVC 3 lớp**, không phụ thuộc framework cồng kềnh, tối ưu hiệu năng vận hành và bảo mật dữ liệu.

Hệ thống quản trị hạ tầng nhân sự quy mô lớn gồm Tổng công ty, các Chi nhánh, Nhà máy chế biến sữa và Hệ thống trang trại bò sữa đạt chuẩn quốc tế (GlobalGAP/Organic) trên toàn quốc.

---

## 🛠️ CÔNG NGHỆ VÀ THƯ THƯ VỊỆN SỬ DỤNG

- **Back-end Core**: Native PHP 8.x, PDO Singleton Pattern, Session Authorization, RESTful AJAX Endpoints.
- **Database**: MySQL Server 8.0 / MariaDB (Import qua phpMyAdmin).
- **Front-end Framework**: HTML5, CSS3 Vanilla, JavaScript (ES6+ Modules), Bootstrap 5.3, Bootstrap Icons.
- **Biểu đồ & Trực quan hóa**: Chart.js 4.x, ApexCharts, Interactive Dynamic OrgChart Tree.
- **Định dạng Xuất bản**: In ấn A4 CSS `@media print` không vỡ layout, Xuất Excel `.xlsx` / CSV, PDF Export.

---

## 📂 CẤU TRÚC THƯ MỤC NGUỒN (MVC ARCHITECTURE)

```text
vinamilk/
├── app/
│   ├── config/
│   │   ├── config.php             # Cấu hình BASE_URL, Session, Timezone
│   │   └── database.php           # Cấu hình PDO Singleton kết nối MySQL
│   ├── controllers/               # Bộ điều khiển (Controllers)
│   │   ├── AuthController.php
│   │   ├── DashboardController.php
│   │   ├── EmployeeController.php
│   │   ├── AttendanceController.php
│   │   ├── PayrollController.php
│   │   ├── RecruitmentController.php
│   │   ├── TrainingController.php
│   │   ├── AiExpertController.php
│   │   ├── ReportController.php
│   │   └── RoleController.php
│   ├── models/                    # Bộ mô hình dữ liệu (Models)
│   │   ├── User.php
│   │   ├── Employee.php
│   │   ├── Attendance.php
│   │   ├── Payroll.php
│   │   ├── Recruitment.php
│   │   ├── Training.php
│   │   ├── AiPredictor.php
│   │   ├── ReportModel.php
│   │   └── Role.php
│   ├── views/                     # Giao diện hiển thị (Views)
│   │   ├── auth/
│   │   ├── dashboard/             # Executive HR Dashboard & Interactive OrgChart
│   │   ├── employees/
│   │   ├── attendance/
│   │   ├── payroll/
│   │   ├── recruitment/
│   │   ├── training/
│   │   ├── ai/                    # Chẩn đoán & Dự báo Nhân sự AI Expert
│   │   ├── reports/               # Bộ biểu mẫu báo cáo HUHA HRM (BM01 - BM04)
│   │   ├── layouts/               # Header, Sidebar, Footer layout chung
│   │   └── errors/
│   └── core/
│       ├── App.php                # Front-Controller & Dynamic Router (Route Aliases)
│       ├── Controller.php         # Base Controller Class
│       └── Database.php           # PDO Singleton Connection
├── database/
│   └── vinamilk_hrm.sql           # Schema SQL hoàn chỉnh & Dữ liệu mẫu (Dummy Data)
├── public/
│   ├── index.php                  # Entry Point duy nhất của ứng dụng
│   ├── css/                       # Custom Style sheets & @media print
│   └── js/                        # ES6 Modules (employee.js, attendance.js, v.v.)
├── uploads/                       # Thư mục lưu trữ hồ sơ scan đính kèm (.pdf, .png)
├── .htaccess                      # Apache URL Rewriting (.htaccess)
└── README.md                      # Hướng dẫn cấu hình & cài đặt
```

---

## ⚙️ HƯỚNG DẪN CÀI ĐẶT & CẤU HÌNH HỆ THỐNG

### 1. Yêu cầu Môi trường
- **Phần mềm máy chủ web**: XAMPP (khuyên dùng), WAMP Server hoặc Laragon.
- **PHP Version**: `8.0` hoặc cao hơn (Bật các extension: `pdo_mysql`, `mbstring`, `openssl`).
- **MySQL Server**: `5.7` / `8.0` hoặc **MariaDB** `10.4+`.
- **Apache Module**: Bật module `mod_rewrite`.

### 2. Cài đặt Cơ sở Dữ liệu qua phpMyAdmin
1. Mở trình duyệt và truy cập phpMyAdmin: `http://localhost/phpmyadmin`
2. Tạo mới một cơ sở dữ liệu có tên: **`vinamilk_hrm`** (Bảng mã / Collation: `utf8mb4_unicode_ci`).
3. Chọn cơ sở dữ liệu `vinamilk_hrm` vừa tạo -> Chọn tab **Import** (Nhập).
4. Chọn file **`database/vinamilk_hrm.sql`** trong bộ mã nguồn dự án -> Ấn **Go** (Thực hiện).
5. Xác nhận hệ thống khởi tạo thành công 16 bảng dữ liệu (users, nhan_vien, phong_ban, cham_cong, bang_luong, dich_vuyen_nhan_su, khen_thuong_ky_luat, v.v.).

### 3. Cấu hình Thư mục Ứng dụng & VirtualHost
- Copy toàn bộ thư mục **`vinamilk`** vào thư mục chạy mặc định của Web Server:
  - Trên **XAMPP**: `C:\xampp\htdocs\vinamilk`
  - Trên **WAMP**: `C:\wamp64\www\vinamilk`
- **Cấu hình BASE_URL** (nếu cần đổi cổng hoặc tên miền):
  - Mở file `app/config/config.php`
  - Chỉnh sửa đường dẫn mặc định:
    ```php
    define('BASE_URL', 'http://localhost/vinamilk');
    ```

### 4. Cấu hình Kết nối CSDL (`app/config/database.php`)
```php
define('DB_HOST', 'localhost');
define('DB_USER', 'root');
define('DB_PASS', '');
define('DB_NAME', 'vinamilk_hrm');
define('DB_PORT', '3306');
define('DB_CHARSET', 'utf8mb4');
```

---

## 🔑 TÀI KHOẢN TRUY CẬP VÀ PHÂN QUYỀN MẶC ĐỊNH

Hệ thống được thiết lập sẵn các tài khoản thử nghiệm theo mô hình **Multi-role Dynamic Permission**:

| Vai trò | Tài khoản Login | Mật khẩu | Phạm vi Quyền hạn |
| :--- | :--- | :--- | :--- |
| **Quản trị hệ thống (System Admin)** | `admin` | `admin123` | Toàn quyền cấu hình hệ thống, quản lý tài khoản, phân quyền vai trò. |
| **Trưởng phòng Tổ chức Cán bộ** | `truongphong` | `123456` | Quản lý Hồ sơ nhân sự toàn tập đoàn, phê duyệt thuyên chuyển, tuyển dụng, đào tạo. |
| **Kế toán Tiền lương (Payroll Accountant)** | `ketoan` | `123456` | Quản lý bảng chấm công, duyệt chi lương, tính toán bảo hiểm & thuế TNCN. |
| **Chuyên viên Nhân sự (HR Specialist)** | `nhansu` | `123456` | Tiếp nhận hồ sơ nhân sự, lập phiếu đề xuất tuyển dụng, lập danh sách đào tạo. |
| **Cán bộ Nhân viên (Employee ESS)** | `nhanvien` | `123456` | Cổng thông tin cá nhân tự phục vụ: Xem phiếu lương, gửi đơn nghỉ phép trực tuyến. |

---

## 📊 DÂN MỤC BÁO CÁO CHUẨN HUHA HRM & CHỨC NĂNG NỔI BẬT

### 1. Executive HR Dashboard (Tổng quan Điều hành)
- **KPIs Thời gian thực**: Tổng số CBNV, Tỷ lệ Nam/Nữ, Độ tuổi trung bình, Quỹ lương tháng hiện tại, Tỷ lệ biến động nhân sự.
- **Biểu đồ trực quan**: Cơ cấu nhân sự theo Đơn vị (Văn phòng, Nhà máy, Trang trại), Cơ cấu Trình độ học vấn & Chuyên môn.
- **Interactive Tree OrgChart**: Sơ đồ cây cơ cấu tổ chức tương tác, click chọn từng phòng ban để xem danh sách nhân sự trực thuộc.

### 2. Phân Hệ Thống Kê Báo Cáo (Report Suite)
- **Biểu mẫu 01**: Danh sách trích ngang toàn bộ CBNV (Họ tên, năm sinh, chức vụ, ngạch bậc, học vấn, quê quán).
- **Biểu mẫu 02**: Báo cáo biến động nhân sự (Thuyên chuyển, bổ nhiệm, tiếp nhận mới).
- **Biểu mẫu 03**: Thống kê Khen thưởng & Kỷ luật toàn công ty theo năm.
- **Biểu mẫu 04**: Thống kê danh sách nhân sự tiệm cận nghỉ hưu (Nam $\ge$ 60 tuổi, Nữ $\ge$ 55 tuổi).
- **Tiện ích In ấn & Xuất file**: Đã tích hợp `@media print` cho khổ giấy **A4 Ngang/Đứng** chuẩn in ấn văn bằng, hỗ trợ Export Excel `.xlsx` và PDF.

### 3. Hệ Chuyên Gia Trí Tuệ Nhân Tạo (AI Expert System)
- Thuật toán chẩn đoán nguy cơ nghỉ việc (**Turnover Risk Diagnosis**).
- Mô hình dự báo nhu cầu định biên nhân sự (**Workforce Demand Forecasting**) phục vụ mở rộng nhà máy và trang trại Vinamilk.

---

## 🚀 QUY TRÌNH KIỂM THỬ VÀ NGHIỆM THU DỰ ÁN

1. Chạy lệnh kiểm tra cú pháp PHP (PHP Linting):
   ```bash
   php -l app/controllers/*.php app/models/*.php app/views/reports/*.php
   ```
2. Mở trình duyệt truy cập: `http://localhost/vinamilk/`
3. Đăng nhập tài khoản `admin` / `admin123`.
4. Trải nghiệm hệ thống báo cáo HUHA HRM tại đường dẫn: `http://localhost/vinamilk/baocao`
5. Thử nghiệm tính năng In trực tiếp A4 và Xuất Excel file.

---
*Copyright © 2026 Vinamilk HRM Enterprise Edition - Built with Native PHP & Pure MVC.*
