<?php
/**
 * =====================================================================
 * VINAMILK HRM - MODEL CHẤM CÔNG & QUẢN LÝ THỜI GIAN LÀM VIỆC
 * =====================================================================
 * Tuân thủ Bộ luật Lao động 2019 & Quy chuẩn Vinamilk:
 *   1. Danh mục 4 ca làm việc: Ca 1, Ca 2, Ca 3 (đêm qua ngày), Hành chính
 *   2. Xếp ca theo tuần cho tổ sản xuất (Weekly Shift Roster Grid)
 *   3. Thuật toán Import file máy chấm công, ghép ca, xử lý ca đêm, đi trễ, về sớm, quên quẹt
 *   4. Bảng công tháng tổng hợp: Công thực tế, phép, lễ, OT phân loại, giờ đêm
 *   5. Quy trình 3 bước: Tổ trưởng xác nhận -> Quản đốc duyệt -> Khóa công
 *   6. Chặn sửa trực tiếp sau khi khóa công, quản lý phiếu điều chỉnh công có lý do
 *   7. Cảnh báo OT vượt ngưỡng tháng (40h) và năm (200h/300h)
 *   8. Áp dụng 100% scopeByUnit() theo cây đơn vị
 */

require_once __DIR__ . '/../core/Model.php';

class AttendanceModel extends Model
{
    protected string $table = 'cham_cong_ngay';
    protected string $primaryKey = 'id';

    /**
     * 1. DANH MỤC CA LÀM VIỆC CHUẨN VINAMILK
     */
    public function getShifts(): array
    {
        $sql = "SELECT * FROM ca_lam_viec WHERE trang_thai = 'HOAT_DONG' ORDER BY gio_bat_dau ASC";
        return $this->fetchAll($sql);
    }

    public function getShiftByCode(string $maCa): ?array
    {
        $sql = "SELECT * FROM ca_lam_viec WHERE ma_ca = :ma_ca LIMIT 1";
        return $this->fetchOne($sql, [':ma_ca' => $maCa]);
    }

    /**
     * 2. MA TRẬN XẾP CA THEO TUẦN CHO TỔ SẢN XUẤT (Weekly Shift Roster Grid)
     * 
     * @param string $maDV Mã đơn vị/tổ sản xuất
     * @param string $mondayDate Ngày Thứ 2 bắt đầu tuần (Y-m-d)
     * @return array Ma trận ngày trong tuần và phân ca của từng nhân viên
     */
    public function getWeeklyRoster(string $maDV, string $mondayDate): array
    {
        $monday = new DateTime($mondayDate);
        $dates = [];
        for ($i = 0; $i < 7; $i++) {
            $cur = clone $monday;
            $cur->modify("+{$i} days");
            $dates[] = [
                'date'    => $cur->format('Y-m-d'),
                'day_name'=> match ($i) {
                    0 => 'Thứ 2', 1 => 'Thứ 3', 2 => 'Thứ 4',
                    3 => 'Thứ 5', 4 => 'Thứ 6', 5 => 'Thứ 7', 6 => 'Chủ Nhật'
                },
                'display' => $cur->format('d/m')
            ];
        }

        // Lấy danh sách nhân viên trong đơn vị (áp scopeByUnit)
        $scope = $this->scopeByUnit('ns', 'MaDV');
        $params = $scope['params'];
        $whereUnit = "ns.TrangThai = 1 AND ({$scope['where']})";

        if (!empty($maDV) && $maDV !== 'ALL') {
            $descendants = $this->getDescendantUnits($maDV);
            $uKeys = [];
            foreach ($descendants as $idx => $u) {
                $k = ':u_roster_' . $idx;
                $uKeys[] = $k;
                $params[$k] = $u;
            }
            $whereUnit .= " AND ns.MaDV IN (" . implode(', ', $uKeys) . ")";
        }

        $sqlEmployees = "SELECT ns.MaNV, ns.HoTen, ns.MaDV, dv.TenDV, cv.TenCV
                         FROM nhansu ns
                         LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                         LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                         WHERE {$whereUnit}
                         ORDER BY ns.HoTen ASC";
        $employees = $this->fetchAll($sqlEmployees, $params);

        // Lấy phân ca trong 7 ngày của tuần
        $startDate = $dates[0]['date'];
        $endDate   = $dates[6]['date'];

        $sqlRoster = "SELECT pc.ma_nv, pc.ngay_lam_viec, pc.ma_ca, ca.ten_ca, ca.la_ca_dem
                      FROM phan_ca pc
                      LEFT JOIN ca_lam_viec ca ON pc.ma_ca = ca.ma_ca
                      WHERE pc.ngay_lam_viec BETWEEN :sdate AND :edate";
        $rosterRows = $this->fetchAll($sqlRoster, [':sdate' => $startDate, ':edate' => $endDate]);

        $rosterMap = [];
        foreach ($rosterRows as $r) {
            $rosterMap[$r['ma_nv']][$r['ngay_lam_viec']] = $r['ma_ca'];
        }

        foreach ($employees as &$emp) {
            $empShifts = [];
            foreach ($dates as $d) {
                $dt = $d['date'];
                // Mặc định nếu chưa phân: Ngày thường là CA_1_SANG hoặc CA_HANH_CHINH, Chủ nhật là OFF
                $defaultShift = (date('N', strtotime($dt)) == 7) ? 'OFF' : 'CA_1_SANG';
                $empShifts[$dt] = $rosterMap[$emp['MaNV']][$dt] ?? $defaultShift;
            }
            $emp['shifts'] = $empShifts;
        }
        unset($emp);

        return [
            'dates'     => $dates,
            'shifts'    => $this->getShifts(),
            'employees' => $employees,
            'start_date'=> $startDate,
            'end_date'  => $endDate
        ];
    }

