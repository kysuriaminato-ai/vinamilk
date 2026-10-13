-- ====================================================================================================
-- DỰ ÁN: HỆ THỐNG QUẢN TRỊ NGUỒN NHÂN LỰC TẬP ĐOÀN VINAMILK (ENTERPRISE HRIS)
-- CƠ SỞ DỮ LIỆU CHUẨN HÓA: MySQL 8.0+ (InnoDB, utf8mb4_unicode_ci)
-- QUY MÔ: 10.000+ Cán bộ công nhân viên (HQ TP.HCM, 13+ Nhà máy, 14+ Trang trại, Chi nhánh & Logistics)
-- QUY ƯỚC: Tên bảng/cột tiếng Anh snake_case, COMMENT tiếng Việt, Tiền tệ DECIMAL(15,0) VND
-- ====================================================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;
SET SQL_MODE = 'STRICT_TRANS_TABLES,NO_ENGINE_SUBSTITUTION';

CREATE DATABASE IF NOT EXISTS `vinamilk_hris` 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

USE `vinamilk_hris`;

-- ====================================================================================================
-- PHẦN 1: TỔ CHỨC VÀ ĐỊNH NGẠCH (ORGANIZATION & JOB STRUCTURE)
-- ====================================================================================================

-- 1.1 Bảng org_units (Cơ cấu tổ chức hình cây đa tầng)
CREATE TABLE IF NOT EXISTS `org_units` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính định danh đơn vị',
    `unit_code`             VARCHAR(50)     NOT NULL COMMENT 'Mã đơn vị duy nhất (VD: VNM_HQ, NM_TIEN_SON, FARM_TAY_NINH)',
    `unit_name`             VARCHAR(255)    NOT NULL COMMENT 'Tên đơn vị tổ chức',
    `unit_type`             ENUM('HEADQUARTERS', 'DIVISION', 'FACTORY', 'DAIRY_FARM', 'BRANCH', 'LOGISTICS_CENTER', 'DEPARTMENT', 'WORKSHOP', 'TEAM') NOT NULL COMMENT 'Loại hình đơn vị',
    `parent_id`             BIGINT UNSIGNED DEFAULT NULL COMMENT 'Khóa ngoại tham chiếu đơn vị cấp cha (NULL = Cấp cao nhất)',
    `path`                  VARCHAR(1000)   NOT NULL COMMENT 'Đường dẫn phân cấp vật lý (Materialized Path, VD: /1/4/15/)',
    `level`                 TINYINT UNSIGNED NOT NULL DEFAULT 1 COMMENT 'Cấp bậc phân tầng cây (1 = Tập đoàn, 2 = Khối/Vùng, 3 = Đơn vị cơ sở...)',
    `region`                ENUM('NORTH', 'CENTRAL', 'SOUTH') NOT NULL DEFAULT 'SOUTH' COMMENT 'Phân vùng địa lý quản lý',
    `address`               VARCHAR(500)    DEFAULT NULL COMMENT 'Địa chỉ trụ sở/nhà máy/trang trại',
    `tax_code`              VARCHAR(50)     DEFAULT NULL COMMENT 'Mã số thuế chi nhánh/đơn vị',
    `cost_center_code`      VARCHAR(50)     DEFAULT NULL COMMENT 'Mã trung tâm chi phí liên kết SAP ERP',
    `manager_employee_id`   BIGINT UNSIGNED DEFAULT NULL COMMENT 'ID của người đứng đầu đơn vị (Giám đốc/Trưởng phòng)',
    `effective_from`        DATE            NOT NULL COMMENT 'Ngày bắt đầu có hiệu lực vận hành',
    `effective_to`          DATE            DEFAULT NULL COMMENT 'Ngày chấm dứt hiệu lực (NULL = Đang hoạt động)',
    `is_active`             BOOLEAN         NOT NULL DEFAULT TRUE COMMENT 'Cờ trạng thái hoạt động',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Thời điểm tạo bản ghi',
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Thời điểm cập nhật',
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL COMMENT 'Thời điểm xóa mềm (Soft delete)',
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_org_units_code` (`unit_code`),
    KEY `idx_org_units_parent` (`parent_id`),
    KEY `idx_org_units_path` (`path`),
    KEY `idx_org_units_type` (`unit_type`),
    KEY `idx_org_units_region` (`region`),
    KEY `idx_org_units_active` (`is_active`, `deleted_at`),
    CONSTRAINT `fk_org_units_parent` FOREIGN KEY (`parent_id`) REFERENCES `org_units` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Cơ cấu tổ chức hình cây đa tầng kết hợp Materialized Path';

-- 1.2 Bảng job_grades (Bậc ngạch chức danh tiêu chuẩn)
CREATE TABLE IF NOT EXISTS `job_grades` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính bậc ngạch',
    `grade_code`            VARCHAR(50)     NOT NULL COMMENT 'Mã bậc ngạch (VD: L1_WORKER, L4_SPECIALIST, M2_DIRECTOR)',
    `grade_name`            VARCHAR(100)    NOT NULL COMMENT 'Tên hiển thị bậc ngạch',
    `grade_level`           TINYINT UNSIGNED NOT NULL COMMENT 'Thứ tự cấp bậc (1 = Thấp nhất)',
    `description`           VARCHAR(500)    DEFAULT NULL COMMENT 'Mô tả tiêu chuẩn bậc ngạch',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_job_grades_code` (`grade_code`),
    KEY `idx_job_grades_level` (`grade_level`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Danh mục bậc ngạch chức danh tiêu chuẩn doanh nghiệp';

-- 1.3 Bảng salary_bands (Dải lương vị trí P1 theo ngạch)
CREATE TABLE IF NOT EXISTS `salary_bands` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính dải lương',
    `band_code`             VARCHAR(50)     NOT NULL COMMENT 'Mã dải lương',
    `job_grade_id`          BIGINT UNSIGNED NOT NULL COMMENT 'FK -> job_grades',
    `min_salary`            DECIMAL(15,0)   NOT NULL COMMENT 'Mức lương P1 sàn tối thiểu (VND)',
    `mid_salary`            DECIMAL(15,0)   NOT NULL COMMENT 'Mức lương P1 trung vị tiêu chuẩn (VND)',
    `max_salary`            DECIMAL(15,0)   NOT NULL COMMENT 'Mức lương P1 trần tối đa (VND)',
    `effective_from`        DATE            NOT NULL COMMENT 'Ngày bắt đầu áp dụng khung lương',
    `effective_to`          DATE            DEFAULT NULL COMMENT 'Ngày hết hạn khung lương (NULL = Đang áp dụng)',
    `is_current`            BOOLEAN         NOT NULL DEFAULT TRUE COMMENT 'Cờ khung lương hiện hành',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_salary_bands_code` (`band_code`),
    KEY `idx_salary_bands_grade` (`job_grade_id`),
    KEY `idx_salary_bands_effective` (`job_grade_id`, `effective_from`, `effective_to`),
    CONSTRAINT `fk_salary_bands_grade` FOREIGN KEY (`job_grade_id`) REFERENCES `job_grades` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Dải lương P1 theo bậc ngạch có quản lý lịch sử hiệu lực';

-- 1.4 Bảng positions (Vị trí công việc và định biên)
CREATE TABLE IF NOT EXISTS `positions` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính vị trí công việc',
    `position_code`         VARCHAR(50)     NOT NULL COMMENT 'Mã vị trí công việc duy nhất',
    `position_name`         VARCHAR(255)    NOT NULL COMMENT 'Tên chức danh vị trí (VD: Kỹ sư vận hành Tetra Pak)',
    `job_grade_id`          BIGINT UNSIGNED NOT NULL COMMENT 'FK -> job_grades',
    `org_unit_id`           BIGINT UNSIGNED NOT NULL COMMENT 'FK -> org_units (Đơn vị quản lý vị trí)',
    `headcount_budget`      INT UNSIGNED    NOT NULL DEFAULT 1 COMMENT 'Số lượng định biên nhân sự được phê duyệt',
    `job_description`       TEXT            DEFAULT NULL COMMENT 'Bản mô tả công việc (JD)',
    `is_active`             BOOLEAN         NOT NULL DEFAULT TRUE COMMENT 'Cờ vị trí đang tuyển/hoạt động',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_positions_code` (`position_code`),
    KEY `idx_positions_org` (`org_unit_id`),
    KEY `idx_positions_grade` (`job_grade_id`),
    CONSTRAINT `fk_positions_org` FOREIGN KEY (`org_unit_id`) REFERENCES `org_units` (`id`) ON UPDATE CASCADE,
    CONSTRAINT `fk_positions_grade` FOREIGN KEY (`job_grade_id`) REFERENCES `job_grades` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Danh mục vị trí công việc kèm định biên theo phòng ban';

-- ====================================================================================================
-- PHẦN 2: HỒ SƠ VÀ QUAN HỆ LAO ĐỘNG (EMPLOYEE & LIFECYCLE)
-- ====================================================================================================

-- 2.1 Bảng employees (Hồ sơ nhân sự cốt lõi)
CREATE TABLE IF NOT EXISTS `employees` (
    `id`                        BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính hồ sơ nhân sự',
    `employee_code`             VARCHAR(50)     NOT NULL COMMENT 'Mã nhân viên Vinamilk (VD: VNM00123)',
    `full_name`                 VARCHAR(150)    NOT NULL COMMENT 'Họ và tên đầy đủ',
    `gender`                    ENUM('MALE', 'FEMALE', 'OTHER') NOT NULL COMMENT 'Giới tính',
    `date_of_birth`             DATE            NOT NULL COMMENT 'Ngày sinh',
    `national_id`               VARCHAR(20)     NOT NULL COMMENT 'Số CMND/CCCD/Hộ chiếu',
    `national_id_issued_date`   DATE            DEFAULT NULL COMMENT 'Ngày cấp CMND/CCCD',
    `national_id_issued_place`  VARCHAR(200)    DEFAULT NULL COMMENT 'Nơi cấp CMND/CCCD',
    `personal_email`            VARCHAR(100)    DEFAULT NULL COMMENT 'Email cá nhân',
    `company_email`             VARCHAR(100)    DEFAULT NULL COMMENT 'Email công vụ Vinamilk',
    `phone_number`              VARCHAR(20)     NOT NULL COMMENT 'Số điện thoại liên lạc',
    `permanent_address`         VARCHAR(300)    DEFAULT NULL COMMENT 'Địa chỉ thường trú theo hộ khẩu',
    `current_address`           VARCHAR(300)    DEFAULT NULL COMMENT 'Địa chỉ tạm trú hiện tại',
    `tax_id`                    VARCHAR(30)     DEFAULT NULL COMMENT 'Mã số thuế thu nhập cá nhân',
    `bank_account_number`       VARCHAR(50)     DEFAULT NULL COMMENT 'Số tài khoản ngân hàng nhận lương',
    `bank_name`                 VARCHAR(100)    DEFAULT NULL COMMENT 'Tên ngân hàng (BIDV, VCB...)',
    `bank_branch`               VARCHAR(100)    DEFAULT NULL COMMENT 'Chi nhánh ngân hàng',
    `social_insurance_number`   VARCHAR(30)     DEFAULT NULL COMMENT 'Mã số sổ Bảo hiểm xã hội',
    `hire_date`                 DATE            NOT NULL COMMENT 'Ngày bắt đầu vào làm việc',
    `probation_end_date`        DATE            DEFAULT NULL COMMENT 'Ngày kết thúc thử việc',
    `termination_date`          DATE            DEFAULT NULL COMMENT 'Ngày chính thức thôi việc',
    `employment_status`         ENUM('PROBATION', 'ACTIVE', 'SUSPENDED', 'TERMINATED', 'RETIRED') NOT NULL DEFAULT 'PROBATION' COMMENT 'Trạng thái nhân sự',
    `avatar_url`                VARCHAR(255)    DEFAULT NULL COMMENT 'Đường dẫn ảnh đại diện',
    `created_at`                TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`                TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`                TIMESTAMP       NULL DEFAULT NULL COMMENT 'Xóa mềm hồ sơ',
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_employees_code` (`employee_code`),
    UNIQUE KEY `uk_employees_national_id` (`national_id`),
    UNIQUE KEY `uk_employees_company_email` (`company_email`),
    KEY `idx_employees_status` (`employment_status`, `deleted_at`),
    KEY `idx_employees_hire_date` (`hire_date`),
    KEY `idx_employees_name` (`full_name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Hồ sơ nhân sự cốt lõi toàn tập đoàn Vinamilk';

-- Bổ sung Khóa ngoại Quản lý đơn vị sau khi tạo bảng employees
ALTER TABLE `org_units` 
ADD CONSTRAINT `fk_org_units_manager` 
FOREIGN KEY (`manager_employee_id`) REFERENCES `employees` (`id`) ON DELETE SET NULL ON UPDATE CASCADE;

-- 2.2 Bảng employee_assignments (Lịch sử bổ nhiệm, điều chuyển công tác - SCD Type 2)
CREATE TABLE IF NOT EXISTS `employee_assignments` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính phân công công tác',
    `employee_id`           BIGINT UNSIGNED NOT NULL COMMENT 'FK -> employees',
    `org_unit_id`           BIGINT UNSIGNED NOT NULL COMMENT 'FK -> org_units',
    `position_id`           BIGINT UNSIGNED NOT NULL COMMENT 'FK -> positions',
    `is_primary`            BOOLEAN         NOT NULL DEFAULT TRUE COMMENT '1 = Vị trí chính thức, 0 = Kiêm nhiệm',
    `assignment_type`       ENUM('INITIAL', 'PROMOTION', 'TRANSFER', 'DEMOTION', 'CONCURRENT') NOT NULL DEFAULT 'INITIAL' COMMENT 'Loại hình biến động công tác',
    `decision_number`       VARCHAR(100)    DEFAULT NULL COMMENT 'Số quyết định điều động/bổ nhiệm',
    `effective_from`        DATE            NOT NULL COMMENT 'Ngày bắt đầu có hiệu lực bổ nhiệm',
    `effective_to`          DATE            DEFAULT NULL COMMENT 'Ngày kết thúc (NULL = Đang đương nhiệm)',
    `is_current`            BOOLEAN         NOT NULL DEFAULT TRUE COMMENT 'Cờ chỉ định bản ghi hiện hành',
    `notes`                 VARCHAR(500)    DEFAULT NULL COMMENT 'Ghi chú lý do bổ nhiệm/thuyên chuyển',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    KEY `idx_assignments_emp` (`employee_id`),
    KEY `idx_assignments_org` (`org_unit_id`),
    KEY `idx_assignments_pos` (`position_id`),
    KEY `idx_assignments_timeline` (`employee_id`, `effective_from`, `effective_to`),
    CONSTRAINT `fk_assignments_emp` FOREIGN KEY (`employee_id`) REFERENCES `employees` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_assignments_org` FOREIGN KEY (`org_unit_id`) REFERENCES `org_units` (`id`) ON UPDATE CASCADE,
    CONSTRAINT `fk_assignments_pos` FOREIGN KEY (`position_id`) REFERENCES `positions` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Quá trình bổ nhiệm và điều động công tác lịch sử SCD Type 2';

-- 2.3 Bảng employee_salaries (Lịch sử xếp lương 3P & Phụ cấp - SCD Type 2)
CREATE TABLE IF NOT EXISTS `employee_salaries` (
    `id`                        BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính biểu lương nhân viên',
    `employee_id`               BIGINT UNSIGNED NOT NULL COMMENT 'FK -> employees',
    `p1_position_salary`        DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Lương vị trí chức danh P1 (VND)',
    `p2_competency_salary`      DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Lương năng lực P2 (VND)',
    `insurance_salary`          DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Mức lương cơ sở đóng BHXH (VND)',
    `toxic_allowance`           DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Phụ cấp độc hại nhà máy chế biến (VND)',
    `farm_field_allowance`      DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Phụ cấp lưu động/đặc thù trang trại (VND)',
    `responsibility_allowance`  DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Phụ cấp trách nhiệm quản lý (VND)',
    `meal_allowance`            DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Phụ cấp tiền ăn trưa (VND)',
    `decision_number`           VARCHAR(100)    DEFAULT NULL COMMENT 'Số quyết định xếp lương',
    `effective_from`            DATE            NOT NULL COMMENT 'Ngày bắt đầu hưởng mức lương',
    `effective_to`              DATE            DEFAULT NULL COMMENT 'Ngày hết hạn mức lương (NULL = Đang hưởng)',
    `is_current`                BOOLEAN         NOT NULL DEFAULT TRUE COMMENT 'Cờ chỉ định mức lương hiện hành',
    `created_at`                TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`                TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`                TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    KEY `idx_emp_salaries_emp` (`employee_id`),
    KEY `idx_emp_salaries_timeline` (`employee_id`, `effective_from`, `effective_to`),
    CONSTRAINT `fk_emp_salaries_emp` FOREIGN KEY (`employee_id`) REFERENCES `employees` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Lịch sử xếp lương 3P và phụ cấp nhân viên theo SCD Type 2';

-- 2.4 Bảng contracts (Hợp đồng lao động)
CREATE TABLE IF NOT EXISTS `contracts` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính hợp đồng',
    `contract_code`         VARCHAR(100)    NOT NULL COMMENT 'Số hợp đồng lao động (VD: HĐLĐ-2026/VNM-001)',
    `employee_id`           BIGINT UNSIGNED NOT NULL COMMENT 'FK -> employees',
    `contract_type`         ENUM('PROBATION', 'DEFINITE_TERM_1Y', 'DEFINITE_TERM_2Y', 'DEFINITE_TERM_3Y', 'INDEFINITE_TERM', 'SEASONAL') NOT NULL COMMENT 'Loại hợp đồng theo BLLĐ',
    `contract_status`       ENUM('DRAFT', 'ACTIVE', 'EXPIRED', 'TERMINATED') NOT NULL DEFAULT 'ACTIVE' COMMENT 'Trạng thái hợp đồng',
    `signed_date`           DATE            NOT NULL COMMENT 'Ngày ký kết hợp đồng',
    `start_date`            DATE            NOT NULL COMMENT 'Ngày bắt đầu có hiệu lực HĐ',
    `end_date`              DATE            DEFAULT NULL COMMENT 'Ngày kết thúc HĐ (NULL = Không xác định thời hạn)',
    `base_salary`           DECIMAL(15,0)   NOT NULL COMMENT 'Mức lương cơ bản ghi trên HĐLĐ (VND)',
    `working_location`      VARCHAR(255)    NOT NULL COMMENT 'Địa điểm làm việc ghi trong HĐ',
    `file_url`              VARCHAR(500)    DEFAULT NULL COMMENT 'Đường dẫn file scan hợp đồng PDF',
    `effective_from`        DATE            NOT NULL COMMENT 'Ngày hiệu lực pháp lý',
    `effective_to`          DATE            DEFAULT NULL COMMENT 'Ngày chấm dứt hiệu lực HĐ',
    `is_current`            BOOLEAN         NOT NULL DEFAULT TRUE COMMENT 'Cờ hợp đồng hiện tại',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_contracts_code` (`contract_code`),
    KEY `idx_contracts_emp` (`employee_id`),
    KEY `idx_contracts_dates` (`start_date`, `end_date`),
    KEY `idx_contracts_status` (`contract_status`),
    CONSTRAINT `fk_contracts_emp` FOREIGN KEY (`employee_id`) REFERENCES `employees` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Quản lý hợp đồng lao động theo chuẩn Bộ luật Lao động';

-- 2.5 Bảng dependents (Người phụ thuộc giảm trừ gia cảnh)
CREATE TABLE IF NOT EXISTS `dependents` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính người phụ thuộc',
    `employee_id`           BIGINT UNSIGNED NOT NULL COMMENT 'FK -> employees',
    `full_name`             VARCHAR(150)    NOT NULL COMMENT 'Họ tên người phụ thuộc',
    `relationship`          ENUM('CHILD', 'SPOUSE', 'PARENT', 'OTHER') NOT NULL COMMENT 'Mối quan hệ nhân thân',
    `date_of_birth`         DATE            NOT NULL COMMENT 'Ngày sinh',
    `national_id`           VARCHAR(20)     DEFAULT NULL COMMENT 'Số CCCD/Mã định danh cá nhân',
    `tax_id`                VARCHAR(30)     DEFAULT NULL COMMENT 'Mã số thuế người phụ thuộc',
    `effective_from`        DATE            NOT NULL COMMENT 'Ngày bắt đầu tính giảm trừ gia cảnh',
    `effective_to`          DATE            DEFAULT NULL COMMENT 'Ngày kết thúc giảm trừ (NULL = Đang giảm trừ)',
    `is_approved`           BOOLEAN         NOT NULL DEFAULT FALSE COMMENT 'Trạng thái HR đã duyệt hồ sơ thuế',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    KEY `idx_dependents_emp` (`employee_id`),
    KEY `idx_dependents_tax` (`tax_id`),
    CONSTRAINT `fk_dependents_emp` FOREIGN KEY (`employee_id`) REFERENCES `employees` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Hồ sơ người phụ thuộc phục vụ quyết toán thuế TNCN';

-- 2.6 Bảng documents (Hồ sơ chứng chỉ số hóa)
CREATE TABLE IF NOT EXISTS `documents` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính tài liệu',
    `employee_id`           BIGINT UNSIGNED NOT NULL COMMENT 'FK -> employees',
    `document_name`         VARCHAR(255)    NOT NULL COMMENT 'Tên văn bằng/chứng chỉ/tài liệu',
    `document_type`         ENUM('ID_CARD', 'DIPLOMA', 'CERTIFICATE', 'LABOR_CONTRACT', 'HEALTH_CERT', 'DECISION', 'OTHER') NOT NULL COMMENT 'Loại tài liệu',
    `file_path`             VARCHAR(500)    NOT NULL COMMENT 'Đường dẫn lưu file trên Server/Cloud Storage',
    `file_size_kb`          INT UNSIGNED    DEFAULT NULL COMMENT 'Dung lượng file (KB)',
    `expiry_date`           DATE            DEFAULT NULL COMMENT 'Ngày hết hạn (đặc biệt Thẻ Xanh thực phẩm/chứng chỉ an toàn)',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    KEY `idx_documents_emp` (`employee_id`),
    KEY `idx_documents_expiry` (`expiry_date`),
    CONSTRAINT `fk_documents_emp` FOREIGN KEY (`employee_id`) REFERENCES `employees` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Số hóa văn bằng, chứng chỉ an toàn vệ sinh và hồ sơ đính kèm';

-- ====================================================================================================
-- PHẦN 3: TUYỂN DỤNG VÀ ĐÁNH GIÁ Y TẾ (RECRUITMENT & HEALTH)
-- ====================================================================================================

-- 3.1 Bảng requisitions (Yêu cầu tuyển dụng định biên)
CREATE TABLE IF NOT EXISTS `requisitions` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính phiếu tuyển dụng',
    `requisition_code`      VARCHAR(50)     NOT NULL COMMENT 'Mã phiếu tuyển dụng (VD: REQ-2026-NMTS-01)',
    `title`                 VARCHAR(255)    NOT NULL COMMENT 'Tiêu đề vị trí cần tuyển',
    `org_unit_id`           BIGINT UNSIGNED NOT NULL COMMENT 'FK -> org_units (Đơn vị yêu cầu tuyển)',
    `position_id`           BIGINT UNSIGNED NOT NULL COMMENT 'FK -> positions (Vị trí cần tuyển)',
    `headcount`             INT UNSIGNED    NOT NULL DEFAULT 1 COMMENT 'Số lượng cần tuyển',
    `job_description`       TEXT            DEFAULT NULL COMMENT 'Mô tả công việc',
    `requirements`          TEXT            DEFAULT NULL COMMENT 'Yêu cầu kỹ năng/bằng cấp',
    `budget_salary_from`    DECIMAL(15,0)   DEFAULT NULL COMMENT 'Ngân sách lương từ (VND)',
    `budget_salary_to`      DECIMAL(15,0)   DEFAULT NULL COMMENT 'Ngân sách lương đến (VND)',
    `deadline`              DATE            NOT NULL COMMENT 'Hạn chót đóng tuyển dụng',
    `status`                ENUM('DRAFT', 'SUBMITTED', 'APPROVED', 'PUBLISHED', 'CLOSED', 'CANCELLED') NOT NULL DEFAULT 'DRAFT' COMMENT 'Trạng thái phiếu',
    `created_by`            BIGINT UNSIGNED NOT NULL COMMENT 'Người tạo phiếu (FK -> employees)',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_requisitions_code` (`requisition_code`),
    KEY `idx_requisitions_org` (`org_unit_id`),
    KEY `idx_requisitions_status` (`status`),
    CONSTRAINT `fk_requisitions_org` FOREIGN KEY (`org_unit_id`) REFERENCES `org_units` (`id`) ON UPDATE CASCADE,
    CONSTRAINT `fk_requisitions_pos` FOREIGN KEY (`position_id`) REFERENCES `positions` (`id`) ON UPDATE CASCADE,
    CONSTRAINT `fk_requisitions_creator` FOREIGN KEY (`created_by`) REFERENCES `employees` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Phiếu yêu cầu tuyển dụng nhân sự theo định biên';

-- 3.2 Bảng candidates (Hồ sơ ứng viên)
CREATE TABLE IF NOT EXISTS `candidates` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính ứng viên',
    `candidate_code`        VARCHAR(50)     NOT NULL COMMENT 'Mã ứng viên (VD: CAN-2026-0001)',
    `full_name`             VARCHAR(150)    NOT NULL COMMENT 'Họ và tên ứng viên',
    `email`                 VARCHAR(100)    NOT NULL COMMENT 'Email ứng viên',
    `phone_number`          VARCHAR(20)     NOT NULL COMMENT 'Số điện thoại',
    `national_id`           VARCHAR(20)     DEFAULT NULL COMMENT 'Số CMND/CCCD',
    `gender`                ENUM('MALE', 'FEMALE', 'OTHER') DEFAULT NULL,
    `education_level`       VARCHAR(100)    DEFAULT NULL COMMENT 'Trình độ học vấn cao nhất',
    `cv_file_url`           VARCHAR(500)    DEFAULT NULL COMMENT 'Đường dẫn file CV',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_candidates_code` (`candidate_code`),
    KEY `idx_candidates_email` (`email`),
    KEY `idx_candidates_phone` (`phone_number`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Hồ sơ thông tin ứng viên nộp tuyển';

-- 3.3 Bảng applications (Hồ sơ ứng tuyển theo đợt)
CREATE TABLE IF NOT EXISTS `applications` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính đơn ứng tuyển',
    `candidate_id`          BIGINT UNSIGNED NOT NULL COMMENT 'FK -> candidates',
    `requisition_id`        BIGINT UNSIGNED NOT NULL COMMENT 'FK -> requisitions',
    `source`                VARCHAR(100)    DEFAULT NULL COMMENT 'Nguồn tuyển (Website, Vietnamworks, Nội bộ giới thiệu...)',
    `applied_date`          DATE            NOT NULL COMMENT 'Ngày nộp hồ sơ',
    `status`                ENUM('NEW', 'SCREENING', 'INTERVIEWING', 'OFFERED', 'HIRED', 'REJECTED', 'WITHDRAWN') NOT NULL DEFAULT 'NEW' COMMENT 'Trạng thái ứng tuyển',
    `notes`                 TEXT            DEFAULT NULL COMMENT 'Ghi chú đánh giá của HR',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_app_candidate_req` (`candidate_id`, `requisition_id`),
    KEY `idx_applications_status` (`status`),
    CONSTRAINT `fk_app_candidate` FOREIGN KEY (`candidate_id`) REFERENCES `candidates` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_app_requisition` FOREIGN KEY (`requisition_id`) REFERENCES `requisitions` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Chi tiết hồ sơ nộp ứng tuyển theo vị trí';

-- 3.4 Bảng interviews (Lịch và kết quả phỏng vấn)
CREATE TABLE IF NOT EXISTS `interviews` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính buổi phỏng vấn',
    `application_id`        BIGINT UNSIGNED NOT NULL COMMENT 'FK -> applications',
    `round_number`          TINYINT UNSIGNED NOT NULL DEFAULT 1 COMMENT 'Vòng phỏng vấn (1 = HR, 2 = Chuyên môn Quản đốc, 3 = Giám đốc)',
    `interview_date`        DATETIME        NOT NULL COMMENT 'Thời gian phỏng vấn',
    `interviewer_id`        BIGINT UNSIGNED NOT NULL COMMENT 'Người phỏng vấn (FK -> employees)',
    `score`                 DECIMAL(4,1)    DEFAULT NULL COMMENT 'Điểm đánh giá thang 10',
    `feedback`              TEXT            DEFAULT NULL COMMENT 'Nhận xét chi tiết của người phỏng vấn',
    `result`                ENUM('PASS', 'FAIL', 'PENDING') NOT NULL DEFAULT 'PENDING' COMMENT 'Kết quả phỏng vấn',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    KEY `idx_interviews_app` (`application_id`),
    KEY `idx_interviews_interviewer` (`interviewer_id`),
    CONSTRAINT `fk_interviews_app` FOREIGN KEY (`application_id`) REFERENCES `applications` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_interviews_emp` FOREIGN KEY (`interviewer_id`) REFERENCES `employees` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Lịch trình và kết quả phỏng vấn tuyển dụng';

-- 3.5 Bảng health_checks (Khám sức khỏe đầu vào và Thẻ Xanh định kỳ)
CREATE TABLE IF NOT EXISTS `health_checks` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính khám sức khỏe',
    `candidate_id`          BIGINT UNSIGNED DEFAULT NULL COMMENT 'FK -> candidates (Dành cho ứng viên mới)',
    `employee_id`           BIGINT UNSIGNED DEFAULT NULL COMMENT 'FK -> employees (Dành cho công nhân định kỳ)',
    `check_date`            DATE            NOT NULL COMMENT 'Ngày khám sức khỏe',
    `medical_facility`      VARCHAR(255)    NOT NULL COMMENT 'Cơ sở y tế thực hiện khám',
    `health_type`           ENUM('PRE_EMPLOYMENT', 'PERIODIC', 'GREEN_CARD_FOOD_SAFETY', 'OCCUPATIONAL') NOT NULL COMMENT 'Loại khám (Thẻ Xanh an toàn sữa, Khám nghề nghiệp...)',
    `health_class`          ENUM('CLASS_1', 'CLASS_2', 'CLASS_3', 'CLASS_4', 'CLASS_5') NOT NULL COMMENT 'Xếp loại sức khỏe (Loại 1 đến 5)',
    `is_passed`             BOOLEAN         NOT NULL DEFAULT TRUE COMMENT 'Kết luận Đạt/Không đạt điều kiện làm việc',
    `expiry_date`           DATE            NOT NULL COMMENT 'Ngày hết hạn giấy khám (Cảnh báo tái khám)',
    `certificate_url`       VARCHAR(500)    DEFAULT NULL COMMENT 'File đính kèm kết quả y tế',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    KEY `idx_health_candidate` (`candidate_id`),
    KEY `idx_health_emp` (`employee_id`),
    KEY `idx_health_expiry` (`expiry_date`),
    CONSTRAINT `fk_health_candidate` FOREIGN KEY (`candidate_id`) REFERENCES `candidates` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_health_emp` FOREIGN KEY (`employee_id`) REFERENCES `employees` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Quản lý hồ sơ sức khỏe và Thẻ Xanh an toàn thực phẩm ngành sữa';

-- 3.6 Bảng offers (Thư mời nhận việc & Xếp lương dự kiến)
CREATE TABLE IF NOT EXISTS `offers` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính thư mời nhận việc',
    `application_id`        BIGINT UNSIGNED NOT NULL UNIQUE COMMENT 'FK -> applications',
    `position_id`           BIGINT UNSIGNED NOT NULL COMMENT 'FK -> positions',
    `offered_p1`            DECIMAL(15,0)   NOT NULL COMMENT 'Mức lương vị trí P1 đề xuất (VND)',
    `offered_p2`            DECIMAL(15,0)   NOT NULL COMMENT 'Mức lương năng lực P2 đề xuất (VND)',
    `probation_rate`        DECIMAL(5,2)    NOT NULL DEFAULT 85.00 COMMENT 'Tỷ lệ % lương thử việc (tối thiểu 85%)',
    `expected_start_date`   DATE            NOT NULL COMMENT 'Ngày dự kiến nhận việc',
    `status`                ENUM('PENDING_APPROVAL', 'APPROVED', 'SENT_TO_CANDIDATE', 'ACCEPTED', 'DECLINED', 'EXPIRED') NOT NULL DEFAULT 'PENDING_APPROVAL' COMMENT 'Trạng thái Offer',
    `approved_by`           BIGINT UNSIGNED DEFAULT NULL COMMENT 'Người phê duyệt offer (FK -> employees)',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `fk_offers_app` FOREIGN KEY (`application_id`) REFERENCES `applications` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_offers_pos` FOREIGN KEY (`position_id`) REFERENCES `positions` (`id`) ON UPDATE CASCADE,
    CONSTRAINT `fk_offers_approver` FOREIGN KEY (`approved_by`) REFERENCES `employees` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Thư mời nhận việc và thỏa thuận lương ban đầu';

-- ====================================================================================================
-- PHẦN 4: CA KÍP, CHẤM CÔNG VÀ NGHỈ PHÉP (TIME & ATTENDANCE)
-- ====================================================================================================

-- 4.1 Bảng shifts (Danh mục ca làm việc đặc thù)
CREATE TABLE IF NOT EXISTS `shifts` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính ca làm việc',
    `shift_code`            VARCHAR(50)     NOT NULL COMMENT 'Mã ca (VD: CA_HC, CA_1_SX, CA_2_SX, CA_3_DEM, CA_VAT_SUA)',
    `shift_name`            VARCHAR(100)    NOT NULL COMMENT 'Tên ca làm việc',
    `start_time`            TIME            NOT NULL COMMENT 'Giờ bắt đầu ca',
    `end_time`              TIME            NOT NULL COMMENT 'Giờ kết thúc ca',
    `break_minutes`         SMALLINT UNSIGNED NOT NULL DEFAULT 60 COMMENT 'Số phút nghỉ giữa ca',
    `standard_hours`        DECIMAL(4,2)    NOT NULL DEFAULT 8.00 COMMENT 'Số giờ công tiêu chuẩn',
    `is_night_shift`        BOOLEAN         NOT NULL DEFAULT FALSE COMMENT '1 = Ca đêm (Hưởng phụ cấp ca đêm 30%)',
    `is_active`             BOOLEAN         NOT NULL DEFAULT TRUE COMMENT 'Cờ kích hoạt ca',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_shifts_code` (`shift_code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Danh mục ca kíp sản xuất, ca hành chính và ca trang trại';

-- 4.2 Bảng shift_schedules (Phân ca làm việc chi tiết)
CREATE TABLE IF NOT EXISTS `shift_schedules` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính lịch phân ca',
    `employee_id`           BIGINT UNSIGNED NOT NULL COMMENT 'FK -> employees',
    `shift_id`              BIGINT UNSIGNED NOT NULL COMMENT 'FK -> shifts',
    `work_date`             DATE            NOT NULL COMMENT 'Ngày làm việc được xếp ca',
    `note`                  VARCHAR(255)    DEFAULT NULL COMMENT 'Ghi chú thay ca/đổi kíp',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_schedules_emp_date` (`employee_id`, `work_date`),
    KEY `idx_schedules_date` (`work_date`),
    KEY `idx_schedules_shift` (`shift_id`),
    CONSTRAINT `fk_schedules_emp` FOREIGN KEY (`employee_id`) REFERENCES `employees` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_schedules_shift` FOREIGN KEY (`shift_id`) REFERENCES `shifts` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Bảng kế hoạch phân ca kíp làm việc chi tiết cho nhân sự';

-- 4.3 Bảng attendance_logs (Dữ liệu chấm công thô từ máy quét FaceID/Vân tay)
CREATE TABLE IF NOT EXISTS `attendance_logs` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính log chấm công',
    `employee_id`           BIGINT UNSIGNED NOT NULL COMMENT 'FK -> employees',
    `device_code`           VARCHAR(50)     NOT NULL COMMENT 'Mã máy chấm công (nhận dạng nhà máy/cổng)',
    `check_time`            DATETIME        NOT NULL COMMENT 'Thời gian thực tế quẹt thẻ/FaceID',
    `check_type`            ENUM('IN', 'OUT', 'UNKNOWN') NOT NULL DEFAULT 'UNKNOWN' COMMENT 'Loại chấm (Vào/Ra/Chưa xác định)',
    `raw_data`              JSON            DEFAULT NULL COMMENT 'Dữ liệu payload thô từ thiết bị SDK',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_att_logs_emp_time` (`employee_id`, `check_time`),
    KEY `idx_att_logs_time` (`check_time`),
    CONSTRAINT `fk_att_logs_emp` FOREIGN KEY (`employee_id`) REFERENCES `employees` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Nhật ký quét quẹt vân tay và FaceID từ máy chấm công';

-- 4.4 Bảng attendance_days (Tổng hợp công và giờ OT theo ngày)
CREATE TABLE IF NOT EXISTS `attendance_days` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính bảng công ngày',
    `employee_id`           BIGINT UNSIGNED NOT NULL COMMENT 'FK -> employees',
    `work_date`             DATE            NOT NULL COMMENT 'Ngày làm việc',
    `shift_id`              BIGINT UNSIGNED DEFAULT NULL COMMENT 'FK -> shifts (Ca làm việc áp dụng)',
    `first_in`              DATETIME        DEFAULT NULL COMMENT 'Giờ vào đầu tiên trong ngày',
    `last_out`              DATETIME        DEFAULT NULL COMMENT 'Giờ ra cuối cùng trong ngày',
    `late_minutes`          SMALLINT UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Số phút đi muộn',
    `early_minutes`         SMALLINT UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Số phút về sớm',
    `actual_hours`          DECIMAL(4,2)    NOT NULL DEFAULT 0.00 COMMENT 'Tổng giờ làm việc thực tế',
    `standard_hours`        DECIMAL(4,2)    NOT NULL DEFAULT 8.00 COMMENT 'Giờ tiêu chuẩn quy định của ca',
    `ot_normal_hours`       DECIMAL(4,2)    NOT NULL DEFAULT 0.00 COMMENT 'Số giờ làm thêm ngày thường (150%)',
    `ot_weekend_hours`      DECIMAL(4,2)    NOT NULL DEFAULT 0.00 COMMENT 'Số giờ làm thêm ngày nghỉ tuần (200%)',
    `ot_holiday_hours`      DECIMAL(4,2)    NOT NULL DEFAULT 0.00 COMMENT 'Số giờ làm thêm ngày lễ/Tết (300%)',
    `ot_night_hours`        DECIMAL(4,2)    NOT NULL DEFAULT 0.00 COMMENT 'Số giờ làm đêm/tăng ca đêm (+30%)',
    `status`                ENUM('PRESENT', 'ABSENT', 'LATE', 'EARLY', 'LEAVE_PAID', 'LEAVE_UNPAID', 'HOLIDAY', 'WEEKEND_OFF') NOT NULL DEFAULT 'PRESENT' COMMENT 'Trạng thái ngày công',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_att_days_emp_date` (`employee_id`, `work_date`),
    KEY `idx_att_days_date_status` (`work_date`, `status`),
    KEY `idx_att_days_shift` (`shift_id`),
    CONSTRAINT `fk_att_days_emp` FOREIGN KEY (`employee_id`) REFERENCES `employees` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_att_days_shift` FOREIGN KEY (`shift_id`) REFERENCES `shifts` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Bảng tổng hợp công, vi phạm giờ giấc và làm thêm giờ theo ngày';

-- 4.5 Bảng leave_types (Danh mục loại nghỉ phép)
CREATE TABLE IF NOT EXISTS `leave_types` (
    `id`                        BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính loại phép',
    `type_code`                 VARCHAR(50)     NOT NULL COMMENT 'Mã loại phép (VD: ANNUAL, MATERNITY, SICK, UNPAID, COMPENSATORY)',
    `type_name`                 VARCHAR(100)    NOT NULL COMMENT 'Tên hiển thị loại phép',
    `is_paid`                   BOOLEAN         NOT NULL DEFAULT TRUE COMMENT '1 = Nghỉ hưởng nguyên lương, 0 = Không lương',
    `deduct_annual_balance`     BOOLEAN         NOT NULL DEFAULT FALSE COMMENT '1 = Trừ vào quỹ phép năm',
    `social_insurance_coverage` BOOLEAN         NOT NULL DEFAULT FALSE COMMENT '1 = Được BHXH chi trả trợ cấp',
    `created_at`                TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`                TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`                TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_leave_types_code` (`type_code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Danh mục loại nghỉ phép và chế độ chi trả lương/bảo hiểm';

-- 4.6 Bảng leave_requests (Đơn xin nghỉ phép và luồng duyệt SoD)
CREATE TABLE IF NOT EXISTS `leave_requests` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính đơn xin nghỉ',
    `request_code`          VARCHAR(50)     NOT NULL COMMENT 'Mã đơn (VD: LR-2026-0001)',
    `employee_id`           BIGINT UNSIGNED NOT NULL COMMENT 'Người xin nghỉ (FK -> employees)',
    `leave_type_id`         BIGINT UNSIGNED NOT NULL COMMENT 'FK -> leave_types',
    `start_date`            DATE            NOT NULL COMMENT 'Ngày bắt đầu nghỉ',
    `end_date`              DATE            NOT NULL COMMENT 'Ngày kết thúc nghỉ',
    `leave_days`            DECIMAL(4,1)    NOT NULL COMMENT 'Tổng số ngày nghỉ đăng ký',
    `reason`                TEXT            NOT NULL COMMENT 'Lý do xin nghỉ',
    `status`                ENUM('PENDING', 'APPROVED', 'REJECTED', 'CANCELLED') NOT NULL DEFAULT 'PENDING' COMMENT 'Trạng thái phê duyệt',
    `approved_by`           BIGINT UNSIGNED DEFAULT NULL COMMENT 'Người phê duyệt (FK -> employees, đảm bảo != employee_id)',
    `approved_at`           DATETIME        DEFAULT NULL COMMENT 'Thời điểm phê duyệt',
    `rejection_reason`      VARCHAR(500)    DEFAULT NULL COMMENT 'Lý do từ chối nếu có',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_leave_requests_code` (`request_code`),
    KEY `idx_leave_requests_emp` (`employee_id`),
    KEY `idx_leave_requests_dates` (`start_date`, `end_date`),
    KEY `idx_leave_requests_status` (`status`),
    CONSTRAINT `fk_leave_requests_emp` FOREIGN KEY (`employee_id`) REFERENCES `employees` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_leave_requests_type` FOREIGN KEY (`leave_type_id`) REFERENCES `leave_types` (`id`) ON UPDATE CASCADE,
    CONSTRAINT `fk_leave_requests_approver` FOREIGN KEY (`approved_by`) REFERENCES `employees` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Đơn xin nghỉ phép và lịch sử phê duyệt theo SoD';

-- 4.7 Bảng leave_balances (Quản lý quỹ phép năm và thâm niên)
CREATE TABLE IF NOT EXISTS `leave_balances` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính số dư phép',
    `employee_id`           BIGINT UNSIGNED NOT NULL COMMENT 'FK -> employees',
    `year`                  SMALLINT UNSIGNED NOT NULL COMMENT 'Năm quản lý quỹ phép',
    `annual_leave_quota`    DECIMAL(4,1)    NOT NULL DEFAULT 12.0 COMMENT 'Số ngày phép năm tiêu chuẩn được cấp',
    `seniority_leave_days`  DECIMAL(4,1)    NOT NULL DEFAULT 0.0 COMMENT 'Số ngày phép thâm niên (mỗi 5 năm + 1 ngày)',
    `transferred_days`      DECIMAL(4,1)    NOT NULL DEFAULT 0.0 COMMENT 'Số ngày phép tồn chuyển từ năm trước sang',
    `used_days`             DECIMAL(4,1)    NOT NULL DEFAULT 0.0 COMMENT 'Số ngày phép đã sử dụng trong năm',
    `remaining_days`        DECIMAL(4,1)    GENERATED ALWAYS AS (`annual_leave_quota` + `seniority_leave_days` + `transferred_days` - `used_days`) STORED COMMENT 'Số ngày phép còn lại (Cột tính toán)',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_leave_balances_emp_year` (`employee_id`, `year`),
    CONSTRAINT `fk_leave_balances_emp` FOREIGN KEY (`employee_id`) REFERENCES `employees` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Quỹ phép năm, thâm niên và số dư phép lũy kế của nhân viên';

-- ====================================================================================================
-- PHẦN 5: HIỆU SUẤT VÀ KPI (PERFORMANCE & KPI)
-- ====================================================================================================

-- 5.1 Bảng kpi_periods (Kỳ đánh giá hiệu suất)
CREATE TABLE IF NOT EXISTS `kpi_periods` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính kỳ KPI',
    `period_code`           VARCHAR(50)     NOT NULL COMMENT 'Mã kỳ KPI (VD: KPI-2026-M10, KPI-2026-Q4)',
    `period_name`           VARCHAR(100)    NOT NULL COMMENT 'Tên kỳ đánh giá',
    `period_type`           ENUM('MONTH', 'QUARTER', 'YEAR') NOT NULL DEFAULT 'MONTH' COMMENT 'Loại chu kỳ đánh giá',
    `start_date`            DATE            NOT NULL COMMENT 'Ngày bắt đầu chu kỳ',
    `end_date`              DATE            NOT NULL COMMENT 'Ngày kết thúc chu kỳ',
    `status`                ENUM('OPEN', 'EVALUATING', 'LOCKED', 'CLOSED') NOT NULL DEFAULT 'OPEN' COMMENT 'Trạng thái kỳ KPI',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_kpi_periods_code` (`period_code`),
    KEY `idx_kpi_periods_dates` (`start_date`, `end_date`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Kỳ đánh giá KPI làm cơ sở tính lương hiệu suất P3';

-- 5.2 Bảng kpi_tasks (Mục tiêu và kết quả đánh giá KPI cá nhân)
CREATE TABLE IF NOT EXISTS `kpi_tasks` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính mục tiêu KPI',
    `kpi_period_id`         BIGINT UNSIGNED NOT NULL COMMENT 'FK -> kpi_periods',
    `employee_id`           BIGINT UNSIGNED NOT NULL COMMENT 'FK -> employees',
    `task_name`             VARCHAR(255)    NOT NULL COMMENT 'Tên chỉ số/nhiệm vụ KPI (VD: Tỷ lệ hao hụt sữa tiệt trùng)',
    `target_description`    TEXT            DEFAULT NULL COMMENT 'Mô tả mục tiêu định lượng/chất lượng',
    `weight_percent`        DECIMAL(5,2)    NOT NULL COMMENT 'Trọng số mục tiêu (%)',
    `self_score`            DECIMAL(5,2)    DEFAULT NULL COMMENT 'Điểm nhân viên tự chấm (0-100)',
    `manager_score`         DECIMAL(5,2)    DEFAULT NULL COMMENT 'Điểm quản lý chấm chính thức (0-100)',
    `final_score`           DECIMAL(5,2)    DEFAULT NULL COMMENT 'Điểm chốt sau phúc khảo/thẩm định',
    `reviewer_id`           BIGINT UNSIGNED DEFAULT NULL COMMENT 'Quản lý đánh giá (FK -> employees)',
    `comments`              TEXT            DEFAULT NULL COMMENT 'Nhận xét của cấp quản lý',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    KEY `idx_kpi_tasks_period_emp` (`kpi_period_id`, `employee_id`),
    KEY `idx_kpi_tasks_reviewer` (`reviewer_id`),
    CONSTRAINT `fk_kpi_tasks_period` FOREIGN KEY (`kpi_period_id`) REFERENCES `kpi_periods` (`id`) ON UPDATE CASCADE,
    CONSTRAINT `fk_kpi_tasks_emp` FOREIGN KEY (`employee_id`) REFERENCES `employees` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_kpi_tasks_reviewer` FOREIGN KEY (`reviewer_id`) REFERENCES `employees` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Chi tiết chỉ tiêu KPI, trọng số và điểm số đánh giá thực tế';

-- ====================================================================================================
-- PHẦN 6: TIỀN LƯƠNG VÀ PHÊ DUYỆT (PAYROLL & APPROVALS)
-- ====================================================================================================

-- 6.1 Bảng payroll_periods (Kỳ tính lương tháng)
CREATE TABLE IF NOT EXISTS `payroll_periods` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính kỳ lương',
    `period_code`           VARCHAR(50)     NOT NULL COMMENT 'Mã kỳ lương (VD: PAY-2026-10)',
    `month`                 TINYINT UNSIGNED NOT NULL COMMENT 'Tháng tính lương (1-12)',
    `year`                  SMALLINT UNSIGNED NOT NULL COMMENT 'Năm tính lương',
    `standard_working_days` DECIMAL(4,1)    NOT NULL DEFAULT 22.0 COMMENT 'Số ngày công chuẩn trong tháng',
    `status`                ENUM('DRAFT', 'CALCULATING', 'CALCULATED', 'HR_APPROVED', 'BOD_APPROVED', 'PAID', 'LOCKED') NOT NULL DEFAULT 'DRAFT' COMMENT 'Trạng thái chu trình lương',
    `calculated_at`         DATETIME        DEFAULT NULL COMMENT 'Thời điểm hoàn tất tính toán',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_payroll_month_year` (`month`, `year`),
    UNIQUE KEY `uk_payroll_code` (`period_code`),
    KEY `idx_payroll_status` (`status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Kỳ tính lương hàng tháng toàn tập đoàn Vinamilk';

-- 6.2 Bảng payslips (Phiếu lương tổng hợp 3P chi tiết từng nhân viên)
CREATE TABLE IF NOT EXISTS `payslips` (
    `id`                            BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính phiếu lương',
    `payroll_period_id`             BIGINT UNSIGNED NOT NULL COMMENT 'FK -> payroll_periods',
    `employee_id`                   BIGINT UNSIGNED NOT NULL COMMENT 'FK -> employees',
    `org_unit_id`                   BIGINT UNSIGNED NOT NULL COMMENT 'FK -> org_units (Đơn vị tính chi phí)',
    `cost_center_code`              VARCHAR(50)     DEFAULT NULL COMMENT 'Mã Cost Center hạch toán sang SAP FI/CO',
    `actual_working_days`           DECIMAL(4,1)    NOT NULL DEFAULT 0.0 COMMENT 'Số ngày công làm việc thực tế',
    `paid_leave_days`               DECIMAL(4,1)    NOT NULL DEFAULT 0.0 COMMENT 'Số ngày nghỉ phép hưởng lương',
    `ot_pay_amount`                 DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Tổng tiền làm thêm giờ (OT) (VND)',
    `p1_position_amount`            DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Lương vị trí thực nhận theo ngày công (VND)',
    `p2_competency_amount`          DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Lương năng lực thực nhận theo ngày công (VND)',
    `p3_performance_amount`         DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Lương mềm hiệu suất P3 theo kết quả KPI (VND)',
    `total_allowance_amount`        DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Tổng các khoản phụ cấp sản xuất/trang trại (VND)',
    `gross_income`                  DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Tổng thu nhập trước giảm trừ thuế/bảo hiểm (VND)',
    `social_insurance_emp`          DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Trích nộp BHXH phần nhân viên (8%) (VND)',
    `health_insurance_emp`          DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Trích nộp BHYT phần nhân viên (1.5%) (VND)',
    `unemployment_insurance_emp`    DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Trích nộp BHTN phần nhân viên (1%) (VND)',
    `personal_income_tax`           DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Thuế thu nhập cá nhân khấu trừ (VND)',
    `other_deductions`              DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Các khoản giảm trừ khác (công đoàn, bồi hoàn...) (VND)',
    `net_income`                    DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Lương thực lĩnh chuyển khoản (VND)',
    `social_insurance_comp`         DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'BHXH phần doanh nghiệp đóng (17.5%) (VND)',
    `health_insurance_comp`         DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'BHYT phần doanh nghiệp đóng (3%) (VND)',
    `unemployment_insurance_comp`   DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'BHTN phần doanh nghiệp đóng (1%) (VND)',
    `trade_union_fee_comp`          DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Kinh phí công đoàn doanh nghiệp (2%) (VND)',
    `status`                        ENUM('DRAFT', 'CONFIRMED', 'PAID') NOT NULL DEFAULT 'DRAFT' COMMENT 'Trạng thái chi trả',
    `created_at`                    TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`                    TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`                    TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_payslips_period_emp` (`payroll_period_id`, `employee_id`),
    KEY `idx_payslips_period_org` (`payroll_period_id`, `org_unit_id`),
    KEY `idx_payslips_emp_period` (`employee_id`, `payroll_period_id`),
    KEY `idx_payslips_cost_center` (`cost_center_code`),
    CONSTRAINT `fk_payslips_period` FOREIGN KEY (`payroll_period_id`) REFERENCES `payroll_periods` (`id`) ON UPDATE CASCADE,
    CONSTRAINT `fk_payslips_emp` FOREIGN KEY (`employee_id`) REFERENCES `employees` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_payslips_org` FOREIGN KEY (`org_unit_id`) REFERENCES `org_units` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Bảng lương tổng hợp 3P và đối soát chi phí từng nhân viên';

-- 6.3 Bảng payslip_lines (Chi tiết cấu phần dòng lương để in phiếu lương)
CREATE TABLE IF NOT EXISTS `payslip_lines` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính mục dòng lương',
    `payslip_id`            BIGINT UNSIGNED NOT NULL COMMENT 'FK -> payslips',
    `line_code`             VARCHAR(50)     NOT NULL COMMENT 'Mã dòng khoản mục lương (VD: P1_SAL, TOXIC_ALW, OT_150)',
    `line_name`             VARCHAR(150)    NOT NULL COMMENT 'Tên khoản mục diễn giải trên phiếu lương',
    `line_type`             ENUM('EARNING', 'ALLOWANCE', 'BONUS', 'STATUTORY_DEDUCTION', 'TAX_DEDUCTION', 'OTHER_DEDUCTION', 'EMPLOYER_CONTRIBUTION') NOT NULL COMMENT 'Phân loại dòng lương',
    `amount`                DECIMAL(15,0)   NOT NULL COMMENT 'Số tiền cấu phần (VND)',
    `calculation_formula`   VARCHAR(500)    DEFAULT NULL COMMENT 'Công thức hoặc giải trình cách tính',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_payslip_lines_slip` (`payslip_id`),
    KEY `idx_payslip_lines_code` (`line_code`),
    CONSTRAINT `fk_payslip_lines_slip` FOREIGN KEY (`payslip_id`) REFERENCES `payslips` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Chi tiết các dòng cấu thành thu nhập và khấu trừ trên phiếu lương';

-- 6.4 Bảng salary_proposals (Đề xuất tăng lương, nâng bậc P1/P2)
CREATE TABLE IF NOT EXISTS `salary_proposals` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính đề xuất lương',
    `proposal_code`         VARCHAR(50)     NOT NULL COMMENT 'Mã đề xuất (VD: SAL-PROP-2026-001)',
    `employee_id`           BIGINT UNSIGNED NOT NULL COMMENT 'Nhân sự được đề xuất (FK -> employees)',
    `proposal_type`         ENUM('ANNUAL_INCREASE', 'PROMOTION', 'COMPETENCY_UPGRADE', 'SPECIAL_ADJUSTMENT') NOT NULL COMMENT 'Loại điều chỉnh',
    `current_p1`            DECIMAL(15,0)   NOT NULL COMMENT 'Mức P1 hiện tại (VND)',
    `proposed_p1`           DECIMAL(15,0)   NOT NULL COMMENT 'Mức P1 đề xuất mới (VND)',
    `current_p2`            DECIMAL(15,0)   NOT NULL COMMENT 'Mức P2 hiện tại (VND)',
    `proposed_p2`           DECIMAL(15,0)   NOT NULL COMMENT 'Mức P2 đề xuất mới (VND)',
    `effective_from`        DATE            NOT NULL COMMENT 'Ngày bắt đầu áp dụng nếu được duyệt',
    `reason`                TEXT            NOT NULL COMMENT 'Lý do đề xuất tăng lương',
    `status`                ENUM('SUBMITTED', 'UNIT_APPROVED', 'HR_APPROVED', 'BOD_APPROVED', 'REJECTED', 'CANCELLED') NOT NULL DEFAULT 'SUBMITTED' COMMENT 'Trạng thái đề xuất',
    `created_by`            BIGINT UNSIGNED NOT NULL COMMENT 'Người lập đề xuất (FK -> employees, đảm bảo != employee_id)',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_salary_proposals_code` (`proposal_code`),
    KEY `idx_proposals_emp` (`employee_id`),
    KEY `idx_proposals_status` (`status`),
    CONSTRAINT `fk_proposals_emp` FOREIGN KEY (`employee_id`) REFERENCES `employees` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_proposals_creator` FOREIGN KEY (`created_by`) REFERENCES `employees` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Đề xuất điều chỉnh thang bậc lương và kiểm soát SoD';

-- 6.5 Bảng approval_steps (Nhật ký các bước duyệt theo cây tổ chức)
CREATE TABLE IF NOT EXISTS `approval_steps` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính bước duyệt',
    `entity_type`           ENUM('SALARY_PROPOSAL', 'LEAVE_REQUEST', 'REQUISITION', 'OFFER', 'TRANSFER') NOT NULL COMMENT 'Thực thể cần duyệt',
    `entity_id`             BIGINT UNSIGNED NOT NULL COMMENT 'ID của thực thể',
    `step_number`           TINYINT UNSIGNED NOT NULL COMMENT 'Thứ tự bước trong quy trình (1, 2, 3...)',
    `approver_id`           BIGINT UNSIGNED NOT NULL COMMENT 'Người phê duyệt (FK -> employees)',
    `approver_role`         VARCHAR(50)     NOT NULL COMMENT 'Vai trò người duyệt tại bước này',
    `action`                ENUM('PENDING', 'APPROVED', 'REJECTED', 'DELEGATED') NOT NULL DEFAULT 'PENDING' COMMENT 'Hành động duyệt',
    `comments`              TEXT            DEFAULT NULL COMMENT 'Ý kiến phê duyệt hoặc lý do từ chối',
    `action_at`             DATETIME        DEFAULT NULL COMMENT 'Thời điểm xử lý',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_approval_entity` (`entity_type`, `entity_id`),
    KEY `idx_approval_approver` (`approver_id`),
    CONSTRAINT `fk_approval_approver` FOREIGN KEY (`approver_id`) REFERENCES `employees` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Nhật ký ghi vết từng cấp phê duyệt của quy trình đa cấp SoD';

-- ====================================================================================================
-- PHẦN 7: THAM SỐ PHÁP LÝ ĐỘNG (STATUTORY & LEGAL PARAMETERS)
-- ====================================================================================================

-- 7.1 Bảng legal_parameters (Tham số tỷ lệ BHXH, BHYT, BHTN, Giảm trừ gia cảnh động)
CREATE TABLE IF NOT EXISTS `legal_parameters` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính tham số pháp lý',
    `param_code`            VARCHAR(50)     NOT NULL COMMENT 'Mã tham số (VD: PERSONAL_TAX_RELIEF, DEPENDENT_TAX_RELIEF, SI_EMP_RATE)',
    `param_name`            VARCHAR(150)    NOT NULL COMMENT 'Tên diễn giải tham số',
    `param_value`           DECIMAL(15,4)   NOT NULL COMMENT 'Giá trị tham số (Số tiền hoặc Tỷ lệ %)',
    `unit`                  VARCHAR(20)     NOT NULL COMMENT 'Đơn vị tính (VND, PERCENT, HOURS, YEARS)',
    `description`           VARCHAR(500)    DEFAULT NULL COMMENT 'Căn cứ pháp lý (Luật/Nghị định/Thông tư)',
    `effective_from`        DATE            NOT NULL COMMENT 'Ngày bắt đầu áp dụng',
    `effective_to`          DATE            DEFAULT NULL COMMENT 'Ngày chấm dứt áp dụng (NULL = Đang có hiệu lực)',
    `is_current`            BOOLEAN         NOT NULL DEFAULT TRUE COMMENT 'Cờ tham số hiện hành',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    KEY `idx_legal_param_code` (`param_code`),
    KEY `idx_legal_param_timeline` (`param_code`, `effective_from`, `effective_to`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Bảng tham số pháp lý lao động và thuế có hiệu lực thời gian';

-- 7.2 Bảng regional_min_wages (Lương tối thiểu vùng theo địa bàn nhà máy/trang trại)
CREATE TABLE IF NOT EXISTS `regional_min_wages` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính lương tối thiểu vùng',
    `region_zone`           ENUM('ZONE_1', 'ZONE_2', 'ZONE_3', 'ZONE_4') NOT NULL COMMENT 'Vùng kinh tế (Vùng 1 đến 4)',
    `monthly_min_wage`      DECIMAL(15,0)   NOT NULL COMMENT 'Mức lương tối thiểu theo tháng (VND)',
    `hourly_min_wage`       DECIMAL(15,0)   NOT NULL COMMENT 'Mức lương tối thiểu theo giờ (VND)',
    `effective_from`        DATE            NOT NULL COMMENT 'Ngày bắt đầu áp dụng theo Nghị định CP',
    `effective_to`          DATE            DEFAULT NULL COMMENT 'Ngày hết hiệu lực (NULL = Đang áp dụng)',
    `is_current`            BOOLEAN         NOT NULL DEFAULT TRUE COMMENT 'Cờ mức lương hiện hành',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    KEY `idx_regional_wages_timeline` (`region_zone`, `effective_from`, `effective_to`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Mức lương tối thiểu vùng theo từng địa bàn đặt cơ sở sản xuất';

-- 7.3 Bảng tax_brackets (Biểu thuế thu nhập cá nhân lũy tiến từng phần 7 bậc)
CREATE TABLE IF NOT EXISTS `tax_brackets` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính bậc thuế',
    `bracket_level`         TINYINT UNSIGNED NOT NULL COMMENT 'Bậc thuế (1 đến 7)',
    `min_income`            DECIMAL(15,0)   NOT NULL COMMENT 'Thu nhập tính thuế từ (VND)',
    `max_income`            DECIMAL(15,0)   DEFAULT NULL COMMENT 'Thu nhập tính thuế đến (VND, NULL = Vô cùng)',
    `tax_rate`              DECIMAL(5,2)    NOT NULL COMMENT 'Thuế suất % (5, 10, 15, 20, 25, 30, 35)',
    `quick_deduction`       DECIMAL(15,0)   NOT NULL DEFAULT 0 COMMENT 'Số trừ nhanh phục vụ tính nhanh (VND)',
    `effective_from`        DATE            NOT NULL COMMENT 'Ngày bắt đầu áp dụng biểu thuế',
    `effective_to`          DATE            DEFAULT NULL COMMENT 'Ngày kết thúc hiệu lực',
    `is_current`            BOOLEAN         NOT NULL DEFAULT TRUE COMMENT 'Cờ biểu thuế hiện hành',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    KEY `idx_tax_brackets_level` (`bracket_level`),
    KEY `idx_tax_brackets_timeline` (`effective_from`, `effective_to`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Biểu thuế thu nhập cá nhân lũy tiến từng phần theo Luật Thuế';

-- 7.4 Bảng ot_rates (Hệ số làm thêm giờ theo Bộ luật Lao động)
CREATE TABLE IF NOT EXISTS `ot_rates` (
    `id`                            BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính hệ số OT',
    `rate_code`                     VARCHAR(50)     NOT NULL COMMENT 'Mã hệ số (VD: OT_WEEKDAY, OT_WEEKEND, OT_HOLIDAY)',
    `rate_name`                     VARCHAR(150)    NOT NULL COMMENT 'Tên gọi chế độ làm thêm giờ',
    `multiplier`                    DECIMAL(4,2)    NOT NULL COMMENT 'Hệ số lương OT (1.50, 2.00, 3.00)',
    `night_additional_multiplier`   DECIMAL(4,2)    NOT NULL DEFAULT 0.30 COMMENT 'Hệ số cộng thêm khi làm ban đêm (+30%)',
    `effective_from`                DATE            NOT NULL COMMENT 'Ngày bắt đầu áp dụng',
    `effective_to`                  DATE            DEFAULT NULL COMMENT 'Ngày hết hiệu lực',
    `is_current`                    BOOLEAN         NOT NULL DEFAULT TRUE COMMENT 'Cờ hiệu lực hiện hành',
    `created_at`                    TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`                    TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`                    TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    KEY `idx_ot_rates_code` (`rate_code`),
    KEY `idx_ot_rates_timeline` (`effective_from`, `effective_to`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Hệ số thanh toán làm thêm giờ theo Bộ luật Lao động';

-- ====================================================================================================
-- PHẦN 8: TÀI KHOẢN, PHÂN QUYỀN VÀ GIÁM SÁT (IDENTITY, ACCESS & AUDITING)
-- ====================================================================================================

-- 8.1 Bảng roles (Danh mục vai trò hệ thống)
CREATE TABLE IF NOT EXISTS `roles` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính vai trò',
    `role_code`             VARCHAR(50)     NOT NULL COMMENT 'Mã vai trò chuẩn (SUPER_ADMIN, TECH_ADMIN, BOD_EXEC, HR_LOCAL...)',
    `role_name`             VARCHAR(100)    NOT NULL COMMENT 'Tên hiển thị vai trò',
    `description`           VARCHAR(500)    DEFAULT NULL COMMENT 'Mô tả phạm vi trách nhiệm',
    `is_system`             BOOLEAN         NOT NULL DEFAULT FALSE COMMENT '1 = Vai trò hệ thống mặc định cấm xóa',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_roles_code` (`role_code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Danh mục vai trò phân quyền chức năng trong hệ thống';

-- 8.2 Bảng permissions (Danh mục quyền hạn theo module)
CREATE TABLE IF NOT EXISTS `permissions` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính quyền hạn',
    `permission_code`       VARCHAR(100)    NOT NULL COMMENT 'Mã quyền chi tiết (VD: ORG_UNIT_VIEW, SALARY_CALCULATE, EMPLOYEE_EDIT)',
    `module_code`           VARCHAR(50)     NOT NULL COMMENT 'Mã module (ORG, EMPLOYEE, ATTENDANCE, PAYROLL, KPI...)',
    `permission_name`       VARCHAR(150)    NOT NULL COMMENT 'Tên quyền hạn',
    `description`           VARCHAR(500)    DEFAULT NULL COMMENT 'Mô tả quyền',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_permissions_code` (`permission_code`),
    KEY `idx_permissions_module` (`module_code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Quyền hạn chi tiết cho từng hành vi tác nghiệp';

-- 8.3 Bảng role_permissions (Bảng nối vai trò - quyền hạn)
CREATE TABLE IF NOT EXISTS `role_permissions` (
    `role_id`               BIGINT UNSIGNED NOT NULL COMMENT 'FK -> roles',
    `permission_id`         BIGINT UNSIGNED NOT NULL COMMENT 'FK -> permissions',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`role_id`, `permission_id`),
    CONSTRAINT `fk_rp_role` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_rp_perm` FOREIGN KEY (`permission_id`) REFERENCES `permissions` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Bảng liên kết quan hệ nhiều-nhiều giữa vai trò và quyền hạn';

-- 8.4 Bảng users (Tài khoản người dùng đăng nhập)
CREATE TABLE IF NOT EXISTS `users` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính tài khoản',
    `username`              VARCHAR(50)     NOT NULL COMMENT 'Tên đăng nhập hệ thống',
    `password_hash`         VARCHAR(255)    NOT NULL COMMENT 'Chuỗi mã hóa mật khẩu bcrypt/Argon2',
    `employee_id`           BIGINT UNSIGNED DEFAULT NULL UNIQUE COMMENT 'FK -> employees (Liên kết 1-1 với hồ sơ nhân sự, NULL nếu là tài khoản kỹ thuật IT)',
    `role_id`               BIGINT UNSIGNED NOT NULL COMMENT 'FK -> roles (Vai trò chính)',
    `is_active`             BOOLEAN         NOT NULL DEFAULT TRUE COMMENT '1 = Tài khoản hoạt động, 0 = Đang bị khóa',
    `two_factor_enabled`    BOOLEAN         NOT NULL DEFAULT FALSE COMMENT 'Bắt buộc xác thực 2 bước (2FA/OTP)',
    `last_login_at`         DATETIME        DEFAULT NULL COMMENT 'Lần đăng nhập thành công gần nhất',
    `failed_login_attempts` TINYINT UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Số lần đăng nhập sai liên tiếp',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`            TIMESTAMP       NULL DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_users_username` (`username`),
    KEY `idx_users_role` (`role_id`),
    CONSTRAINT `fk_users_emp` FOREIGN KEY (`employee_id`) REFERENCES `employees` (`id`) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT `fk_users_role` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Tài khoản người dùng đăng nhập hệ thống HRIS';

-- 8.5 Bảng user_scopes (Phạm vi dữ liệu được phép tác nghiệp theo nút cây tổ chức)
CREATE TABLE IF NOT EXISTS `user_scopes` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính phạm vi dữ liệu',
    `user_id`               BIGINT UNSIGNED NOT NULL COMMENT 'FK -> users',
    `org_unit_id`           BIGINT UNSIGNED NOT NULL COMMENT 'FK -> org_units (Nút gốc được gán phạm vi)',
    `include_sub_units`     BOOLEAN         NOT NULL DEFAULT TRUE COMMENT '1 = Kế thừa quản lý toàn bộ cây con bên dưới',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_user_scopes_user_unit` (`user_id`, `org_unit_id`),
    KEY `idx_user_scopes_unit` (`org_unit_id`),
    CONSTRAINT `fk_scopes_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_scopes_org` FOREIGN KEY (`org_unit_id`) REFERENCES `org_units` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Phạm vi dữ liệu phân cấp theo nút cây tổ chức của từng người dùng';

-- 8.6 Bảng audit_logs (Nhật ký kiểm toán toàn vẹn dữ liệu - Bất biến)
CREATE TABLE IF NOT EXISTS `audit_logs` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính bản ghi kiểm toán',
    `table_name`            VARCHAR(100)    NOT NULL COMMENT 'Tên bảng phát sinh thay đổi',
    `record_id`             BIGINT UNSIGNED NOT NULL COMMENT 'Khóa chính của bản ghi bị thay đổi',
    `action`                ENUM('INSERT', 'UPDATE', 'DELETE', 'SOFT_DELETE') NOT NULL COMMENT 'Loại hành vi',
    `performed_by`          BIGINT UNSIGNED DEFAULT NULL COMMENT 'User ID thực hiện (NULL = Hệ thống tự động)',
    `ip_address`            VARCHAR(45)     DEFAULT NULL COMMENT 'Địa chỉ IP máy người dùng (IPv4/IPv6)',
    `user_agent`            VARCHAR(255)    DEFAULT NULL COMMENT 'Thông tin trình duyệt/ứng dụng',
    `old_values`            JSON            DEFAULT NULL COMMENT 'Trạng thái dữ liệu cũ (JSON Snapshot)',
    `new_values`            JSON            DEFAULT NULL COMMENT 'Trạng thái dữ liệu mới (JSON Snapshot)',
    `performed_at`          TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Thời điểm ghi vết bất biến',
    PRIMARY KEY (`id`),
    KEY `idx_audit_table_record` (`table_name`, `record_id`),
    KEY `idx_audit_performed_at` (`performed_at`),
    KEY `idx_audit_user` (`performed_by`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Nhật ký kiểm toán biến động dữ liệu bất biến (Immutable Audit Trail)';

-- 8.7 Bảng notifications (Thông báo nhắc việc và cảnh báo)
CREATE TABLE IF NOT EXISTS `notifications` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Khóa chính thông báo',
    `user_id`               BIGINT UNSIGNED NOT NULL COMMENT 'FK -> users (Người nhận thông báo)',
    `title`                 VARCHAR(255)    NOT NULL COMMENT 'Tiêu đề thông báo',
    `content`               TEXT            NOT NULL COMMENT 'Nội dung chi tiết thông báo',
    `type`                  ENUM('APPROVAL_REQUEST', 'CONTRACT_EXPIRY', 'HEALTH_CHECK_WARNING', 'PAYROLL_PUBLISHED', 'SYSTEM_ALERT') NOT NULL COMMENT 'Phân loại thông báo',
    `link_url`              VARCHAR(255)    DEFAULT NULL COMMENT 'Đường dẫn liên kết chuyển trang tác vụ',
    `is_read`               BOOLEAN         NOT NULL DEFAULT FALSE COMMENT '0 = Chưa đọc, 1 = Đã đọc',
    `created_at`            TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_notifications_user_read` (`user_id`, `is_read`),
    CONSTRAINT `fk_notifications_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Hộp thư thông báo tác nghiệp và cảnh báo nhắc việc';

SET FOREIGN_KEY_CHECKS = 1;

-- ====================================================================================================
-- PHẦN 9: TRIGGER BẢO VỆ CHỐNG CHỒNG LẤN KHOẢNG THỜI GIAN HIỆU LỰC (EFFECTIVE DATING OVERLAP ENFORCEMENT)
-- ====================================================================================================

DELIMITER $$

-- Trigger 9.1: Kiểm tra chống chồng lấn hiệu lực lương nhân viên (employee_salaries) trước khi INSERT
CREATE TRIGGER `trg_prevent_salary_overlap_insert`
BEFORE INSERT ON `employee_salaries`
FOR EACH ROW
BEGIN
    DECLARE overlap_count INT DEFAULT 0;
    
    -- Kiểm tra có khoảng thời gian nào bị giao thoa cho cùng một nhân sự
    SELECT COUNT(*) INTO overlap_count
    FROM `employee_salaries`
    WHERE `employee_id` = NEW.`employee_id`
      AND `deleted_at` IS NULL
      AND (
          (NEW.`effective_to` IS NULL AND (`effective_to` IS NULL OR `effective_to` >= NEW.`effective_from`))
          OR
          (NEW.`effective_to` IS NOT NULL AND (
              (`effective_to` IS NULL AND `effective_from` <= NEW.`effective_to`)
              OR
              (`effective_from` <= NEW.`effective_to` AND `effective_to` >= NEW.`effective_from`)
          ))
      );
      
    IF overlap_count > 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Lỗi nghiệp vụ: Khoảng thời gian hiệu lực lương bị chồng lấn với một bản ghi đã tồn tại!';
    END IF;
END$$

-- Trigger 9.2: Kiểm tra chống chồng lấn hiệu lực điều động công tác (employee_assignments) trước khi INSERT
CREATE TRIGGER `trg_prevent_assignment_overlap_insert`
BEFORE INSERT ON `employee_assignments`
FOR EACH ROW
BEGIN
    DECLARE overlap_count INT DEFAULT 0;
    
    -- Chỉ kiểm tra ràng buộc chồng lấn đối với vị trí chính thức (is_primary = TRUE)
    IF NEW.`is_primary` = TRUE THEN
        SELECT COUNT(*) INTO overlap_count
        FROM `employee_assignments`
        WHERE `employee_id` = NEW.`employee_id`
          AND `is_primary` = TRUE
          AND `deleted_at` IS NULL
          AND (
              (NEW.`effective_to` IS NULL AND (`effective_to` IS NULL OR `effective_to` >= NEW.`effective_from`))
              OR
              (NEW.`effective_to` IS NOT NULL AND (
                  (`effective_to` IS NULL AND `effective_from` <= NEW.`effective_to`)
                  OR
                  (`effective_from` <= NEW.`effective_to` AND `effective_to` >= NEW.`effective_from`)
              ))
          );
          
        IF overlap_count > 0 THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Lỗi nghiệp vụ: Nhân sự đã có một vị trí công tác chính thức có hiệu lực trong khoảng thời gian này!';
        END IF;
    END IF;
END$$

DELIMITER ;
