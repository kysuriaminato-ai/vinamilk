# TÀI LIỆU YÊU CẦU NGHIỆP VỤ (BRD - BUSINESS REQUIREMENTS DOCUMENT)
## HỆ THỐNG QUẢN TRỊ NGUỒN NHÂN LỰC TẬP TRUNG (ENTERPRISE HRIS)
**Khách hàng mục tiêu:** Công ty Cổ phần Sữa Việt Nam (Vinamilk)  
**Quy mô triển khai:** 10.000+ Cán bộ công nhân viên (CBCNV)  
**Phiên bản tài liệu:** 1.0 - Trạng thái: Đã phê duyệt yêu cầu cơ sở  
**Chủ trì soạn thảo:** Senior Business Analyst & Solution Architect (10 năm kinh nghiệm HRIS Sản xuất)

---

## 1. MỤC TIÊU VÀ PHẠM VI DỰ ÁN (OBJECTIVES & SCOPE)

### 1.1. Mục tiêu Chiến lược (Business Objectives)
1. **Hợp nhất nền tảng phân tán:** Xóa bỏ tình trạng phân mảnh giữa 2 bản source code (PHP MVC và .NET 9 Clean Architecture), quy tụ về một hệ thống HRIS lõi duy nhất đáp ứng yêu cầu vận hành đồng nhất của Tập đoàn.
2. **Quản trị cơ cấu phân cấp đa tầng:** Chuẩn hóa sơ đồ cây tổ chức từ Hội đồng Quản trị, Ban Tổng Giám đốc đến 6 Khối nghiệp vụ tại Trụ sở TP.HCM và toàn bộ mạng lưới cơ sở (13+ Nhà máy chế biến sữa, 14+ Trang trại bò sữa Green Farm, 3 Chi nhánh kinh doanh Bắc/Trung/Nam và Trung tâm Logistics).
3. **Chuẩn hóa chu trình tính lương 3P tự động:** Tự động hóa 100% quy trình tính công, tính lương theo mô hình 3P (P1 Vị trí, P2 Năng lực, P3 Hiệu suất KPI) kết hợp phụ cấp độc hại, phụ cấp ca kíp sản xuất và trừ đóng bảo hiểm/thuế; cam kết thời gian chạy bảng lương toàn tập đoàn hơn 10.000 người dưới 30 phút.
4. **Kiểm soát rủi ro tuân thủ & Pháp lý Việt Nam:** Động hóa toàn bộ các tham số luật định (Bộ luật Lao động 2019, Luật Thuế TNCN, Luật BHXH, lộ trình tuổi nghỉ hưu NĐ 135/2020/NĐ-CP, lương tối thiểu vùng) theo cơ chế ngày hiệu lực, không hardcode.
5. **Thực thi phân tách trách nhiệm (SoD) & Minh bạch kiểm toán:** Triệt tiêu hoàn toàn khả năng tự phê duyệt đề xuất; ghi nhận vết kiểm toán (Audit Logs) 100% giao dịch trọng yếu; bảo đảm quyền riêng tư dữ liệu lương theo Nghị định 13/2023/NĐ-CP về bảo vệ dữ liệu cá nhân.

### 1.2. Bảng Phân định Phạm vi (In-Scope vs Out-of-Scope)

| STT | Hạng mục / Nghiệp vụ | Trong phạm vi (In-Scope) | Ngoài phạm vi (Out-of-Scope) | Ghi chú & Ranh giới trách nhiệm |
|:---:|:---|:---:|:---:|:---|
| 1 | **Cơ cấu tổ chức** | Quản lý cây tổ chức đa cấp, định biên nhân sự, vị trí chức danh, quản lý theo ngày hiệu lực. | Tự động vẽ lại sơ đồ 3D/OrgChart theo thời gian thực trên VR/AR. | Hỗ trợ Materialized Path tối ưu truy vấn đệ quy cây đơn vị. |
| 2 | **Hồ sơ nhân sự** | Vòng đời CBCNV (Thử việc, Chính thức, Thuyên chuyển, Nghỉ việc), số hóa hợp đồng, bằng cấp, chứng chỉ an toàn sữa. | Chấm điểm tín dụng cá nhân, thẩm tra an ninh nhân thân đa quốc gia. | Áp dụng Soft Delete và SCD Type 2 cho lịch sử hồ sơ. |
| 3 | **Ca kíp & Chấm công** | Lập lịch ca kíp phức tạp (3 ca 4 kíp, ca gãy, ca vắt sữa bò), thu thập log từ máy chấm công FaceID/Vân tay, giải bài toán OT. | Trực tiếp sản xuất phần cứng máy chấm công. | Thu thập dữ liệu qua API Gateway trung gian từ các Agent nhà máy/trang trại. |
| 4 | **Nghỉ phép & Nghỉ việc** | Quản lý quỹ phép năm, nghỉ bù, nghỉ thai sản, ốm đau, quy trình bàn giao tài sản bảo hộ và phỏng vấn nghỉ việc (Exit Interview). | Chi trả trợ cấp thất nghiệp của Nhà nước. | Hệ thống cung cấp hồ sơ trích xuất để làm việc với cơ quan BHXH. |
| 5 | **Đánh giá KPI** | Phân bổ mục tiêu theo tầng cây tổ chức, đánh giá KPI tháng/quý cho khối văn phòng và nhà máy, tính điểm P3. | Tự động hóa đánh giá năng suất qua camera giám sát AI thời gian thực. | Kết quả P3 là đầu vào trực tiếp cho bảng tính lương. |
| 6 | **Tăng lương & Bổ nhiệm**| Đề xuất, thẩm định ngân sách quỹ lương, quy trình duyệt đa cấp SoD, lưu vết lịch sử quyết định lương. | Tự động quyết định tăng lương không qua phê duyệt của con người. | Tuân thủ nguyên tắc không ai tự duyệt đề xuất của chính mình. |
| 7 | **Bảng lương 3P & Thuế**| Tính lương 3P, phụ cấp sản xuất, hạch toán làm thêm giờ, khấu trừ BHXH/BHYT/BHTN, tính thuế TNCN lũy tiến, xuất file chi lương Bank. | Trực tiếp thực hiện lệnh chuyển tiền liên ngân hàng qua Core Banking (Payment Gateway trực tiếp). | Xuất file chuẩn định dạng thanh toán điện tử (BIDV, Vietcombank, Vietinbank). |
| 8 | **Tích hợp Kế toán (SAP)** | Sinh bảng phân bổ chi phí lương tổng hợp theo Cost Center (Mã đơn vị/Xưởng) để đối soát với SAP FI/CO. | Thay thế hệ thống ERP SAP của Vinamilk. | Cung cấp cổng API hoặc file trung gian chuẩn RFC/CSV cho SAP ERP. |

---

## 2. BẢNG TÁC NHÂN HỆ THỐNG (STAKEHOLDER & ACTOR MATRIX)

Bảng phân quyền thực thi bảo đảm nguyên tắc **Quyền hạn = Vai trò (Role) × Phạm vi cây tổ chức (Org Scope)**:

| Mã Vai trò | Tên Vai trò (Actor) | Vị trí thực tế tại Vinamilk | Phạm vi Dữ liệu (Data Scope) | Chức năng Chính trên Hệ thống | Quyền hạn đối với Dữ liệu Lương |
|:---|:---|:---|:---|:---|:---:|
| `SUPER_ADMIN` | **Super Admin (Ban TCCB)** | Ban Tổ chức Cán bộ tại Trụ sở chính TP.HCM | Toàn bộ Tập đoàn (Toàn bộ các nút cây tổ chức) | Cấu hình tham số pháp lý, phê duyệt định biên toàn công ty, giám sát luồng thuyên chuyển liên đơn vị, quản trị toàn diện chính sách nhân sự. | **Toàn quyền** (Xem, Cấu hình, Phê duyệt) |
| `TECH_ADMIN` | **Quản trị Kỹ thuật (IT Admin)** | Phòng Công nghệ Thông tin (IT HQ) | Toàn hệ thống (Phạm vi Kỹ thuật) | Quản lý tài khoản người dùng, phân quyền vai trò, cấu hình kết nối hệ thống, giám sát hiệu năng, sao lưu CSDL, kiểm tra nhật ký lỗi hệ thống. | **BỊ CẤM TUYỆT ĐỐI** (Không xem, Không sửa, Masking dữ liệu) |
| `BOD_EXEC` | **Ban Tổng Giám đốc (BOD/CEO)** | Tổng Giám đốc, các Phó TGĐ Khối | Toàn bộ Tập đoàn hoặc Khối phụ trách | Xem Dashboard tổng quan nhân sự & quỹ lương toàn tập đoàn, phê duyệt ngân sách lương hàng năm, ký duyệt bổ nhiệm/tăng lương cán bộ cấp cao. | **Chỉ đọc báo cáo tổng hợp** / Duyệt cấp cao nhất |
| `UNIT_DIRECTOR`| **Giám đốc Đơn vị / Vùng** | Giám đốc Nhà máy, Giám đốc Trang trại, GĐ Chi nhánh | Cây con (Subtree) của Đơn vị / Vùng được giao quản lý | Phê duyệt kế hoạch ca kíp tháng của nhà máy/trang trại, duyệt đề xuất tuyển dụng cơ sở, duyệt bảng công & bảng lương P3 của đơn vị, ký tờ trình nhân sự. | Xem bảng lương đơn vị mình quản lý |
| `HR_LOCAL` | **Cán bộ Nhân sự Cơ sở (HR Local)**| Chuyên viên Nhân sự tại từng Nhà máy / Trang trại | Cây con (Subtree) của Đơn vị trực thuộc | Quản lý hồ sơ công nhân tại cơ sở, tiếp nhận ứng viên, theo dõi hợp đồng, cấp phát BHLĐ, chốt dữ liệu chấm công hàng ngày/tháng, chạy dự thảo bảng lương đơn vị. | Xem/Xử lý dữ liệu lương đơn vị trực thuộc |
| `LINE_MANAGER` | **Quản lý Trực tiếp (MSS)** | Quản đốc phân xưởng, Trưởng phòng, Tổ trưởng sản xuất | Cây con trực thuộc phòng/tổ/ca mình phụ trách | Lập lịch làm việc ca kíp, phê duyệt đơn nghỉ phép, đơn làm thêm giờ (OT), đánh giá KPI định kỳ cho nhân viên cấp dưới; lập đề xuất khen thưởng/tăng lương. | Chỉ xem điểm KPI & Đề xuất lương, **Không xem** tổng thu nhập chi tiết |
| `PAYROLL_ACC` | **Kế toán Lương (Payroll)** | Phòng Tài chính Kế toán Tập đoàn / Kế toán Đơn vị | Toàn tập đoàn (với Kế toán HQ) hoặc Đơn vị (Kế toán Local) | Kiểm tra dữ liệu chấm công, đối soát các khoản giảm trừ, tính toán bảng lương 3P, tính thuế TNCN, bảo hiểm, xuất file chi trả ngân hàng và file hạch toán Cost Center. | **Toàn quyền nghiệp vụ tính & xuất bảng lương** |
| `EMPLOYEE` | **Nhân viên (ESS)** | Hơn 10.000 CBCNV (Văn phòng, Kỹ sư, Công nhân, Nhân viên trại) | Cá nhân người dùng (`user_id = self`) | Xem hồ sơ cá nhân, tra cứu hợp đồng, nộp đơn xin nghỉ phép/OT/giải trình chấm công, tự đánh giá KPI, xem phiếu lương điện tử (Payslip) được mã hóa. | Chỉ xem phiếu lương cá nhân của chính mình |
| `CANDIDATE` | **Ứng viên (Candidate)** | Người nộp hồ sơ ứng tuyển vào Vinamilk | Cá nhân hồ sơ ứng tuyển (`candidate_id = self`) | Đăng ký tài khoản, nộp CV trực tuyến, cập nhật bằng cấp/chứng chỉ, theo dõi trạng thái ứng tuyển và nhận thư mời nhận việc (Offer Letter). | Không có quyền truy cập dữ liệu nội bộ |

---

## 3. DANH SÁCH PHÂN HỆ XẾP THEO MÔ HÌNH MOSCOW

| STT | Phân hệ Nghiệp vụ | Phân loại MoSCoW | Mục tiêu cốt lõi | Lý do phân loại (Rationale) |
|:---:|:---|:---:|:---|:---|
| 1 | **Quản lý Cơ cấu Tổ chức** | **MUST HAVE** | Quản lý cây phân cấp đơn vị, định biên chức danh, làm nền tảng phân quyền dữ liệu và định tuyến luồng duyệt SoD. | Là xương sống của toàn hệ thống; nếu không có cây tổ chức chuẩn thì không thể phân quyền phạm vi và không thể duyệt đơn từ. |
| 2 | **Quản lý Hồ sơ & Hợp đồng**| **MUST HAVE** | Số hóa 10.000+ hồ sơ CBCNV, quản lý hợp đồng lao động theo SCD Type 2, cảnh báo hết hạn hợp đồng trước 30-60 ngày. | Bắt buộc theo Bộ luật Lao động Việt Nam để phòng ngừa tranh chấp pháp lý và kiểm soát thông tin nhân thân. |
| 3 | **Quản lý Ca kíp & Chấm công**| **MUST HAVE** | Lập lịch ca kíp sản xuất (3 ca 4 kíp, ca vắt sữa 24/7), tích hợp dữ liệu máy chấm công vân tay/FaceID, xử lý OT. | Nhà máy và trang trại Vinamilk không thể vận hành nếu thiếu phân hệ lập ca kíp và chốt công tự động mỗi ngày. |
| 4 | **Quản lý Nghỉ phép & Nghỉ việc**| **MUST HAVE** | Quy trình xin nghỉ phép (phép năm, nghỉ ốm, thai sản, nghỉ bù) và thủ tục thôi việc, bàn giao tài sản bảo hộ theo SoD. | Tuân thủ chế độ thai sản, ốm đau BHXH; kiểm soát tỷ lệ vắng mặt và thu hồi tài sản doanh nghiệp khi nghỉ việc. |
| 5 | **Quản lý Đánh giá Hiệu suất KPI** | **MUST HAVE** | Thiết lập chỉ số KPI theo cấp bậc, chấm điểm hiệu suất tháng/quý làm căn cứ tính cấu phần lương mềm P3. | Cốt lõi của mô hình lương 3P; tạo động lực tăng năng suất sản xuất và công bằng trong đãi ngộ. |
| 6 | **Quản lý Đề xuất Tăng lương & Bổ nhiệm** | **MUST HAVE** | Quy trình đề xuất điều chỉnh lương P1/P2, bổ nhiệm chức vụ có kiểm soát SoD và lưu lịch sử theo ngày hiệu lực. | Đảm bảo tính minh bạch, chống tiêu cực/lợi ích nhóm; bảo lưu toàn vẹn lộ trình phát triển của CBCNV. |
| 7 | **Quản lý Bảng lương 3P & Chế độ** | **MUST HAVE** | Động cơ tính lương 3P cho hơn 10.000 người < 30 phút, áp dụng tham số luật định động (BHXH, Thuế TNCN, Lương tối thiểu vùng). | Chức năng quan trọng sống còn nhất đối với người lao động và tuân thủ nghĩa vụ thuế/bảo hiểm với Nhà nước. |
| 8 | **Quản lý Tuyển dụng & Onboarding** | **SHOULD HAVE** | Quản lý đề xuất tuyển dụng theo định biên, tiếp nhận hồ sơ ứng viên, đặt lịch phỏng vấn và chuyển đổi thành hồ sơ nhân viên. | Giảm tải nhập liệu thủ công cho HR; có thể tạm thời xử lý bán tự động trong giai đoạn đầu nếu nguồn lực giới hạn. |
| 9 | **Báo cáo & Dashboard Phân tích** | **SHOULD HAVE** | Báo cáo nhân sự định kỳ (biến động nhân sự, tỷ lệ nghỉ việc, cơ cấu tuổi/giới tính) và Dashboard quỹ lương trực quan cho BOD. | Hỗ trợ Ban Lãnh đạo ra quyết định chiến lược; các báo cáo tác nghiệp cơ bản đã được nhúng trong từng phân hệ Must. |
| 10 | **Quản lý Tài sản & Đồ bảo hộ (PPE)** | **COULD HAVE** | Theo dõi cấp phát đồng phục, giày ủng, thẻ từ, trang bị an toàn sinh học phòng dịch tại trang trại bò sữa. | Cải tiến quản trị tài sản nội bộ; có thể quản lý qua bảng tính hoặc hệ thống kho vật tư trong giai đoạn 1. |
| 11 | **Cổng Tuyển dụng Ngoài (Career Site)**| **COULD HAVE** | Cổng web tuyển dụng thương hiệu Vinamilk tích hợp mạng xã hội để ứng viên tự tạo hồ sơ và nộp CV. | Nâng cao trải nghiệm ứng viên; hiện tại có thể tích hợp qua nhận hồ sơ email hoặc form trung gian. |
| 12 | **Dự báo Nhân sự & AI Analytics** | **WON'T HAVE** | Ứng dụng Machine Learning dự đoán nguy cơ nghỉ việc, tối ưu hóa phân ca tự động dựa trên dự báo thời tiết/sản lượng sữa. | Để dành cho Giai đoạn 2 sau khi hệ thống cốt lõi và dữ liệu lịch sử đã vận hành ổn định từ 12-24 tháng. |

---

## 4. ĐẶC TẢ USER STORIES & TIÊU CHÍ NGHIỆM THU CHO CÁC PHÂN HỆ "MUST HAVE"

### 4.1. Phân hệ 1: Quản lý Cơ cấu Tổ chức (Organizational Hierarchy & Units)

#### US-ORG-01: Khởi tạo và Quản trị Nút Cây Đơn vị
* **User Story:** Là **Super Admin (Ban TCCB)**, tôi muốn **tạo mới hoặc điều chỉnh các đơn vị/phòng ban theo mô hình cây phân cấp cha-con có gán mã định danh và loại hình (Nhà máy, Trang trại, Chi nhánh, Phòng ban HQ)**, để **chuẩn hóa cấu trúc bộ máy vận hành toàn tập đoàn**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Tạo thành công nút đơn vị con):**
    * *Given:* Super Admin đã đăng nhập và đang ở màn hình Cây sơ đồ tổ chức.
    * *When:* Nhập tên đơn vị "Trang trại Bò sữa Green Farm Tây Ninh", chọn nút cha là "Khối Phát triển Vùng nguyên liệu", chọn loại hình "Trang trại", nhập ngày hiệu lực `effective_from = 01/01/2026`.
    * *Then:* Hệ thống lưu thành công bản ghi, tự động sinh mã đường dẫn `node_path` (ví dụ: `/1/4/15/`), và hiển thị nút mới trên cây tổ chức ngay lập tức.
  * **Kịch bản 2 (Ngăn chặn vòng lặp cha-con):**
    * *Given:* Đơn vị "Phân xưởng Chế biến Sữa chua" là con của "Nhà máy Mega Bình Dương".
    * *When:* Người dùng cố tình sửa đơn vị cha của "Nhà máy Mega Bình Dương" thành "Phân xưởng Chế biến Sữa chua".
    * *Then:* Hệ thống chặn hành động, báo lỗi "Không thể thiết lập đơn vị con làm đơn vị cha (Cyclic dependency)", đồng thời ghi log cảnh báo.