    /**
     * LƯU PHÂN CA TUẦN CHO TỔ SẢN XUẤT
     */
    public function saveWeeklyRoster(array $rosterData): bool
    {
        if (empty($rosterData['assignments'])) {
            return false;
        }

        $this->beginTransaction();
        try {
            require_once __DIR__ . '/../services/CertificateService.php';
            $certService = new CertificateService($this->db);

            $sql = "INSERT INTO phan_ca (ma_nv, ngay_lam_viec, ma_ca, ma_dv, nguoi_phan_ca, created_at)
                    VALUES (:manv, :ngay, :maca, :madv, :nguoi, NOW())
                    ON DUPLICATE KEY UPDATE ma_ca = VALUES(ma_ca), nguoi_phan_ca = VALUES(nguoi_phan_ca)";

            $stmt = $this->db->prepare($sql);
            $currentUser = Auth::username() ?? 'SYSTEM';

            foreach ($rosterData['assignments'] as $assign) {
                // CHẶN XẾP CA DÂY CHUYỀN SẢN XUẤT NẾU THẺ XANH ATTP HẾT HẠN HOẶC CHƯA CÓ
                $certService->validateShiftAssignment(
                    $assign['ma_nv'], 
                    $assign['ma_ca'], 
                    $assign['ngay'], 
                    $assign['ma_dv'] ?? null
                );

                $stmt->execute([
                    ':manv'  => $assign['ma_nv'],
                    ':ngay'  => $assign['ngay'],
                    ':maca'  => $assign['ma_ca'],
                    ':madv'  => $assign['ma_dv'] ?? 'NM_TS',
                    ':nguoi' => $currentUser
                ]);
            }

            $this->logAudit('SAVE_ROSTER', 'phan_ca', 'WEEKLY', null, ['count' => count($rosterData['assignments'])]);
            $this->commit();
            return true;
        } catch (Throwable $e) {
            $this->rollBack();
            throw $e;
        }
    }

    /**
     * 3. IMPORT DỮ LIỆU THÔ TỪ MÁY CHẤM CÔNG (Raw Swipe Logs)
     * 
     * @param array $logs Mảng các bản ghi quẹt thẻ [['ma_nv' => '...', 'thoi_gian_quet' => 'YYYY-MM-DD HH:mm:ss']]
     * @param string $sourceFileName Tên file import
     * @return int Số dòng import thành công
     */
    public function importSwipeLogs(array $logs, string $sourceFileName = ''): int
    {
        if (empty($logs)) return 0;

        $this->beginTransaction();
        try {
            $sql = "INSERT INTO quet_the_tho (ma_nv, thoi_gian_quet, ma_may, loai_quet, nguon_import, created_at)
                    VALUES (:manv, :thoigian, :mamay, 'KHONG_XAC_DINH', :nguon, NOW())";
            $stmt = $this->db->prepare($sql);

            $count = 0;
            foreach ($logs as $row) {
                if (empty($row['ma_nv']) || empty($row['thoi_gian_quet'])) continue;
                $stmt->execute([
                    ':manv'     => trim($row['ma_nv']),
                    ':thoigian' => trim($row['thoi_gian_quet']),
                    ':mamay'    => $row['ma_may'] ?? 'MCC-01',
                    ':nguon'    => $sourceFileName
                ]);
                $count++;
            }

            $this->logAudit('IMPORT_SWIPE', 'quet_the_tho', 'BATCH', null, ['count' => $count, 'file' => $sourceFileName]);
            $this->commit();
            return $count;
        } catch (Throwable $e) {
            $this->rollBack();
            throw $e;
        }
    }

