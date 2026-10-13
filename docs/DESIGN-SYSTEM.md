# HỆ THỐNG THIẾT KẾ VINAMILK HRM (DESIGN SYSTEM SPECIFICATION)
*Phiên bản: 9.0 (Tháng 10/2026)*
*Quy chuẩn: Enterprise Web UI/UX, WCAG 2.2 Level AA, Mobile-First ESS & Desktop Operations*

---

## 1. Triết Lý Thiết Kế & Nhận Diện Thương Hiệu
Hệ thống Vinamilk HRM kế thừa và phát triển từ hệ thống nhận diện thương hiệu chuẩn của Tập đoàn Vinamilk:
- **Tập trung & Chuyên nghiệp**: Tối ưu hóa cho năng suất làm việc của chuyên viên nhân sự (C&B, Tuyển dụng, Đào tạo, Trưởng đơn vị) và trải nghiệm tiện lợi của 10.000+ Cán bộ Nhân viên (CBNV).
- **Rõ ràng & Minh bạch**: Mọi số liệu tính toán (Lương, Thuế, KPI, Compa-Ratio) đều có công thức giải trình chi tiết.
- **Tiêu chuẩn Tiếp cận (Accessibility)**: Tuân thủ nghiêm ngặt **WCAG 2.2 Level AA** với độ tương phản màu sắc chữ tối thiểu 4.5:1 đối với văn bản thông thường và 3.0:1 đối với thành phần đồ họa / văn bản kích thước lớn.

---

## 2. Bảng Màu & Tokens (Color Tokens)

### 2.1. Màu Thương Hiệu & Giao Diện Sáng (Light Mode - Mặc định)
| Token CSS | Mã Màu HEX | HSL | Mục đích sử dụng | WCAG 2.2 AA |
|:---|:---|:---|:---|:---:|
| `--vnm-primary` | `#0033A0` | `hsl(221, 100%, 31%)` | Màu xanh Vinamilk chủ đạo, nút chính, liên kết, header sidebar | **PASS** (8.6:1 trên trắng) |
| `--vnm-primary-dark` | `#00227A` | `hsl(223, 100%, 24%)` | Hover nút chính, sidebar active item | **PASS** (11.8:1 trên trắng) |
| `--vnm-primary-light`| `#E6F0FA` | `hsl(210, 71%, 95%)` | Nền tag nhẹ, highlight dòng được chọn | **PASS** |
| `--vnm-bg` | `#F8F9FA` | `hsl(210, 17%, 98%)` | Nền ứng dụng chính (trắng sữa) | N/A |
| `--vnm-surface` | `#FFFFFF` | `hsl(0, 0%, 100%)` | Nền card, bảng, modal, drawer | N/A |
| `--vnm-border` | `#E2E8F0` | `hsl(214, 32%, 91%)` | Đường viền ngăn cách card, bảng | N/A |
| `--vnm-text-main` | `#1E293B` | `hsl(222, 47%, 18%)` | Tiêu đề, chữ chính (Slate 800) | **PASS** (13.5:1 trên trắng) |
| `--vnm-text-muted` | `#64748B` | `hsl(215, 16%, 47%)` | Chữ phụ, nhãn hướng dẫn (Slate 500) | **PASS** (4.6:1 trên trắng) |

### 2.2. Trạng Thái & Màu Nghiệp Vụ (Semantic & Status Colors)
| Trạng thái | Token CSS | Mã Màu | Ý nghĩa & Quy ước nghiệp vụ |
|:---|:---|:---|:---|
| **Success** | `--vnm-success` | `#10B981` | Hoàn thành, Hợp lệ, Duyệt thành công, Tăng trưởng dương |
| **Warning** | `--vnm-warning` | `#F59E0B` | Cảnh báo vi phạm phân ca, Compa-Ratio lệch, Chờ hiệu chuẩn |
| **Danger** | `--vnm-danger` | `#EF4444` | Từ chối, Lỗi nghiệp vụ, Vi phạm ràng buộc nghiêm trọng |
| **Info** | `--vnm-info` | `#0284C7` | Thông tin hướng dẫn, Tiến trình đang chạy |

### 2.3. Quy Chuẩn Màu Huy Hiệu Trạng Thái (Status Badges)
Màu sắc cố định bất biến cho các trạng thái phê duyệt trong toàn bộ hệ thống:
- `Draft` (Nháp): Nền xám `#F1F5F9`, chữ xám đậm `#475569`.
- `Submitted` (Đã nộp / Chờ duyệt): Nền xanh lam nhạt `#E0F2FE`, chữ xanh đậm `#0369A1`.
- `Approved` (Đã duyệt): Nền xanh lục nhạt `#D1FAE5`, chữ xanh lá đậm `#047857`.
- `Locked` (Đã khóa / Đóng sổ): Nền tím nhạt `#EDE9FE`, chữ tím đậm `#6D28D9`.
- `Rejected` (Từ chối): Nền đỏ nhạt `#FEE2E2`, chữ đỏ đậm `#B91C1C`.

### 2.4. Chế Độ Tối (Dark Mode Tokens - `[data-theme="dark"]`)
- Nền chính (`--vnm-bg`): `#0F172A` (Slate 900)
- Nền bề mặt (`--vnm-surface`): `#1E293B` (Slate 800)
- Đường viền (`--vnm-border`): `#334155` (Slate 700)
- Chữ chính (`--vnm-text-main`): `#F8FAFC` (Slate 50)
- Chữ phụ (`--vnm-text-muted`): `#94A3B8` (Slate 400)

---

