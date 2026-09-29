<?php
/**
 * =====================================================================
 * VINAMILK HRM - Database Connection (Singleton Pattern)
 * =====================================================================
 * 
 * Lớp kết nối CSDL MySQL sử dụng PDO với Singleton Pattern.
 * Đảm bảo chỉ có DUY NHẤT 1 instance kết nối trong toàn bộ ứng dụng.
 * 
 * Cách sử dụng:
 *   $db = Database::getInstance();
 *   $pdo = $db->getConnection();
 * 
 * Hoặc shorthand:
 *   $pdo = Database::getInstance()->getConnection();
 * 
 * @package     VinamilkHRM
 * @subpackage  Core
 * @version     1.0
 * @author      Vinamilk HRM Development Team
 * @created     2026-09-28
 * =====================================================================
 */

declare(strict_types=1);

class Database
{
    // =====================================================================
    // CẤU HÌNH KẾT NỐI CSDL
    // =====================================================================
    
    /** @var string Hostname máy chủ MySQL */
    private const DB_HOST = 'localhost';

    /** @var int Cổng kết nối MySQL (mặc định 3306) */
    private const DB_PORT = 3306;

    /** @var string Tên cơ sở dữ liệu */
    private const DB_NAME = 'vinamilk_hrm';

    /** @var string Tên đăng nhập MySQL */
    private const DB_USER = 'root';

    /** @var string Mật khẩu MySQL (để trống nếu dùng XAMPP/WAMP mặc định) */
    private const DB_PASS = '';

    /** @var string Bộ ký tự kết nối */
    private const DB_CHARSET = 'utf8mb4';

    // =====================================================================
    // SINGLETON INSTANCE
    // =====================================================================

    /** @var Database|null Instance duy nhất của class Database */
    private static ?Database $instance = null;

    /** @var PDO|null Đối tượng kết nối PDO */
    private ?PDO $connection = null;

    /** @var string DSN (Data Source Name) cho PDO */
    private string $dsn;

    /** @var array<int, mixed> Các tùy chọn PDO */
    private array $options;

    // =====================================================================
    // CONSTRUCTOR (PRIVATE - chặn khởi tạo từ bên ngoài)
    // =====================================================================

    /**
     * Constructor - Thiết lập kết nối PDO tới MySQL.
     * Private để đảm bảo Singleton Pattern.
     * 
     * @throws PDOException Khi không thể kết nối đến CSDL
     */
    private function __construct()
    {
        // Xây dựng DSN
        $this->dsn = sprintf(
            'mysql:host=%s;port=%d;dbname=%s;charset=%s',
            self::DB_HOST,
            self::DB_PORT,
            self::DB_NAME,
            self::DB_CHARSET
        );

        // Cấu hình PDO options
        $this->options = [
            // Chế độ báo lỗi: ném Exception khi có lỗi SQL
            PDO::ATTR_ERRMODE            => PDO::ERRMODE_EXCEPTION,

            // Chế độ fetch mặc định: trả về mảng kết hợp (associative array)
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,

            // Tắt giả lập Prepared Statement (dùng native prepared statement của MySQL)
            // Giúp bảo vệ tốt hơn trước SQL Injection
            PDO::ATTR_EMULATE_PREPARES   => false,

            // Tắt chế độ persistent connection
            PDO::ATTR_PERSISTENT         => false,

            // Timeout kết nối (giây)
            PDO::ATTR_TIMEOUT            => 5,

            // Yêu cầu MySQL trả kiểu dữ liệu gốc (int, float thay vì string)
            PDO::ATTR_STRINGIFY_FETCHES  => false,
        ];

        // Thực hiện kết nối
        $this->connect();
    }

    // =====================================================================
    // CHẶN CLONE & UNSERIALIZE (bảo vệ Singleton)
    // =====================================================================

    /**
     * Chặn clone instance.
     */
    private function __clone(): void
    {
        // Không cho phép clone
    }

    /**
     * Chặn unserialize instance.
     * 
     * @throws \RuntimeException
     */
    public function __wakeup(): void
    {
        throw new \RuntimeException('Không thể unserialize Singleton Database.');
    }

