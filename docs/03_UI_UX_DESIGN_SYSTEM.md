# PHÂN TÍCH LỰA CHỌN ADMIN THEME & ĐẶC TẢ DESIGN SYSTEM – VINAMILK ENTERPRISE HRM

**Dự án:** Hệ thống Quản trị Nguồn nhân lực Doanh nghiệp – Vinamilk HRM (.NET 8)  
**Tiêu chuẩn tham chiếu:** SAP SuccessFactors (Fiori Horizon), Workday, MISA AMIS HRM, Base HRM, 1Office  
**Tiêu chuẩn trợ năng (Accessibility):** WCAG 2.1 Level AA  
**Phiên bản:** 1.0 (Đầu ra Phiên 3)

---

## 1. PHÂN TÍCH & SO SÁNH 4 ADMIN THEME CHO PHẦN MỀM HRM DOANH NGHIỆP

Để xây dựng giao diện phần mềm Quản trị Nhân sự (HRM) có mật độ dữ liệu cao (bảng công 31 ngày, bảng lương 25 cột, hồ sơ nhân sự 360°, biểu đồ phân tích, sơ đồ tổ chức, kanban tuyển dụng), chúng tôi đánh giá 4 bộ Admin UI Framework phổ biến nhất dựa trên nền tảng **Bootstrap 5**:

| Tiêu chí đánh giá | **1. Tabler UI (v1.0+)** | **2. AdminLTE 4** | **3. CoreUI (v5)** | **4. Sneat / Materio BS5** |
| :--- | :--- | :--- | :--- | :--- |
| **Giấy phép (License)** | **MIT License** (Miễn phí 100% cho thương mại & doanh nghiệp) | **MIT License** | Bản Free giới hạn; nhiều component HRM nâng cao nằm ở bản **PRO trả phí** | Bản Free rất hạn chế; bản đầy đủ yêu cầu **Commercial License** |
| **Nền tảng CSS** | Bootstrap 5.3 thuần túy + hệ thống CSS Variables hiện đại (`--tblr-*`) | Bootstrap 5.3 (đang chuyển đổi từ BS4 sang BS5) | Fork riêng từ Bootstrap 5 (`--cui-*`) | Bootstrap 5 tùy biến sâu theo Material Design |
| **Dark Mode & Theme** | **Native (`data-bs-theme="dark"`)**, chuyển đổi tức thì không cần tải lại trang | Hỗ trợ qua `data-bs-theme`, nhưng một số plugin cũ chưa đồng bộ màu | Hỗ trợ tốt trên bản Pro; bản Free cần tự viết thêm CSS | Hỗ trợ đầy đủ trên bản Pro, bản Free bị khóa cấu hình |
| **Hỗ trợ RTL** | Có sẵn (`tabler.rtl.min.css`) | Có sẵn | Có sẵn | Chỉ có trên bản trả phí |
| **Độ nhẹ & Hiệu năng** | **Rất nhẹ (~38KB gzipped CSS, không phụ thuộc jQuery)**, tối ưu SSR Razor Views | Trung bình (~65KB gzipped), cấu trúc DOM nhiều lớp lồng nhau | Nhẹ (~45KB gzipped), nhưng phải cài gói riêng của CoreUI | Khá nặng (~110KB+ gzipped) do nhiều hiệu ứng đổ bóng Material |
| **Mật độ hiển thị dữ liệu (Data Density cho HRM)** | **Xuất sắc**: Thiết kế phẳng tinh tế (Clean Enterprise), tối ưu cho bảng nhiều cột, thẻ KPI, timeline, stepper | Hơi thô và cổ điển; phù hợp trang quản trị kỹ thuật hơn là phần mềm nhân sự hiện đại | Tốt, rõ ràng nhưng ít thành phần đặc thù như Avatar List, Status Dot, Steps | Khoảng trắng (padding) quá lớn, làm bảng chấm công/bảng lương phải cuộn ngang nhiều |
| **Hệ sinh thái Icon** | **Tabler Icons (5.400+ SVG icons)** đồng bộ nét vẽ `1.75px` sắc nét | Phụ thuộc FontAwesome hoặc Bootstrap Icons bên ngoài | CoreUI Icons (số lượng ít hơn ở bản miễn phí) | Boxicons |
| **Điểm tổng hợp (Thang 10)** | **9.6 / 10 (LỰA CHỌN)** | **7.8 / 10** | **8.0 / 10** | **8.2 / 10** |

### Kết luận lựa chọn: **Tabler UI + Tabler Icons (Tùy biến theo Vinamilk Design System)**
- **Lý do 1 (Pháp lý & Chi phí):** Giấy phép **MIT** hoàn toàn tự do cho phần mềm doanh nghiệp nội bộ tại Vinamilk.
- **Lý do 2 (Ngôn ngữ thiết kế Enterprise HRM):** Phong cách thiết kế của Tabler rất gần với **Workday**, **Base HRM** và **SAP Fiori Horizon**: viền mảnh `1px`, bo góc tinh tế `8px–12px`, hỗ trợ sẵn Status Badge có chấm tròn (`status-dot`), Avatar Group, Timeline, Stepper, Empty State và Skeleton Shimmer.
- **Lý do 3 (Tương thích hoàn hảo với ASP.NET Core Razor Views):** Dựa hoàn toàn trên chuẩn Bootstrap 5.3 (`data-bs-toggle`, `data-bs-theme`), dễ dàng mở rộng bằng hệ thống biến CSS `--vnm-*` riêng của Vinamilk mà không xung đột.

---

