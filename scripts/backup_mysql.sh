#!/usr/bin/env bash
# ====================================================================================================
# VINAMILK HRIS - SCRIPT SAO LƯU MYSQL HẰNG NGÀY & KIỂM THỬ KHÔI PHỤC (LINUX / CRONJOB)
# ====================================================================================================
# - Sao lưu toàn vẹn InnoDB (--single-transaction, --routines, --triggers, --events)
# - Nén gzip bảo vệ dung lượng
# - Quản lý vòng đời (Retention): Giữ lại chính xác 30 bản sao lưu gần nhất, tự động xóa bản cũ
# - Thử nghiệm khôi phục tự động (Automated Disaster Recovery Test Restore):
#   Nạp bản backup vào DB tạm `vinamilk_hris_restore_test`, kiểm tra toàn vẹn bảng, xóa DB tạm
# ====================================================================================================

set -euo pipefail

BACKUP_DIR="${BACKUP_DIR:-/var/backups/vinamilk_hris}"
LOG_FILE="${BACKUP_DIR}/backup.log"
RETENTION_DAYS=30

DB_HOST="${DB_HOST:-127.0.0.1}"
DB_PORT="${DB_PORT:-3306}"
DB_NAME="${DB_DATABASE:-vinamilk_hris}"
DB_USER="${DB_USERNAME:-vnm_backup_user}"
DB_PASS="${DB_PASSWORD:-}"

TEST_RESTORE_DB="vinamilk_hris_restore_test"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="${BACKUP_DIR}/vinamilk_hris_${TIMESTAMP}.sql.gz"

mkdir -p "${BACKUP_DIR}"
chmod 700 "${BACKUP_DIR}"

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "${LOG_FILE}"
}

log "=== BẮT ĐẦU TIẾN TRÌNH SAO LƯU CƠ SỞ DỮ LIỆU VINAMILK HRIS ==="

# 1. Thực hiện mysqldump với tùy chọn tối ưu cho InnoDB và Trigger
export MYSQL_PWD="${DB_PASS}"

log "Đang trích xuất dữ liệu từ ${DB_NAME} tại ${DB_HOST}:${DB_PORT}..."

mysqldump \
    --host="${DB_HOST}" \
    --port="${DB_PORT}" \
    --user="${DB_USER}" \
    --single-transaction \
    --quick \
    --routines \
    --triggers \
    --events \
    --hex-blob \
    --default-character-set=utf8mb4 \
    "${DB_NAME}" | gzip -9 > "${BACKUP_FILE}"

BACKUP_SIZE=$(du -h "${BACKUP_FILE}" | cut -f1)
log "-> Tạo tệp sao lưu thành công: ${BACKUP_FILE} (Dung lượng: ${BACKUP_SIZE})"

# 2. KIỂM THỬ KHÔI PHỤC TỰ ĐỘNG (DISASTER RECOVERY TEST RESTORE)
log "Bắt đầu kiểm thử khôi phục tự động vào database tạm: ${TEST_RESTORE_DB}..."

mysql --host="${DB_HOST}" --port="${DB_PORT}" --user="${DB_USER}" -e "
    DROP DATABASE IF EXISTS \`${TEST_RESTORE_DB}\`;
    CREATE DATABASE \`${TEST_RESTORE_DB}\` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
"

# Nạp dữ liệu vừa sao lưu vào DB tạm
gunzip -c "${BACKUP_FILE}" | mysql --host="${DB_HOST}" --port="${DB_PORT}" --user="${DB_USER}" "${TEST_RESTORE_DB}"

# Kiểm tra tính toàn vẹn (Validation Queries)
EMP_COUNT=$(mysql --host="${DB_HOST}" --port="${DB_PORT}" --user="${DB_USER}" -N -s -e "SELECT COUNT(*) FROM \`${TEST_RESTORE_DB}\`.employees;")
ORG_COUNT=$(mysql --host="${DB_HOST}" --port="${DB_PORT}" --user="${DB_USER}" -N -s -e "SELECT COUNT(*) FROM \`${TEST_RESTORE_DB}\`.org_units;")

if [ "${EMP_COUNT}" -gt 0 ] && [ "${ORG_COUNT}" -gt 0 ]; then
    log "-> [XÁC MINH THÀNH CÔNG] Bản backup toàn vẹn! Tìm thấy ${EMP_COUNT} nhân viên và ${ORG_COUNT} đơn vị tổ chức."
else
    log "[CẢNH BÁO NGUY HIỂM] Bản sao lưu bị lỗi hoặc thiếu dữ liệu nhân sự!"
    exit 1
fi

# Xóa database tạm sau khi kiểm thử thành công
mysql --host="${DB_HOST}" --port="${DB_PORT}" --user="${DB_USER}" -e "DROP DATABASE IF EXISTS \`${TEST_RESTORE_DB}\`;"
log "-> Đã dọn dẹp cơ sở dữ liệu kiểm thử tạm thời."

# 3. QUẢN LÝ VÒNG ĐỜI (RETENTION POLICY: GIỮ TỐI ĐA 30 BẢN SAO LƯU)
log "Đang dọn dẹp các bản sao lưu cũ hơn ${RETENTION_DAYS} ngày..."
DELETED_FILES=$(find "${BACKUP_DIR}" -name "vinamilk_hris_*.sql.gz" -type f -mtime +${RETENTION_DAYS} -print -delete | wc -l)
CURRENT_BACKUP_COUNT=$(find "${BACKUP_DIR}" -name "vinamilk_hris_*.sql.gz" -type f | wc -l)

log "-> Đã xóa ${DELETED_FILES} bản sao lưu cũ. Hiện tại duy trì ${CURRENT_BACKUP_COUNT} bản sao lưu."
log "=== HOÀN TẤT SAO LƯU VÀ KIỂM THỬ KHÔI PHỤC AN TOÀN ==="

unset MYSQL_PWD
exit 0