#### US-ORG-02: Thiết lập Định biên Nhân sự (Headcount Budget)
* **User Story:** Là **Super Admin (Ban TCCB)**, tôi muốn **thiết lập số lượng định biên nhân sự tối đa cho từng chức danh tại từng đơn vị theo từng năm tài chính**, để **kiểm soát quy mô nhân sự và ngăn chặn việc tuyển dụng vượt ngân sách**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Thiết lập định biên hợp lệ):**
    * *Given:* Đơn vị "Nhà máy Sữa Tiên Sơn" có chức danh "Kỹ sư Vận hành Máy rót tiệt trùng Tetra Pak".
    * *When:* Ban TCCB nhập số lượng định biên là 25 người cho năm 2026 và nhấn Lưu.
    * *Then:* Hệ thống lưu dữ liệu định biên, hiển thị trạng thái hiện tại (ví dụ: Đang có 22/25 nhân sự, còn trống 3 vị trí).
  * **Kịch bản 2 (Cảnh báo vượt định biên khi điều chuyển):**
    * *Given:* Chức danh "Nhân viên QC" tại Nhà máy Dielac đã đủ 10/10 người theo định biên.
    * *When:* HR Local tạo đề xuất điều chuyển thêm 01 nhân sự vào chức danh này.
    * *Then:* Hệ thống cảnh báo đỏ "Vượt quá định biên nhân sự đã duyệt (10/10)", bắt buộc phải có phê duyệt ngoại lệ của Tổng Giám đốc mới được gửi đi.

#### US-ORG-03: Quản lý Vị trí Chức danh Công việc (Job Titles & Grades)
* **User Story:** Là **Super Admin (Ban TCCB)**, tôi muốn **quản lý danh mục chức danh kèm bậc ngạch lương chuẩn (Job Grade/Band)**, để **chuẩn hóa thang vị trí làm căn cứ xác định mức lương P1**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Khai báo chức danh chuẩn):**
    * *Given:* Đang ở danh mục chức danh nghề nghiệp.
    * *When:* Tạo chức danh "Tổ trưởng Tổ Vắt sữa Bò", gán Job Grade "L4-SX", dải lương P1 từ 12.000.000 đến 16.000.000 VND, hiệu lực từ `01/01/2026`.
    * *Then:* Hệ thống lưu thành công và chỉ cho phép các nhân sự có chức danh này được áp dải lương P1 trong khung quy định.
  * **Kịch bản 2 (Lưu vết thay đổi dải lương theo ngày hiệu lực):**
    * *Given:* Chức danh "Chuyên viên Nghiên cứu Sản phẩm R&D" đang áp dụng dải lương cũ đến hết ngày `31/12/2025`.
    * *When:* Cập nhật dải lương mới áp dụng từ `01/01/2026`.
    * *Then:* Hệ thống tự động đóng `effective_to = 31/12/2025` ở dải lương cũ, tạo dải lương mới với `effective_from = 01/01/2026`, không ghi đè dữ liệu quá khứ.

#### US-ORG-04: Phân định Phạm vi Quản lý Dữ liệu theo Cây Tổ chức
* **User Story:** Là **Quản trị Kỹ thuật (IT Admin)**, tôi muốn **gán tài khoản người dùng với một nút cây tổ chức cụ thể**, để **hạn chế vùng dữ liệu mà người dùng đó được phép truy xuất theo đúng trách nhiệm**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Giới hạn truy cập cho HR Local):**
    * *Given:* Tài khoản HR "Nguyễn Văn A" được phân vai trò `HR_LOCAL` và gán vào nút "Nhà máy Sữa Cần Thơ".
    * *When:* Người dùng này mở danh sách nhân sự hoặc danh sách chấm công.
    * *Then:* Hệ thống chỉ tải dữ liệu của các nhân sự có `org_unit_id` thuộc nhánh con của "Nhà máy Sữa Cần Thơ", hoàn toàn không nhìn thấy nhân sự của các nhà máy khác.
  * **Kịch bản 2 (Kế thừa quyền đối với cấp quản lý vùng):**
    * *Given:* Giám đốc Vùng Nam Bộ được gán vào nút cha "Chi nhánh & Khối SX Nam Bộ" (chứa 4 nhà máy con).
    * *When:* Giám đốc vùng xem báo cáo quân số.
    * *Then:* Hệ thống tự động tổng hợp đệ quy dữ liệu quân số của cả 4 nhà máy cấp dưới.

#### US-ORG-05: Tra cứu Cây Tổ chức Tác nghiệp Real-time
* **User Story:** Là **Nhân viên (ESS)**, tôi muốn **tra cứu sơ đồ cơ cấu tổ chức và thông tin liên hệ công việc của các phòng ban trong tập đoàn**, để **dễ dàng kết nối phối hợp công tác liên phòng ban**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Tra cứu danh bạ nội bộ):**
    * *Given:* Nhân viên văn phòng đăng nhập vào cổng ESS.
    * *When:* Tìm kiếm từ khóa "Phòng Đảm bảo Chất lượng QA Trụ sở".
    * *Then:* Hệ thống hiển thị cây phân cấp của phòng, tên Trưởng phòng, email công vụ, số điện thoại nội bộ máy lẻ, nhưng ẩn toàn bộ thông tin mức lương và đánh giá nội bộ.

#### US-ORG-06: Tách / Sáp nhập / Đổi tên Đơn vị bảo toàn Lịch sử
* **User Story:** Là **Super Admin (Ban TCCB)**, tôi muốn **thực hiện nghiệp vụ giải thể, sáp nhập hoặc đổi tên đơn vị có lưu ngày hiệu lực**, để **duy trì tính toàn vẹn dữ liệu báo cáo lịch sử trước và sau sáp nhập**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Đổi tên đơn vị từ ngày tương lai):**
    * *Given:* Đơn vị "Trang trại Bò sữa Tuyên Quang" có quyết định đổi tên thành "Tổ hợp Trang trại Sinh thái Vinamilk Tuyên Quang" từ ngày `01/07/2026`.
    * *When:* Admin thiết lập thông tin đổi tên với `effective_from = 01/07/2026`.
    * *Then:* Khi tra cứu báo cáo lương tháng 05/2026 hệ thống vẫn hiển thị tên cũ, và tra cứu từ tháng 07/2026 trở đi sẽ hiển thị tên mới.

---

### 4.2. Phân hệ 2: Quản lý Hồ sơ & Hợp đồng Lao động (Employee Records & Contracts)

#### US-EMP-01: Tạo mới Hồ sơ Nhân sự và Mã Nhân viên Tự động
* **User Story:** Là **Cán bộ Nhân sự Cơ sở (HR Local)**, tôi muốn **tiếp nhận nhân sự mới và được hệ thống sinh Mã nhân viên (Employee ID) tự động theo quy tắc chuẩn**, để **đảm bảo tính duy nhất và nhất quán trong toàn tập đoàn**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Tạo hồ sơ thành công):**
    * *Given:* HR Local nhập đầy đủ thông tin bắt buộc: Họ tên, CCCD/Hộ chiếu, Ngày sinh, Quê quán, Giới tính, Đơn vị trực thuộc, Chức danh.
    * *When:* Bấm nút "Lưu hồ sơ".
    * *Then:* Hệ thống sinh mã nhân viên định dạng `VNMxxxxx` tăng tự động không trùng lặp, khởi tạo trạng thái `PROBATION` (Thử việc), và lưu ngày bắt đầu làm việc.
  * **Kịch bản 2 (Chặn trùng lặp số CCCD):**
    * *Given:* Nhân sự có số CCCD `079088xxxxxx` đã tồn tại trong cơ sở dữ liệu (kể cả nhân sự đã nghỉ việc trước đây).
    * *When:* HR Local nhập lại số CCCD này cho một hồ sơ mới.
    * *Then:* Hệ thống chặn lưu và thông báo: "Số CCCD này đã tồn tại trên hồ sơ nhân viên [Mã NV - Tên]. Vui lòng sử dụng chức năng Tiếp nhận lại (Re-hire)".

#### US-EMP-02: Quản lý Vòng đời Hợp đồng Lao động theo SCD Type 2
* **User Story:** Là **Cán bộ Nhân sự Cơ sở (HR Local)**, tôi muốn **ký kết hợp đồng lao động mới (Thử việc -> Có thời hạn -> Không xác định thời hạn) với ngày hiệu lực rõ ràng**, để **bảo lưu toàn bộ lịch sử biến động pháp lý lao động của nhân viên**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Chuyển tiếp từ thử việc sang chính thức):**
    * *Given:* Nhân viên đang có Hợp đồng thử việc hiệu lực từ `01/10/2025` đến `30/11/2025`.
    * *When:* HR Local tạo Hợp đồng xác định thời hạn 12 tháng từ `01/12/2025` đến `30/11/2026`.
    * *Then:* Hợp đồng thử việc chuyển trạng thái `EXPIRED`, hợp đồng mới được tạo ở trạng thái `ACTIVE`, trường `is_current = TRUE`, dữ liệu hợp đồng cũ không bị sửa đổi hay xóa.
  * **Kịch bản 2 (In hợp đồng lao động mẫu chuẩn):**
    * *Given:* Hợp đồng lao động đã được duyệt trên hệ thống.
    * *When:* HR Local bấm nút "Xuất văn bản hợp đồng".
    * *Then:* Hệ thống tự động điền các trường thông tin (Lương P1, Phụ cấp, Địa điểm làm việc, Quyền lợi BHXH) vào biểu mẫu Word/PDF chuẩn của Vinamilk.