    // =====================================================================
    // KẾT NỐI & SINGLETON
    // =====================================================================

    /**
     * Thiết lập kết nối PDO tới MySQL.
     * 
     * @return void
     * @throws \RuntimeException Khi kết nối thất bại
     */
    private function connect(): void
    {
        try {
            $this->connection = new PDO(
                $this->dsn,
                self::DB_USER,
                self::DB_PASS,
                $this->options
            );

            // Thiết lập timezone cho MySQL session
            $this->connection->exec("SET time_zone = '+07:00'");

            // Đảm bảo sử dụng utf8mb4
            $this->connection->exec("SET NAMES 'utf8mb4' COLLATE 'utf8mb4_unicode_ci'");

        } catch (PDOException $e) {
            // Log lỗi (trong production, ghi vào file log thay vì hiển thị)
            error_log(sprintf(
                '[Vinamilk HRM] Lỗi kết nối CSDL: %s | File: %s | Line: %d',
                $e->getMessage(),
                $e->getFile(),
                $e->getLine()
            ));

            // Trong môi trường development, hiển thị lỗi chi tiết
            if ($this->isDevelopment()) {
                throw new \RuntimeException(
                    'Kết nối CSDL thất bại: ' . $e->getMessage(),
                    (int) $e->getCode(),
                    $e
                );
            }

            // Trong production, hiển thị thông báo chung
            throw new \RuntimeException(
                'Hệ thống đang bảo trì. Vui lòng thử lại sau.',
                500
            );
        }
    }

    /**
     * Lấy instance duy nhất của Database (Singleton).
     * 
     * @return self Instance Database
     * 
     * @example
     * ```php
     * $db = Database::getInstance();
     * $pdo = $db->getConnection();
     * 
     * $stmt = $pdo->prepare("SELECT * FROM nhansu WHERE MaNV = :maNV");
     * $stmt->execute([':maNV' => 'VNM-0001']);
     * $employee = $stmt->fetch();
     * ```
     */
    public static function getInstance(): self
    {
        if (self::$instance === null) {
            self::$instance = new self();
        }
        return self::$instance;
    }

    /**
     * Lấy đối tượng PDO connection.
     * 
     * @return PDO Đối tượng PDO đang kết nối
     */
    public function getConnection(): PDO
    {
        // Kiểm tra và tái kết nối nếu connection bị mất
        if ($this->connection === null) {
            $this->connect();
        }
        return $this->connection;
    }

    // =====================================================================
    // CÁC PHƯƠNG THỨC TIỆN ÍCH (HELPER METHODS)
    // =====================================================================

    /**
     * Thực thi câu truy vấn SELECT với Prepared Statement.
     * 
     * @param string $sql Câu SQL có placeholder
     * @param array<string, mixed> $params Mảng tham số [':key' => value]
     * @return array<int, array<string, mixed>> Mảng kết quả
     * 
     * @example
     * ```php
     * $employees = Database::getInstance()->query(
     *     "SELECT * FROM nhansu WHERE MaDV = :maDV AND TrangThai = :tt",
     *     [':maDV' => 'PB_NHANSU', ':tt' => 1]
     * );
     * ```
     */
    public function query(string $sql, array $params = []): array
    {
        try {
            $stmt = $this->getConnection()->prepare($sql);
            $stmt->execute($params);
            return $stmt->fetchAll();
        } catch (PDOException $e) {
            error_log('[Vinamilk HRM] Query Error: ' . $e->getMessage() . ' | SQL: ' . $sql);
            throw $e;
        }
    }

    /**
     * Thực thi câu truy vấn SELECT và trả về 1 bản ghi duy nhất.
     * 
     * @param string $sql Câu SQL
     * @param array<string, mixed> $params Tham số
     * @return array<string, mixed>|false Bản ghi hoặc false nếu không tìm thấy
     * 
     * @example
     * ```php
     * $employee = Database::getInstance()->queryOne(
     *     "SELECT * FROM nhansu WHERE MaNV = :maNV",
     *     [':maNV' => 'VNM-0020']
     * );
     * ```
     */
    public function queryOne(string $sql, array $params = []): array|false
    {
        try {
            $stmt = $this->getConnection()->prepare($sql);
            $stmt->execute($params);
            return $stmt->fetch();
        } catch (PDOException $e) {
            error_log('[Vinamilk HRM] QueryOne Error: ' . $e->getMessage());
            throw $e;
        }
    }

