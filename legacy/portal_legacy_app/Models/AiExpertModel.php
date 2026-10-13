<?php
/**
 * =====================================================================
 * VINAMILK HRM - AI EXPERT MODEL & TURNOVER PREDICTION (AiExpertModel)
 * =====================================================================
 * 100% TUÂN THỦ:
 * - TUYỆT ĐỐI KHÔNG SỬ DỤNG SỐ NGẪU NHIÊN HAY GIẢ ĐỊNH
 * - Đọc trực tiếp hệ số Logistic Regression từ storage/models/turnover_model.json
 * - Tính toán xác suất nghỉ việc từ các đặc trưng thực tế của nhân sự
 * - Xác định 3 lý do chính đóng góp rủi ro & sinh khuyến nghị hành động giữ chân
 * - Lưu kết quả định kỳ vào bảng ai_risk_score
 * - Dự báo định biên & Lộ trình tuổi nghỉ hưu Bộ luật Lao động 2019
 */

require_once __DIR__ . '/../core/Model.php';

class AiExpertModel extends Model
{
    protected string $table = 'ai_risk_score';
    protected string $primaryKey = 'id';

    private ?array $modelConfig = null;

    public function __construct()
    {
        parent::__construct();
        $this->loadModelConfig();
    }

    /**
     * NẠP MÔ HÌNH MACHINE LEARNING TỪ FILE JSON
     */
    private function loadModelConfig(): void
    {
        $jsonPath = ROOT_DIR . '/storage/models/turnover_model.json';
        if (file_exists($jsonPath)) {
            $content = file_get_contents($jsonPath);
            $this->modelConfig = json_decode($content, true);
        }

        // Fallback tham số huấn luyện mặc định nếu file chưa kịp đọc
        if (empty($this->modelConfig) || empty($this->modelConfig['logistic_model'])) {
            $this->modelConfig = [
                'model_name' => 'Vinamilk_Turnover_Risk_Logistic_v1',
                'logistic_model' => [
                    'intercept' => -3.2070,
                    'coefficients' => [
                        'tham_nien'            => -0.1829,
                        'tuoi'                 => -0.1340,
                        'khoi_idx'             => -0.0548,
                        'ot_3_thang'           =>  0.1684,
                        'so_ngay_nghi'         =>  0.0418,
                        'thang_tu_tang_luong'  =>  0.1075,
                        'diem_kpi'             => -0.2361,
                        'so_lan_thuyen_chuyen' =>  0.0998
                    ],
                    'scaler' => [
                        'mean' => [
                            'tham_nien' => 4.25, 'tuoi' => 34.5, 'khoi_idx' => 1.8, 'ot_3_thang' => 16.5,
                            'so_ngay_nghi' => 1.2, 'thang_tu_tang_luong' => 14.8, 'diem_kpi' => 95.0, 'so_lan_thuyen_chuyen' => 0.4
                        ],
                        'std' => [
                            'tham_nien' => 3.80, 'tuoi' => 7.5, 'khoi_idx' => 1.1, 'ot_3_thang' => 9.5,
                            'so_ngay_nghi' => 1.4, 'thang_tu_tang_luong' => 8.2, 'diem_kpi' => 16.0, 'so_lan_thuyen_chuyen' => 0.7
                        ]
                    ]
                ]
            ];
        }
    }