    /**
     * 4. THUẬT TOÁN GHÉP CA, XỬ LÝ CA ĐÊM (OVERNIGHT), TÍNH ĐI TRỄ, VỀ SỚM, QUÊN QUẸT & OT
     * 
     * @param string $maNV Mã nhân sự
     * @param string $date Ngày làm việc (Y-m-d)
     * @return array Kết quả tính toán chấm công ngày
     */
    public function processAttendanceDay(string $maNV, string $date): array
    {
        // 1. Xác định ca làm việc của ngày đó từ bảng phan_ca
        $sqlPC = "SELECT pc.ma_ca, ca.* 
                  FROM phan_ca pc 
                  LEFT JOIN ca_lam_viec ca ON pc.ma_ca = ca.ma_ca 
                  WHERE pc.ma_nv = :manv AND pc.ngay_lam_viec = :ngay 
                  LIMIT 1";
        $shift = $this->fetchOne($sqlPC, [':manv' => $maNV, ':ngay' => $date]);

        // Nếu chưa phân ca, mặc định lấy ca Hành chính (hoặc Ca 1 nếu là công nhân)
        if (!$shift || empty($shift['ma_ca']) || $shift['ma_ca'] === 'OFF') {
            if ($shift && $shift['ma_ca'] === 'OFF') {
                // Ngày nghỉ hàng tuần được phân ca OFF
                return [
                    'ma_nv'        => $maNV,
                    'ngay'         => $date,
                    'ma_ca'        => 'OFF',
                    'cong_thuc_te' => 0,
                    'loai_cong'    => 'NGHI_PHEP_NAM',
                    'ghi_chu'      => 'Nghỉ hàng tuần'
                ];
            }
            $shift = $this->getShiftByCode('CA_HANH_CHINH');
        }

        $maCa      = $shift['ma_ca'] ?? 'CA_HANH_CHINH';
        $shiftStart= $shift['gio_bat_dau']; // VD: 06:00:00, 14:00:00, 22:00:00
        $shiftEnd  = $shift['gio_ket_thuc'];   // VD: 14:00:00, 22:00:00, 06:00:00
        $isOvernight= (bool)($shift['la_ca_dem'] ?? 0); // Ca 3: 22h -> 06h

        // 2. Tìm các lần quẹt thẻ trong khung giờ hợp lệ
        if ($isOvernight) {
            // CA 3 QUA ĐÊM: Vào từ 21:00 đến 23:59 hôm nay; Ra từ 05:00 đến 08:30 sáng hôm sau!
            $nextDate = date('Y-m-d', strtotime($date . ' +1 day'));
            $sqlIn = "SELECT thoi_gian_quet FROM quet_the_tho 
                      WHERE ma_nv = :manv 
                        AND thoi_gian_quet BETWEEN '{$date} 20:30:00' AND '{$date} 23:59:59' 
                      ORDER BY thoi_gian_quet ASC LIMIT 1";

            $sqlOut = "SELECT thoi_gian_quet FROM quet_the_tho 
                       WHERE ma_nv = :manv 
                         AND thoi_gian_quet BETWEEN '{$nextDate} 04:30:00' AND '{$nextDate} 09:00:00' 
                       ORDER BY thoi_gian_quet DESC LIMIT 1";
        } else {
            // CA NGÀY THƯỜNG: Ghép các lần quẹt trong ngày
            $sqlIn = "SELECT thoi_gian_quet FROM quet_the_tho 
                      WHERE ma_nv = :manv 
                        AND thoi_gian_quet BETWEEN '{$date} 05:00:00' AND '{$date} 16:00:00' 
                      ORDER BY thoi_gian_quet ASC LIMIT 1";

            $sqlOut = "SELECT thoi_gian_quet FROM quet_the_tho 
                       WHERE ma_nv = :manv 
                         AND thoi_gian_quet BETWEEN '{$date} 12:00:00' AND '{$date} 23:59:59' 
                       ORDER BY thoi_gian_quet DESC LIMIT 1";
        }

        $rowIn  = $this->fetchOne($sqlIn, [':manv' => $maNV]);
        $rowOut = $this->fetchOne($sqlOut, [':manv' => $maNV]);

        $gioVaoStr = $rowIn ? date('H:i:s', strtotime($rowIn['thoi_gian_quet'])) : null;
        $gioRaStr  = $rowOut ? date('H:i:s', strtotime($rowOut['thoi_gian_quet'])) : null;

        // Nếu chỉ có đúng 1 lần quẹt trong ngày, rowIn và rowOut sẽ trỏ tới cùng 1 bản ghi
        if ($rowIn && $rowOut && $rowIn['thoi_gian_quet'] === $rowOut['thoi_gian_quet']) {
            $swipeTs      = strtotime($rowIn['thoi_gian_quet']);
            $shiftStartTs = strtotime($date . ' ' . $shiftStart);
            $shiftEndTs   = strtotime(($isOvernight ? date('Y-m-d', strtotime($date . ' +1 day')) : $date) . ' ' . $shiftEnd);
            // Nếu lần quẹt gần giờ vào hơn -> coi như quẹt vào và quên quẹt ra
            if (abs($swipeTs - $shiftStartTs) <= abs($swipeTs - $shiftEndTs)) {
                $gioRaStr = null;
            } else {
                $gioVaoStr = null;
            }
        }

        // 3. Xử lý Đi trễ, Về sớm & Quên quẹt
        $diMuonPhut = 0;
        $veSomPhut  = 0;
        $quenQuet   = 0;
        $congThucTe = 1.00;
        $loaiCong   = 'DI_LAM';
        $trangThaiDuyet = 'HOP_LE';

        if (!$gioVaoStr && !$gioRaStr) {
            // Không quẹt lần nào: Kiểm tra xem có đơn xin nghỉ phép hợp lệ không
            $sqlLeave = "SELECT loai_nghi FROM don_nghi_phep 
                         WHERE ma_nv = :manv 
                           AND :ngay BETWEEN tu_ngay AND den_ngay 
                           AND trang_thai = 'DA_DUYET' 
                         LIMIT 1";
            $leave = $this->fetchOne($sqlLeave, [':manv' => $maNV, ':ngay' => $date]);

            if ($leave) {
                $loaiCong = ($leave['loai_nghi'] === 'PHEP_NAM') ? 'NGHI_PHEP_NAM' : 'NGHI_OM_BHXH';
                $congThucTe = 1.00; // Nghỉ có phép hưởng lương
            } else {
                $loaiCong = 'NGHI_KHONG_PHEP';
                $congThucTe = 0.00;
            }
        } elseif (!$gioVaoStr || !$gioRaStr) {
            // Quên quẹt 1 đầu (quên quẹt vào hoặc quên quẹt ra)
            $quenQuet = 1;
            $congThucTe = 0.50; // Tạm tính nửa công chờ giải trình
            $trangThaiDuyet = 'CHO_GIAI_TRINH';
        } else {
            // Quẹt đủ cả 2 đầu -> Tính số phút đi muộn, về sớm
            $shiftStartTime = strtotime($date . ' ' . $shiftStart);
            $shiftEndTime   = strtotime(($isOvernight ? date('Y-m-d', strtotime($date . ' +1 day')) : $date) . ' ' . $shiftEnd);
            $actualInTime   = strtotime($rowIn['thoi_gian_quet']);
            $actualOutTime  = strtotime($rowOut['thoi_gian_quet']);

            // Ân hạn đi muộn 5 phút
            if ($actualInTime > ($shiftStartTime + 300)) {
                $diMuonPhut = (int)floor(($actualInTime - $shiftStartTime) / 60);
            }

            // Về sớm
            if ($actualOutTime < $shiftEndTime) {
                $veSomPhut = (int)floor(($shiftEndTime - $actualOutTime) / 60);
            }

            // Nếu đi muộn > 120 phút hoặc về sớm > 120 phút -> Trừ công
            if ($diMuonPhut > 120 || $veSomPhut > 120) {
                $congThucTe = 0.50;
            }
        }

        // 4. Tính toán Giờ làm đêm (22:00 -> 06:00)
        $gioLamDem = 0.00;
        if ($isOvernight && $congThucTe > 0) {
            $gioLamDem = 8.00; // Ca 3 trọn vẹn trong khung giờ đêm
        }

        // 5. Tính toán Giờ làm thêm (OT)
        $otNgayThuong = 0.00;
        $otNgayNghi   = 0.00;
        $otNgayLe     = 0.00;

        if ($gioVaoStr && $gioRaStr) {
            $actualOutTime = strtotime($rowOut['thoi_gian_quet']);
            $shiftEndTime  = strtotime(($isOvernight ? date('Y-m-d', strtotime($date . ' +1 day')) : $date) . ' ' . $shiftEnd);

            // Nếu làm thêm sau giờ kết thúc ca trên 30 phút
            if ($actualOutTime > ($shiftEndTime + 1800)) {
                $otHours = round(($actualOutTime - $shiftEndTime) / 3600, 2);

                // Phân loại OT theo ngày
                $dayOfWeek = date('N', strtotime($date)); // 7 là Chủ nhật
                if ($dayOfWeek == 7) {
                    $otNgayNghi = $otHours;
                } else {
                    $otNgayThuong = $otHours;
                }
            }
        }

        $tongGioOT = $otNgayThuong + $otNgayNghi + $otNgayLe;

        // 6. Lưu / Cập nhật vào bảng cham_cong_ngay
        $sqlSave = "INSERT INTO cham_cong_ngay 
                    (ma_nv, ngay, ma_ca, gio_vao, gio_ra, di_muon_phut, ve_som_phut, quen_quet,
                     gio_lam_them, ot_ngay_thuong, ot_ngay_nghi, ot_ngay_le, gio_lam_dem, 
                     cong_chuan, cong_thuc_te, loai_cong, trang_thai_duyet, created_at, updated_at)
                    VALUES 
                    (:manv, :ngay, :maca, :giovao, :giora, :dimuon, :vesom, :quenquet,
                     :tongot, :otthuong, :otnghi, :otle, :giodem,
                     1.00, :congtt, :loaicong, :ttduyet, NOW(), NOW())
                    ON DUPLICATE KEY UPDATE 
                        gio_vao = VALUES(gio_vao), gio_ra = VALUES(gio_ra),
                        di_muon_phut = VALUES(di_muon_phut), ve_som_phut = VALUES(ve_som_phut),
                        quen_quet = VALUES(quen_quet), gio_lam_them = VALUES(gio_lam_them),
                        ot_ngay_thuong = VALUES(ot_ngay_thuong), ot_ngay_nghi = VALUES(ot_ngay_nghi),
                        ot_ngay_le = VALUES(ot_ngay_le), gio_lam_dem = VALUES(gio_lam_dem),
                        cong_thuc_te = VALUES(cong_thuc_te), loai_cong = VALUES(loai_cong),
                        trang_thai_duyet = VALUES(trang_thai_duyet), updated_at = NOW()";

        $this->query($sqlSave, [
            ':manv'     => $maNV,
            ':ngay'     => $date,
            ':maca'     => $maCa,
            ':giovao'   => $gioVaoStr,
            ':giora'    => $gioRaStr,
            ':dimuon'   => $diMuonPhut,
            ':vesom'    => $veSomPhut,
            ':quenquet' => $quenQuet,
            ':tongot'   => $tongGioOT,
            ':otthuong' => $otNgayThuong,
            ':otnghi'   => $otNgayNghi,
            ':otle'     => $otNgayLe,
            ':giodem'   => $gioLamDem,
            ':congtt'   => $congThucTe,
            ':loaicong' => $loaiCong,
            ':ttduyet'  => $trangThaiDuyet
        ]);

        return [
            'ma_nv'         => $maNV,
            'ngay'          => $date,
            'ma_ca'         => $maCa,
            'gio_vao'       => $gioVaoStr,
            'gio_ra'        => $gioRaStr,
            'di_muon_phut'  => $diMuonPhut,
            've_som_phut'   => $veSomPhut,
            'quen_quet'     => $quenQuet,
            'cong_thuc_te'  => $congThucTe,
            'gio_lam_dem'   => $gioLamDem,
            'ot_ngay_thuong'=> $otNgayThuong,
            'ot_ngay_nghi'  => $otNgayNghi,
            'ot_ngay_le'    => $otNgayLe,
            'tong_gio_ot'   => $tongGioOT
        ];
    }