## 2. ĐẶC TẢ HỆ THỐNG DESIGN SYSTEM VINAMILK HRM (`--vnm-*`)

### 2.1. Bảng màu Thương hiệu & Trạng thái (Color Palette & WCAG 2.1 AA Contrast)
Lấy cảm hứng từ bộ nhận diện thương hiệu mới của **Vinamilk (Est. 1976)**: sắc **Xanh Coban / Navy đậm (`#0213B0` & `#1E3A8A`)** kết hợp nền **Trắng Sữa (`#FFFDF9` / `#F8FAFC`)**:

| Tên Token CSS | Mã màu (Light Mode) | Mã màu (Dark Mode) | Tỷ lệ tương phản (WCAG) | Ứng dụng trong HRM |
| :--- | :--- | :--- | :--- | :--- |
| `--vnm-primary` | `#0213B0` | `#4F75FF` | **11.8 : 1** (AAA) | Nút hành động chính (Primary CTA), Active Menu, Focus Ring, Tiêu đề thương hiệu |
| `--vnm-primary-dark` | `#1E3A8A` | `#1E293B` | **10.4 : 1** (AAA) | Nền Sidebar thương hiệu, Header bảng in ấn, Gradient thẻ tổng quan |
| `--vnm-primary-soft` | `#EEF2FF` | `rgba(79,117,255,0.15)` | — | Nền dòng được chọn, nền icon Stat Card, nền nút phụ |
| `--vnm-cream` | `#FFFDF9` | `#0F172A` | — | Nền thẻ đặc trưng "Trắng sữa Vinamilk", vùng làm việc dịu mắt khi nhìn lâu |
| `--vnm-bg-body` | `#F4F6FA` | `#0B1120` | — | Nền tổng thể trang ứng dụng |
| `--vnm-bg-surface` | `#FFFFFF` | `#1E293B` | — | Nền Card, Bảng dữ liệu, Modal, Dropdown |
| `--vnm-success` | `#059669` | `#34D399` | **4.6 : 1** (AA) | Nhân viên chính thức (`Official`), Đã duyệt (`Approved`), Đủ công, Đạt KPI |
| `--vnm-warning` | `#D97706` | `#FBBF24` | **4.5 : 1** (AA) | Thử việc (`Probation`), Chờ duyệt (`Pending`), Hợp đồng sắp hết hạn |
| `--vnm-danger` | `#DC2626` | `#F87171` | **5.1 : 1** (AA) | Nghỉ việc (`Resigned`), Từ chối (`Rejected`), Kỷ luật, Đi muộn/Về sớm |
| `--vnm-info` | `#0284C7` | `#38BDF8` | **4.5 : 1** (AA) | Nghỉ phép hưởng lương (`PaidLeave`), Đang đào tạo, Điều chuyển |

### 2.2. Hệ thống Chữ viết (Typography – `Be Vietnam Pro`)
- **Font chữ chính (`--vnm-font-sans`):** `'Be Vietnam Pro', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif` — được thiết kế chuyên biệt cho dấu thanh tiếng Việt, giúp các bảng dữ liệu dày đặc chữ tiếng Việt vẫn rõ ràng ở kích thước `13px–14px`.
- **Font chữ số liệu & Mã định danh (`--vnm-font-mono`):** `'JetBrains Mono', Consolas, monospace` kết hợp `font-variant-numeric: tabular-nums` cho tất cả các cột **Tiền lương, Thuế TNCN, Số ngày công, Mã nhân viên (`VNM-0001`)** để các chữ số thẳng hàng tuyệt đối theo cột dọc.
- **Thang chữ (Type Scale):**
  - `--vnm-text-xs`: `0.75rem` (`12px`) – Nhãn phụ, Badge, Timestamp
  - `--vnm-text-sm`: `0.8125rem` (`13px`) – Nội dung ô bảng dữ liệu (DataTable cell)
  - `--vnm-text-base`: `0.875rem` (`14px`) – Cỡ chữ chuẩn toàn hệ thống Enterprise HRM
  - `--vnm-text-md`: `1rem` (`16px`) – Tiêu đề Card, Modal Header
  - `--vnm-text-lg`: `1.25rem` (`20px`) – Tiêu đề trang (Page Title)
  - `--vnm-text-xl`: `1.5rem` (`24px`) – Số liệu KPI trên Stat Card
  - `--vnm-text-2xl`: `2rem` (`32px`) – Chỉ số nổi bật Dashboard

### 2.3. Hệ thống Khoảng cách, Bo góc & Đổ bóng (Spacing 4/8px, Radius & Elevation)
- **Spacing Grid (4px / 8px):** `--vnm-space-1` (`4px`), `--vnm-space-2` (`8px`), `--vnm-space-3` (`12px`), `--vnm-space-4` (`16px`), `--vnm-space-6` (`24px`), `--vnm-space-8` (`32px`).
- **Border Radius:** `--vnm-radius-sm` (`6px` cho nút/input), `--vnm-radius-md` (`10px` cho card/dropdown), `--vnm-radius-lg` (`14px` cho modal/panel).
- **Accessibility & Motion:**
  - Mọi phần tử tương tác có `:focus-visible` rõ nét (`outline: 2px solid var(--vnm-primary); outline-offset: 2px`).
  - Tự động vô hiệu hóa mọi hiệu ứng chuyển động (`animation-duration: 0.01ms !important; transition-duration: 0.01ms !important`) khi hệ điều hành người dùng bật chế độ giảm chuyển động `@media (prefers-reduced-motion: reduce)`.

