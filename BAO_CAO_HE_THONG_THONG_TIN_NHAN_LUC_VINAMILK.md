# PHÂN TÍCH VÀ THIẾT KẾ HỆ THỐNG THÔNG TIN NHÂN LỰC (HRIS)
## Báo Cáo Phân Tích & Đặc Tả Yêu Cầu Phần Mềm Quản Lý Nhân Lực
**Mô hình áp dụng: Công ty Cổ phần Sữa Việt Nam (Vinamilk)**

> **Dự án**: Nghiên cứu, Thiết kế và Hiện thực hóa Phần mềm Quản trị Nguồn nhân lực Doanh nghiệp (HRIS System)  
> **Kiến trúc Kỹ thuật**: HTML, JS (Frontend Tác nghiệp) & PHP 8.x MVC, MySQL (Backend Server)  

---

##  MỤC LỤC
1. [Giới thiệu Cơ quan Tổ chức và Nhu cầu Hệ thống](#1-giới-thiệu-cơ-quan-tổ-chức-và-nhu-cầu-hệ-thống)
2. [Cơ cấu Tổ chức và Chức năng](#2-cơ-cấu-tổ-chức-và-chức-năng)
3. [Phân tích Quy trình Nghiệp vụ (Tuyển dụng & Quản lý Hồ sơ)](#3-phân-tích-quy-trình-nghiệp-vụ)
4. [Phân tích Thiết kế và Xây dựng Phần mềm (HUHA HRM Model)](#4-phân-tích-thiết-kế-và-xây-dựng-phần-mềm)
5. [Các Phân hệ Chức năng Cốt lõi](#5-các-phân-hệ-chức-năng-cốt-lõi)
6. [Tiến trình Phát triển Phần mềm (Từ Tác nghiệp đến Trí tuệ Nhân tạo AI)](#6-tiến-trình-phát-triển-phần-mềm)
7. [Kết luận](#7-kết-luận)

---

## 1. Giới thiệu Cơ quan Tổ chức và Nhu cầu Hệ thống

### 1.1. Tổng quan vị thế và tầm nhìn chiến lược
Công ty Cổ phần Sữa Việt Nam (Vinamilk) được thành lập từ năm 1976. Trải qua gần 50 năm phát triển, Vinamilk là doanh nghiệp trụ cột trong ngành công nghiệp thực phẩm và đồ uống (F&B) tại Việt Nam. Không chỉ chiếm lĩnh thị phần nội địa, Vinamilk còn nằm trong Top 40 công ty sữa lớn nhất toàn cầu và xuất khẩu sản phẩm tới hơn 50 quốc gia.
*   **Tổng quy mô nhân sự**: Hơn 10.156 Cán bộ công nhân viên (CBNV).
*   **Mạng lưới hoạt động**: Bao gồm Trụ sở chính (TP.HCM), 8 Nhà máy sản xuất, 6 Trang trại bò sữa công nghệ cao và 3 Chi nhánh kinh doanh trải dài cả nước.

### 1.2. Nhu cầu Chuyển đổi Số & Xây dựng Phần mềm
Với mạng lưới hoạt động rộng lớn, Vinamilk (và các cơ quan, đơn vị tương tự) đối mặt với bài toán **Quản trị Thông tin Nhân lực (HRIS)** khổng lồ. 
Hệ thống đòi hỏi phải chuẩn hóa toàn bộ hồ sơ theo chuẩn quy trình hành chính (lấy cảm hứng từ chuẩn **HUHA HRM** - chuẩn quản trị nhân sự cho trường Đại học/Cơ quan Nhà nước, nhưng tùy biến cho Doanh nghiệp). Phần mềm phải giải quyết từ các thao tác **Tác nghiệp cơ bản** (nhập hồ sơ, lưu trữ), tiến đến **Tính công, tính lương** và cuối cùng là **Tích hợp Hệ chuyên gia AI** để phân tích chiến lược.

---

## 2. Cơ cấu tổ chức và Chức năng (Đầu vào để phân quyền Hệ thống)

Để phần mềm có thể vận hành chính xác các quy trình phê duyệt (Ví dụ: Công nhân xin nghỉ phép -> Tổ trưởng duyệt -> Giám đốc xưởng duyệt -> Nhân sự cập nhật), hệ thống HRIS phải được thiết kế một cơ sở dữ liệu sơ đồ tổ chức (Tree-view) đa phân tầng cực kỳ chặt chẽ như sau:

### 2.1. Cấu trúc Thượng tầng (Cấp Trụ sở chính / Tập đoàn)
Đây là đầu não, nơi hoạch định chính sách, phân bổ ngân sách nhân sự và quản trị toàn hệ thống.
1.  **Ban Lãnh đạo tối cao (Hội đồng Quản trị & Ban Tổng Giám đốc)**: 
    *   *Chức năng*: Phê duyệt quỹ lương toàn tập đoàn, duyệt định biên nhân sự hàng năm, ra quyết định bổ nhiệm các chức danh quản lý cấp cao.
    *   *Vai trò trên HRIS*: Xem Dashboard báo cáo tổng quan (Dữ liệu BI), duyệt ngân sách.
2.  **Ban Tổ chức Cán bộ (Phòng Nhân sự)**: Chịu trách nhiệm trực tiếp tham mưu, xử lý hồ sơ, tuyển dụng và xếp lương.
3.  **Các Khối/Phòng Ban (Bộ phận bên dưới)**:
    *   *Khối Văn phòng*: R&D, Kế toán, Hành chính, CNTT, Marketing.
    *   *Khối Nhà máy*: Nhà máy Tiên Sơn, Nhà máy Bình Dương...
    *   *Khối Trang trại*: Trang trại Green Farm, Trang trại Tuyên Quang.

### 2.2. Ý nghĩa của Cơ cấu này đối với Thiết kế Phần mềm HRIS
Khi xây dựng hệ thống phần mềm quản lý, cấu trúc trên sẽ quyết định các yếu tố cốt lõi:
*   **Phân quyền dữ liệu (Data Privacy)**: Một chuyên viên HR (HR Local) tại Nhà máy Tiên Sơn chỉ được xem hồ sơ và bảng lương của công nhân Tiên Sơn. Chỉ có Ban Tổ chức Cán bộ Tập đoàn mới xem được toàn bộ.
*   **Định tuyến quy trình (Workflow Routing)**: Hệ thống phải hiểu cấu trúc hình cây để tự động gửi thông báo xin duyệt nghỉ phép lên đúng cấp trên quản lý trực tiếp.
*   **Khai báo linh hoạt**: Cho phép phần mềm không chỉ phục vụ một công ty, mà có thể nhân bản cho nhiều cơ quan, tổ chức khác nhau sử dụng. Mỗi phòng ban có thông tin: `Mã phòng ban`, `Tên phòng ban`, `Số lượng nhân sự hiện tại`, và `Phòng ban cấp cha`.

---

## 3. Phân tích Quy trình Nghiệp vụ

### 3.1. Quy trình Tuyển dụng Chi tiết
Quy trình tuyển dụng diễn ra qua các bước cực kỳ chặt chẽ, được luân chuyển chứng từ trên phần mềm:
1.  **Đề xuất nhu cầu**: Các phòng ban bên dưới cơ sở (Ví dụ: Nhà máy thiếu Kỹ sư) lập *Phiếu đề xuất vị trí việc làm* gửi lên Ban Tổ chức Cán bộ (Phòng Nhân sự).
2.  **Phê duyệt**: Ban Tổ chức Cán bộ thẩm định ngân sách nhân sự, sau đó làm tờ trình đề xuất lên Ban Giám đốc (CEO). Ban Giám đốc xem xét và **Đồng ý**.
3.  **Tổ chức Tuyển dụng**: Bắt đầu đăng tuyển bằng nhiều hình thức đa dạng như: đăng đài, đăng báo, hoặc thu nhận hồ sơ Online/Offline qua Cổng thông tin Tuyển dụng.
4.  **Phỏng vấn & Đánh giá**: Tổ chức các vòng phỏng vấn chuyên môn và phỏng vấn nhân sự.
5.  **Xếp lương & Tiếp nhận**: Nếu phỏng vấn OK, Ban Tổ chức Cán bộ sẽ gửi Thông báo trúng tuyển (Offer Letter) và thỏa thuận để **Xếp lương**.

### 3.2. Quy trình Quản lý Hồ sơ & Vòng đời Nhân sự (Lifecycle)
Từ lúc ứng viên được nhận vào làm cho đến khi xin nghỉ, quy trình diễn ra như sau:
1.  **Thu nhận hồ sơ ban đầu**: Nhân viên mới mang hồ sơ giấy (Sơ yếu lý lịch, bằng cấp, CCCD...) đến Ban Tổ chức Cán bộ.
2.  **Số hóa & Lưu trữ**: Cán bộ nhân sự sẽ **scan hồ sơ** và tải (upload) lên phần mềm. Phần mềm quản lý song song hai dạng:
    *   **Hồ sơ điện tử (E-Profile)**: Quản lý đầy đủ Họ tên, ngày tháng năm sinh, quê quán, bằng cấp, văn bằng chứng chỉ.
    *   **Hồ sơ bản cứng**: Cất hồ sơ vào tủ lưu trữ vật lý (Có trường dữ liệu `fileLocation` trong phần mềm để dễ dàng tìm kiếm khi cần).
3.  **Thử việc & Đào tạo hội nhập**: Nhân viên trải qua thời gian thử việc. Pháp lý: Phần mềm tự động trích xuất dữ liệu, sinh ra văn bản hợp đồng thử việc.
4.  **Xếp lương chính thức**: Sau khi thử việc thành công, bộ phận HR chốt hợp đồng lao động và Xếp bậc lương chính thức.
5.  **Tăng lương & Đánh giá**: Dựa trên hiệu suất, nhân viên sẽ được đánh giá Tăng lương.
6.  **Đánh giá Tái ký và Gia hạn Hợp đồng (Contract Renewal)**: 
    *   Theo Luật Lao động, việc theo dõi thời hạn hợp đồng thủ công cho hàng ngàn nhân sự là một rủi ro pháp lý khổng lồ. 
    *   Phân hệ HRIS giải quyết bài toán này bằng cơ chế tự động hóa: **Cảnh báo tự động (Auto-Alert)** căn cứ vào trường dữ liệu Ngày hết hạn, gửi thông báo trước 30-60 ngày cho HR.
7.  **Chấm công - Chấm lương**: Hệ thống ghi nhận chấm công hằng ngày để chạy bảng tính lương cuối tháng.

---

## 4. Phân tích Thiết kế và Xây dựng Phần mềm 

Phần mềm được thiết kế theo đúng quy chuẩn báo cáo (Tham khảo mẫu **HUHA HRM**) với các thao tác chuyên sâu để quản trị dữ liệu nhân viên.

*   **Tính năng Khai báo**: 
    *   Cho phép khai báo Cơ quan tổ chức, khai báo danh mục các phòng ban.
    *   Đã nhập liệu Demo **50+ hồ sơ nhân viên** cho **>3 phòng ban khác nhau** (Khối SX, Khối VP, Khối Trang trại...).
*   **In ấn Biểu mẫu**:
    *   Tích hợp tính năng **In ra Hồ sơ nhân sự** chuẩn mẫu HUHA HRM (Click vào tên để vào trang Hồ sơ chi tiết, bấm "In hồ sơ").
    *   Tính năng **In ra Danh sách Cán bộ, Giảng viên, Nhân viên** từ bảng dữ liệu danh sách tổng.
*   **Trường Dữ liệu**:
    *   Phần mềm lưu trữ cực kỳ đầy đủ: Mã NV, Tên, Ngày sinh, CCCD, Quê quán, Địa chỉ thường trú, Điện thoại, Dân tộc, Tôn giáo, Hôn nhân, Trình độ chuyên môn, Ngoại ngữ, Tin học, Quá trình công tác, Quá trình lương.

---

## 5. Các Phân hệ Chức năng Cốt lõi

1.  **Phân hệ Hồ sơ (Profile Management)**
    *   Tra cứu thông tin danh sách nhân sự (Tra cứu theo thông tin: Mã NV, Tên, Chức vụ).
    *   Tra cứu theo **Quá trình** (Lọc những ai đang thử việc, ai đã nghỉ việc).
    *   Click vào chi tiết để xem toàn vẹn Sơ yếu lý lịch số.
2.  **Phân hệ Quản lý Lương, Tính công**
    *   Ghi nhận hệ số lương, P1 (Vị trí), P2 (Năng lực), P3 (Hiệu suất).
3.  **Phân hệ Thuyên chuyển Công tác**
    *   Ghi nhận quá trình điều động nhân sự từ Phòng ban này sang Phòng ban khác (Ghi rõ Ngày hiệu lực, Đơn vị cũ, Đơn vị mới, Lý do). Lịch sử thuyên chuyển được gắn thẳng vào Hồ sơ nhân sự.
4.  **Phân hệ Khen thưởng - Kỷ luật**
    *   Thống kê Báo cáo những cá nhân có thành tích xuất sắc hoặc vi phạm quy chế (Kỷ luật cảnh cáo, sa thải...).
5.  **Phân hệ Nghỉ việc & Nghỉ hưu (Offboarding)**
    *   Cập nhật trạng thái "Đã nghỉ việc" hoặc "Nghỉ hưu".
    *   In báo cáo thống kê nghỉ hưu.
6.  **Hệ thống các Danh mục Từ điển (Dictionary Categories)**
    *   Để đảm bảo tính nhất quán của dữ liệu (tránh việc nhập sai chính tả), hệ thống bắt buộc áp dụng cấu trúc **Danh mục chọn sẵn** cho hàng loạt các trường thông tin.
    *   **Danh mục Cá nhân**: Dân tộc, Tôn giáo, Tình trạng hôn nhân, Thành phần xuất thân, Tình trạng sức khỏe, Đối tượng chính sách.
    *   **Danh mục Cơ cấu & Nghề nghiệp**: Đơn vị quản lý, Ngạch công chức/viên chức, Chức danh chức vụ, Chức vụ Đảng/Đoàn, Công việc chuyên môn đảm nhiệm.
    *   **Danh mục Trình độ**: Học vấn phổ thông, Trình độ chuyên môn, Ngành đào tạo, Trường đào tạo, Ngoại ngữ, Tin học, Lý luận chính trị, Quản lý nhà nước.
    *   **Danh mục Khác**: Hình thức khen thưởng, kỷ luật, Danh hiệu được phong.
7.  **Tra cứu & Tìm kiếm động (Advanced Dynamic Search)**
    *   Cho phép người dùng xây dựng các câu truy vấn (Query) phức tạp bằng cách kết hợp các toán tử (AND, OR, =, >, <).
    *   Ví dụ: Tìm nhân viên (Giới tính = Nữ) VÀ (Tuổi > 30) VÀ (Trình độ = Đại học) HOẶC (Đơn vị = Phòng Đào Tạo).
8.  **Quản trị Hệ thống (System Administration)**
    *   **Quản lý người sử dụng**: Phân quyền truy cập theo từng module (Chỉ đọc, Thêm/Sửa/Xóa).
    *   **Sao lưu, Phục hồi và Dọn dẹp dữ liệu**: Tính năng kết xuất toàn bộ cơ sở dữ liệu dự phòng và khôi phục khi có sự cố.
    *   **Kiểm tra tính chính xác của dữ liệu**: Thuật toán Audit phát hiện các hồ sơ thiếu thông tin quan trọng hoặc nhập sai định dạng.
9.  **Phân hệ Quản lý Tài sản & Đồ bảo hộ (PPE)**
    *   Quản lý việc cấp phát đồng phục, giày bảo hộ, thẻ từ, laptop cho nhân viên.
    *   Tự động liên kết: Nếu nhân viên nghỉ việc không hoàn trả tài sản, hệ thống cảnh báo chuyển sang Kế toán để khấu trừ lương.
10. **Định biên Nhân sự & Quản lý Chứng chỉ (Headcount Budget & Certificates)**
    *   Hệ thống thiết lập định biên (số lượng nhân viên tối đa) cho từng dự án/nhà máy và cảnh báo Thừa/Thiếu trên Dashboard.
    *   Cảnh báo tự động trước 60 ngày đối với Thẻ Xanh (Khám sức khỏe thực phẩm), Visa, Thẻ An toàn lao động.

---

## 6. Lộ trình Triển khai và Phân tầng Kiến trúc Hệ thống

(Từ Cốt lõi đến ứng dụng Trí tuệ Nhân tạo - AI)

Để xây dựng thành công một hệ thống đồ sộ cho 10.000+ nhân sự, Vinamilk không thể triển khai tất cả trong một đêm. Quá trình phát triển phần mềm được chia thành 3 mức độ (Levels) có tính kế thừa chặt chẽ, đi từ việc số hóa cơ bản đến áp dụng Hệ chuyên gia dự đoán tương lai.

### Mức 1: Số hóa Lõi & Tác nghiệp Hành chính (Core HR & Administration)
*   Số hóa toàn bộ Sơ yếu lý lịch, chứng chỉ, hợp đồng.
*   Giải quyết bài toán thêm, sửa, xóa, tìm kiếm, tra cứu hồ sơ cán bộ.
*   Khai báo cơ cấu tổ chức, in danh sách, in biểu mẫu hồ sơ chuẩn.

### Mức 2: Mức Tính Công, Tính Lương (Payroll & Time-tracking)
*   Kết nối máy chấm công, tính tự động ngày nghỉ đột xuất, tăng ca đột xuất.
*   Chạy bảng lương hàng tháng, quyết toán thuế, tự động hóa quy trình Tăng lương.

### Mức 3: Tích hợp AI vào Hệ chuyên gia (Expert System) & BI Dashboards
*   **Bảng điều khiển Hỗ trợ Ra quyết định (BI Dashboards & DSS)**: Cung cấp cho Ban Tổng Giám đốc các biểu đồ dự báo tài chính nhân sự. Ví dụ: Nếu năm sau tăng lương cơ sở lên 10%, quỹ lương toàn tập đoàn sẽ biến động bao nhiêu tỷ đồng? Giúp đưa ra quyết sách kinh doanh nhanh chóng, chính xác.
*   **Chức năng Phân tích & Đánh giá nhu cầu**: AI tự động "quét" hồ sơ kỹ năng của cán bộ hiện tại so với nhu cầu tổ chức.
*   **Dự đoán Lộ trình Đào tạo**: Dự báo khi nào cơ quan cần mở lớp đào tạo chứng chỉ hoặc cần tuyển mới để nâng cao trình độ đội ngũ.
*   **Cảnh báo Khủng hoảng Nhân sự**: Học máy (Machine Learning) nhận diện hành vi nhân sự sắp nghỉ việc để ban lãnh đạo có biện pháp giữ chân tài năng.

---

## 7. Kết Luận
Phần mềm HRIS được xây dựng không chỉ là một công cụ quản lý cơ sở dữ liệu (CRUD) thông thường, mà là một **Hệ sinh thái Quản trị Nguồn nhân lực toàn diện**. Từ khâu hoạch định Cơ cấu chức năng, Tuyển dụng đến Vòng đời hồ sơ, Thuyên chuyển và Nghỉ việc, mọi thứ đều được số hóa, hỗ trợ in ấn báo cáo chuẩn quy cách, tiến tới ứng dụng Trí tuệ Nhân tạo để trở thành "Cố vấn ảo" đắc lực cho Ban Lãnh đạo.

