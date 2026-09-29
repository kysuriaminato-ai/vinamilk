-- DDL cho Hệ thống Quản trị Nhân lực Vinamilk HRIS
-- Chạy script này trong phpMyAdmin hoặc MySQL Workbench

CREATE DATABASE IF NOT EXISTS vinamilk_hris CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE vinamilk_hris;

-- Bảng Cơ cấu tổ chức (Phòng ban/Nhà máy)
CREATE TABLE IF NOT EXISTS departments (
    id VARCHAR(20) PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    parent_id VARCHAR(20) NULL,
    manager_id VARCHAR(20) NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (parent_id) REFERENCES departments(id) ON DELETE SET NULL
);

-- Bảng Chỉ số KPI Đánh giá năng lực
CREATE TABLE IF NOT EXISTS kpi_metrics (
    id INT AUTO_INCREMENT PRIMARY KEY,
    employee_id VARCHAR(20) NOT NULL,
    score DECIMAL(5,2) NOT NULL COMMENT 'Điểm KPI từ 0-100',
    evaluation_date DATE NOT NULL,
    reviewer_id VARCHAR(20) NOT NULL,
    comments TEXT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Thêm dữ liệu mẫu cho departments
INSERT IGNORE INTO departments (id, name, parent_id) VALUES 
('TGĐ', 'Ban Tổng Giám Đốc', NULL),
('NM_TS', 'Nhà máy Tiên Sơn', 'TGĐ'),
('PB_NS', 'Phòng Nhân sự', 'TGĐ');