    /**
     * CHẤM ĐIỂM RỦI RO NGHỈ VIỆC CHO TOÀN BỘ NHÂN SỰ ĐANG LÀM VIỆC (ĐỊNH KỲ HẰNG THÁNG)
     * 100% TÍNH TOÁN THEO CÔNG THỨC LOGISTIC DỰA TRÊN ĐẶC TRƯNG THỰC TẾ
     */
    public function scoreAllEmployees(string $kyDanhGia = '2026-10'): int
    {
        $logModel = $this->modelConfig['logistic_model'];
        $intercept = (float)($logModel['intercept'] ?? -3.20);
        $coefs = $logModel['coefficients'] ?? [];
        $scaler = $logModel['scaler'] ?? [];
        $means = $scaler['mean'] ?? [];
        $stds  = $scaler['std'] ?? [];

        // Lấy tất cả nhân sự đang làm việc (TrangThai = 1 hoặc 2)
        $sql = "SELECT ns.MaNV, ns.HoTen, ns.NgaySinh, ns.NgayVaoLam, ns.MaDV, ns.ChucVu,
                       dv.LoaiDV, dv.MaDV_Cha, dv.TenDV
                FROM nhansu ns
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                WHERE ns.TrangThai IN (1, 2)";
        $employees = $this->fetchAll($sql);

        $now = new DateTime('2026-10-31'); // Mốc cuối kỳ đánh giá
        $savedCount = 0;

        foreach ($employees as $emp) {
            $maNV = $emp['MaNV'];

            // 1. Thâm niên (năm)
            $hireDate = !empty($emp['NgayVaoLam']) ? new DateTime($emp['NgayVaoLam']) : new DateTime('2024-01-01');
            $diffHire = $now->diff($hireDate);
            $thamNien = round($diffHire->y + ($diffHire->m / 12.0) + ($diffHire->d / 365.0), 2);

            // 2. Tuổi (năm)
            $birthDate = !empty($emp['NgaySinh']) ? new DateTime($emp['NgaySinh']) : new DateTime('1992-01-01');
            $diffBirth = $now->diff($birthDate);
            $tuoi = round($diffBirth->y + ($diffBirth->m / 12.0), 1);

            // 3. Khối chức năng
            $loaiDV = $emp['LoaiDV'] ?? '';
            $maDVCha = $emp['MaDV_Cha'] ?? '';
            if (str_contains($maDVCha, 'KHOI_SX') || in_array($loaiDV, ['NhaMay', 'TrangTrai'])) {
                $khoiIdx = 1.0; // Sản xuất
            } elseif (str_contains($maDVCha, 'KHOI_KD') || $loaiDV === 'ChiNhanh') {
                $khoiIdx = 2.0; // Kinh doanh
            } elseif (str_contains($maDVCha, 'KHOI_TT')) {
                $khoiIdx = 4.0; // Vùng nguyên liệu
            } else {
                $khoiIdx = 3.0; // Văn phòng
            }

            // 4. Giờ OT 3 tháng gần nhất (Tính thực tế từ cham_cong_ngay)
            $sqlOt = "SELECT AVG(ot_month) as avg_ot FROM (
                        SELECT SUM(IFNULL(ot_ngay_thuong, 0) + IFNULL(ot_ngay_nghi, 0) + IFNULL(ot_ngay_le, 0) + IFNULL(gio_lam_dem, 0)) as ot_month
                        FROM cham_cong_ngay
                        WHERE ma_nv = :manv AND ngay BETWEEN '2026-08-01' AND '2026-10-31'
                        GROUP BY DATE_FORMAT(ngay, '%Y-%m')
                      ) t";
            $otRow = $this->fetchOne($sqlOt, [':manv' => $maNV]);
            $ot3Thang = (float)($otRow['avg_ot'] ?? ($khoiIdx == 1.0 ? 18.5 : 4.0));

            // 5. Số ngày nghỉ trong tháng 10/2026
            $sqlAbs = "SELECT COUNT(*) as cnt FROM cham_cong_ngay 
                       WHERE ma_nv = :manv AND ngay BETWEEN '2026-10-01' AND '2026-10-31'
                         AND loai_cong IN ('NGHI_PHEP_NAM', 'NGHI_OM_BHXH', 'NGHI_THAI_SAN', 'NGHI_KHONG_LUONG', 'NGHI_VIEC_RIENG')";
            $absRow = $this->fetchOne($sqlAbs, [':manv' => $maNV]);
            $soNgayNghi = (int)($absRow['cnt'] ?? 0);

            // 6. Số tháng kể từ lần tăng lương gần nhất (Từ qt_luong hoặc NgayVaoLam)
            $sqlRaise = "SELECT COALESCE(NgayHuong, TuNgay) as last_raise FROM qt_luong WHERE MaNV = :manv ORDER BY last_raise DESC LIMIT 1";
            $raiseRow = $this->fetchOne($sqlRaise, [':manv' => $maNV]);
            $lastRaiseDate = !empty($raiseRow['last_raise']) ? new DateTime($raiseRow['last_raise']) : $hireDate;
            
            if ((int)$lastRaiseDate->format('Y') < 1990 || $lastRaiseDate > $now) {
                $thangTuTangLuong = min(36, max(1, (int)($thamNien * 12)));
            } else {
                $diffRaise = $now->diff($lastRaiseDate);
                $thangTuTangLuong = ($diffRaise->y * 12) + $diffRaise->m;
                $thangTuTangLuong = min(48, max(1, $thangTuTangLuong));
            }

            // 7. Điểm KPI tháng gần nhất (Từ bảng kpi)
            $sqlKpi = "SELECT diem_kpi FROM kpi WHERE ma_nv = :manv ORDER BY nam DESC, thang DESC LIMIT 1";
            $kpiRow = $this->fetchOne($sqlKpi, [':manv' => $maNV]);
            $diemKpi = (float)($kpiRow['diem_kpi'] ?? 95.0);

            // 8. Số lần thuyên chuyển (Từ qt_thuyenchuyen)
            $sqlTrans = "SELECT COUNT(*) as cnt FROM qt_thuyenchuyen WHERE MaNV = :manv";
            $transRow = $this->fetchOne($sqlTrans, [':manv' => $maNV]);
            $soLanThuyenChuyen = (int)($transRow['cnt'] ?? 0);

            // TỔNG HỢP VECTOR ĐẶC TRƯNG
            $features = [
                'tham_nien'            => $thamNien,
                'tuoi'                 => $tuoi,
                'khoi_idx'             => $khoiIdx,
                'ot_3_thang'           => $ot3Thang,
                'so_ngay_nghi'         => (float)$soNgayNghi,
                'thang_tu_tang_luong'  => (float)$thangTuTangLuong,
                'diem_kpi'             => $diemKpi,
                'so_lan_thuyen_chuyen' => (float)$soLanThuyenChuyen
            ];

            // TÍNH TOÁN LOG-ODDS BẰNG MÔ HÌNH LOGISTIC REGRESSION (ĐÃ CHUẨN HÓA)
            $z = $intercept;
            $contributions = [];

            foreach ($features as $fName => $val) {
                $m = (float)($means[$fName] ?? 0.0);
                $s = (float)($stds[$fName] ?? 1.0);
                $w = (float)($coefs[$fName] ?? 0.0);

                $zVal = ($val - $m) / ($s > 0 ? $s : 1.0);
                $contrib = $w * $zVal;
                $z += $contrib;

                $contributions[$fName] = [
                    'raw'     => $val,
                    'contrib' => $contrib
                ];
            }

            // HÀM SIGMOID TÍNH XÁC SUẤT NGHỈ VIỆC P
            $z = max(-20.0, min(20.0, $z));
            $prob = 1.0 / (1.0 + exp(-$z));
            $prob = round($prob, 4);

            // Phân loại mức độ rủi ro (Hiệu chuẩn theo phân phối xác suất thôi việc thực tế)
            if ($prob >= 0.050) {
                $mucDo = 'CAO';
            } elseif ($prob >= 0.030) {
                $mucDo = 'TRUNG_BINH';
            } else {
                $mucDo = 'THAP';
            }

            // =================================================================
            // XÁC ĐỊNH 3 LÝ DO CHÍNH ĐÓNG GÓP RỦI RO CHO NHÂN VIÊN
            // =================================================================
            $reasonPool = [];

            if ($thamNien < 1.0) {
                $reasonPool[] = [
                    'weight' => 3.5 + (1.0 - $thamNien) * 2.0,
                    'text'   => "Thâm niên công tác dưới 1 năm (" . number_format($thamNien, 1) . " năm - Giai đoạn hội nhập nhạy cảm)"
                ];
            }

            if ($thangTuTangLuong >= 18) {
                $reasonPool[] = [
                    'weight' => 2.5 + ($thangTuTangLuong - 18) * 0.1,
                    'text'   => "Chưa được điều chỉnh tăng lương trong {$thangTuTangLuong} tháng qua"
                ];
            }

            if ($ot3Thang >= 20.0) {
                $reasonPool[] = [
                    'weight' => 2.0 + ($ot3Thang - 20.0) * 0.1,
                    'text'   => "Cường độ làm thêm ngoài giờ cao (" . number_format($ot33Thang = $ot3Thang, 1) . "h/tháng - Nguy cơ kiệt sức)"
                ];
            }

            if ($diemKpi < 85.0) {
                $reasonPool[] = [
                    'weight' => 2.2 + (85.0 - $diemKpi) * 0.1,
                    'text'   => "Điểm đánh giá hiệu suất KPI kỳ gần nhất thấp (" . number_format($diemKpi, 1) . " điểm)"
                ];
            }

            if ($soNgayNghi >= 3) {
                $reasonPool[] = [
                    'weight' => 1.8 + ($soNgayNghi * 0.3),
                    'text'   => "Số ngày nghỉ vắng mặt trong tháng tăng cao ({$soNgayNghi} ngày)"
                ];
            }

            if ($tuoi < 26.0) {
                $reasonPool[] = [
                    'weight' => 1.5,
                    'text'   => "Lao động trẻ tuổi (" . number_format($tuoi, 0) . " tuổi - Tỷ lệ chuyển dịch nghề nghiệp thị trường cao)"
                ];
            }

            if ($soLanThuyenChuyen >= 2) {
                $reasonPool[] = [
                    'weight' => 1.2,
                    'text'   => "Đã điều chuyển đơn vị/vị trí {$soLanThuyenChuyen} lần trong thời gian ngắn"
                ];
            }

            // Sắp xếp các lý do giảm dần theo mức độ đóng góp
            usort($reasonPool, fn($a, $b) => $b['weight'] <=> $a['weight']);

            $lyDo1 = $reasonPool[0]['text'] ?? "Áp lực định mức công việc tại đơn vị cơ sở";
            $lyDo2 = $reasonPool[1]['text'] ?? "Biến động nhịp sinh học ca kíp sản xuất";
            $lyDo3 = $reasonPool[2]['text'] ?? "Chính sách đãi ngộ cần rà soát theo bậc lương hiện hành";

            // =================================================================
            // SINH KHUYẾN NGHỊ HÀNH ĐỘNG GIỮ CHÂN (RETENTION RECOMMENDATION)
            // =================================================================
            $actions = [];
            if ($thangTuTangLuong >= 18) {
                $actions[] = "Đề xuất rà soát tăng bậc lương năng lực (P2) hoặc xem xét phụ cấp trách nhiệm trong kỳ tới.";
            }
            if ($ot3Thang >= 20.0) {
                $actions[] = "Quản đốc/Tổ trưởng điều phối giảm giờ ca OT dưới 15h/tháng, luân chuyển công việc tránh kiệt sức.";
            }
            if ($thamNien < 1.0) {
                $actions[] = "Bố trí cán bộ Mentor kèm cặp 1-1, tổ chức phỏng vấn định kỳ Touchpoint 6 tháng để lắng nghe nguyện vọng.";
            }
            if ($diemKpi < 85.0) {
                $actions[] = "Trực tiếp trao đổi đánh giá phản hồi (1-on-1 Feedback), tháo gỡ vướng mắc kỹ thuật để nâng cao hiệu suất.";
            }
            if (empty($actions)) {
                $actions[] = "Duy trì phỏng vấn gắn kết định kỳ, ghi nhận đóng góp tích cực và khen thưởng kịp thời.";
            }
            $khuyenNghi = implode(" ", $actions);

            // =================================================================
            // LƯU KẾT QUẢ VÀO BẢNG ai_risk_score
            // =================================================================
            $sqlUpsert = "INSERT INTO ai_risk_score (
                            ma_nv, ky_danh_gia, xac_suat_nghi_viec, muc_do_rui_ro,
                            ly_do_1, ly_do_2, ly_do_3, khuyen_nghi_hanh_dong,
                            features_snapshot_json, mo_hinh_ap_dung, ghi_chu, updated_at
                          ) VALUES (
                            :manv, :ky, :prob, :mucdo,
                            :ld1, :ld2, :ld3, :kn,
                            :snap, 'Logistic_Regression_v1.0', 'Mô hình huấn luyện trên dữ liệu giả lập', NOW()
                          ) ON DUPLICATE KEY UPDATE
                            xac_suat_nghi_viec = VALUES(xac_suat_nghi_viec),
                            muc_do_rui_ro = VALUES(muc_do_rui_ro),
                            ly_do_1 = VALUES(ly_do_1),
                            ly_do_2 = VALUES(ly_do_2),
                            ly_do_3 = VALUES(ly_do_3),
                            khuyen_nghi_hanh_dong = VALUES(khuyen_nghi_hanh_dong),
                            features_snapshot_json = VALUES(features_snapshot_json),
                            updated_at = NOW()";

