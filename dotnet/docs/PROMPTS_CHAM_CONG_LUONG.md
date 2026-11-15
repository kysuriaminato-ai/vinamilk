# PROMPT CHI TIẾT – MẢNG CHẤM CÔNG & TIỀN LƯƠNG (Vinamilk HRM .NET 8)

> Cách dùng: mỗi phiên dán **Phiên 0 (prompt nền)** trong `PROMPTS_ANTIGRAVITY_HRM.md` trước, sau đó dán **Khối quy tắc chung C&B** bên dưới, rồi dán prompt của phiên. Làm lần lượt CL1 → CL8, xong phiên nào gửi báo cáo để kiểm tra rồi mới sang phiên sau.
>
> Hiện trạng đã có: WorkShift, ShiftSchedule, Holiday, AttendanceLog, DailyAttendance, MonthlyAttendanceSummary, OvertimeRequest, LeaveType/LeaveBalance/LeaveRequest, WebCheckIn, giải trình công, khóa bảng công; PayrollPeriod (vòng đời Draft→Paid), PayrollRecord/Detail, SalaryComponent, tham số pháp lý theo ngày hiệu lực, biểu thuế, phiếu lương PDF, file ngân hàng.

---

## KHỐI QUY TẮC CHUNG C&B (dán sau Phiên 0, trước mỗi phiên CL)

```
QUY TẮC RIÊNG CHO MẢNG CHẤM CÔNG & LƯƠNG:
- Mọi con số pháp lý (tỷ lệ, mức trần, số giờ tối đa, số ngày phép, mức miễn thuế…) lưu trong bảng cấu hình có EffectiveFrom/EffectiveTo, có cột LegalReference ghi văn bản căn cứ. Không ghi cứng trong code. Seed theo hiểu biết tốt nhất và đánh dấu NeedsHrVerification = true để HR đối chiếu.
- Mọi quy định nội bộ công ty (giờ ân hạn, làm tròn, phụ cấp, chính sách thưởng) cấu hình được theo Nhóm chính sách (PolicyGroup) gán cho đơn vị/chức danh/nhân viên, ưu tiên: nhân viên > chức danh > đơn vị > mặc định công ty.
- Tiền dùng decimal(18,0) cho số tiền cuối, decimal(18,4) cho trung gian; quy tắc làm tròn cấu hình được (mặc định làm tròn đến đồng ở bước cuối mỗi khoản).
- Dữ liệu đã khóa (bảng công tháng, kỳ lương) không sửa trực tiếp; mọi điều chỉnh sau khóa đi qua "điều chỉnh kỳ sau" (truy lĩnh/truy thu) có lý do và AuditLog.
- Không dùng fallback người thực hiện: luôn RequireUserId()/RequireUsername(); job nền dùng tài khoản system.job.
- Test nghiệp vụ dùng số tính tay ghi trong comment, KHÔNG tham chiếu hằng số của engine.
- KHÔNG tự chạy git pull/rebase/push/merge. Chỉ commit cục bộ.
- Cuối phiên: bảng file đã sửa, migration mới, kết quả dotnet test thật, coverage, danh sách việc tồn đọng.
```

---

## PHIÊN CL1 – CA LÀM VIỆC & CHÍNH SÁCH CHẤM CÔNG

