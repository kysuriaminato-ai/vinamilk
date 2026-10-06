-- Chạy SQL này để tạo bảng employees và dữ liệu mẫu (cho Bước 3)
CREATE TABLE IF NOT EXISTS employees (
    id VARCHAR(20) PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    gender VARCHAR(10) NULL,
    birth_date DATE NULL,
    department_id VARCHAR(20) NULL,
    position VARCHAR(255) NULL,
    status VARCHAR(50) DEFAULT 'Đang làm việc',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (department_id) REFERENCES departments(id) ON DELETE SET NULL
);

INSERT IGNORE INTO employees (id, name, gender, birth_date, department_id, position, status) VALUES 
('VNM-0001', 'Nguyễn Thị A', 'Nữ', '1990-05-15', 'PB_NS', 'Trưởng phòng Nhân sự', 'Đang làm việc'),
('VNM-0002', 'Trần Văn B', 'Nam', '1992-10-20', 'NM_TS', 'Giám đốc Nhà máy', 'Đang làm việc'),
('VNM-0003', 'Lê C', 'Nam', '1995-01-01', 'PB_NS', 'Chuyên viên Tuyển dụng', 'Đang làm việc');