#### US-EMP-03: Cảnh báo Tự động Hết hạn Hợp đồng Lao động
* **User Story:** Là **Cán bộ Nhân sự Cơ sở (HR Local)**, tôi muốn **nhận thông báo danh sách hợp đồng sắp hết hạn trước 30 ngày và 60 ngày**, để **kịp thời thực hiện quy trình đánh giá tái ký hoặc chấm dứt hợp đồng theo đúng Luật Lao động**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Kích hoạt cảnh báo trên Dashboard):**
    * *Given:* Có 15 công nhân tại Nhà máy Bình Dương có hợp đồng hết hạn vào ngày `15/11/2026`.
    * *When:* Hôm nay là ngày `15/10/2026` (tròn 30 ngày trước ngày hết hạn).
    * *Then:* Hệ thống tự động gửi thông báo chuông và email nhắc việc đến HR Local của Nhà máy Bình Dương kèm danh sách chi tiết 15 công nhân trên.

#### US-EMP-04: Quản lý Chứng chỉ Kỹ thuật & An toàn Vệ sinh Thực phẩm
* **User Story:** Là **Cán bộ Nhân sự Cơ sở (HR Local)**, tôi muốn **quản lý ngày hết hạn của Thẻ Xanh khám sức khỏe, Chứng chỉ An toàn Lao động, Chứng chỉ Vận hành Nồi hơi/Xe nâng**, để **đảm bảo 100% công nhân nhà máy và trang trại đáp ứng tiêu chuẩn kiểm toán quốc tế (ISO, FSSC 22000, GlobalGAP)**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Cảnh báo Thẻ Xanh sắp hết hạn):**
    * *Given:* Công nhân dây chuyền chế biến sữa tiệt trùng có Thẻ Xanh sức khỏe hết hạn sau 45 ngày nữa.
    * *When:* Hệ thống quét job định kỳ ban đêm.
    * *Then:* Đưa tên công nhân vào danh sách "Cần tái khám sức khỏe định kỳ" gửi tới Quản đốc xưởng và HR Local.

#### US-EMP-05: Thuyên chuyển Đơn vị / Điều động Công tác (Internal Transfer)
* **User Story:** Là **Super Admin (Ban TCCB)**, tôi muốn **tạo quyết định điều động nhân sự từ Đơn vị này sang Đơn vị khác kèm ngày hiệu lực**, để **cập nhật lại quyền hạn truy cập dữ liệu và hạch toán chi phí lương đúng địa bàn**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Điều chuyển có hiệu lực ngay):**
    * *Given:* Kỹ sư đang công tác tại "Nhà máy Sữa Thống Nhất".
    * *When:* Có Quyết định điều động sang "Nhà máy Sữa Lam Sơn" hiệu lực từ ngày `01/11/2026`.
    * *Then:* Bản ghi công tác cũ đóng `effective_to = 31/10/2026`, bản ghi mới có `effective_from = 01/11/2026`, quyền xem dữ liệu của HR Nhà máy Thống Nhất bị cắt và tự động bàn giao quyền quản lý cho HR Nhà máy Lam Sơn kể từ ngày `01/11/2026`.

#### US-EMP-06: Cập nhật Thông tin Bản thân (ESS Profile Update)
* **User Story:** Là **Nhân viên (ESS)**, tôi muốn **gửi đề xuất cập nhật thông tin cá nhân (Số tài khoản ngân hàng, Địa chỉ thường trú, Thông tin người phụ thuộc giảm trừ gia cảnh)**, để **đảm bảo dữ liệu thuế và nhận lương luôn chính xác**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Cập nhật cần phê duyệt):**
    * *Given:* Nhân viên thay đổi số tài khoản ngân hàng nhận lương và tải lên ảnh chụp xác nhận của ngân hàng.
    * *When:* Bấm gửi yêu cầu cập nhật.
    * *Then:* Trạng thái chuyển sang `WAITING_HR_APPROVAL`; HR Local kiểm tra đối chiếu và bấm "Duyệt", thông tin mới chính thức được ghi đè kèm bản ghi trong `audit_logs`.

---

### 4.3. Phân hệ 3: Quản lý Ca kíp & Chấm công (Shifts & Attendance)

#### US-ATT-01: Thiết lập Danh mục Ca làm việc Sản xuất Đa dạng
* **User Story:** Là **Quản lý Trực tiếp (MSS - Quản đốc/Tổ trưởng)**, tôi muốn **khai báo các loại ca làm việc: Ca Hành chính (8h), Ca 1 (06:00-14:00), Ca 2 (14:00-22:00), Ca 3 Đêm (22:00-06:00 hôm sau), và Ca Vắt sữa Trang trại (04:00-08:00 & 14:00-18:00)**, để **phục vụ lập lịch làm việc cho từng khối đặc thù**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Tạo ca đêm có phụ cấp):**
    * *Given:* Quản đốc xưởng thiết lập Ca 3 từ 22:00 đến 06:00 ngày hôm sau.
    * *When:* Đánh dấu cờ "Ca làm việc ban đêm" và thiết lập giờ nghỉ giữa ca là 45 phút.
    * *Then:* Hệ thống tự động nhận diện ca này có thời gian làm việc ban đêm để tự động kích hoạt tính Phụ cấp ca đêm (+30% theo Luật Lao động) khi tính lương.

#### US-ATT-02: Phân ca Kíp Làm việc Tuần/Tháng cho Phân xưởng
* **User Story:** Là **Quản lý Trực tiếp (MSS)**, tôi muốn **xếp lịch làm việc (Roster) theo tổ đội theo mô hình 3 ca 4 kíp hoặc xoay ca tự động**, để **bố trí nhân sự vận hành liên tục dây chuyền sản xuất sữa 24/7**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Xếp ca hàng loạt cho Tổ sản xuất):**
    * *Given:* Tổ Vận hành Máy UHT có 12 công nhân.
    * *When:* Quản đốc chọn mẫu xoay ca "Kíp A: Tuần 1 Ca 1, Tuần 2 Ca 2, Tuần 3 Ca 3, Tuần 4 Nghỉ bù" cho tháng tiếp theo và nhấn "Áp dụng".
    * *Then:* Hệ thống sinh lịch làm việc chi tiết cho từng công nhân theo từng ngày trong tháng, cảnh báo nếu có công nhân bị xếp làm 2 ca liên tiếp không đủ 12 tiếng nghỉ ngơi giữa hai ca.

#### US-ATT-03: Thu thập và Khớp Dữ liệu Chấm công (Log In/Out Matching)
* **User Story:** Là **Cán bộ Nhân sự Cơ sở (HR Local)**, tôi muốn **hệ thống tự động đồng bộ dữ liệu quẹt thẻ/FaceID từ máy chấm công và so khớp với lịch phân ca**, để **xác định chính xác số giờ làm việc thực tế, đi muộn, về sớm**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Khớp giờ vào ra chuẩn xác):**
    * *Given:* Công nhân được xếp Ca 1 (06:00 - 14:00). Có log quẹt FaceID lúc 05:52 và 14:05.
    * *When:* Job đồng bộ dữ liệu chấm công chạy lúc 14:30.
    * *Then:* Hệ thống ghi nhận trạng thái: Đi đúng giờ, Về đúng giờ, Tổng giờ công thực tế: 8.0 giờ tiêu chuẩn.
  * **Kịch bản 2 (Xử lý đi muộn có trừ công):**
    * *Given:* Nhân viên ca hành chính (08:00 - 17:00) quẹt thẻ lúc 08:25 và không có đơn giải trình.
    * *When:* Hệ thống xử lý dữ liệu công trong ngày.
    * *Then:* Hệ thống ghi nhận trạng thái: Đi muộn 25 phút, số công ghi nhận = 7.5 giờ (hoặc trừ theo quy chế công ty).

#### US-ATT-04: Đăng ký và Phê duyệt Làm thêm giờ (Overtime - OT)
* **User Story:** Là **Quản lý Trực tiếp (MSS)**, tôi muốn **lập Phiếu đăng ký làm thêm giờ (OT) trước khi thực hiện ca làm thêm kèm lý do sản xuất (ví dụ: Tăng sản lượng đơn hàng Tết)**, để **kiểm soát giờ công và tuân thủ giới hạn OT theo luật**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Duyệt đơn OT hợp lệ):**
    * *Given:* Tổ trưởng lập phiếu OT 2 giờ sau ca làm việc ngày thường cho 5 công nhân đóng gói.
    * *When:* Giám đốc Nhà máy bấm duyệt phiếu OT.
    * *Then:* Hệ thống kích hoạt ghi nhận dữ liệu OT này vào bảng công của 5 công nhân với hệ số OT 150%.
  * **Kịch bản 2 (Cảnh báo vượt trần OT theo Luật Lao động):**
    * *Given:* Công nhân Trần Thị B đã tích lũy 38 giờ OT trong tháng hiện tại (trần quy định tối đa 40 giờ/tháng).
    * *When:* Tổ trưởng tạo phiếu OT thêm 4 giờ nữa cho công nhân B.
    * *Then:* Hệ thống cảnh báo đỏ: "Tổng giờ OT trong tháng sẽ vượt quá 40 giờ (42/40 giờ) theo Điều 107 BLLĐ 2019", chặn gửi phiếu trừ khi có xác nhận ngoại lệ của Ban TGĐ.