```
NHIỆM VỤ CL1 – CA LÀM & CHÍNH SÁCH:
1. Mở rộng WorkShift:
   - Loại ca: Cố định, Linh hoạt (core time + tổng giờ), Ca đêm qua ngày (giờ vào 22:00 ra 06:00 hôm sau), Ca gãy (2 khoảng), Ca theo giờ.
   - Giờ nghỉ giữa ca (có/không tính công), số công quy đổi của ca (1 / 0.5…), khung giờ được phép chấm (vd vào sớm tối đa 2h).
   - Phân định giờ làm đêm (cấu hình mặc định 22:00–06:00) để tính phụ cấp đêm.
2. AttendancePolicy (gán theo PolicyGroup):
   - Giờ ân hạn đi muộn/về sớm (phút), quy tắc làm tròn giờ (5/10/15 phút, lên/xuống/gần nhất).
   - Ngưỡng tính nửa công / không công khi đi muộn quá X phút.
   - Quên chấm công: số lần được giải trình/tháng; vượt thì trừ công hoặc phạt theo cấu hình.
   - Có tính OT tự động hay bắt buộc có đơn OT được duyệt trước.
   - Ngày làm việc trong tuần (T2–T6, T2–T7, xoay ca).
3. Xếp ca:
   - Lịch xếp ca dạng calendar kéo thả theo tuần/tháng cho đơn vị; mẫu xoay ca (vd 3 ca 4 kíp) tự sinh lịch N tuần.
   - Đổi ca giữa 2 nhân viên qua đơn có duyệt.
   - Cảnh báo xung đột: trùng ca, nghỉ giữa 2 ca < số giờ tối thiểu cấu hình, vượt số giờ làm/tuần cấu hình.
4. Ngày lễ: lịch nghỉ lễ theo năm, ngày nghỉ bù khi lễ trùng ngày nghỉ hằng tuần, đánh dấu ngày lễ để tính OT ngày lễ.
5. Test: ca đêm qua ngày ghi công đúng ngày bắt đầu ca; làm tròn & ân hạn theo từng cấu hình; xung đột xếp ca bị cảnh báo.
```

---

## PHIÊN CL2 – THU THẬP DỮ LIỆU CHẤM CÔNG

```
NHIỆM VỤ CL2 – NGUỒN DỮ LIỆU CHẤM CÔNG:
1. AttendanceLog thống nhất mọi nguồn: Máy chấm công, Web, Mobile/GPS, QR, Import Excel, Nhập tay bởi HR. Lưu: thời điểm, nguồn, thiết bị, toạ độ, IP, ảnh (tùy chọn), người nhập (nếu nhập tay), lý do.
2. Máy chấm công:
   - Interface IAttendanceDeviceConnector + bản cài đặt đọc file xuất (CSV/TXT/DAT) chuẩn phổ biến; cấu trúc sẵn để thêm kết nối SDK/API (ZKTeco, Hikvision) sau.
   - Bảng ánh xạ Mã chấm công trên máy ↔ Mã nhân viên; log không khớp đưa vào hàng chờ "chưa ánh xạ".
   - Hangfire job đồng bộ định kỳ (cấu hình phút), chống trùng bản ghi.
3. Web/Mobile:
   - Geofence: danh sách địa điểm (nhà máy, văn phòng) có toạ độ + bán kính; ngoài vùng thì từ chối hoặc ghi nhận "ngoài vùng" chờ duyệt (cấu hình).
   - QR động đổi mỗi 30–60 giây hiển thị tại điểm chấm công; chặn chấm hộ (1 thiết bị/1 tài khoản, kiểm tra thời gian QR).
4. Import Excel bảng công: file mẫu, xem trước, báo lỗi từng dòng, chỉ ghi khi người dùng xác nhận.
5. Nhập tay/sửa log bởi HR bắt buộc lý do, AuditLog, hiển thị khác màu trên bảng công.
6. Test: trùng log bị loại; ngoài geofence xử lý đúng cấu hình; QR hết hạn bị từ chối; log chưa ánh xạ không mất.
```

---

## PHIÊN CL3 – TÍNH CÔNG NGÀY & BẢNG CÔNG THÁNG

