# Kiến Trúc An Toàn Thông Tin & Ma Trận Phân Quyền (Security Matrix)
**Dự án**: Vinamilk Enterprise HRM  
**Phiên bản**: 1.0 — Chuẩn Doanh nghiệp 10.000 CBNV  
**Áp dụng cho**: .NET Core Web API (Backend Lõi) & Portal Web PHP (Giao diện Người dùng)

---

## 1. Nguyên Tắc An Toàn Thông Tin (Security Principles)
1. **Zero Trust & Nguyên tắc Đặc quyền Tối thiểu (Least Privilege)**: Mọi yêu cầu HTTP vào API đều phải được xác thực danh tính qua JWT Bearer token và kiểm tra quyền hạn chi tiết (Fine-grained Permission Check).
2. **Bất khả Chối bỏ (Non-repudiation)**: Mọi thao tác tạo mới, sửa đổi hoặc xóa dữ liệu nhạy cảm (nhân sự, lương, KPI, hợp đồng, chấm công) đều được ghi nhận tự động vào bảng `AuditLogs` kèm theo `UserId` thật và chuỗi JSON khác biệt (Diff) trước/sau bởi `AuditableEntityInterceptor`.
3. **Phòng vệ Chiều sâu (Defense-in-Depth)**:
   - Rate limiting trên các endpoint nhạy cảm (`/api/v1/auth/*`) chống tấn công vét cạn Brute-force.
   - Cơ chế khóa tài khoản tự động sau 5 lần đăng nhập thất bại.
   - Xác thực 2 yếu tố (2FA TOTP) bắt buộc cho các vị trí trọng yếu nắm giữ dữ liệu nhạy cảm (HR và Kế toán).
   - Kiểm soát phạm vi dữ liệu đa điểm (`IDataScope`) đảm bảo người dùng chỉ được xem và thao tác trên dữ liệu thuộc thẩm quyền của đơn vị mình.

---

## 2. Danh Mục 10 Vai Trò Doanh Nghiệp (Enterprise Roles)

| STT | Mã Vai Trò (`RoleName`) | Tên Tiếng Việt | Đối Tượng Áp Dụng Tại Vinamilk | Yêu Cầu 2FA |
| :---: | :--- | :--- | :--- | :---: |
| 1 | `SystemAdmin` | Quản trị Hệ thống | Đội ngũ IT SysAdmin tập đoàn; quản lý hạ tầng và cấu hình hệ thống. | Tùy chọn |
| 2 | `Executive` | Ban Tổng Giám đốc | Tổng Giám đốc (CEO), các Phó Tổng Giám đốc; giám sát toàn tập đoàn. | Tùy chọn |
| 3 | `HRDirector` | Giám đốc Nhân sự | Giám đốc Khối Nhân sự Tập đoàn; phê duyệt lương, KPI, chính sách 3P. | **Bắt buộc** |
| 4 | `HRManager` | Trưởng phòng Nhân sự | Trưởng phòng HR Trụ sở, HR Nhà máy chế biến, HR Trang trại bò sữa. | **Bắt buộc** |
| 5 | `HRSpecialist` | Chuyên viên Nhân sự | Chuyên viên C&B (Tiền lương & Phúc lợi), Tuyển dụng, Đào tạo. | **Bắt buộc** |
| 6 | `ChiefAccountant` | Kế toán trưởng | Kế toán trưởng Tập đoàn; thẩm tra và phê duyệt chi trả lương tài chính. | **Bắt buộc** |
| 7 | `PayrollAccountant`| Kế toán Tiền lương | Kế toán viên phụ trách tính lương, trích đóng BHXH, BHYT, quyết toán thuế TNCN. | **Bắt buộc** |
| 8 | `UnitManager` | Giám đốc Đơn vị | Giám đốc Nhà máy, Giám đốc Trang trại, Trưởng phòng nghiệp vụ. | Không |
| 9 | `TeamLeader` | Quản lý Ca / Tổ trưởng | Tổ trưởng sản xuất nhà máy, Trưởng ca trang trại, Cửa hàng trưởng. | Không |
| 10| `Employee` | Cán bộ Nhân viên / Công nhân | Toàn thể người lao động sử dụng cổng tự phục vụ (Employee Self-Service - ESS). | Không |

