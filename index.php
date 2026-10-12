<?php
/**
 * Root Entry Point - Chuyển tiếp tới public/index.php
 */
if (file_exists(__DIR__ . '/public/index.php')) {
    require_once __DIR__ . '/public/index.php';
} else {
    die("Hệ thống Vinamilk HRM: Không tìm thấy public/index.php");
}