```
NHIỆM VỤ CL3 – ENGINE TÍNH CÔNG:
1. AttendanceCalculationEngine (thuần, không phụ thuộc DB, dễ test): đầu vào = ca được xếp + log + đơn từ đã duyệt + ngày lễ + chính sách; đầu ra DailyAttendance gồm:
   giờ vào/ra thực tế, giờ công chuẩn, số công, phút đi muộn, phút về sớm, giờ làm đêm, giờ OT theo loại (ngày thường / ngày nghỉ hằng tuần / ngày lễ, ban ngày / ban đêm), mã trạng thái ngày.
2. Ký hiệu công chuẩn hóa, cấu hình được (ví dụ: X = đi làm, P = phép năm, L = lễ, Ô = ốm, TS = thai sản, CT = công tác, R = nghỉ riêng có lương, Ro = nghỉ không lương, KL = vắng không phép, ½ = nửa công…).
3. Xử lý: nhiều lần chấm trong ngày (lấy đầu/cuối hoặc theo cặp), thiếu chấm vào/ra, ca qua đêm, nghỉ nửa ngày + đi làm nửa ngày, công tác cả ngày không cần chấm.
4. Bảng công tháng:
   - Lưới nhân viên × ngày, màu theo trạng thái, click ô xem chi tiết log & đơn liên quan.
   - Tổng hợp: công thực tế, công chuẩn tháng, các loại nghỉ, OT theo loại, giờ đêm, số lần muộn/sớm, số lần quên chấm.
   - Tính lại theo nhân viên/đơn vị/khoảng ngày (job nền, có tiến độ).
5. Quy trình chốt công: HR đơn vị chốt → Quản lý xác nhận → HR tổng khóa. Nhân viên có X ngày xem & giải trình trước khi khóa (cấu hình). Đã khóa thì chỉ điều chỉnh qua kỳ sau.
6. Xuất bảng công Excel theo mẫu (có ký hiệu, tổng hợp, chữ ký).
7. Test bảng dữ liệu mẫu ≥ 20 tình huống (đi muộn trong/ngoài ân hạn, ca đêm, OT ngày lễ ban đêm, nửa ngày phép, quên chấm có/không giải trình…) với kết quả tính tay.
```

---

## PHIÊN CL4 – NGHỈ PHÉP, ĐƠN TỪ & LÀM THÊM GIỜ

```
NHIỆM VỤ CL4 – ĐƠN TỪ:
1. Loại nghỉ cấu hình được (LeaveType): có lương/không lương/BHXH chi trả, tính vào phép năm hay không, cho nghỉ nửa ngày/theo giờ, cần đính kèm giấy tờ, số ngày tối đa/lần/năm. Seed theo Bộ luật Lao động 2019 (phép năm cơ bản 12 ngày, cộng thêm theo thâm niên; nghỉ việc riêng có lương: kết hôn, con kết hôn, tang…) và đánh dấu NeedsHrVerification.
2. Phép năm: tính theo tháng làm việc thực tế cho người vào/nghỉ giữa năm, cộng thâm niên, phép tồn chuyển năm có hạn sử dụng (cấu hình ngày hết hạn), phép ứng trước (cho/không cho, tối đa), thanh toán tiền phép chưa nghỉ khi nghỉ việc (đẩy sang lương).
3. Các loại đơn đi qua Workflow: nghỉ phép, đi muộn/về sớm, công tác, làm việc từ xa, giải trình công, đổi ca, đăng ký OT. Hủy/thu hồi đơn đã duyệt có quy trình.
4. Làm thêm giờ:
   - Đăng ký OT trước (theo cá nhân hoặc theo danh sách nhóm), duyệt, đối chiếu giờ OT thực tế từ chấm công: lấy MIN(đăng ký, thực tế) hoặc theo cấu hình.
   - Giới hạn OT theo ngày/tháng/năm lưu ở tham số pháp lý (BLLĐ 2019: tối đa 40 giờ/tháng, 200 giờ/năm, một số ngành 300 giờ/năm – cần HR xác nhận). Cảnh báo khi gần chạm, chặn khi vượt (cấu hình).
   - Lựa chọn nghỉ bù thay vì trả tiền OT (cấu hình).
5. Lịch nghỉ của team (calendar), báo cáo số dư phép toàn công ty.
6. Test: phép năm người vào giữa năm; phép tồn hết hạn; OT vượt hạn mức bị chặn; đơn hủy sau duyệt hoàn lại số dư.
```

---

## PHIÊN CL5 – CẤU TRÚC LƯƠNG & THÀNH PHẦN LƯƠNG