    /**
     * 5. BẢNG CÔNG THÁNG TỔNG HỢP & CẢNH BÁO GIỚI HẠN OT
     * Tổng hợp từ cham_cong_ngay vào bang_cong_thang
     * 
     * @param int $month Tháng (1..12)
     * @param int $year Năm (2026)
     * @param string|null $maDV Đơn vị lọc
     * @return int Số nhân viên đã được tổng hợp công
     */
    public function generateMonthlyTimesheet(int $month, int $year, ?string $maDV = null): int
    {
        $startDate = sprintf('%04d-%02d-01', $year, $month);
        $endDate   = date('Y-m-t', strtotime($startDate));

        $scope = $this->scopeByUnit('ns', 'MaDV');
        $empParams = $scope['params'];

        $where = "ns.TrangThai = 1 AND ({$scope['where']})";
        if (!empty($maDV) && $maDV !== 'ALL') {
            $descendants = $this->getDescendantUnits($maDV);
            $uKeys = [];
            foreach ($descendants as $idx => $u) {
                $k = ':u_mth_' . $idx;
                $uKeys[] = $k;
                $empParams[$k] = $u;
            }
            $where .= " AND ns.MaDV IN (" . implode(', ', $uKeys) . ")";
        }

        // Lấy danh sách nhân viên cần tính
        $employees = $this->fetchAll("SELECT ns.MaNV, ns.MaDV FROM nhansu ns WHERE {$where}", $empParams);
        $processedCount = 0;

        $this->beginTransaction();
        try {
            foreach ($employees as $emp) {
                $manv = $emp['MaNV'];

                // Kiểm tra xem bảng công đã bị khóa chưa
                $checkLock = $this->fetchOne(
                    "SELECT trang_thai FROM bang_cong_thang WHERE thang = :m AND nam = :y AND ma_nv = :manv",
                    [':m' => $month, ':y' => $year, ':manv' => $manv]
                );

                if ($checkLock && $checkLock['trang_thai'] === 'KHOA_CONG') {
                    // Đã khóa công -> Bỏ qua không ghi đè tự động
                    continue;
                }

                // Tổng hợp dữ liệu từ cham_cong_ngay
                $sqlAgg = "SELECT 
                                SUM(cc.cong_thuc_te) as TongCongThucTe,
                                SUM(CASE WHEN cc.loai_cong = 'NGHI_PHEP_NAM' THEN 1 ELSE 0 END) as PhepNam,
                                SUM(CASE WHEN cc.loai_cong = 'NGHI_LE_TET' THEN 1 ELSE 0 END) as LeTet,
                                SUM(CASE WHEN cc.loai_cong = 'NGHI_KHONG_LUONG' THEN 1 ELSE 0 END) as KhongLuong,
                                SUM(CASE WHEN cc.loai_cong IN ('NGHI_OM_BHXH', 'NGHI_THAI_SAN') THEN 1 ELSE 0 END) as OmThaiSan,
                                SUM(cc.ot_ngay_thuong) as OTThuong,
                                SUM(cc.ot_ngay_nghi) as OTNghi,
                                SUM(cc.ot_ngay_le) as OTLe,
                                SUM(cc.gio_lam_them) as TongOT,
                                SUM(cc.gio_lam_dem) as GioDem,
                                SUM(CASE WHEN cc.di_muon_phut > 0 OR cc.ve_som_phut > 0 THEN 1 ELSE 0 END) as DiMuonVeSomLan,
                                SUM(cc.quen_quet) as QuenQuetLan
                           FROM cham_cong_ngay cc
                           WHERE cc.ma_nv = :manv AND cc.ngay BETWEEN :sdate AND :edate";

                $agg = $this->fetchOne($sqlAgg, [':manv' => $manv, ':sdate' => $startDate, ':edate' => $endDate]);

                $congTT    = (float)($agg['TongCongThucTe'] ?? 0);
                $phepNam   = (float)($agg['PhepNam'] ?? 0);
                $leTet     = (float)($agg['LeTet'] ?? 0);
                $tongCong  = $congTT + $phepNam + $leTet;
                $otThuong  = (float)($agg['OTThuong'] ?? 0);
                $otNghi    = (float)($agg['OTNghi'] ?? 0);
                $otLe      = (float)($agg['OTLe'] ?? 0);
                $tongOT    = (float)($agg['TongOT'] ?? 0);
                $gioDem    = (float)($agg['GioDem'] ?? 0);

                // CẢNH BÁO GIỚI HẠN OT: Tháng (40h) & Năm (200h)
                $canhBaoOT = null;
                if ($tongOT > 40) {
                    $canhBaoOT = 'VUOT_THANG_40H';
                } elseif ($tongOT >= 35) {
                    $canhBaoOT = 'CANH_BAO_35H';
                }

                // Kiểm tra lũy kế OT trong năm từ đầu năm
                $sqlYtd = "SELECT SUM(tong_gio_ot) as YtdOT 
                           FROM bang_cong_thang 
                           WHERE nam = :nam AND thang <= :m AND ma_nv = :manv";
                $ytd = $this->fetchOne($sqlYtd, [':nam' => $year, ':m' => $month, ':manv' => $manv]);
                $ytdHours = (float)($ytd['YtdOT'] ?? 0) + $tongOT;

                if ($ytdHours > 200) {
                    $canhBaoOT = $canhBaoOT ? ($canhBaoOT . '_VUOT_NAM_200H') : 'VUOT_NAM_200H';
                }

                // Lưu vào bang_cong_thang
                $sqlUpsert = "INSERT INTO bang_cong_thang 
                              (thang, nam, ma_nv, ma_dv, cong_chuan_thang, cong_thuc_te, nghi_phep_huong_luong,
                               nghi_le_tet, nghi_khong_luong, nghi_om_thai_san, tong_cong_tinh_luong,
                               gio_ot_ngay_thuong, gio_ot_ngay_nghi, gio_ot_ngay_le, tong_gio_ot,
                               gio_lam_dem, di_muon_ve_som_lan, quen_quet_the_lan, canh_bao_ot, trang_thai, created_at)
                              VALUES 
                              (:thang, :nam, :manv, :madv, 26.00, :congtt, :phep, :le, :khongluong, :om, :tongcong,
                               :otthuong, :otnghi, :otle, :tongot, :giodem, :dimuon, :quenquet, :canhbao, 'KHOI_TAO', NOW())
                              ON DUPLICATE KEY UPDATE 
                                  cong_thuc_te = VALUES(cong_thuc_te),
                                  nghi_phep_huong_luong = VALUES(nghi_phep_huong_luong),
                                  nghi_le_tet = VALUES(nghi_le_tet),
                                  tong_cong_tinh_luong = VALUES(tong_cong_tinh_luong),
                                  gio_ot_ngay_thuong = VALUES(gio_ot_ngay_thuong),
                                  gio_ot_ngay_nghi = VALUES(gio_ot_ngay_nghi),
                                  gio_ot_ngay_le = VALUES(gio_ot_ngay_le),
                                  tong_gio_ot = VALUES(tong_gio_ot),
                                  gio_lam_dem = VALUES(gio_lam_dem),
                                  canh_bao_ot = VALUES(canh_bao_ot),
                                  updated_at = NOW()";

                $this->query($sqlUpsert, [
                    ':thang'     => $month,
                    ':nam'       => $year,
                    ':manv'      => $manv,
                    ':madv'      => $emp['MaDV'],
                    ':congtt'    => $congTT,
                    ':phep'      => $phepNam,
                    ':le'        => $leTet,
                    ':khongluong'=> (float)($agg['KhongLuong'] ?? 0),
                    ':om'        => (float)($agg['OmThaiSan'] ?? 0),
                    ':tongcong'  => $tongCong,
                    ':otthuong'  => $otThuong,
                    ':otnghi'    => $otNghi,
                    ':otle'      => $otLe,
                    ':tongot'    => $tongOT,
                    ':giodem'    => $gioDem,
                    ':dimuon'    => (int)($agg['DiMuonVeSomLan'] ?? 0),
                    ':quenquet'  => (int)($agg['QuenQuetLan'] ?? 0),
                    ':canhbao'   => $canhBaoOT
                ]);

                $processedCount++;
            }

            $this->commit();
            return $processedCount;
        } catch (Throwable $e) {
            $this->rollBack();
            throw $e;
        }
    }

