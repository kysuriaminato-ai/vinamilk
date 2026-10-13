<?php
/**
 * =====================================================================
 * VINAMILK HRM - THÔNG BÁO TẬP TRUNG (NOTIFICATION HELPER)
 * =====================================================================
 * Tải các thông báo nghiệp vụ:
 *   1. Hợp đồng lao động sắp hết hạn (trong vòng 30 ngày)
 *   2. Đơn từ / Tờ trình chờ phê duyệt
 *   3. Chứng chỉ / Thẻ xanh ATTP, ATLĐ sắp hết hạn (trong vòng 45 ngày)
 */

require_once __DIR__ . '/../core/Database.php';
require_once __DIR__ . '/Auth.php';

class NotificationHelper
{
    /**
     * Lấy toàn bộ danh sách thông báo phân theo nhóm
     */
    public static function getNotifications(): array
    {
        if (!Auth::check()) {
            return [
                'contracts'    => [],
                'requests'     => [],
                'certificates' => [],
                'total_count'  => 0
            ];
        }

        try {
            $db = Database::getInstance()->getConnection();

            // 1. Hợp đồng sắp hết hạn (trong vòng 30 ngày tới)
            $contractSql = "
                SELECT hd.id, hd.so_hd, hd.loai_hd, hd.den_ngay, hd.ma_nv, 
                       ns.HoTen, ns.MaDV, dv.TenDV,
                       DATEDIFF(hd.den_ngay, CURDATE()) as days_left
                FROM hop_dong_lao_dong hd
                JOIN nhansu ns ON hd.ma_nv = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                WHERE hd.trang_thai = 'HIEU_LUC'
                  AND hd.den_ngay IS NOT NULL
                  AND hd.den_ngay BETWEEN CURDATE() AND DATE_ADD(CURDATE(), INTERVAL 30 DAY)
                ORDER BY hd.den_ngay ASC
                LIMIT 5
            ";
            $contracts = $db->query($contractSql)->fetchAll(PDO::FETCH_ASSOC);

            // 2. Đơn từ / Tờ trình đang chờ duyệt
            $requestSql = "
                SELECT dt.id, dt.ma_don, dt.loai_don, dt.tieu_de, dt.created_at, dt.ma_nv,
                       ns.HoTen, ns.MaDV, dv.TenDV
                FROM don_tu dt
                JOIN nhansu ns ON dt.ma_nv = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                WHERE dt.trang_thai = 'CHO_DUYET'
                ORDER BY dt.created_at DESC
                LIMIT 5
            ";
            $requests = $db->query($requestSql)->fetchAll(PDO::FETCH_ASSOC);

            // 3. Chứng chỉ / Thẻ xanh sắp hết hạn (trong vòng 45 ngày tới)
            $certSql = "
                SELECT cc.id, cc.ten_chung_chi, cc.loai_chung_chi, cc.so_hieu_chung_chi, 
                       cc.ngay_het_han, cc.ma_nv, ns.HoTen, ns.MaDV, dv.TenDV,
                       DATEDIFF(cc.ngay_het_han, CURDATE()) as days_left
                FROM chung_chi_nhan_vien cc
                JOIN nhansu ns ON cc.ma_nv = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                WHERE cc.ngay_het_han IS NOT NULL
                  AND cc.ngay_het_han BETWEEN CURDATE() AND DATE_ADD(CURDATE(), INTERVAL 45 DAY)
                ORDER BY cc.ngay_het_han ASC
                LIMIT 5
            ";
            $certificates = $db->query($certSql)->fetchAll(PDO::FETCH_ASSOC);

            $totalCount = count($contracts) + count($requests) + count($certificates);

            return [
                'contracts'    => $contracts,
                'requests'     => $requests,
                'certificates' => $certificates,
                'total_count'  => $totalCount
            ];
        } catch (Throwable $e) {
            error_log('NotificationHelper Error: ' . $e->getMessage());
            return [
                'contracts'    => [],
                'requests'     => [],
                'certificates' => [],
                'total_count'  => 0
            ];
        }
    }
}