    /**
     * Thực thi câu lệnh INSERT, UPDATE, DELETE.
     * 
     * @param string $sql Câu SQL
     * @param array<string, mixed> $params Tham số
     * @return int Số dòng bị ảnh hưởng
     * 
     * @example
     * ```php
     * $affected = Database::getInstance()->execute(
     *     "UPDATE nhansu SET TrangThai = :tt WHERE MaNV = :maNV",
     *     [':tt' => 3, ':maNV' => 'VNM-0037']
     * );
     * ```
     */
    public function execute(string $sql, array $params = []): int
    {
        try {
            $stmt = $this->getConnection()->prepare($sql);
            $stmt->execute($params);
            return $stmt->rowCount();
        } catch (PDOException $e) {
            error_log('[Vinamilk HRM] Execute Error: ' . $e->getMessage() . ' | SQL: ' . $sql);
            throw $e;
        }
    }

    /**
     * Lấy ID của bản ghi vừa INSERT.
     * 
     * @return string ID cuối cùng được insert
     */
    public function lastInsertId(): string
    {
        return $this->getConnection()->lastInsertId();
    }

    // =====================================================================
    // TRANSACTION SUPPORT
    // =====================================================================

    /**
     * Bắt đầu transaction.
     * 
     * @return bool
     */
    public function beginTransaction(): bool
    {
        return $this->getConnection()->beginTransaction();
    }

    /**
     * Commit transaction.
     * 
     * @return bool
     */
    public function commit(): bool
    {
        return $this->getConnection()->commit();
    }

    /**
     * Rollback transaction.
     * 
     * @return bool
     */
    public function rollBack(): bool
    {
        return $this->getConnection()->rollBack();
    }

    /**
     * Thực thi callback trong transaction.
     * Tự động rollback nếu có exception.
     * 
     * @param callable $callback Hàm callback nhận PDO làm tham số
     * @return mixed Kết quả trả về từ callback
     * @throws \Throwable
     * 
     * @example
     * ```php
     * $result = Database::getInstance()->transaction(function ($pdo) {
     *     $pdo->prepare("INSERT INTO nhansu (...) VALUES (...)")->execute([...]);
     *     $pdo->prepare("INSERT INTO qt_luong (...) VALUES (...)")->execute([...]);
     *     return $pdo->lastInsertId();
     * });
     * ```
     */
    public function transaction(callable $callback): mixed
    {
        $this->beginTransaction();
        try {
            $result = $callback($this->getConnection());
            $this->commit();
            return $result;
        } catch (\Throwable $e) {
            $this->rollBack();
            error_log('[Vinamilk HRM] Transaction Error: ' . $e->getMessage());
            throw $e;
        }
    }

    // =====================================================================
    // KIỂM TRA MÔI TRƯỜNG
    // =====================================================================

    /**
     * Kiểm tra có đang ở môi trường development không.
     * 
     * @return bool
     */
    private function isDevelopment(): bool
    {
        // Kiểm tra biến môi trường hoặc constant
        if (defined('APP_ENV')) {
            return constant('APP_ENV') === 'development';
        }

        $env = getenv('APP_ENV');
        if ($env !== false) {
            return $env === 'development';
        }

        // Mặc định: coi là development nếu chạy trên localhost
        $serverName = $_SERVER['SERVER_NAME'] ?? 'localhost';
        return in_array($serverName, ['localhost', '127.0.0.1', '::1']);
    }

    // =====================================================================
    // DESTRUCTOR
    // =====================================================================

    /**
     * Destructor - Đóng kết nối khi đối tượng bị hủy.
     */
    public function __destruct()
    {
        $this->connection = null;
    }

    /**
     * Reset Singleton instance (dùng cho unit testing).
     * 
     * @return void
     */
    public static function resetInstance(): void
    {
        if (self::$instance !== null) {
            self::$instance->connection = null;
            self::$instance = null;
        }
    }
}