    /**
     * 5.1. LẤY DANH SÁCH CHẤM CÔNG NGÀY THEO PHẠM VI scopeByUnit()
     */
    public function getAttendanceList(array $filters = [], int $limit = 25, int $offset = 0): array
    {
        $scope = $this->scopeByUnit('ns', 'MaDV');
        $whereClauses = [$scope['where']];
        $params = $scope['params'];

        // Lọc theo ngày
        if (!empty($filters['from_date'])) {
            $whereClauses[] = "cc.ngay >= :flt_fdate";
            $params[':flt_fdate'] = $filters['from_date'];
        }
        if (!empty($filters['to_date'])) {
            $whereClauses[] = "cc.ngay <= :flt_tdate";
            $params[':flt_tdate'] = $filters['to_date'];
        }

        // Lọc theo đơn vị (cây đơn vị đệ quy)
        if (!empty($filters['ma_dv']) && $filters['ma_dv'] !== 'ALL') {
            $descendants = $this->getDescendantUnits($filters['ma_dv']);
            $uKeys = [];
            foreach ($descendants as $idx => $u) {
                $k = ':u_att_' . $idx;
                $uKeys[] = $k;
                $params[$k] = $u;
            }
            $whereClauses[] = "ns.MaDV IN (" . implode(', ', $uKeys) . ")";
        }

        // Lọc theo mã nhân viên hoặc họ tên
        if (!empty($filters['ma_nv'])) {
            $whereClauses[] = "(cc.ma_nv LIKE :flt_q OR ns.HoTen LIKE :flt_q)";
            $params[':flt_q'] = '%' . $filters['ma_nv'] . '%';
        }

        // Lọc theo loại công
        if (!empty($filters['loai_cong'])) {
            $whereClauses[] = "cc.loai_cong = :flt_loaicong";
            $params[':flt_loaicong'] = $filters['loai_cong'];
        }

        $whereSql = implode(' AND ', $whereClauses);

        // Đếm tổng số bản ghi
        $countSql = "SELECT COUNT(*) as total 
                     FROM cham_cong_ngay cc 
                     JOIN nhansu ns ON cc.ma_nv = ns.MaNV 
                     WHERE {$whereSql}";
        $totalRow = $this->fetchOne($countSql, $params);
        $totalRecords = (int)($totalRow['total'] ?? 0);

        // Lấy danh sách phân trang
        $sql = "SELECT cc.*, ns.HoTen, ns.MaDV, dv.TenDV, cv.TenCV as ChucVu
                FROM cham_cong_ngay cc
                JOIN nhansu ns ON cc.ma_nv = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                WHERE {$whereSql}
                ORDER BY cc.ngay DESC, cc.ma_nv ASC
                LIMIT {$limit} OFFSET {$offset}";

        $rows = $this->fetchAll($sql, $params);

        return [
            'data'         => $rows,
            'total'        => $totalRecords,
            'limit'        => $limit,
            'offset'       => $offset,
            'current_page' => floor($offset / $limit) + 1,
            'total_pages'  => max(1, (int)ceil($totalRecords / $limit))
        ];
    }