---

## 3. Ma Trận Vai Trò × Quyền Hạn (Role-Permission Matrix)
Bộ mã quyền được chuẩn hóa dạng `Module.Action`, đồng bộ 100% giữa .NET Web API và Portal PHP:

| STT | Mã Quyền (`Permission`) | `SysAdmin` | `Exec` | `HRDir` | `HRMgr` | `HRSpec` | `ChiefAcc`| `PayAcc` | `UnitMgr`| `Leader` | `Emp` |
| :---: | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **I** | **HỒ SƠ NHÂN SỰ** | | | | | | | | | | |
| 1 | `NHANSU.View` | [x] | [x] | [x] | [x] | [x] | [ ] | [ ] | [x] | [x] | [x]* |
| 2 | `NHANSU.Add` | [x] | [ ] | [x] | [x] | [x] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 3 | `NHANSU.Edit` | [x] | [ ] | [x] | [x] | [x] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 4 | `NHANSU.Delete` | [x] | [ ] | [x] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 5 | `NHANSU.Export` | [x] | [ ] | [x] | [x] | [x] | [ ] | [ ] | [ ] | [ ] | [ ] |
| **II**| **CHẤM CÔNG & CA KÍP** | | | | | | | | | | |
| 6 | `CHAM_CONG.View` | [x] | [x] | [x] | [x] | [x] | [x] | [x] | [x] | [x] | [x]* |
| 7 | `CHAM_CONG.Add` | [x] | [ ] | [x] | [x] | [x] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 8 | `CHAM_CONG.Edit` | [x] | [ ] | [x] | [x] | [x] | [ ] | [ ] | [ ] | [x] | [ ] |
| 9 | `CHAM_CONG.Approve` | [x] | [ ] | [x] | [x] | [ ] | [ ] | [ ] | [x] | [ ] | [ ] |
| 10| `CHAM_CONG.Export` | [x] | [ ] | [x] | [x] | [ ] | [ ] | [x] | [ ] | [ ] | [ ] |
| **III**| **TIỀN LƯƠNG & PHÚC LỢI** | | | | | | | | | | |
| 11| `LUONG.View` | [x] | [x] | [x] | [ ] | [ ] | [x] | [x] | [ ] | [ ] | [x]* |
| 12| `LUONG.Add` | [x] | [ ] | [ ] | [ ] | [ ] | [ ] | [x] | [ ] | [ ] | [ ] |
| 13| `LUONG.Edit` | [x] | [ ] | [ ] | [ ] | [ ] | [ ] | [x] | [ ] | [ ] | [ ] |
| 14| `LUONG.Approve` | [x] | [x] | [x] | [ ] | [ ] | [x] | [ ] | [ ] | [ ] | [ ] |
| 15| `LUONG.Export` | [x] | [ ] | [x] | [ ] | [ ] | [x] | [x] | [ ] | [ ] | [ ] |
| **IV**| **QUẢN TRỊ HIỆU SUẤT (KPI)** | | | | | | | | | | |
| 16| `KPI.View` | [x] | [x] | [x] | [x] | [ ] | [ ] | [ ] | [x] | [x] | [x]* |
| 17| `KPI.Evaluate` | [x] | [ ] | [x] | [x] | [ ] | [ ] | [ ] | [x] | [x] | [ ] |
| 18| `KPI.Approve` | [x] | [x] | [x] | [ ] | [ ] | [ ] | [ ] | [x] | [ ] | [ ] |
| **V** | **CƠ CẤU TỔ CHỨC** | | | | | | | | | | |
| 19| `DONVI.View` | [x] | [x] | [x] | [x] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 20| `DONVI.Edit` | [x] | [ ] | [x] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| **VI**| **TUYỂN DỤNG & ONBOARDING**| | | | | | | | | | |
| 21| `TUYEN_DUNG.View` | [x] | [x] | [x] | [x] | [x] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 22| `TUYEN_DUNG.Add` | [x] | [ ] | [x] | [x] | [x] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 23| `TUYEN_DUNG.Edit` | [x] | [ ] | [x] | [x] | [x] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 24| `TUYEN_DUNG.Delete` | [x] | [ ] | [x] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 25| `TUYEN_DUNG.Approve`| [x] | [x] | [x] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| **VII**| **ĐÀO TẠO & PHÁT TRIỂN** | | | | | | | | | | |
| 26| `DAO_TAO.View` | [x] | [ ] | [x] | [x] | [x] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 27| `DAO_TAO.Add` | [x] | [ ] | [x] | [x] | [x] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 28| `DAO_TAO.Edit` | [x] | [ ] | [x] | [x] | [x] | [ ] | [ ] | [ ] | [ ] | [ ] |
| **VIII**| **KHEN THƯỞNG - KỶ LUẬT** | | | | | | | | | | |
| 29| `KTKL.View` | [x] | [x] | [x] | [x] | [ ] | [ ] | [ ] | [x] | [ ] | [ ] |
| 30| `KTKL.Add` | [x] | [ ] | [x] | [x] | [ ] | [ ] | [ ] | [x] | [ ] | [ ] |
| 31| `KTKL.Approve` | [x] | [x] | [x] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| **IX**| **BÁO CÁO & HR ANALYTICS** | | | | | | | | | | |
| 32| `BAO_CAO.View` | [x] | [x] | [x] | [x] | [x] | [x] | [x] | [x] | [ ] | [ ] |
| 33| `BAO_CAO.Export` | [x] | [x] | [x] | [ ] | [ ] | [x] | [ ] | [ ] | [ ] | [ ] |
| **X** | **QUẢN TRỊ HỆ THỐNG** | | | | | | | | | | |
| 34| `HE_THONG.View` | [x] | [x] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |
| 35| `HE_THONG.Edit` | [x] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] |