#### US-ATT-05: Giải trình Chấm công Quên quẹt thẻ (Attendance Discrepancy)
* **User Story:** Là **Nhân viên (ESS)**, tôi muốn **tạo đơn giải trình quên chấm công hoặc lỗi thiết bị nhận diện khuôn mặt kèm xác nhận của quản lý**, để **được ghi nhận đủ công làm việc thực tế trong tháng**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Giải trình được chấp thuận):**
    * *Given:* Ngày 10/10/2026 hệ thống ghi nhận thiếu quẹt thẻ lúc ra ca do cúp điện tại cổng phụ trang trại.
    * *When:* Nhân viên gửi đơn giải trình giờ ra là 17:00 kèm chứng từ, Tổ trưởng xác nhận và HR Local duyệt.
    * *Then:* Hệ thống tự động cập nhật lại giờ ra thực tế thành 17:00, chuyển trạng thái ngày công từ "Bất thường" sang "Hợp lệ (Đã duyệt giải trình)".

#### US-ATT-06: Chốt Bảng Tổng hợp Công Tháng của Đơn vị (Timekeeping Lock)
* **User Story:** Là **Cán bộ Nhân sự Cơ sở (HR Local)**, tôi muốn **kiểm tra toàn diện và thực hiện nghiệp vụ Chốt Bảng công tháng của đơn vị**, để **chuyển giao dữ liệu công sạch sang phân hệ Tính lương**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Chốt bảng công không còn lỗi):**
    * *Given:* Toàn bộ các bất thường đi muộn, vắng mặt, đơn nghỉ phép trong tháng của Nhà máy Cần Thơ đã được xử lý xong 100%.
    * *When:* HR Local bấm nút "Khóa & Chốt bảng công tháng 10/2026".
    * *Then:* Bảng công chuyển trạng thái `LOCKED`, dữ liệu đóng băng không cho phép chỉnh sửa, đồng thời gửi thông báo sẵn sàng tính lương sang bộ phận Kế toán Lương.

---

### 4.4. Phân hệ 4: Quản lý Nghỉ phép & Nghỉ việc (Leave & Offboarding)

#### US-LEV-01: Quản lý Quỹ Phép năm và Thâm niên Tự động
* **User Story:** Là **Nhân viên (ESS)**, tôi muốn **hệ thống tự động cộng dồn ngày phép năm hàng tháng và cộng thêm ngày phép thâm niên (mỗi 5 năm làm việc được thêm 1 ngày phép)**, để **tôi chủ động nắm rõ số ngày phép còn lại khi lập kế hoạch nghỉ**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Tích lũy phép theo thâm niên):**
    * *Given:* Nhân viên có thâm niên làm việc tại Vinamilk tròn 5 năm tính đến tháng `01/2026`.
    * *When:* Hệ thống chạy job cộng quỹ phép đầu năm 2026.
    * *Then:* Quỹ phép năm của nhân viên được cấp là 13 ngày (12 ngày tiêu chuẩn + 1 ngày thâm niên theo Điều 114 BLLĐ).
  * **Kịch bản 2 (Chuyển phép tồn sang năm sau):**
    * *Given:* Nhân viên còn dư 3 ngày phép năm 2025. Chính sách công ty cho phép sử dụng đến hết ngày 31/03/2026.
    * *When:* Ngày 01/01/2026 bắt đầu năm mới.
    * *Then:* Hệ thống tách riêng 2 dòng: "Phép tồn năm trước (Hạn dùng 31/03/2026): 3 ngày" và "Phép năm hiện tại: 12 ngày".

#### US-LEV-02: Nộp Đơn Xin Nghỉ phép Trực tuyến (Leave Request)
* **User Story:** Là **Nhân viên (ESS)**, tôi muốn **nộp đơn xin nghỉ phép (Phép năm, Nghỉ kết hôn, Nghỉ thai sản, Nghỉ ốm BHXH) trên cổng portal điện tử**, để **thay thế hoàn toàn đơn giấy và theo dõi tiến độ duyệt**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Nộp đơn nghỉ phép năm thành công):**
    * *Given:* Nhân viên còn đủ 4 ngày phép năm.
    * *When:* Chọn loại nghỉ "Phép năm", chọn khoảng thời gian từ `12/10/2026` đến `13/10/2026` (2 ngày) và bấm Gửi.
    * *Then:* Hệ thống tạm giữ 2 ngày phép (trạng thái Pending), gửi thông báo duyệt đến Quản lý trực tiếp trên hệ thống và email.
  * **Kịch bản 2 (Chặn khi vượt quá số dư phép khả dụng):**
    * *Given:* Nhân viên chỉ còn dư 1 ngày phép năm khả dụng.
    * *When:* Tạo đơn xin nghỉ phép năm 2 ngày.
    * *Then:* Hệ thống cảnh báo "Số dư phép năm không đủ (Còn 1 ngày, yêu cầu 2 ngày). Bạn có muốn chuyển sang Nghỉ không hưởng lương?", không cho nộp ở loại Phép năm.

#### US-LEV-03: Luồng Duyệt Đơn Nghỉ phép Tuân thủ SoD
* **User Story:** Là **Quản lý Trực tiếp (MSS)**, tôi muốn **duyệt hoặc từ chối đơn xin nghỉ phép của nhân viên cấp dưới trực thuộc phân xưởng/phòng ban của mình**, để **đảm bảo sắp xếp đủ nhân sự vận hành sản xuất**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Quản lý duyệt đơn của cấp dưới):**
    * *Given:* Quản đốc xưởng nhận được đơn xin nghỉ 1 ngày của công nhân trong tổ.
    * *When:* Quản đốc bấm "Đồng ý phê duyệt".
    * *Then:* Trạng thái đơn chuyển sang `APPROVED`, hệ thống tự động cập nhật lịch chấm công ngày đó thành ký hiệu `P` (Nghỉ phép hưởng lương), trừ chính thức 1 ngày trong quỹ phép của công nhân.
  * **Kịch bản 2 (Tuân thủ SoD - Cấm tự phê duyệt):**
    * *Given:* Bản thân Quản đốc xưởng tạo đơn xin nghỉ phép cho chính mình.
    * *When:* Đơn được tạo thành công.
    * *Then:* Hệ thống cấm hiển thị nút tự duyệt trên tài khoản của Quản đốc, tự động định tuyến đơn lên cấp trên là Giám đốc Nhà máy để duyệt.

#### US-LEV-04: Đăng ký Nghỉ Chế độ BHXH (Thai sản, Tai nạn, Ốm đau)
* **User Story:** Là **Nhân viên (ESS)**, tôi muốn **nộp giấy ra viện/chứng nhận nghỉ việc hưởng BHXH lên hệ thống**, để **HR hoàn tất hồ sơ giải quyết chế độ ốm đau/thai sản với cơ quan Bảo hiểm Xã hội**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Tải chứng từ BHXH):**
    * *Given:* Nhân viên nữ nghỉ chế độ khám thai 1 ngày.
    * *When:* Chọn loại nghỉ "Khám thai hưởng BHXH", tải ảnh Giấy khám thai theo mẫu C65-HD.
    * *Then:* Đơn được gửi thẳng tới HR Local, ngày công được ghi nhận mã `TS`, không trừ lương công ty chi trả mà chuyển vào danh sách đề nghị BHXH thanh toán trợ cấp.

#### US-LEV-05: Khởi tạo Quy trình Thôi việc & Bàn giao Tài sản (Offboarding Checklist)
* **User Story:** Là **Nhân viên (ESS) hoặc HR Local**, tôi muốn **khởi tạo Đơn xin thôi việc hoặc Quyết định chấm dứt hợp đồng lao động**, để **kích hoạt quy trình bàn giao công việc và thu hồi tài sản doanh nghiệp theo quy định**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Kích hoạt checklist bàn giao tự động):**
    * *Given:* Đơn xin nghỉ việc của nhân viên được Giám đốc Đơn vị phê duyệt ngày làm việc cuối cùng là `31/10/2026`.
    * *When:* Quyết định thôi việc được kích hoạt.
    * *Then:* Hệ thống sinh luồng bàn giao tự động gửi tới 3 bên: Quản lý trực tiếp (Bàn giao hồ sơ/công việc), Bộ phận IT (Thu hồi laptop, khóa tài khoản email/hệ thống vào ngày 31/10), Bộ phận Hành chính/Kho (Thu hồi thẻ từ, đồng phục bảo hộ, công cụ dụng cụ).

#### US-LEV-06: Quyết toán Thôi việc và Đóng Hồ sơ Nhân sự (Final Settlement)
* **User Story:** Là **Kế toán Lương (Payroll)**, tôi muốn **tự động tính toán số tiền quyết toán thôi việc (Tiền lương ngày làm việc thực tế, tiền thanh toán những ngày phép năm chưa sử dụng hết, các khoản khấu trừ tài sản chưa bàn giao)**, để **chi trả chính xác cho người lao động nghỉ việc**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Tính tiền phép tồn khi thôi việc):**
    * *Given:* Nhân viên chính thức nghỉ việc còn dư 4.5 ngày phép năm chưa nghỉ hết.
    * *When:* Kế toán kích hoạt tính bảng quyết toán thôi việc.
    * *Then:* Hệ thống tính tiền thanh toán phép tồn = `(Mức lương theo HĐLĐ / Ngày công chuẩn) * 4.5`, cộng vào phiếu quyết toán lương cuối cùng.
  * **Kịch bản 2 (Soft Delete và đóng tài khoản):**
    * *Given:* Nhân viên đã hoàn tất quyết toán và nhận tiền.
    * *When:* HR Local đánh dấu hồ sơ "Đã hoàn tất thủ tục nghỉ việc".
    * *Then:* Trạng thái nhân viên đổi thành `TERMINATED`, `deleted_at = NOW()`, tài khoản đăng nhập bị khóa vĩnh viễn, toàn bộ dữ liệu lịch sử được giữ nguyên trong cơ sở dữ liệu.