    /**
     * 6. LẤY BẢNG CÔNG THÁNG THEO PHẠM VI scopeByUnit()
     */
    public function getMonthlyTimesheets(array $filters = [], int $limit = 30, int $offset = 0): array
    {
        $scope = $this->scopeByUnit('ns', 'MaDV');
        $whereClauses = [$scope['where']];
        $params = $scope['params'];

        $month = (int)($filters['thang'] ?? date('m'));
        $year  = (int)($filters['nam'] ?? date('Y'));

        $whereClauses[] = "bc.thang = :flt_m AND bc.nam = :flt_y";
        $params[':flt_m'] = $month;
        $params[':flt_y'] = $year;

        if (!empty($filters['ma_dv']) && $filters['ma_dv'] !== 'ALL') {
            $descendants = $this->getDescendantUnits($filters['ma_dv']);
            $uKeys = [];
            foreach ($descendants as $idx => $u) {
                $k = ':u_flt_' . $idx;
                $uKeys[] = $k;
                $params[$k] = $u;
            }
            $whereClauses[] = "bc.ma_dv IN (" . implode(', ', $uKeys) . ")";
        }

        if (!empty($filters['trang_thai'])) {
            $whereClauses[] = "bc.trang_thai = :flt_status";
            $params[':flt_status'] = $filters['trang_thai'];
        }

        $whereSql = implode(' AND ', $whereClauses);

        $countSql = "SELECT COUNT(*) as total 
                     FROM bang_cong_thang bc 
                     JOIN nhansu ns ON bc.ma_nv = ns.MaNV 
                     WHERE {$whereSql}";
        $totalRow = $this->fetchOne($countSql, $params);
        $totalRecords = (int)($totalRow['total'] ?? 0);

        $sql = "SELECT bc.*, ns.HoTen, ns.MaDV, dv.TenDV, cv.TenCV
                FROM bang_cong_thang bc
                JOIN nhansu ns ON bc.ma_nv = ns.MaNV
                LEFT JOIN dm_donvi dv ON bc.ma_dv = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                WHERE {$whereSql}
                ORDER BY bc.ma_dv ASC, ns.HoTen ASC
                LIMIT {$limit} OFFSET {$offset}";

        $rows = $this->fetchAll($sql, $params);

        return [
            'data'         => $rows,
            'total'        => $totalRecords,
            'limit'        => $limit,
            'offset'       => $offset,
            'current_page' => floor($offset / $limit) + 1,
            'total_pages'  => max(1, (int)ceil($totalRecords / $limit)),
            'thang'        => $month,
            'nam'          => $year
        ];
    }