*\* Ghi chú (`*`): Đối với vai trò `Employee`, các quyền chỉ có hiệu lực trên dữ liệu của chính bản thân họ (Self-Service) thông qua cơ chế lọc của `IDataScope`.*

---

## 4. Kiểm Soát Phạm Vi Dữ Liệu (`IDataScope`)
Bên cạnh quyền năng chức năng (Functional Permissions), hệ thống bắt buộc kiểm tra thẩm quyền dữ liệu không gian theo đơn vị hành chính:
- **Truy cập Toàn tập đoàn (Global Access)**: Áp dụng cho `SystemAdmin`, `Executive`, `HRDirector`, `ChiefAccountant`. Được phép xem và truy xuất dữ liệu của tất cả đơn vị trên toàn quốc.
- **Truy cập Cây Đơn vị (Hierarchy Scoping)**: Áp dụng cho `HRManager`, `UnitManager`, `TeamLeader`. Người dùng chỉ xem được nhân sự, công, KPI, lương thuộc đơn vị của họ và toàn bộ các đơn vị cấp con trực thuộc (sử dụng phương thức `u.Path.IsDescendantOf(userUnitPath)` trên cột `hierarchyid`).
- **Truy cập Cá nhân (Self-Service Scoping)**: Áp dụng cho vai trò `Employee`. Mọi truy vấn nếu cố tình gửi `EmployeeId` khác hoặc `PayslipId` của đồng nghiệp đều bị chặn đứng với mã lỗi HTTP `403 Forbidden`.

---

## 5. Quy Chuẩn Token Xác Thực (JWT Specification)
- **Thuật toán ký**: HMAC-SHA256 (`HS256`).
- **Thời hạn sống của Access Token**: 15 phút.
- **Cơ chế Refresh Token**:
  - Độ dài chuỗi ngẫu nhiên bí mật: 64 bytes (Base64).
  - Không bao giờ lưu token thô trong CSDL; chỉ lưu chuỗi băm SHA-256 (`TokenHash`).
  - Hỗ trợ **Token Rotation**: Cứ mỗi lần gọi `/api/v1/auth/refresh`, Refresh Token cũ sẽ bị thu hồi (`RevokedAt`) và được thay thế bằng một Refresh Token mới.
  - Hỗ trợ **Thu hồi tức thời (Revocation)**: Khi người dùng đăng xuất hoặc phát hiện xâm nhập, token có thể bị vô hiệu hóa ngay lập tức qua `/api/v1/auth/revoke`.