---

### 4.5. Phân hệ 5: Quản lý Hiệu suất KPI (Performance & KPI Management)

#### US-KPI-01: Thiết lập Bộ Chỉ số KPI theo Khối Chức năng và Vị trí
* **User Story:** Là **Super Admin (Ban TCCB)**, tôi muốn **xây dựng ngân hàng chỉ số KPI (Thư viện KPI) phân chia theo Khối: Văn phòng (Tài chính, MKT, R&D), Sản xuất (Nhà máy) và Nông nghiệp (Trang trại)**, để **chuẩn hóa phương pháp đo lường hiệu suất**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Khai báo chỉ số KPI đặc thù nhà máy):**
    * *Given:* Ở màn hình Danh mục KPI chuẩn.
    * *When:* Tạo chỉ số "Tỷ lệ hao hụt sữa nguyên liệu trong chế biến UHT", đơn vị đo "%", loại đo "Càng thấp càng tốt", mục tiêu chuẩn `<= 0.8%`.
    * *Then:* Chỉ số được lưu vào danh mục chuẩn để các Nhà máy sử dụng khi giao KPI cho Quản đốc và Kỹ sư vận hành.

#### US-KPI-02: Phân bổ và Giao Chỉ tiêu KPI Kỳ Đánh giá (KPI Assignment)
* **User Story:** Là **Quản lý Trực tiếp (MSS)**, tôi muốn **gán bộ chỉ tiêu KPI kèm trọng số (Tổng trọng số = 100%) cho từng nhân viên dưới quyền vào đầu mỗi chu kỳ đánh giá (tháng hoặc quý)**, để **thiết lập cam kết hiệu suất làm việc rõ ràng**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Gán KPI hợp lệ 100% trọng số):**
    * *Given:* Quản lý thiết lập 4 chỉ tiêu KPI cho nhân viên phụ trách trang trại.
    * *When:* Nhập trọng số: KPI 1 (30%), KPI 2 (30%), KPI 3 (20%), KPI 4 (20%) -> Tổng = 100% và bấm Gửi cam kết.
    * *Then:* Hệ thống chuyển bảng KPI sang trạng thái `COMMITTED`, gửi thông báo đến nhân viên để xác nhận cam kết.
  * **Kịch bản 2 (Chặn khi tổng trọng số khác 100%):**
    * *Given:* Tổng trọng số các KPI người dùng vừa nhập bằng 90%.
    * *When:* Bấm nút Gửi giao KPI.
    * *Then:* Hệ thống báo lỗi đỏ "Tổng trọng số các mục tiêu phải bằng chính xác 100% (Hiện tại: 90%)", không cho phép lưu.

#### US-KPI-03: Nhân viên Tự đánh giá Hiệu suất (Self-Assessment)
* **User Story:** Là **Nhân viên (ESS)**, tôi muốn **nhập kết quả thực hiện thực tế và tự chấm điểm hiệu quả công việc vào cuối chu kỳ đánh giá**, để **chủ động giải trình thành tích trước khi quản lý đánh giá chính thức**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Hoàn thành tự đánh giá):**
    * *Given:* Kỳ đánh giá KPI tháng mở đến hết ngày 28 hàng tháng.
    * *When:* Nhân viên nhập kết quả thực tế, hệ thống tự động tính điểm tự chấm theo công thức định sẵn, nhập ý kiến phản hồi và bấm "Gửi Quản lý".
    * *Then:* Bảng KPI khóa quyền sửa của nhân viên, chuyển trạng thái sang `WAITING_MANAGER_REVIEW`.

#### US-KPI-04: Quản lý Trực tiếp Đánh giá và Xếp loại Hiệu suất
* **User Story:** Là **Quản lý Trực tiếp (MSS)**, tôi muốn **thẩm định kết quả thực tế, chấm điểm chính thức và xếp loại nhân viên (A, B, C, D)**, để **làm căn cứ tính toán hệ số lương hiệu suất P3**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Chấm điểm và phân loại chuẩn):**
    * *Given:* Nhân viên đạt tổng điểm KPI đánh giá là 92/100 điểm.
    * *When:* Quản lý hoàn tất đánh giá và xác nhận.
    * *Then:* Hệ thống tự động xếp loại "Hạng A (Xuất sắc)", tự động gán Hệ số P3 tương ứng = 1.2 (theo quy chế công ty).
  * **Kịch bản 2 (Tuân thủ SoD):**
    * *Given:* Quản lý không thể tự xếp loại đánh giá cho chính bản thân mình.
    * *When:* Quản lý mở bảng KPI của chính mình.
    * *Then:* Màn hình chỉ cho phép Tự đánh giá (Self-score), phần Đánh giá chính thức (Manager Score) chỉ có cấp trên của người quản lý đó mới có quyền truy cập để chấm điểm.

#### US-KPI-05: Chốt Điểm KPI và Tự động Đồng bộ sang Bảng Lương P3
* **User Story:** Là **Cán bộ Nhân sự Cơ sở (HR Local)**, tôi muốn **kiểm tra phân bổ chuông (Bell Curve) và phê duyệt chốt kết quả KPI toàn đơn vị**, để **chuyển điểm và hệ số P3 sang động cơ tính lương tháng**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Khóa điểm KPI kỳ đánh giá):**
    * *Given:* 100% nhân viên Nhà máy Tiên Sơn đã hoàn thành đánh giá KPI tháng 10.
    * *When:* HR Local bấm nút "Chốt kết quả KPI Đơn vị".
    * *Then:* Toàn bộ dữ liệu điểm số bị đóng băng, sinh ra bảng ánh xạ `kpi_results` liên kết sẵn sàng với module `payroll_calculation`.

---

### 4.6. Phân hệ 6: Quản lý Đề xuất Tăng lương & Bổ nhiệm (Salary Proposal & Promotion)

#### US-SAL-01: Lập Tờ trình Đề xuất Điều chỉnh Lương / Bổ nhiệm
* **User Story:** Là **Quản lý Trực tiếp (MSS)**, tôi muốn **lập tờ trình đề xuất tăng lương (P1 hoặc P2) hoặc bổ nhiệm chức danh mới cho nhân viên xuất sắc kèm ngày hiệu lực áp dụng**, để **tạo động lực phát triển nhân tài**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Tạo đề xuất hợp lệ):**
    * *Given:* Công nhân hoàn thành xuất sắc chứng chỉ kỹ thuật bảo trì máy đóng gói Tetra Pak.
    * *When:* Quản đốc lập đề xuất tăng lương P2 từ bậc 2 (2.000.000 VND) lên bậc 3 (3.500.000 VND), chọn `effective_from = 01/11/2026`, nhập lý do và đính kèm chứng chỉ.
    * *Then:* Tờ trình được sinh mã `PROP-2026-xxxx`, trạng thái `PENDING_UNIT_LEADER`.
  * **Kịch bản 2 (Tuân thủ SoD - Cấm tự đề xuất tăng lương cho chính mình):**
    * *Given:* Tổ trưởng sản xuất muốn được tăng lương.
    * *When:* Tìm kiếm tên nhân sự để tạo đề xuất.
    * *Then:* Hệ thống ẩn tên chính người dùng trong danh sách chọn nhân viên được đề xuất; bắt buộc phải do cấp trên trực tiếp của Tổ trưởng đó lập đề xuất.

#### US-SAL-02: Kiểm soát Ngân sách Quỹ Lương khi Thẩm định Đề xuất
* **User Story:** Là **Giám đốc Đơn vị / Vùng**, tôi muốn **hệ thống hiển thị tỷ lệ biến động quỹ lương và ngân sách định biên còn lại của đơn vị khi xem xét phê duyệt đề xuất**, để **tránh vượt hạn mức ngân sách tiền lương được Tập đoàn giao**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Hiển thị tác động tài chính trực quan):**
    * *Given:* Giám đốc Nhà máy xem xét danh sách 10 nhân sự được đề xuất tăng bậc lương.
    * *When:* Mở giao diện phê duyệt.
    * *Then:* Hệ thống hiển thị bảng so sánh: Tổng quỹ lương hiện tại của nhà máy, Số tiền lương tăng thêm hàng tháng (+18.500.000 VND), Tỷ lệ tăng so với ngân sách kế hoạch năm (+0.4%), cảnh báo màu vàng nếu tiệm cận trần ngân sách.