    /**
     * 7. QUY TRÌNH DUYỆT 3 BƯỚC: Tổ Trưởng Xác Nhận -> Quản Đốc Duyệt -> Khóa Công
     */
    public function confirmByTeamLead(int $id, string $approverName): bool
    {
        $sql = "UPDATE bang_cong_thang 
                SET trang_thai = 'TO_TRUONG_XAC_NHAN', nguoi_xac_nhan = :approver, ngay_xac_nhan = NOW() 
                WHERE id = :id AND trang_thai = 'KHOI_TAO'";
        $stmt = $this->query($sql, [':approver' => $approverName, ':id' => $id]);
        $ok = $stmt->rowCount() > 0;
        if ($ok) {
            $this->logAudit('CONFIRM_TIMESHEET', 'bang_cong_thang', (string)$id, null, ['status' => 'TO_TRUONG_XAC_NHAN']);
        }
        return $ok;
    }

    public function approveByManager(int $id, string $approverName): bool
    {
        $sql = "UPDATE bang_cong_thang 
                SET trang_thai = 'QUAN_DOC_DUYET', nguoi_duyet = :approver, ngay_duyet = NOW() 
                WHERE id = :id AND trang_thai IN ('KHOI_TAO', 'TO_TRUONG_XAC_NHAN')";
        $stmt = $this->query($sql, [':approver' => $approverName, ':id' => $id]);
        $ok = $stmt->rowCount() > 0;
        if ($ok) {
            $this->logAudit('APPROVE_TIMESHEET', 'bang_cong_thang', (string)$id, null, ['status' => 'QUAN_DOC_DUYET']);
        }
        return $ok;
    }

