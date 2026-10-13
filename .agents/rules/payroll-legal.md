---
trigger: model_decision
description: "Tham số và công thức lương, thuế TNCN, bảo hiểm, tăng ca theo luật Việt Nam 2026"
---
# Quy chuẩn Pháp lý Tiền lương, Thuế & Bảo hiểm (Luật Việt Nam 2026)

## 1. Nguyên tắc Pháp lý
- Tuyệt đối không hard-code các tham số pháp lý vào mã nguồn logic (lương tối thiểu vùng, mức lương cơ sở, mức giảm trừ gia cảnh bản thân và người phụ thuộc, biểu thuế lũy tiến từng phần, tỷ lệ trích nộp BHXH/BHYT/BHTN/KPCĐ, hệ số tăng ca ngày thường/ngày nghỉ/ngày lễ/ca đêm).
- Toàn bộ tham số pháp lý phải được lưu trữ trong bảng cấu hình tham số theo ngày hiệu lực (`EffectiveFrom`, `EffectiveTo`) và nạp động từ CSDL.
- Bảng lương sau khi được Ban Giám Đốc ký duyệt và giải ngân phải được khóa và trở thành bất biến (Immutable). Mọi điều chỉnh hoặc sai sót số liệu phải thực hiện thông qua khoản điều chỉnh (Arrears/Adjustments) trong kỳ lương kế tiếp.

## 2. Bảng Tham số Pháp lý & Biểu thuế (Áp dụng từ 2026)

### 2.1. Tham số Giảm trừ Gia cảnh và Tiền lương Cơ sở
| Mã tham số | Tên tham số | Giá trị | Đơn vị | Ngày hiệu lực | Ngày hết hạn | Căn cứ pháp lý |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `PIT_SELF_DEDUCTION` | Giảm trừ gia cảnh bản thân | 11,000,000 | VNĐ/tháng | 01/07/2020 | 31/12/2025 | Nghị quyết 954/2020/UBTVQH14 |
| `PIT_SELF_DEDUCTION` | Giảm trừ gia cảnh bản thân | 15,500,000 | VNĐ/tháng | 01/01/2026 | Không thời hạn | Luật Thuế TNCN sửa đổi 2025 |
| `PIT_DEPENDENT_DEDUCTION` | Giảm trừ người phụ thuộc | 4,400,000 | VNĐ/tháng/người | 01/07/2020 | 31/12/2025 | Nghị quyết 954/2020/UBTVQH14 |
| `PIT_DEPENDENT_DEDUCTION` | Giảm trừ người phụ thuộc | 6,200,000 | VNĐ/tháng/người | 01/01/2026 | Không thời hạn | Luật Thuế TNCN sửa đổi 2025 |
| `BASE_SALARY` | Mức lương cơ sở | 2,340,000 | VNĐ/tháng | 01/07/2024 | 30/06/2026 | Nghị định 73/2024/NĐ-CP |
| `BASE_SALARY` | Mức lương cơ sở | 2,530,000 | VNĐ/tháng | 01/07/2026 | Không thời hạn | Nghị định cải cách tiền lương 2026 |
| `TAX_EXEMPT_MEAL_CAP` | Mức trần miễn thuế ăn ca | 730,000 | VNĐ/tháng | 01/01/2020 | Không thời hạn | Thông tư 26/2016/TT-BLĐTBXH |

### 2.2. Lương Tối thiểu Vùng (Áp dụng theo phân loại địa bàn hành chính 2026)
| Vùng | Mức tháng (VNĐ) | Mức giờ (VNĐ/giờ) | Ngày hiệu lực | Căn cứ pháp lý | Ghi chú địa bàn Vinamilk tiêu biểu |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Vùng I** | 5,310,000 | 25,500 | 01/01/2026 | NĐ 74/2024/NĐ-CP & Luật 2025 | Trụ sở TP.HCM, Mega BD, NM Sữa Thống Nhất, NM Sữa Tiên Sơn |
| **Vùng II** | 4,730,000 | 22,700 | 01/01/2026 | NĐ 74/2024/NĐ-CP & Luật 2025 | NM Sữa Lam Sơn, Chi nhánh Cần Thơ, NM Sữa Dielac |
| **Vùng III** | 4,140,000 | 19,900 | 01/01/2026 | NĐ 74/2024/NĐ-CP & Luật 2025 | Trang trại Bò sữa Tây Ninh, Trang trại Bò sữa Thanh Hóa |
| **Vùng IV** | 3,700,000 | 17,800 | 01/01/2026 | NĐ 74/2024/NĐ-CP & Luật 2025 | Vùng xa xôi hẻo lánh, trạm thu mua sữa tươi nông hộ xa |

*Ràng buộc nghiệp vụ*: Lương vị trí P1 trong hợp đồng/thang lương không được phép thấp hơn mức lương tối thiểu vùng của đơn vị nơi người lao động làm việc.