#### US-SAL-03: Luồng Duyệt Đa cấp Đi theo Cây Tổ chức (Multi-level Escalation)
* **User Story:** Là **Super Admin (Ban TCCB)**, tôi muốn **quy trình phê duyệt đề xuất tăng lương/bổ nhiệm tự động leo thang đúng cấp thẩm quyền: Quản đốc -> Giám đốc Đơn vị -> Ban TCCB -> Tổng Giám đốc**, để **bảo đảm tính chuẩn hóa và phân quyền đúng quy chế tài chính**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Luồng duyệt tuần tự tự động):**
    * *Given:* Đề xuất tăng lương vượt trên 20% yêu cầu phê duyệt cấp Tổng Giám đốc.
    * *When:* Giám đốc Đơn vị phê duyệt xong.
    * *Then:* Hệ thống tự động chuyển tiếp đề xuất lên hàng chờ duyệt của Ban TCCB để thẩm tra, sau đó chuyển lên Tổng Giám đốc; nếu bất kỳ cấp nào từ chối (Reject), quy trình dừng lại và gửi thông báo kèm lý do về người khởi tạo.

#### US-SAL-04: Cập nhật Lịch sử Lương Không ghi đè (Effective Dating Activation)
* **User Story:** Là **Super Admin (Ban TCCB)**, tôi muốn **khi Tổng Giám đốc bấm duyệt Quyết định tăng lương, hệ thống tự động cập nhật biểu lương mới theo đúng ngày hiệu lực mà không ghi đè lịch sử cũ**, để **bảo đảm tính chính xác khi tính lương hồi tố (Retroactive Pay)**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Kích hoạt quyết định lương mới):**
    * *Given:* Quyết định tăng lương P1 được duyệt ngày 25/10/2026 với `effective_from = 01/10/2026`.
    * *When:* Hệ thống xử lý kích hoạt quyết định.
    * *Then:* Bản ghi lương cũ đóng `effective_to = 30/09/2026`, bản ghi lương mới được tạo với `effective_from = 01/10/2026`, `is_current = TRUE`, ghi nhận toàn bộ vào `audit_logs` với `action = SALARY_ADJUSTMENT`.

#### US-SAL-05: Tra cứu Lịch sử Quá trình Lương Nhân viên (Salary History Audit)
* **User Story:** Là **Kế toán Lương (Payroll)**, tôi muốn **tra cứu toàn bộ dòng thời gian biến động thu nhập (P1, P2, phụ cấp) của nhân viên qua các năm**, để **phục vụ giải trình thanh tra lao động và bảo hiểm xã hội**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Xem dòng thời gian lương):**
    * *Given:* Nhân viên có 8 năm công tác với 5 lần điều chỉnh lương.
    * *When:* Kế toán tra cứu hồ sơ lương của nhân viên.
    * *Then:* Hệ thống hiển thị dạng Timeline chi tiết: từng giai đoạn hiệu lực, mức lương cũ, mức lương mới, số quyết định, người ký duyệt, lý do điều chỉnh, không thiếu sót bất kỳ mốc thời gian nào.

---

### 4.7. Phân hệ 7: Quản lý Bảng lương 3P & Chế độ Phúc lợi (3P Payroll & Benefits)

#### US-PAY-01: Cấu hình Tham số Pháp lý Động (Dynamic Statutory Configuration)
* **User Story:** Là **Super Admin (Ban TCCB)**, tôi muốn **quản lý các tham số pháp lý: Mức giảm trừ gia cảnh thuế TNCN, Tỷ lệ trích BHXH/BHYT/BHTN, Mức lương tối thiểu vùng theo địa bàn, và Mức trần đóng bảo hiểm có ngày hiệu lực**, để **khi nhà nước thay đổi chính sách, hệ thống tự động áp dụng đúng thời điểm mà không cần sửa code**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Cấu hình thay đổi mức giảm trừ gia cảnh):**
    * *Given:* Quốc hội ban hành biểu giảm trừ gia cảnh mới có hiệu lực từ `01/07/2026`: Bản thân = 15.000.000 VND, Người phụ thuộc = 6.000.000 VND.
    * *When:* Ban TCCB thêm bản ghi mới vào bảng `statutory_tax_reliefs` với `effective_from = 01/07/2026`.
    * *Then:* Khi chạy bảng lương tháng 06/2026 hệ thống vẫn áp dụng mức cũ (11tr/4.4tr); khi chạy bảng lương tháng 07/2026 hệ thống tự động áp dụng mức mới (15tr/6tr).
  * **Kịch bản 2 (Cấu hình lương tối thiểu theo vùng cho các nhà máy):**
    * *Given:* Nhà máy tại Cần Thơ thuộc Vùng II, Nhà máy tại Bình Dương thuộc Vùng I.
    * *When:* Cấu hình mức sàn đóng BHTN theo `statutory_minimum_wages`.
    * *Then:* Hệ thống tự động chặn mức đóng BHTN tối đa bằng 20 lần mức lương tối thiểu vùng tương ứng của từng địa phương nơi đặt nhà máy.

#### US-PAY-02: Động cơ Tính Lương 3P Tự động Toàn Tập đoàn
* **User Story:** Là **Kế toán Lương (Payroll)**, tôi muốn **kích hoạt tính toán tự động bảng lương 3P cho hơn 10.000 CBCNV với thời gian chạy dưới 30 phút**, để **chốt kỳ lương nhanh chóng, chính xác tuyệt đối**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Công thức tính toán 3P chuẩn xác):**
    * *Given:* Dữ liệu chấm công đã chốt, kết quả KPI đã duyệt, thông tin lương P1, P2 đang hiệu lực.
    * *When:* Kế toán nhấn "Chạy tính toán lương tháng 10/2026".
    * *Then:* Thu nhập từng nhân sự được tính:
      $$\text{Lương vị trí thực tế} = \frac{\text{P1}}{\text{Ngày công chuẩn}} \times \text{Ngày công thực tế}$$
      $$\text{Lương năng lực thực tế} = \frac{\text{P2}}{\text{Ngày công chuẩn}} \times \text{Ngày công thực tế}$$
      $$\text{Lương hiệu suất} = \text{P3} \times \text{Hệ số KPI}$$
      $$\text{Tổng thu nhập} = \text{P1 thực tế} + \text{P2 thực tế} + \text{P3 thực tế} + \text{Lương OT} + \text{Phụ cấp ca kíp/độc hại}$$
  * **Kịch bản 2 (Hiệu năng chạy bảng lương lớn):**
    * *Given:* Cơ sở dữ liệu chứa hơn 10.000 hồ sơ nhân viên kèm chi tiết chấm công cả tháng.
    * *When:* Kích hoạt tiến trình Batch Job tính toán lương.
    * *Then:* Toàn bộ quá trình tính toán hoàn tất trong thời gian dưới 30 phút (kỳ vọng thực tế dưới 10 phút trên hạ tầng tiêu chuẩn), sinh đầy đủ chi tiết bảng lương từng nhân sự.

#### US-PAY-03: Tính Khấu trừ Bảo hiểm Bắt buộc và Thuế TNCN Lũy tiến
* **User Story:** Là **Kế toán Lương (Payroll)**, tôi muốn **hệ thống tự động tính toán các khoản trích nộp BHXH (8%), BHYT (1.5%), BHTN (1%) và khấu trừ Thuế thu nhập cá nhân theo biểu thuế lũy tiến từng phần 7 bậc**, để **tuân thủ pháp luật thuế và bảo hiểm Việt Nam**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Áp dụng trần đóng BHXH/BHYT):**
    * *Given:* Nhân viên quản lý cấp cao có mức lương đóng bảo hiểm là 50.000.000 VND (vượt trần 20 lần mức lương cơ sở = 46.800.000 VND).
    * *When:* Tính toán trích nộp BHXH.
    * *Then:* Mức tính BHXH bị chặn ở mức trần 46.800.000 VND × 8% = 3.744.000 VND, không trích trên mức 50.000.000 VND.
  * **Kịch bản 2 (Tính thuế TNCN lũy tiến từng phần):**
    * *Given:* Thu nhập tính thuế sau giảm trừ của nhân viên là 18.000.000 VND.
    * *When:* Động cơ tính thuế thực thi.
    * *Then:* Thuế TNCN được tính chính xác theo từng bậc: Bậc 1 (5tr × 5%), Bậc 2 (5tr × 10%), Bậc 3 (8tr × 15%) = 250.000 + 500.000 + 1.200.000 = 1.950.000 VND.

#### US-PAY-04: Khóa Bảng Lương và Xuất File Chi trả Ngân hàng
* **User Story:** Là **Kế toán Lương (Payroll)**, tôi muốn **chốt khóa bảng lương chính thức và kết xuất file dữ liệu chuyển khoản tương thích định dạng các ngân hàng thương mại (BIDV, Vietcombank, Vietinbank)**, để **thực hiện chi lương đúng ngày hẹn cho người lao động**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Xuất file chuyển khoản ngân hàng):**
    * *Given:* Bảng lương đã được Giám đốc Tài chính và Tổng Giám đốc duyệt chốt.
    * *When:* Kế toán chọn định dạng "BIDV Payroll Text/Excel Format" và bấm "Kết xuất".
    * *Then:* Hệ thống sinh file chính xác cấu trúc: Số tài khoản, Tên chủ tài khoản, Số tiền VND (kiểu số nguyên không dấu), Nội dung chuyển khoản; đồng thời khóa cứng bảng lương tháng đó, không ai có thể can thiệp sửa đổi.

#### US-PAY-05: Phiếu Lương Điện tử Bảo mật (E-Payslip Masking)
* **User Story:** Là **Nhân viên (ESS)**, tôi muốn **nhận và tra cứu Phiếu lương điện tử chi tiết hàng tháng trên cổng portal hoặc ứng dụng di động có xác thực mật khẩu cấp 2**, để **nắm rõ các cấu phần thu nhập và khoản giảm trừ của bản thân một cách riêng tư, minh bạch**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Tra cứu phiếu lương bảo mật):**
    * *Given:* Bảng lương tháng đã được công bố chính thức.
    * *When:* Nhân viên bấm vào mục "Phiếu lương", nhập mã PIN bảo mật cá nhân.
    * *Then:* Hệ thống hiển thị phiếu lương chi tiết từng mục: P1, P2, P3, Giờ làm việc, Giờ OT, Phụ cấp, Tiền BHXH đã trừ, Thuế TNCN đã khấu trừ, Thực lĩnh VND; tuyệt đối không thể tra cứu phiếu lương của người khác.

