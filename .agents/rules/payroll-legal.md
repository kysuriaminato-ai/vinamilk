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
*(Phần bảng tham số chi tiết sẽ được điền đầy đủ và hoàn thiện ở Phiên 2 theo quy chuẩn pháp luật lao động và thuế thu nhập cá nhân 2026).*

<!-- PHẦN BẢNG THAM SỐ: SẼ ĐƯỢC ĐIỀN Ở PHIÊN 2 -->
| Tham số pháp lý | Giá trị quy định | Đơn vị tính | Ngày hiệu lực | Căn cứ văn bản |
| :--- | :--- | :--- | :--- | :--- |
| *(Để trống - Phiên 2 sẽ điền)* | | | | |

## 3. Công thức Nghiệp vụ Cốt lõi
*(Chi tiết các công thức toán học và kiểm thử đơn vị xUnit với bộ số ví dụ cụ thể sẽ được triển khai đầy đủ ở Phiên 2).*