    public function lockTimesheet(int $id, string $lockerName): bool
    {
        $sql = "UPDATE bang_cong_thang 
                SET trang_thai = 'KHOA_CONG', nguoi_khoa = :locker, ngay_khoa = NOW() 
                WHERE id = :id";
        $stmt = $this->query($sql, [':locker' => $lockerName, ':id' => $id]);
        $ok = $stmt->rowCount() > 0;
        if ($ok) {
            $this->logAudit('LOCK_TIMESHEET', 'bang_cong_thang', (string)$id, null, ['status' => 'KHOA_CONG']);
        }
        return $ok;
    }

    /**
     * KIỂM TRA BẢNG CÔNG ĐÃ KHÓA CHƯA (Chặn sửa sau khi khóa)
     */
    public function isTimesheetLocked(string $maNV, int $month, int $year): bool
    {
        $sql = "SELECT trang_thai FROM bang_cong_thang WHERE ma_nv = :manv AND thang = :m AND nam = :y LIMIT 1";
        $row = $this->fetchOne($sql, [':manv' => $maNV, ':m' => $month, ':y' => $year]);
        return ($row && $row['trang_thai'] === 'KHOA_CONG');
    }

    /**
     * 8. QUẢN LÝ PHIẾU ĐIỀU CHỈNH CÔNG SAU KHI ĐÃ KHÓA (Cần ghi rõ lý do)
     */
    public function createAdjustmentRequest(array $data): int
    {
        $maNV     = $data['ma_nv'];
        $ngayCong = $data['ngay_cong'];
        $thang    = (int)date('m', strtotime($ngayCong));
        $nam      = (int)date('Y', strtotime($ngayCong));

        if (empty($data['ly_do'])) {
            throw new InvalidArgumentException("Bắt buộc phải nhập lý do điều chỉnh công.");
        }

        $sql = "INSERT INTO dieu_chinh_cong 
                (ma_nv, ngay_cong, thang, nam, loai_dieu_chinh, gia_tri_cu, gia_tri_moi, ly_do, nguoi_de_xuat, trang_thai, created_at)
                VALUES 
                (:manv, :ngay, :thang, :nam, :loai, :cu, :moi, :lydo, :nguoi, 'CHO_DUYET', NOW())";

        $this->query($sql, [
            ':manv'  => $maNV,
            ':ngay'  => $ngayCong,
            ':thang' => $thang,
            ':nam'   => $nam,
            ':loai'  => $data['loai_dieu_chinh'] ?? 'BO_SUNG_GIO_QUET',
            ':cu'    => $data['gia_tri_cu'] ?? '',
            ':moi'   => $data['gia_tri_moi'],
            ':lydo'  => $data['ly_do'],
            ':nguoi' => Auth::username() ?? 'HR'
        ]);

        $id = (int)$this->lastInsertId();
        $this->logAudit('CREATE_ADJUSTMENT', 'dieu_chinh_cong', (string)$id, null, $data);
        return $id;
    }

    public function approveAdjustmentRequest(int $id, string $approverName): bool
    {
        $adj = $this->fetchOne("SELECT * FROM dieu_chinh_cong WHERE id = :id LIMIT 1", [':id' => $id]);
        if (!$adj || $adj['trang_thai'] !== 'CHO_DUYET') {
            return false;
        }

        $this->beginTransaction();
        try {
            // Cập nhật trạng thái phiếu điều chỉnh
            $this->query(
                "UPDATE dieu_chinh_cong SET trang_thai = 'DA_DUYET', nguoi_phe_duyet = :appr, ngay_phe_duyet = NOW() WHERE id = :id",
                [':appr' => $approverName, ':id' => $id]
            );

            // Cập nhật giá trị vào cham_cong_ngay
            if ($adj['loai_dieu_chinh'] === 'DIEU_CHINH_CONG_NGAY') {
                $this->query(
                    "UPDATE cham_cong_ngay SET cong_thuc_te = :val WHERE ma_nv = :manv AND ngay = :ngay",
                    [':val' => (float)$adj['gia_tri_moi'], ':manv' => $adj['ma_nv'], ':ngay' => $adj['ngay_cong']]
                );
            } elseif ($adj['loai_dieu_chinh'] === 'DIEU_CHINH_GIO_OT') {
                $this->query(
                    "UPDATE cham_cong_ngay SET gio_lam_them = :val WHERE ma_nv = :manv AND ngay = :ngay",
                    [':val' => (float)$adj['gia_tri_moi'], ':manv' => $adj['ma_nv'], ':ngay' => $adj['ngay_cong']]
                );
            }

            $this->logAudit('APPROVE_ADJUSTMENT', 'dieu_chinh_cong', (string)$id, $adj, ['status' => 'DA_DUYET']);
            $this->commit();
            return true;
        } catch (Throwable $e) {
            $this->rollBack();
            throw $e;
        }
    }
}