#### US-PAY-06: Phân bổ Chi phí Lương theo Cost Center Đối soát ERP SAP
* **User Story:** Là **Kế toán Lương (Payroll)**, tôi muốn **kết xuất bảng phân bổ chi phí lương tổng hợp phân loại theo Mã đơn vị / Mã xưởng sản xuất (Cost Center / Profit Center)**, để **đối soát và import bút toán chi phí nhân công vào hệ thống ERP SAP FI/CO**.
* **Tiêu chí nghiệm thu (Acceptance Criteria):**
  * **Kịch bản 1 (Kết xuất bảng phân bổ Cost Center):**
    * *Given:* Bảng lương toàn tập đoàn đã hoàn tất tính toán.
    * *When:* Kế toán nhấn "Kết xuất bút toán phân bổ chi phí lương SAP".
    * *Then:* Hệ thống tổng hợp toàn bộ chi phí (Lương trực tiếp sản xuất, Lương quản lý, BHXH phần doanh nghiệp gánh chịu 17.5%, Kinh phí công đoàn 2%) theo đúng các mã Cost Center tương ứng của 13 nhà máy và 14 trang trại.

---

## 5. YÊU CẦU PHI CHỨC NĂNG (NON-FUNCTIONAL REQUIREMENTS - NFR)

### 5.1. Hiệu năng & Xử lý Đồng thời (Performance & Scalability)

| Hạng mục NFR | Chỉ số Cam kết (SLA / Target) | Giải pháp Kiến trúc Thực thi |
|:---|:---|:---|
| **Thời gian tính toán lương** | **< 30 phút cho 10.000+ CBCNV** | Áp dụng xử lý lô song song (*Parallel Batch Processing / Partitioning* theo từng Đơn vị/Nhà máy). Tách luồng tính toán vào Background Worker Service, tối ưu hóa câu lệnh tính toán theo tập hợp (*Set-based SQL Operations*), không chạy vòng lặp xử lý từng dòng dữ liệu (*Row-by-row / Cursor*). |
| **Thời gian phản hồi giao diện** | $\le 500$ ms cho 95% truy vấn tác nghiệp thông thường | Phân trang ở tầng CSDL (Limit/Offset hoặc Keyset Pagination). Đánh chỉ mục (*Index*) chuyên sâu cho `employee_id`, `org_unit_id`, `node_path`, `effective_from`, `effective_to`. Caching danh mục từ điển bằng Redis / In-memory cache. |
| **Báo cáo tổng hợp phức tạp** | $\le 2$ giây cho báo cáo toàn tập đoàn | Sử dụng Read-Replica CSDL hoặc bảng tổng hợp định kỳ (*Materialized Views / Aggregation Tables*) phục vụ riêng cho các Dashboard và báo cáo thống kê nặng. |
| **Xử lý đồng thời (Concurrency)** | 2.500 người dùng truy cập đồng thời vào giờ cao điểm | Kết nối CSDL thông qua Cơ chế Connection Pooling tối ưu; hỗ trợ mở rộng ngang (*Horizontal Scaling*) các máy chủ ứng dụng thông qua Load Balancer (Nginx / HAProxy / Azure Application Gateway). |

### 5.2. Bảo mật & Kiểm soát Tuân thủ (Security & Privacy Compliance)

```
+-----------------------------------------------------------------------------------------+
|                                    AN NINH ĐA LỚP                                       |
|  +-----------------------------------------------------------------------------------+  |
|  | Lớp 1: Mã hóa Truyền dẫn & Lưu trữ (TLS 1.3 in-transit, AES-256 at-rest)          |  |
|  +-----------------------------------------------------------------------------------+  |
|  | Lớp 2: Kiểm soát Phân quyền (RBAC kết hợp Data Scope theo nút cây Org Unit)       |  |
|  +-----------------------------------------------------------------------------------+  |
|  | Lớp 3: Bảo mật Trường dữ liệu (Field-Level Security & Masking: IT Admin cấm xem)  |  |
|  +-----------------------------------------------------------------------------------+  |
|  | Lớp 4: Kiểm soát Rủi ro & Audit Trail (Immutable Audit Logs ghi nhận 100% thay đổi)|  |
|  +-----------------------------------------------------------------------------------+  |
+-----------------------------------------------------------------------------------------+
```

1. **Phân tách Quyền lực Tuyệt đối (Field-Level Hard Separation):**
   * Tài khoản Quản trị Kỹ thuật (`TECH_ADMIN`) bị tước quyền đọc trên các cột nhạy cảm (`basic_salary`, `p1_amount`, `p2_amount`, `p3_amount`, `net_salary`, `bank_account_number`) ở cả tầng API và Data Access. Dữ liệu này được che mờ (*Masking: `*****`*) khi xuất hiện trong log hoặc giao diện kỹ thuật.
2. **Mã hóa Dữ liệu Nhạy cảm (Encryption Standard):**
   * Dữ liệu định danh cá nhân (Số CCCD, Mã số thuế, Số tài khoản ngân hàng) và mức lương được mã hóa trong cơ sở dữ liệu bằng thuật toán **AES-256**.
   * Toàn bộ kênh truyền thông mạng bắt buộc sử dụng **HTTPS / TLS 1.3**; cấm mọi giao thức truyền tải không mã hóa.
3. **Tuân thủ Nghị định 13/2023/NĐ-CP về Bảo vệ Dữ liệu Cá nhân:**
   * Hệ thống ghi nhận sự đồng thuận (*Consent Management*) của người lao động khi thu thập dữ liệu sinh trắc học (vân tay, khuôn mặt chấm công).
   * Dữ liệu nhân viên nghỉ việc được áp dụng cơ chế Soft Delete và lưu trữ ẩn (*Archiving*), bảo đảm quyền yêu cầu kiểm tra dữ liệu của chủ thể theo pháp luật.
4. **Phòng chống Tấn công An ninh mạng (OWASP Top 10):**
   * Chống SQL Injection bằng 100% tham số hóa truy vấn (Parameterized Queries / ORM chuẩn).
   * Chống Cross-Site Scripting (XSS), CSRF Token bảo vệ mọi giao dịch gửi biểu mẫu.
   * Cơ chế phòng chống Brute-Force: Tự động khóa tài khoản sau 5 lần đăng nhập thất bại liên tiếp trong 10 phút; bắt buộc xác thực 2 yếu tố (2FA / OTP) đối với tài khoản `SUPER_ADMIN`, `PAYROLL_ACC`, và `BOD_EXEC`.

### 5.3. Sao lưu & Phục hồi Thảm họa (Backup & Disaster Recovery - DR)

| Chỉ số / Hoạt động | Quy định Chi tiết | Phương án Triển khai |
|:---|:---|:---|
| **RPO (Recovery Point Objective)** | **$\le 15$ phút** | Mất mát dữ liệu tối đa không quá 15 phút giao dịch khi xảy ra thảm họa toàn diện. |
| **RTO (Recovery Time Objective)** | **$\le 2$ giờ** | Hệ thống phải được phục hồi hoàn toàn và đưa vào vận hành bình thường trong vòng 2 giờ. |
| **Chiến lược Sao lưu Dữ liệu** | * **Full Backup:** Thực hiện hàng ngày vào lúc 01:00 sáng, lưu giữ tối thiểu 30 ngày gần nhất.<br>* **Differential Backup:** Thực hiện 6 tiếng một lần.<br>* **Transaction Log / Binlog Backup:** Thực hiện liên tục mỗi 15 phút. | Các tệp sao lưu được mã hóa tự động và đồng bộ sang cụm lưu trữ Off-site (vùng địa lý độc lập, ví dụ: cụm Datacenter thứ cấp tại Bình Dương hoặc Cloud Storage S3 tương thích). |
| **Kịch bản Diễn tập DR** | Định kỳ 6 tháng / lần | Giả lập sự cố sập máy chủ CSDL chính, thực hiện quy trình kích hoạt chuyển đổi dự phòng (*Failover*) sang máy chủ Standby và đo lường thời gian phục hồi thực tế. |

### 5.4. Khả năng Mở rộng & Sẵn sàng Cao (Scalability & High Availability)

* **Chỉ số Sẵn sàng (High Availability - HA):** Đạt tối thiểu **99.9% Uptime** (thời gian ngừng hoạt động ngoài kế hoạch không quá 8.76 giờ/năm). Các hoạt động bảo trì, nâng cấp phiên bản định kỳ được thực hiện ngoài giờ hành chính (22:00 - 04:00 sáng cuối tuần) với cơ chế Blue-Green Deployment hoặc Rolling Update không gián đoạn dịch vụ.
* **Khả năng Mở rộng (Scalability):** Thiết kế kiến trúc dạng phi trạng thái (*Stateless Services*) ở tầng Web/API, cho phép nâng quy mô phục vụ lên **30.000+ nhân sự** khi Vinamilk mở rộng quy mô thông qua hoạt động mua bán sáp nhập (M&A) các công ty sữa trong nước và quốc tế (như Driftwood, Angkor Dairy) mà không cần viết lại kiến trúc lõi.
