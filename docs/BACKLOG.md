# TÀI LIỆU KỸ THUẬT BACKLOG & THIẾT KẾ KIẾN TRÚC TƯƠNG LAI (PHASE 2 & PHASE 3)

**Hệ thống**: Vinamilk Enterprise HRM (~10.000 CBNV)  
**Vai trò**: Solution Architect  
**Cập nhật**: Phiên 11 (2026-10-13)

---

## MỤC LỤC
1. [Khung Triển Khai Giai Đoạn 2 (P2 Skeleton & TODOs)](#1-khung-triển-khai-giai-đoạn-2-p2-skeleton--todos)
   - 1.1. ATS (Applicant Tracking System)
   - 1.2. Đào Tạo & Chứng Chỉ Có Hạn (Training & Certifications)
   - 1.3. Năng Lực / 360° / Ma Trận 9 Ô (Talent Management & 9-Box Grid)
   - 1.4. An Toàn Vệ Sinh Lao Động (Occupational Health & Safety - OHS)
   - 1.5. Đa Pháp Nhân (Multi-Legal Entity)
   - 1.6. Trình Tạo Báo Cáo Cơ Quan Nhà Nước (BM01 – BM04)
2. [Thiết Kế Kiến Trúc Giai Đoạn 3 (P3 Architectural Designs)](#2-thiết-kế-kiến-trúc-giai-đoạn-3-p3-architectural-designs)
   - 2.1. AI Copilot Trợ Lý Nhân Sự Nội Bộ (Private LLM & RAG)
   - 2.2. Đồng Bộ Đa Đám Mây & Sao Lưu Ngoại Biên (Hybrid Cloud & Geo-Replication)
   - 2.3. Tích Hợp Chấm Công Sinh Trắc Học IoT Biên (Edge Biometric Devices & MQTT)
   - 2.4. Phân Rã Kiến Trúc Dịch Vụ Microservices (Scalability to 50,000 CBNV)
   - 2.5. Tự Động Hóa Quyết Toán Thuế TNCN & Cổng eTax Tổng Cục Thuế

---

## 1. KHUNG TRIỂN KHAI GIAI ĐOẠN 2 (P2 SKELETON & TODOS)

Các thực thể (Entity) cốt lõi và endpoint Web API cơ bản đã được dựng sẵn tại VinamilkHRM.Domain.Entities.P2 và VinamilkHRM.WebApi.Controllers.P2Controller. Dưới đây là danh sách chi tiết các hạng mục TODO cần hoàn thiện trong các chu kỳ sprint tiếp theo:

### 1.1. ATS (Applicant Tracking System)
- **Hiện trạng đã dựng**:
  - Thực thể JobRequisition (Mã đề xuất, Tiêu đề, Đơn vị, Định biên tuyển dụng, Ngân sách lương, Hạn tuyển dụng).
  - API cơ bản: GET /api/v1/p2/ats/requisitions, POST /api/v1/ats/requisitions.
- **Danh sách TODO kỹ thuật**:
  - [ ] **TODO-ATS-01**: Tích hợp luồng phê duyệt đề xuất tuyển dụng JobRequisition qua WorkflowEngine (DirectManager -> UnitLeader -> HRManager -> CEO nếu vượt ngân sách).
  - [ ] **TODO-ATS-02**: Xây dựng bảng Kanban theo dõi ứng viên 5 giai đoạn: *Ứng tuyển -> Sơ loại CV -> Phỏng vấn vòng 1 -> Đánh giá chuyên môn -> Đề nghị nhận việc (Offer)*.
  - [ ] **TODO-ATS-03**: Tự động đồng bộ ứng viên nhận việc (Hired) sang quy trình Onboarding tự động của P1-4.

### 1.2. Đào Tạo & Chứng Chỉ Có Hạn (Training & Certifications)
- **Hiện trạng đã dựng**:
  - Thực thể TrainingCourse (Mã khóa học, Tên, Đơn vị đào tạo, Thời lượng, Chi phí/người, Tính bắt buộc).
  - Thực thể EmployeeCertification (Nhân viên, Tên chứng chỉ, Tổ chức cấp, Ngày cấp, Ngày hết hạn, Trạng thái).
  - API cơ bản: GET /api/v1/p2/training/courses, GET /api/v1/p2/training/certifications/expiring?withinDays=30.
- **Danh sách TODO kỹ thuật**:
  - [ ] **TODO-TRN-01**: Xây dựng Hangfire recurring job chạy hàng tuần quét các chứng chỉ sắp hết hạn trong vòng 60/30/15 ngày để phát thông báo cảnh báo qua INotificationService.
  - [ ] **TODO-TRN-02**: Ràng buộc bắt buộc chứng chỉ An toàn thực phẩm / Vận hành nồi hơi trước khi xếp ca công nhân nhà máy sữa.

### 1.3. Năng Lực / 360° / Ma Trận 9 Ô (Talent & 9-Box Grid)
- **Hiện trạng đã dựng**:
  - Thực thể CompetencyFramework (Từ điển năng lực theo chuẩn ASK: Attitude - Skill - Knowledge).
  - Thực thể Evaluation360 (Tự đánh giá, Quản lý, Đồng cấp, Cấp dưới).
  - Thực thể NineBoxPlacement (Trục Hiệu suất 1-3 x Trục Tiềm năng 1-3).
  - API cơ bản: GET /api/v1/p2/talent/nine-box?period=2026.
- **Danh sách TODO kỹ thuật**:
  - [ ] **TODO-TLN-01**: Tự động tính toán điểm trục Hiệu suất (Performance) từ điểm khóa KPI cuối năm của Phiên 5 (KpiResult.FinalScore).
  - [ ] **TODO-TLN-02**: Giao diện ma trận kéo-thả 9 ô dành cho Hội đồng nhân tài (Talent Review Board) điều chỉnh quy hoạch cán bộ nguồn.

### 1.4. An Toàn Vệ Sinh Lao Động (OHS)
- **Hiện trạng đã dựng**:
  - Thực thể SafetyIncident (Mã sự cố, Ngày xảy ra, Đơn vị, Mức độ nghiêm trọng: Minor/Moderate/Severe/Fatal, Nguyên nhân gốc rễ, Biện pháp khắc phục).
  - API cơ bản: GET /api/v1/p2/safety/incidents.
- **Danh sách TODO kỹ thuật**:
  - [ ] **TODO-OHS-01**: Tích hợp quy trình điều tra tai nạn lao động với thời hạn báo cáo cơ quan chức năng trong vòng 24 giờ.
  - [ ] **TODO-OHS-02**: Quản lý lịch khám sức khỏe định kỳ và phân loại sức khỏe 5 nhóm (Loại I đến Loại V theo quy định Bộ Y tế).

### 1.5. Đa Pháp Nhân (Multi-Legal Entity)
- **Hiện trạng đã dựng**:
  - Thực thể LegalEntity (Mã số thuế, Tên pháp nhân, Tên giao dịch, Địa chỉ trụ sở, Đại diện pháp luật).
  - API cơ bản: GET /api/v1/p2/legal-entities.
- **Danh sách TODO kỹ thuật**:
  - [ ] **TODO-MLE-01**: Bổ sung khóa ngoại LegalEntityId vào OrganizationalUnits và PayrollRuns để phân lập bảng lương độc lập giữa Công ty mẹ Vinamilk và các Công ty con (CTCP Nước giải khát Sài Gòn, CTCP Bò sữa Việt Nam).
  - [ ] **TODO-MLE-02**: Cho phép một nhân viên có hợp đồng kiêm nhiệm tại pháp nhân thành viên với biểu thuế TNCN lũy tiến từng phần thống nhất.

### 1.6. Trình Tạo Báo Cáo Nhà Nước (BM01 – BM04)
- **Hiện trạng đã dựng**:
  - Thực thể ReportTemplateDefinition và endpoint xuất báo cáo: GET /api/v1/p2/reports/government/{code}.
- **Danh sách TODO kỹ thuật**:
  - [ ] **TODO-RPT-01**: Hoàn thiện bộ xuất tệp Excel OpenXML cho **BM01** (Mẫu số 01/PLI Nghị định 145/2020/NĐ-CP - Báo cáo khai trình tình hình sử dụng lao động 6 tháng và hàng năm).
  - [ ] **TODO-RPT-02**: Hoàn thiện bộ xuất **BM02** (Phụ lục II Nghị định 39/2016/NĐ-CP - Báo cáo tổng hợp tai nạn lao động).
  - [ ] **TODO-RPT-03**: Hoàn thiện bộ xuất **BM03** (Thông tư 07/2016/TT-BLĐTBXH - Báo cáo định kỳ công tác an toàn, vệ sinh lao động).
  - [ ] **TODO-RPT-04**: Hoàn thiện bộ xuất **BM04** (Báo cáo tình hình quản lý, sử dụng người lao động nước ngoài tại Việt Nam).

---

## 2. THIẾT KẾ KIẾN TRÚC GIAI ĐOẠN 3 (P3 ARCHITECTURAL DESIGNS)

### 2.1. AI Copilot Trợ Lý Nhân Sự Nội Bộ (Private LLM & RAG)
`mermaid
graph LR
    User["CBNV Vinamilk"] --> WebPortal["Web Portal / Mobile PWA"]
    WebPortal --> CopilotAPI["AI Copilot Gateway API"]
    CopilotAPI --> VectorDB[("Milvus / pgvector<br/>Chính sách, Thỏa ước lao động, Quy chế Vinamilk")]
    CopilotAPI --> PrivateLLM["Local Private LLM (Llama-3-70B-Instruct)<br/>Bảo mật dữ liệu tại On-Premise GPU Server"]
    CopilotAPI --> HRMAPI["HRM Internal API (Bảng lương, Phép, KPI)"]
    CopilotAPI --> AuditLog["AI Audit & Data Privacy Guard (Nghị định 13)"]
`
- **Kiến trúc bảo mật**: Mô hình Private LLM đặt tại cụm máy chủ On-Premise trung tâm dữ liệu Vinamilk, không gửi dữ liệu ra OpenAI/Cloud công cộng.
- **Tính năng trọng yếu**:
  - Giải đáp tức thì: Quy chế nghỉ phép, chế độ thai sản, công thức tính thưởng KPI, chính sách bảo hiểm sức khỏe Vinamilk Care.
  - Tự động tóm tắt nhận xét KPI 360° và đề xuất mục tiêu cá nhân cho kỳ tiếp theo.
  - Cảnh báo dự báo xu hướng nghỉ việc bằng mô hình học máy Attrition Predictive Modeling.

### 2.2. Đồng Bộ Đa Đám Mây & Sao Lưu Ngoại Biên (Hybrid Cloud & Geo-Replication)
- **Mô hình triển khai**:
  - Trung tâm dữ liệu chính (Primary Data Center): Cụm SQL Server 2022 Always On Availability Groups tại Trụ sở Quận 7.
  - Trung tâm dự phòng thảm họa (Disaster Recovery Center): Azure Southeast Asia (Singapore) với chế độ Asynchronous Geo-Replication, RPO $\le 5$ phút, RTO $\le 15$ phút.
- **Cơ chế mã hóa**: TDE (Transparent Data Encryption) với HSM phần cứng và Always Encrypted với Enclaves cho CCCD và tài khoản ngân hàng.

### 2.3. Tích Hợp Chấm Công Sinh Trắc Học IoT Biên (Edge Biometric Devices & MQTT)
- **Thiết bị biên (Edge Hardware)**: Máy chấm công nhận diện khuôn mặt Hikvision / ZKTeco đặt tại 4 Nhà máy Sữa và 2 Trang trại Green Farm.
- **Giao thức truyền thông**:
  - Giao tiếp qua MQTT Broker (Mosquitto/EMQX) qua kênh an toàn TLS 1.3.
  - Bộ đệm cục bộ (Offline Buffer Storage): Khi đứt cáp quang Internet, máy chấm công lưu trữ tối đa 100.000 lượt chấm công ngoại tuyến tại bộ nhớ flash và tự động đẩy bù khi kết nối được phục hồi.
  - Chống gian lận (Anti-spoofing): Thuật toán 3D Liveness Detection chống chụp ảnh qua màn hình điện thoại.

### 2.4. Phân Rã Kiến Trúc Dịch Vụ Microservices (Scalability to 50,000 CBNV)
- Khi tập đoàn mở rộng quy mô lên 50.000 CBNV tại Đông Nam Á (Việt Nam, Campuchia, Ba Lan, Mỹ), tiến hành tách 3 phân hệ độc lập:
  1. VinamilkHRM.CoreService: Quản lý nhân sự, hồ sơ, cơ cấu tổ chức, hợp đồng, phân quyền.
  2. VinamilkHRM.TimeAttendanceService: Máy chấm công, ca kíp, phân lịch roster, phép, tăng ca.
  3. VinamilkHRM.PayrollEngineService: Động cơ tính lương 3P, thuế, bảo hiểm, quyết toán.
- **Message Bus**: Apache Kafka phục vụ truyền tải sự kiện Event-Driven (ví dụ: AttendanceLockedEvent kích hoạt PayrollCalculationJob).
- **Distributed Cache**: Redis Cluster đa nút với dung lượng lưu trữ session và catalog dữ liệu.

### 2.5. Tự Động Hóa Quyết Toán Thuế TNCN & Cổng eTax Tổng Cục Thuế
- **Mục tiêu**: Xóa bỏ hoàn toàn thao tác trích xuất Excel thủ công khi quyết toán thuế TNCN cuối năm.
- **Quy trình tự động hóa**:
  - Tổng hợp dữ liệu thu nhập chịu thuế, các khoản giảm trừ gia cảnh người phụ thuộc, các khoản đóng bảo hiểm bắt buộc trong năm dương lịch.
  - Tự động sinh tệp tờ khai quyết toán thuế TNCN theo định dạng XML chuẩn của Tổng cục Thuế (Mẫu 05/QTT-TNCN kèm Phụ lục 05-1/BK-TNCN, 05-2/BK-TNCN).
  - Ký số tập trung bằng USB Token / HSM qua IESignProvider và gửi trực tiếp vào Cổng thông tin điện tử Tổng cục Thuế (eTax).

---
*Tài liệu được phê duyệt bởi Hội đồng Kiến trúc Kỹ thuật Vinamilk (Enterprise Architecture Board).*