```
NHIỆM VỤ CL5 – CẤU TRÚC LƯƠNG:
1. Hồ sơ lương nhân viên có lịch sử theo ngày hiệu lực: lương cơ bản, lương đóng BH (tách riêng khỏi lương thực trả), bậc/ngạch, các phụ cấp cố định, hình thức trả (tháng/ngày/giờ/sản phẩm), lương thử việc (tỷ lệ cấu hình, mặc định ≥ 85%), là đoàn viên công đoàn hay không.
2. SalaryComponent cấu hình đầy đủ: mã, tên, loại (Thu nhập/Khấu trừ/Thông tin), công thức (NCalc, cho phép tham chiếu mã thành phần khác và biến chấm công: CONG_THUC_TE, CONG_CHUAN, OT150, OT200, OT300, GIO_DEM…), thứ tự tính, có chịu thuế TNCN không, phần miễn thuế tối đa (tham số), có tính vào lương đóng BH không, hiển thị trên phiếu lương không.
3. Màn hình kiểm tra công thức: nhập biến mẫu → xem kết quả, phát hiện vòng lặp tham chiếu, lỗi cú pháp trước khi lưu.
4. Mẫu cấu trúc lương (Salary Template) theo nhóm: khối văn phòng, khối nhà máy (có ca đêm, độc hại), khối kinh doanh (hoa hồng/KPI).
5. Thay đổi lương giữa tháng: chia tỷ lệ theo số ngày công trước/sau ngày hiệu lực.
6. Test: công thức tham chiếu nhiều tầng; vòng lặp bị chặn; thay lương ngày 16 tính đúng 2 phần.
```

---

## PHIÊN CL6 – ENGINE TÍNH LƯƠNG ĐẦY ĐỦ

```
NHIỆM VỤ CL6 – TÍNH LƯƠNG:
1. Kết nối bảng công đã khóa → kỳ lương (chỉ tính khi bảng công tháng đã khóa; cảnh báo nếu chưa).
2. Thu nhập:
   - Lương theo công thực tế/công chuẩn; người vào/nghỉ giữa tháng; lương thử việc.
   - OT theo loại ngày và ca ngày/đêm; phụ cấp làm đêm; tách phần OT được miễn thuế (phần trả cao hơn giờ làm bình thường) theo quy định, tỷ lệ lưu tham số.
   - Phụ cấp cố định & theo công, thưởng KPI (lấy từ module KPI), hoa hồng, thưởng tháng 13/lễ Tết (kỳ chi riêng).
   - Tiền phép năm chưa nghỉ khi nghỉ việc; truy lĩnh từ kỳ trước.
3. Khấu trừ: BHXH/BHYT/BHTN phần NLĐ trên lương đóng BH (có trần theo tham số, theo vùng); đoàn phí NLĐ (nếu đoàn viên); tạm ứng; vay/khấu trừ trả góp; phạt/kỷ luật; truy thu.
4. Chi phí công ty: BHXH/BHYT/BHTN/BHTNLĐ-BNN phần DN, kinh phí công đoàn – tính và lưu để báo cáo chi phí.
5. Thuế TNCN:
   - Cư trú có HĐ ≥ 3 tháng: biểu lũy tiến theo tham số kỳ; giảm trừ gia cảnh theo số NPT đã đăng ký còn hiệu lực trong tháng.
   - HĐ < 3 tháng/không HĐ: khấu trừ tỷ lệ cố định theo tham số, hỗ trợ trường hợp có cam kết (mẫu 08/CK-TNCN) thì không khấu trừ.
   - Người nước ngoài không cư trú: tỷ lệ cố định theo tham số.
   - Các khoản miễn thuế/không chịu thuế theo cấu hình thành phần lương.
6. Lương Net → Gross (cho HĐ cam kết lương net): giải ngược bằng tìm kiếm nhị phân đến sai số 1 đồng.
7. Mô phỏng kỳ lương (Simulation) không ghi DB; so sánh với kỳ trước theo nhân viên/khoản, đánh dấu biến động bất thường.
8. Bảng chi tiết cách tính từng nhân viên (drill-down: khoản → công thức → giá trị biến).
9. Test ≥ 15 hồ sơ mẫu tính tay: NV thường, NV có 2 NPT, lương trên trần BH, vào ngày 10, nghỉ ngày 20, thử việc, OT đêm ngày lễ, HĐ < 3 tháng có/không cam kết, người nước ngoài, net→gross, có tạm ứng + truy thu.
```