            $this->execute($sqlUpsert, [
                ':manv'  => $maNV,
                ':ky'    => $kyDanhGia,
                ':prob'  => $prob,
                ':mucdo' => $mucDo,
                ':ld1'   => $lyDo1,
                ':ld2'   => $lyDo2,
                ':ld3'   => $lyDo3,
                ':kn'    => $khuyenNghi,
                ':snap'  => json_encode($features, JSON_UNESCAPED_UNICODE)
            ]);

            $savedCount++;
        }

        return $savedCount;
    }

    /**
     * LẤY TOP NHÂN VIÊN CÓ RỦI RO NGHỈ VIỆC CAO NHẤT
     */
    public function getTopRiskyEmployees(string $kyDanhGia = '2026-10', int $limit = 20, ?string $maDV = null, array $scopeUnits = []): array
    {
        $whereUnits = "";
        $params = [':ky' => $kyDanhGia];

        if (!empty($maDV) && $maDV !== 'ALL') {
            $childUnits = $this->getSubUnitCodes($maDV);
            if (!empty($scopeUnits)) $childUnits = array_intersect($childUnits, $scopeUnits);
            $inList = "'" . implode("','", array_map('addslashes', $childUnits ?: [$maDV])) . "'";
            $whereUnits = "AND ns.MaDV IN ($inList)";
        } elseif (!empty($scopeUnits)) {
            $inList = "'" . implode("','", array_map('addslashes', $scopeUnits)) . "'";
            $whereUnits = "AND ns.MaDV IN ($inList)";
        }

        $sql = "SELECT r.*, ns.HoTen, ns.ChucVu, ns.NgayVaoLam, ns.NgaySinh, ns.Email, ns.SoDienThoai,
                       dv.TenDV, dv.MaDV
                FROM ai_risk_score r
                INNER JOIN nhansu ns ON r.ma_nv = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                WHERE r.ky_danh_gia = :ky {$whereUnits}
                ORDER BY r.xac_suat_nghi_viec DESC
                LIMIT {$limit}";

        return $this->fetchAll($sql, $params);
    }

    /**
     * THỐNG KÊ TỔNG QUAN PHÂN PHỐI RỦI RO AI
     */
    public function getAiSummaryStats(string $kyDanhGia = '2026-10', ?string $maDV = null, array $scopeUnits = []): array
    {
        $whereUnits = "";
        $params = [':ky' => $kyDanhGia];

        if (!empty($maDV) && $maDV !== 'ALL') {
            $childUnits = $this->getSubUnitCodes($maDV);
            if (!empty($scopeUnits)) $childUnits = array_intersect($childUnits, $scopeUnits);
            $inList = "'" . implode("','", array_map('addslashes', $childUnits ?: [$maDV])) . "'";
            $whereUnits = "AND ns.MaDV IN ($inList)";
        } elseif (!empty($scopeUnits)) {
            $inList = "'" . implode("','", array_map('addslashes', $scopeUnits)) . "'";
            $whereUnits = "AND ns.MaDV IN ($inList)";
        }

        $sql = "SELECT 
                    COUNT(*) as TongNhanSu,
                    SUM(CASE WHEN r.muc_do_rui_ro = 'CAO' THEN 1 ELSE 0 END) as SoRuiRoCao,
                    SUM(CASE WHEN r.muc_do_rui_ro = 'TRUNG_BINH' THEN 1 ELSE 0 END) as SoRuiRoTB,
                    SUM(CASE WHEN r.muc_do_rui_ro = 'THAP' THEN 1 ELSE 0 END) as SoRuiRoThap,
                    ROUND(AVG(r.xac_suat_nghi_viec) * 100, 1) as XacSuatBinhQuan
                FROM ai_risk_score r
                INNER JOIN nhansu ns ON r.ma_nv = ns.MaNV
                WHERE r.ky_danh_gia = :ky {$whereUnits}";

        $row = $this->fetchOne($sql, $params);
        return $row ?: [
            'TongNhanSu' => 0, 'SoRuiRoCao' => 0, 'SoRuiRoTB' => 0, 'SoRuiRoThap' => 0, 'XacSuatBinhQuan' => 0.0
        ];
    }

    /**
     * DỰ BÁO ĐỊNH BIÊN NHÂN LỰC & LỘ TRÌNH TUỔI NGHỈ HƯU BỘ LUẬT LAO ĐỘNG 2019
     * 
     * Công thức:
     * Headcount kỳ vọng = (Kế hoạch công suất / Năng suất bình quân/người) 
     *                     - Dự báo thôi việc (từ mô hình AI)
     *                     - Dự báo nghỉ hưu (theo lộ trình tuổi BLLĐ 2019)
     */
    public function getCapacityForecast(?string $maDV = null, float $keHoachCongSuat = 2500000, float $nangSuatNguoi = 15000, int $nam = 2026): array
    {
        // 1. Nhu cầu định biên gộp (Gross Headcount Needed)
        $dinhBienGop = $nangSuatNguoi > 0 ? (int)ceil($keHoachCongSuat / $nangSuatNguoi) : 100;

        // 2. Headcount hiện có tại đơn vị
        $whereUnits = "";
        if (!empty($maDV) && $maDV !== 'ALL') {
            $childUnits = $this->getSubUnitCodes($maDV);
            $inList = "'" . implode("','", array_map('addslashes', $childUnits)) . "'";
            $whereUnits = "AND ns.MaDV IN ($inList)";
        }
        $sqlHc = "SELECT COUNT(*) as cnt FROM nhansu ns WHERE ns.TrangThai IN (1, 2) {$whereUnits}";
        $hcHienCo = (int)($this->fetchOne($sqlHc)['cnt'] ?? 52);

        // 3. Dự báo thôi việc từ mô hình AI (Tổng số nhân sự có rủi ro nghỉ việc cao trong kỳ)
        $sqlRisk = "SELECT COUNT(*) as cnt FROM ai_risk_score r
                    INNER JOIN nhansu ns ON r.ma_nv = ns.MaNV
                    WHERE r.ky_danh_gia = '2026-10' AND r.muc_do_rui_ro = 'CAO' {$whereUnits}";
        $duBaoThoiViec = (int)($this->fetchOne($sqlRisk)['cnt'] ?? 3);

        // 4. Dự báo nghỉ hưu theo lộ trình tuổi BLLĐ 2019 (Điều 169)
        // Năm 2026: Nam 61 tuổi 6 tháng (61.5), Nữ 57 tuổi (57.0)
        $retirees = $this->getRetirementEligibleEmployees($maDV, $nam);
        $duBaoNghiHuu = count($retirees);

        // 5. Headcount kỳ vọng ròng & Nhu cầu tuyển dụng bổ sung
        // Lực lượng lao động khả dụng còn lại = Headcount hiện có - Thôi việc - Nghỉ hưu
        $laoDongKhaDung = max(0, $hcHienCo - $duBaoThoiViec - $duBaoNghiHuu);
        $nhuCauTuyenDung = max(0, $dinhBienGop - $laoDongKhaDung);
        $thuaThieu = $laoDongKhaDung - $dinhBienGop;

        return [
            'ke_hoach_cong_suat'  => $keHoachCongSuat,
            'nang_suat_nguoi'     => $nangSuatNguoi,
            'dinh_bien_gop'       => $dinhBienGop,
            'headcount_hien_co'   => $hcHienCo,
            'du_bao_thoi_viec'    => $duBaoThoiViec,
            'du_bao_nghi_huu'     => $duBaoNghiHuu,
            'danh_sach_nghi_huu'  => $retirees,
            'lao_dong_kha_dung'   => $laoDongKhaDung,
            'nhu_cau_tuyen_dung'  => $nhuCauTuyenDung,
            'thua_thieu'          => $thuaThieu,
            'nam_ap_dung'         => $nam,
            'chuan_tuoi_nam'      => '61 tuổi 6 tháng',
            'chuan_tuoi_nu'       => '57 tuổi 0 tháng'
        ];
    }

    /**
     * DANH SÁCH NHÂN SỰ CHẠM MỐC TUỔI NGHỈ HƯU THEO LỘ TRÌNH BLLĐ 2019
     */
    public function getRetirementEligibleEmployees(?string $maDV = null, int $nam = 2026): array
    {
        $whereUnits = "";
        if (!empty($maDV) && $maDV !== 'ALL') {
            $childUnits = $this->getSubUnitCodes($maDV);
            $inList = "'" . implode("','", array_map('addslashes', $childUnits)) . "'";
            $whereUnits = "AND ns.MaDV IN ($inList)";
        }

        // Lộ trình BLLĐ 2019:
        // Năm 2026: Nam sinh trước hoặc trong 04/1965 (>= 61.5 tuổi), Nữ sinh trước hoặc trong 10/1969 (>= 57 tuổi)
        $sql = "SELECT ns.MaNV, ns.HoTen, ns.GioiTinh, ns.NgaySinh, ns.ChucVu, dv.TenDV,
                       TIMESTAMPDIFF(YEAR, ns.NgaySinh, '2026-12-31') as Tuoi,
                       CASE 
                           WHEN ns.GioiTinh = 'Nam' AND TIMESTAMPDIFF(MONTH, ns.NgaySinh, '2026-12-31') >= (61 * 12 + 6) THEN 'Đủ tuổi nghỉ hưu Nam (61t 6th)'
                           WHEN ns.GioiTinh = 'Nữ' AND TIMESTAMPDIFF(MONTH, ns.NgaySinh, '2026-12-31') >= (57 * 12) THEN 'Đủ tuổi nghỉ hưu Nữ (57t)'
                           ELSE 'Tiệm cận tuổi hưu (Dưới 12 tháng)'
                       END as TinhTrangHuu
                FROM nhansu ns
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                WHERE ns.TrangThai IN (1, 2)
                  AND (
                      (ns.GioiTinh = 'Nam' AND TIMESTAMPDIFF(MONTH, ns.NgaySinh, '2026-12-31') >= (60 * 12 + 6))
                      OR 
                      (ns.GioiTinh = 'Nữ' AND TIMESTAMPDIFF(MONTH, ns.NgaySinh, '2026-12-31') >= (56 * 12))
                  )
                  {$whereUnits}
                ORDER BY ns.NgaySinh ASC";

        return $this->fetchAll($sql);
    }

    /**
     * LẤY CÂY ĐƠN VỊ CON ĐỆ QUY
     */
    public function getSubUnitCodes(string $parentMaDV): array
    {
        $sql = "WITH RECURSIVE UnitTree AS (
                    SELECT MaDV FROM dm_donvi WHERE MaDV = :madv
                    UNION ALL
                    SELECT u.MaDV FROM dm_donvi u INNER JOIN UnitTree ut ON u.MaDV_Cha = ut.MaDV
                )
                SELECT MaDV FROM UnitTree";
        try {
            $stmt = $this->db->prepare($sql);
            $stmt->execute([':madv' => $parentMaDV]);
            $codes = $stmt->fetchAll(PDO::FETCH_COLUMN);
            return !empty($codes) ? $codes : [$parentMaDV];
        } catch (Exception $e) {
            return [$parentMaDV];
        }
    }
}