## 3. Kiểu Chữ & Định Dạng (Typography)
- **Phông chữ chuẩn**: `Be Vietnam Pro`, `system-ui`, `-apple-system`, `sans-serif`.
- **Cột Tiền & Số liệu**: Bắt buộc sử dụng `font-variant-numeric: tabular-nums` để các chữ số có độ rộng đồng nhất, thẳng hàng khi hiển thị trong bảng dữ liệu kế toán và bảng lương.
- **Quy tắc định dạng địa phương hóa (vi-VN)**:
  - Tiền tệ: `1.234.567 ₫` (Dấu chấm ngăn cách hàng nghìn, ký hiệu `₫` ở cuối).
  - Tỷ lệ phần trăm: `3,03%`, `2,42%` (Dấu phẩy cho phần thập phân).
  - Ngày tháng: `dd/MM/yyyy` (Ví dụ: `13/10/2026`).
  - Giờ phút: `HH:mm` (Ví dụ: `14:30`).

---

## 4. Khoảng Cách, Bo Góc & Bóng Đổ (Spacing & Elevation)
- **Thang khoảng cách (4/8 px scale)**:
  - `4px` (`--vnm-space-1`), `8px` (`--vnm-space-2`), `12px` (`--vnm-space-3`), `16px` (`--vnm-space-4`), `24px` (`--vnm-space-6`), `32px` (`--vnm-space-8`), `48px` (`--vnm-space-12`).
- **Bo góc (Border Radius)**:
  - `--vnm-radius-sm`: `4px` (Tags nhỏ, checkbox)
  - `--vnm-radius-md`: `8px` (Inputs, Buttons, Cards, Table rows)
  - `--vnm-radius-lg`: `12px` (Modals, Drawers, Containers lớn)
  - `--vnm-radius-full`: `9999px` (Badges hình viên thuốc, Avatars)
- **Bóng đổ (Box Shadow)**:
  - `--vnm-shadow-sm`: `0 1px 2px 0 rgb(0 0 0 / 0.05)`
  - `--vnm-shadow-md`: `0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1)`
  - `--vnm-shadow-lg`: `0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1)` (dành cho modal/drawer và sticky table header khi cuộn).

---

## 5. Quy Chuẩn Hiệu Ứng Chuyển Động (Motion & Animation)
- **Quy tắc cơ bản**: Chỉ animate các thuộc tính `transform` và `opacity` để đảm bảo tốc độ dựng khung hình 60fps trên GPU.
- **Thời lượng**: `200ms - 250ms`, easing `cubic-bezier(0, 0, 0.2, 1)` (ease-out).
- **Hỗ trợ View Transitions API**: Chuyển trang đa trang (MPA) mượt mà không nhấp nháy, có fallback tự nhiên cho trình duyệt cũ.
- **TUÂN THỦ PREFERS-REDUCED-MOTION**:
  Khi người dùng kích hoạt thiết lập giảm chuyển động trong hệ điều hành:
  ```css
  @media (prefers-reduced-motion: reduce) {
    *, *::before, *::after {
      animation-duration: 0.01ms !important;
      animation-iteration-count: 1 !important;
      transition-duration: 0.01ms !important;
      scroll-behavior: auto !important;
    }
  }
  ```
  Tất cả các hiệu ứng đếm số, trượt drawer, mờ modal, hover nâng nhẹ đều lập tức chuyển về trạng thái tĩnh tức thì.

---

## 6. Danh Mục Thành Phần Chuẩn (Component Library)
1. **KPI Card**: Hiển thị chỉ số trọng yếu, % thay đổi so với kỳ trước, mini sparkline, animation đếm số tự nhiên.
2. **Data Table**: Header dính (sticky) có bóng đổ khi cuộn, phân trang server-side, chọn nhiều dòng, hành động nhanh theo hàng.
3. **Filter Bar**: Tìm kiếm từ khóa, chọn đơn vị, lọc theo trạng thái, nút xóa lọc, nạp dữ liệu bằng HTMX.
4. **Multi-step Form**: Thanh tiến trình các bước, kiểm tra hợp lệ từng bước, lưu tạm nháp.
5. **Detail Drawer**: Bảng trượt từ cạnh phải (slide-over) 220ms, hiển thị thông tin chuyên sâu của nhân sự hoặc phiếu duyệt.
6. **Confirm Modal**: Cảnh báo xác nhận các thao tác quan trọng (Khóa sổ, Từ chối, Áp dụng tăng lương).
7. **Toast Notifications**: Thông báo nổi góc trên bên phải, hỗ trợ 4 loại (Success, Error, Warning, Info), tự ẩn sau 3.5s.
8. **Empty State**: Hình minh họa SVG thân thiện, tiêu đề rõ nghĩa và nút kêu gọi hành động (CTA).
9. **Skeleton Loader**: Placeholder khung xương hiển thị trong khi HTMX / API đang tải dữ liệu.
10. **Audit Timeline**: Dòng thời gian trực quan ghi nhận chi tiết lịch sử các bước phê duyệt và ký duyệt số.
11. **Shift Matrix Grid**: Lưới ma trận phân ca Nhân viên × Ngày với hỗ trợ kéo thả và cảnh báo vi phạm ràng buộc trực tiếp.
12. **Mobile Bottom Nav**: Thanh điều hướng đáy dành riêng cho Cổng tự phục vụ CBNV trên màn hình di động (< 768px).

---

## 7. Responsive Breakpoints
- **Desktop (≥ 1280px)**: Sidebar đầy đủ, bảng dữ liệu mở rộng, hiển thị đồng thời cả bảng điều khiển và chi tiết.
- **Tablet (768px - 1279px)**: Sidebar thu gọn thành icon, bảng cuộn ngang, modal vừa vặn màn hình.
- **Mobile (< 768px)**: Ẩn sidebar truyền thống, chuyển sang Topbar rút gọn + Bottom Navigation 5 mục: Trang chủ, Chấm công, Ca của tôi, Đơn từ, Tài khoản.