---

## PHIÊN CL7 – KỲ CHI, ĐIỀU CHỈNH & TẠM ỨNG

```
NHIỆM VỤ CL7 – KỲ CHI & ĐIỀU CHỈNH:
1. Nhiều đợt chi trong tháng: tạm ứng giữa tháng, lương chính cuối tháng, thưởng/tháng 13 riêng; thuế TNCN của tháng tính trên tổng các đợt chi trong tháng.
2. Đơn tạm ứng lương qua ESS, có hạn mức (% lương), duyệt, tự khấu trừ ở kỳ lương chính.
3. Khoản vay/trả góp nội bộ: lịch trừ hằng tháng, số dư còn lại, tất toán khi nghỉ việc.
4. Điều chỉnh sau khóa: tạo AdjustmentEntry (truy lĩnh/truy thu) gắn kỳ gốc, lý do, tự đưa vào kỳ kế tiếp; thuế tính lại đúng kỳ phát sinh theo cấu hình.
5. Quyết toán nghỉ việc (final settlement): lương ngày làm cuối, phép chưa nghỉ, trợ cấp thôi việc (theo tham số), trừ tạm ứng/vay, xuất biên bản quyết toán PDF.
6. Test: thuế tháng có 2 đợt chi; tạm ứng tự trừ; truy thu sau khóa vào kỳ sau; quyết toán nghỉ việc đủ các khoản.
```

---

## PHIÊN CL8 – BÁO CÁO, BẢO HIỂM, THUẾ & KẾ TOÁN

```
NHIỆM VỤ CL8 – BÁO CÁO & TÍCH HỢP:
o lương: bảng lương chi tiết theo đơn vị (định dạng in A3 ngang, có chữ ký), tổng hợp quỹ lương theo tháng/quý/năm, so sánh kỳ, chi phí nhân sự theo trung tâm chi phí.
2. Bảo hiểm: danh sách tăng/giảm lao động và điều chỉnh mức đóng theo tháng, xuất Excel theo mẫu phổ biến dùng cho phần mềm khai BHXH (cấu trúc để cập nhật mẫu khi cơ quan BHXH thay đổi); đối chiếu số phải nộp.
3. Thuế TNCN: tờ khai khấu trừ tháng/quý (số liệu tổng hợp), dữ liệu quyết toán năm theo từng người (tổng thu nhập, giảm trừ, thuế đã khấu trừ, phải nộp thêm/hoàn), chứng từ khấu trừ thuế TNCN PDF cho từng nhân viên, xuất Excel để nhập phần mềm khai thuế.
4. Hạch toán kế toán: bút toán lương (Nợ chi phí theo trung tâm chi phí / Có phải trả NLĐ, BH, thuế), xuất file Excel/CSV theo cấu hình tài khoản kế toán để nhập vào phần mềm kế toán.
5. Dashboard C&B: quỹ lương theo tháng, cơ cấu thu nhập, tỷ lệ OT, chi phí BH, top biến động.
6. ESS: nhân viên xem bảng công tháng 1. Báo cáchi tiết, số dư phép, lịch sử lương, chứng từ khấu trừ thuế năm.
7. Test: tổng các báo cáo khớp nhau và khớp tổng kỳ lương (tổng thực lĩnh = file ngân hàng = bút toán phải trả NLĐ).
```

---

## SAU KHI XONG CL1–CL8: KIỂM THỬ SONG SONG

```
Tạo bộ dữ liệu mẫu 1 tháng cho 30 nhân viên đa dạng (văn phòng, nhà máy 3 ca, kinh doanh, thử việc, người nước ngoài, vào/nghỉ giữa tháng), sinh log chấm công giả lập có lỗi thực tế (quên chấm, đi muộn, OT đêm, ngày lễ), chạy trọn quy trình: chốt công → tính lương → duyệt → khóa → phiếu lương → file ngân hàng → báo cáo BH/thuế/kế toán. Xuất file Excel đối chiếu cho kế toán kiểm tra tay và liệt kê mọi giả định đã dùng.
```