### 2.3. Tỷ lệ Trích nộp Bảo hiểm & Kinh phí Công đoàn
| Loại quỹ bảo hiểm | Ký hiệu Kind | Tỷ lệ NLĐ đóng (%) | Tỷ lệ NSDLĐ đóng (%) | Tổng cộng (%) | Trần trích nộp |
| :--- | :--- | :--- | :--- | :--- | :--- |
| BHXH - Hưu trí & Tử tuất | `BHXH_HuuTri` | 8.0% | 14.0% | 22.0% | 20 × Lương cơ sở |
| BHXH - Ốm đau & Thai sản | `BHXH_OmDau` | 0.0% | 3.0% | 3.0% | 20 × Lương cơ sở |
| BHTNLĐ - BNN | `TNLD_BNN` | 0.0% | 0.5% | 0.5% | 20 × Lương cơ sở |
| Bảo hiểm Y tế (BHYT) | `BHYT` | 1.5% | 3.0% | 4.5% | 20 × Lương cơ sở |
| Bảo hiểm Thất nghiệp (BHTN) | `BHTN` | 1.0% | 1.0% | 2.0% | 20 × Lương tối thiểu vùng |
| Kinh phí Công đoàn (KPCĐ) | `KPCD` | 0.0% | 2.0% | 2.0% | Không trần |
| **Tổng cộng** | | **10.5%** | **23.5%** | **34.0%** | |

### 2.4. Biểu thuế Thu nhập Cá nhân Lũy tiến Từng phần

#### Biểu 7 bậc (Áp dụng đến 31/12/2025):
| Bậc | Thu nhập tính thuế/tháng (VNĐ) | Thuế suất | Số trừ nhanh (VNĐ) |
| :--- | :--- | :--- | :--- |
| 1 | Đến 5,000,000 | 5% | 0 |
| 2 | Trên 5,000,000 đến 10,000,000 | 10% | 250,000 |
| 3 | Trên 10,000,000 đến 18,000,000 | 15% | 750,000 |
| 4 | Trên 18,000,000 đến 32,000,000 | 20% | 1,650,000 |
| 5 | Trên 32,000,000 đến 52,000,000 | 25% | 3,250,000 |
| 6 | Trên 52,000,000 đến 80,000,000 | 30% | 5,850,000 |
| 7 | Trên 80,000,000 | 35% | 9,850,000 |

#### Biểu 5 bậc mới (Áp dụng từ 01/01/2026):
| Bậc | Thu nhập tính thuế/tháng (VNĐ) | Thuế suất | Số trừ nhanh (VNĐ) |
| :--- | :--- | :--- | :--- |
| 1 | Đến 10,000,000 | 5% | 0 |
| 2 | Trên 10,000,000 đến 30,000,000 | 10% | 500,000 |
| 3 | Trên 30,000,000 đến 60,000,000 | 20% | 3,500,000 |
| 4 | Trên 60,000,000 đến 100,000,000 | 30% | 9,500,000 |
| 5 | Trên 100,000,000 | 35% | 14,500,000 |

### 2.5. Hệ số Làm thêm giờ (Tăng ca) - Điều 98 Bộ luật Lao động 2019
| Loại ngày làm việc | Ban ngày (Multiplier) | Ban đêm (22h - 06h) | Căn cứ tính toán |
| :--- | :--- | :--- | :--- |
| Ngày làm việc bình thường | **150%** (1.5) | **200%** (2.0) | Ít nhất 150% + 30% ca đêm + 20% tăng ca đêm |
| Ngày nghỉ hằng tuần (Weekly Rest) | **200%** (2.0) | **270%** (2.7) | Ít nhất 200% + 30% ca đêm + 20% tăng ca đêm |
| Ngày lễ, Tết, ngày nghỉ có hưởng lương | **300%** (3.0) | **390%** (3.9) | Ít nhất 300% + 30% ca đêm + 20% tăng ca đêm |

## 3. Công thức Nghiệp vụ Cốt lõi
- **Thu nhập chịu thuế** = Tổng thu nhập - Các khoản phụ cấp/trợ cấp miễn thuế (ăn ca trong hạn mức 730k, điện thoại theo quy chế, đồng phục, xăng xe công tác).
- **Thu nhập tính thuế** = Thu nhập chịu thuế - Các khoản giảm trừ (Bảo hiểm bắt buộc + Giảm trừ bản thân + Giảm trừ người phụ thuộc).
- **Thuế TNCN** = Áp dụng biểu thuế lũy tiến từng phần tương ứng với kỳ lương tính toán (tra cứu theo ngày hiệu lực).
- **Kiểm tra sàn P1**: `SalaryScale.Amount >= Unit.MinimumWage` $\rightarrow$ Nếu vi phạm ném `ERR_SAL_BELOW_MIN_WAGE`.
