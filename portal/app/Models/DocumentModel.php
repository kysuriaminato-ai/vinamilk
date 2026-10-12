<?php
/**
 * =====================================================================
 * VINAMILK HRM - MODEL QUẢN LÝ TÀI LIỆU SCAN (tai_lieu_nhan_su)
 * =====================================================================
 * Tuân thủ OWASP & Nghị định 13/2023:
 *   - Upload an toàn: PDF, JPG, PNG
 *   - Giới hạn kích thước tối đa 5MB (5,242,880 bytes)
 *   - Lưu trữ ngoài public/ (storage/uploads/documents)
 *   - Sinh tên file ngẫu nhiên (UUID/Hash) chống path traversal & RCE
 *   - Xác thực quyền sở hữu / scopeByUnit() khi tải tài liệu
 */

require_once __DIR__ . '/../core/Model.php';

class DocumentModel extends Model
{
    protected string $table = 'tai_lieu_nhan_su';
    protected string $primaryKey = 'id';

    public const MAX_FILE_SIZE = 5242880; // 5MB in bytes
    public const ALLOWED_MIME_TYPES = [
        'application/pdf' => 'pdf',
        'image/jpeg'      => 'jpg',
        'image/png'       => 'png'
    ];

    /**
     * Lấy danh sách tài liệu của một nhân viên
     */
    public function getDocumentsByEmployee(string $maNV): array
    {
        $sql = "SELECT tl.* 
                FROM tai_lieu_nhan_su tl
                WHERE tl.ma_nv = :manv
                ORDER BY tl.ngay_upload DESC";

        return $this->fetchAll($sql, [':manv' => $maNV]);
    }

    /**
     * Lấy thông tin tài liệu theo ID
     */
    public function getDocumentById(int $id): ?array
    {
        $sql = "SELECT tl.*, ns.HoTen, ns.MaDV
                FROM tai_lieu_nhan_su tl
                INNER JOIN nhansu ns ON tl.ma_nv = ns.MaNV
                WHERE tl.id = :id
                LIMIT 1";

        return $this->fetchOne($sql, [':id' => $id]);
    }

    /**
     * Lưu trữ tài liệu scan tải lên
     * 
     * @param string $maNV Mã nhân sự
     * @param array $file Mảng $_FILES['document']
     * @param string $tenTaiLieu Tên mô tả tài liệu
     * @param string $loaiTaiLieu Phân loại (CCCD_CMND, HOP_DONG_SCAN, ...)
     * @param string $ghiChu Ghi chú
     * @return int ID bản ghi vừa tạo
     * @throws Exception Khi file không hợp lệ hoặc quá dung lượng
     */
    public function uploadDocument(string $maNV, array $file, string $tenTaiLieu, string $loaiTaiLieu = 'KHAC', string $ghiChu = ''): int
    {
        // 1. Kiểm tra lỗi tải lên
        if (!isset($file['error']) || is_array($file['error']) || $file['error'] !== UPLOAD_ERR_OK) {
            throw new InvalidArgumentException("Lỗi tải file (Mã lỗi: " . ($file['error'] ?? 'Unknown') . ")");
        }

        // 2. Kiểm tra dung lượng tối đa 5MB
        $fileSize = (int)$file['size'];
        if ($fileSize > self::MAX_FILE_SIZE) {
            throw new InvalidArgumentException("Dung lượng file vượt quá giới hạn cho phép (Tối đa 5MB).");
        }

        // 3. Kiểm tra MIME Type thực tế từ nội dung file (Finfo)
        $finfo = new finfo(FILEINFO_MIME_TYPE);
        $mimeType = $finfo->file($file['tmp_name']);

        if (!array_key_exists($mimeType, self::ALLOWED_MIME_TYPES)) {
            throw new InvalidArgumentException("Định dạng file không được hỗ trợ ({$mimeType}). Chỉ chấp nhận file PDF, JPG, PNG.");
        }

        $extension = self::ALLOWED_MIME_TYPES[$mimeType];

        // 4. Chuẩn bị thư mục lưu trữ ngoài public
        $uploadDir = ROOT_DIR . DIRECTORY_SEPARATOR . 'storage' . DIRECTORY_SEPARATOR . 'uploads' . DIRECTORY_SEPARATOR . 'documents';
        if (!is_dir($uploadDir)) {
            mkdir($uploadDir, 0755, true);
        }

        // 5. Sinh tên file ngẫu nhiên chống trùng lặp và path traversal
        $randomName = 'DOC_' . bin2hex(random_bytes(12)) . '_' . time() . '.' . $extension;
        $destination = $uploadDir . DIRECTORY_SEPARATOR . $randomName;

        $saved = is_uploaded_file($file['tmp_name']) 
            ? move_uploaded_file($file['tmp_name'], $destination) 
            : @rename($file['tmp_name'], $destination);

        if (!$saved && !file_exists($destination)) {
            if (!@copy($file['tmp_name'], $destination)) {
                throw new RuntimeException("Không thể lưu file vào thư mục lưu trữ bảo mật.");
            }
        }

        // 6. Ghi bản ghi vào CSDL
        $sql = "INSERT INTO tai_lieu_nhan_su 
                (ma_nv, ten_tai_lieu, loai_tai_lieu, file_path, file_name_original, file_size, mime_type, ngay_upload, nguoi_upload, ghi_chu)
                VALUES 
                (:ma_nv, :ten_tai_lieu, :loai_tai_lieu, :file_path, :file_name_original, :file_size, :mime_type, NOW(), :nguoi_upload, :ghi_chu)";

        $originalName = basename($file['name']);
        $params = [
            ':ma_nv'              => $maNV,
            ':ten_tai_lieu'       => $tenTaiLieu,
            ':loai_tai_lieu'      => $loaiTaiLieu,
            ':file_path'          => 'storage/uploads/documents/' . $randomName,
            ':file_name_original' => $originalName,
            ':file_size'          => $fileSize,
            ':mime_type'          => $mimeType,
            ':nguoi_upload'       => Auth::username() ?? 'SYSTEM',
            ':ghi_chu'            => $ghiChu
        ];

        $this->query($sql, $params);
        $docId = (int)$this->lastInsertId();

        $this->logAudit('UPLOAD_DOC', 'tai_lieu_nhan_su', (string)$docId, null, [
            'ma_nv'        => $maNV,
            'ten_tai_lieu' => $tenTaiLieu,
            'file_name'    => $originalName,
            'file_size'    => $fileSize
        ]);

        return $docId;
    }

    /**
     * Xóa tài liệu scan
     */
    public function deleteDocument(int $id): bool
    {
        $doc = $this->getDocumentById($id);
        if (!$doc) {
            return false;
        }

        $fullPath = ROOT_DIR . DIRECTORY_SEPARATOR . str_replace('/', DIRECTORY_SEPARATOR, $doc['file_path']);
        if (file_exists($fullPath)) {
            @unlink($fullPath);
        }

        $this->query("DELETE FROM tai_lieu_nhan_su WHERE id = :id", [':id' => $id]);
        $this->logAudit('DELETE_DOC', 'tai_lieu_nhan_su', (string)$id, $doc, null);
        return true;
    }
}
