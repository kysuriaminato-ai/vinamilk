<?php
require_once __DIR__ . '/../layouts/header.php';

$percent = round(($offboarding['so_buoc_hoan_thanh'] / 6) * 100);
$isDone = ($offboarding['trang_thai'] === 'HOAN_TAT');
?>

<div class="container-fluid px-4 py-4">
    <!-- Header -->
    <div class="d-flex flex-wrap justify-content-between align-items-center mb-4">
        <div>
            <nav aria-label="breadcrumb">
                <ol class="breadcrumb mb-1 small">
                    <li class="breadcrumb-item"><a href="<?= url('dashboard') ?>" class="text-decoration-none">Trang chủ</a></li>
                    <li class="breadcrumb-item"><a href="<?= url('offboarding') ?>" class="text-decoration-none">Thôi việc</a></li>
                    <li class="breadcrumb-item active"><?= e($offboarding['ma_ho_so']) ?></li>
                </ol>
            </nav>
            <h4 class="fw-bold text-dark mb-0">
                <i class="bi bi-person-x-fill text-danger me-2"></i>Chi Tiết Quy Trình Thôi Việc: <?= e($offboarding['HoTen']) ?> (<?= e($offboarding['ma_nv']) ?>)
            </h4>
            <small class="text-muted"><?= e($offboarding['TenDV']) ?> • <?= e($offboarding['TenCV']) ?> • Hồ sơ: <strong><?= e($offboarding['ma_ho_so']) ?></strong></small>
        </div>
        <div>
            <a href="<?= url('employees/show/' . $offboarding['ma_nv'] . '?tab=offboarding') ?>" class="btn btn-outline-secondary btn-sm me-2">
                <i class="bi bi-person-badge me-1"></i> Xem Hồ Sơ Nhân Sự
            </a>
            <a href="<?= url('offboarding') ?>" class="btn btn-outline-secondary btn-sm">
                <i class="bi bi-arrow-left me-1"></i> Danh Sách Offboarding
            </a>
        </div>
    </div>

    <!-- Header Card Tiến Độ -->
    <div class="card border-0 shadow-sm rounded-3 mb-4">
        <div class="card-body p-4">
            <div class="row align-items-center">
                <div class="col-md-8">
                    <div class="d-flex align-items-center mb-2">
                        <span class="badge <?= $isDone ? 'bg-success' : 'bg-primary' ?> px-3 py-2 me-3 fs-6">
                            <?= $isDone ? '<i class="bi bi-check-all me-1"></i> HOÀN TẤT THÔI VIỆC' : '<i class="bi bi-arrow-repeat me-1"></i> ĐANG THỰC HIỆN' ?>
                        </span>
                        <span class="fw-bold fs-5 text-dark">Tiến độ: <?= $offboarding['so_buoc_hoan_thanh'] ?> / 6 bước (<?= $percent ?>%)</span>
                    </div>
                    <div class="progress mb-2" style="height: 10px;">
                        <div class="progress-bar <?= $isDone ? 'bg-success' : 'bg-danger progress-bar-striped progress-bar-animated' ?>" style="width: <?= $percent ?>%;"></div>
                    </div>
                    <div class="small text-muted">
                        <span>Ngày đề xuất: <strong><?= format_date($offboarding['ngay_de_xuat']) ?></strong></span>
                        <span class="mx-2">•</span>
                        <span>Ngày làm việc cuối: <strong class="text-danger"><?= format_date($offboarding['ngay_lam_viec_cuoi']) ?></strong></span>
                        <span class="mx-2">•</span>
                        <span>Lý do: <em><?= e($offboarding['ly_do_nghi']) ?></em></span>
                    </div>
                </div>
                <div class="col-md-4 text-md-end mt-3 mt-md-0">
                    <?php if (!empty($offboarding['tong_tien_khau_tru_ts']) && (float)$offboarding['tong_tien_khau_tru_ts'] > 0): ?>
                        <div class="alert alert-danger p-2 mb-0 d-inline-block text-start">
                            <small class="d-block fw-bold"><i class="bi bi-exclamation-triangle-fill me-1"></i> Khấu trừ tài sản chưa trả:</small>
                            <span class="fs-5 fw-bold text-danger">-<?= number_format((float)$offboarding['tong_tien_khau_tru_ts'], 0, ',', '.') ?> VNĐ</span>
                        </div>
                    <?php else: ?>
                        <div class="alert alert-success p-2 mb-0 d-inline-block text-start">
                            <small class="d-block fw-bold text-success"><i class="bi bi-check-circle me-1"></i> Tài sản bàn giao:</small>
                            <span class="small text-muted">Đã thu hồi đủ, không nợ tài sản.</span>
                        </div>
                    <?php endif; ?>
                </div>
            </div>
        </div>
    </div>

    <!-- 6 BƯỚC CHI TIẾT TƯƠNG TÁC -->
    <div class="row g-4 mb-4">
        <!-- BƯỚC 1: BÀN GIAO CÔNG VIỆC -->
        <div class="col-md-6">
            <div class="card h-100 border shadow-sm <?= $offboarding['buoc_1_ban_giao_cong_viec'] ? 'border-success' : '' ?>">
                <div class="card-header bg-white py-3 d-flex justify-content-between align-items-center">
                    <span class="fw-bold text-dark"><i class="bi bi-briefcase me-2 text-primary"></i>Bước 1: Bàn Giao Công Việc</span>
                    <?= $offboarding['buoc_1_ban_giao_cong_viec'] ? '<span class="badge bg-success"><i class="bi bi-check-lg me-1"></i>Đã Hoàn Tất</span>' : '<span class="badge bg-secondary">Chưa Xong</span>' ?>
                </div>
                <div class="card-body p-3">
                    <p class="small text-muted mb-2">Bàn giao toàn bộ tài liệu dự án, quyền quản trị mã nguồn, liên hệ khách hàng và công việc đang xử lý cho quản lý hoặc nhân sự kế thừa.</p>
                    <?php if ($offboarding['buoc_1_ban_giao_cong_viec']): ?>
                        <div class="alert alert-success py-2 px-3 small mb-0">
                            <strong><i class="bi bi-person-check me-1"></i> Đã duyệt bởi:</strong> <?= e($offboarding['nguoi_xac_nhan_bg']) ?> (<?= date('d/m/Y H:i', strtotime($offboarding['ngay_xac_nhan_bg'])) ?>)<br>
                            <em><?= e($offboarding['noi_dung_ban_giao']) ?></em>
                        </div>
                    <?php else: ?>
                        <form action="<?= url('offboarding/update_step') ?>" method="POST">
                            <?= csrf_field() ?>
                            <input type="hidden" name="offboarding_id" value="<?= $offboarding['id'] ?>">
                            <input type="hidden" name="step" value="1">
                            <div class="mb-2">
                                <textarea name="content" class="form-control form-control-sm" rows="2" placeholder="Ghi chú tóm tắt nội dung bàn giao công việc..." required></textarea>
                            </div>
                            <button type="submit" class="btn btn-success btn-sm w-100 fw-semibold">
                                <i class="bi bi-check-circle me-1"></i> Xác Nhận Bàn Giao Công Việc Xong
                            </button>
                        </form>
                    <?php endif; ?>
                </div>
            </div>
        </div>

        <!-- BƯỚC 2: THU HỒI TÀI SẢN & TỰ ĐỘNG KHẤU TRỪ LƯƠNG -->
        <div class="col-md-6">
            <div class="card h-100 border shadow-sm <?= $offboarding['buoc_2_thu_hoi_tai_san'] ? 'border-success' : '' ?>">
                <div class="card-header bg-white py-3 d-flex justify-content-between align-items-center">
                    <span class="fw-bold text-dark"><i class="bi bi-laptop me-2 text-primary"></i>Bước 2: Thu Hồi Tài Sản / PPE & Khấu Trừ</span>
                    <?= $offboarding['buoc_2_thu_hoi_tai_san'] ? '<span class="badge bg-success"><i class="bi bi-check-lg me-1"></i>Đã Xử Lý</span>' : '<span class="badge bg-warning text-dark">Chờ Kiểm Kê</span>' ?>
                </div>
                <div class="card-body p-3">
                    <p class="small text-muted mb-2">Kiểm tra toàn bộ Laptop, Thẻ từ, PPE phòng sạch. <strong>Tài sản chưa hoàn trả hoặc báo mất sẽ tự động chuyển thành khoản khấu trừ vào lương quyết toán.</strong></p>
                    
                    <!-- Danh sách tài sản nợ -->
                    <?php if (!empty($assets)): ?>
                        <div class="table-responsive mb-2">
                            <table class="table table-sm table-bordered small align-middle mb-0">
                                <thead class="table-light">
                                    <tr>
                                        <th>Tài sản chưa trả</th>
                                        <th>Trạng thái</th>
                                        <th class="text-end">Tiền khấu trừ</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <?php foreach ($assets as $a): 
                                        $pen = (float)($a['gia_tri_khau_tru'] > 0 ? $a['gia_tri_khau_tru'] : ($a['gia_tri_tai_san'] ?: 500000));
                                    ?>
                                        <tr>
                                            <td><?= e($a['ten_tai_san']) ?></td>
                                            <td><span class="badge bg-danger"><?= e($a['trang_thai']) ?></span></td>
                                            <td class="text-end text-danger fw-bold"><?= number_format($pen, 0, ',', '.') ?>đ</td>
                                        </tr>
                                    <?php endforeach; ?>
                                </tbody>
                            </table>
                        </div>
                    <?php else: ?>
                        <div class="alert alert-info py-1 px-2 small mb-2"><i class="bi bi-check2 me-1"></i> Không ghi nhận tài sản thất thoát.</div>
                    <?php endif; ?>

                    <?php if ($offboarding['buoc_2_thu_hoi_tai_san']): ?>
                        <div class="alert alert-success py-2 px-3 small mb-0">
                            <strong><i class="bi bi-person-check me-1"></i> Đã kiểm kê bởi:</strong> <?= e($offboarding['nguoi_xac_nhan_ts']) ?> (<?= date('d/m/Y H:i', strtotime($offboarding['ngay_xac_nhan_ts'])) ?>)<br>
                            <?= e($offboarding['ghi_chu_tai_san']) ?>
                        </div>
                    <?php else: ?>
                        <form action="<?= url('offboarding/update_step') ?>" method="POST">
                            <?= csrf_field() ?>
                            <input type="hidden" name="offboarding_id" value="<?= $offboarding['id'] ?>">
                            <input type="hidden" name="step" value="2">
                            <input type="text" name="note" class="form-control form-control-sm mb-2" placeholder="Ghi chú kiểm kê bàn giao...">
                            <button type="submit" class="btn btn-primary btn-sm w-100 fw-semibold" style="background: #0033A0;">
                                <i class="bi bi-arrow-repeat me-1"></i> Chốt Thu Hồi & Tự Động Khấu Trừ Tiền Tài Sản
                            </button>
                        </form>
                    <?php endif; ?>
                </div>
            </div>
        </div>

        <!-- BƯỚC 3: KHÓA TÀI KHOẢN NGƯỜI DÙNG -->
        <div class="col-md-6">
            <div class="card h-100 border shadow-sm <?= $offboarding['buoc_3_khoa_tai_khoan'] ? 'border-success' : '' ?>">
                <div class="card-header bg-white py-3 d-flex justify-content-between align-items-center">
                    <span class="fw-bold text-dark"><i class="bi bi-lock-fill me-2 text-danger"></i>Bước 3: Khóa Tài Khoản Người Dùng</span>
                    <?= $offboarding['buoc_3_khoa_tai_khoan'] ? '<span class="badge bg-danger"><i class="bi bi-lock-fill me-1"></i>ĐÃ KHÓA</span>' : '<span class="badge bg-secondary">Chưa Khóa</span>' ?>
                </div>
                <div class="card-body p-3">
                    <p class="small text-muted mb-2">Vô hiệu hóa tài khoản đăng nhập HRM, email công vụ, thu hồi quyền truy cập hệ thống thông tin nhằm đảm bảo an toàn thông tin doanh nghiệp.</p>
                    <?php if ($offboarding['buoc_3_khoa_tai_khoan']): ?>
                        <div class="alert alert-danger py-2 px-3 small mb-0">
                            <strong><i class="bi bi-shield-lock me-1"></i> Đã khóa tài khoản bởi:</strong> <?= e($offboarding['nguoi_xac_nhan_tk']) ?> (User: <?= e($offboarding['username_khoa']) ?>)<br>
                            <?= e($offboarding['ghi_chu_khoa_tk']) ?>
                        </div>
                    <?php else: ?>
                        <form action="<?= url('offboarding/update_step') ?>" method="POST">
                            <?= csrf_field() ?>
                            <input type="hidden" name="offboarding_id" value="<?= $offboarding['id'] ?>">
                            <input type="hidden" name="step" value="3">
                            <button type="submit" class="btn btn-danger btn-sm w-100 fw-semibold" onclick="return confirm('Bạn có chắc chắn muốn khóa tài khoản đăng nhập người dùng ngay lập tức?');">
                                <i class="bi bi-lock-fill me-1"></i> Khóa Ngay Tài Khoản Hệ Thống
                            </button>
                        </form>
                    <?php endif; ?>
                </div>
            </div>
        </div>

        <!-- BƯỚC 4: CHỐT CÔNG & PHÉP TỒN -->
        <div class="col-md-6">
            <div class="card h-100 border shadow-sm <?= $offboarding['buoc_4_chot_cong'] ? 'border-success' : '' ?>">
                <div class="card-header bg-white py-3 d-flex justify-content-between align-items-center">
                    <span class="fw-bold text-dark"><i class="bi bi-calendar2-check me-2 text-primary"></i>Bước 4: Chốt Công & Phép Tồn</span>
                    <?= $offboarding['buoc_4_chot_cong'] ? '<span class="badge bg-success"><i class="bi bi-check-lg me-1"></i>Đã Chốt Công</span>' : '<span class="badge bg-secondary">Chờ Chốt</span>' ?>
                </div>
                <div class="card-body p-3">
                    <p class="small text-muted mb-2">Chốt tổng số ngày công làm việc thực tế trong tháng cuối cùng và số ngày phép năm còn tồn để tính chi trả theo quy định.</p>
                    <?php if ($offboarding['buoc_4_chot_cong']): ?>
                        <div class="alert alert-success py-2 px-3 small mb-0">
                            <strong><i class="bi bi-person-check me-1"></i> Đã chốt bởi:</strong> <?= e($offboarding['nguoi_xac_nhan_cong']) ?> (<?= date('d/m/Y H:i', strtotime($offboarding['ngay_xac_nhan_cong'])) ?>)<br>
                            Công thực tế: <strong><?= $offboarding['so_cong_chot'] ?> ngày</strong> • Phép tồn: <strong><?= $offboarding['so_phep_con_lai'] ?> ngày</strong>
                        </div>
                    <?php else: ?>
                        <form action="<?= url('offboarding/update_step') ?>" method="POST">
                            <?= csrf_field() ?>
                            <input type="hidden" name="offboarding_id" value="<?= $offboarding['id'] ?>">
                            <input type="hidden" name="step" value="4">
                            <div class="row g-2 mb-2">
                                <div class="col-6">
                                    <label class="small fw-semibold text-muted">Số công thực tế</label>
                                    <input type="number" step="0.5" name="work_days" class="form-control form-control-sm" value="22" required>
                                </div>
                                <div class="col-6">
                                    <label class="small fw-semibold text-muted">Số phép tồn (ngày)</label>
                                    <input type="number" step="0.5" name="remaining_leave" class="form-control form-control-sm" value="2" required>
                                </div>
                            </div>
                            <button type="submit" class="btn btn-primary btn-sm w-100 fw-semibold">
                                <i class="bi bi-check2-square me-1"></i> Xác Nhận Chốt Bảng Chấm Công
                            </button>
                        </form>
                    <?php endif; ?>
                </div>
            </div>
        </div>

        <!-- BƯỚC 5: QUYẾT TOÁN LƯƠNG & TỰ ĐỘNG KHẤU TRỪ TÀI SẢN -->
        <div class="col-md-6">
            <div class="card h-100 border shadow-sm <?= $offboarding['buoc_5_quyet_toan_luong'] ? 'border-success' : '' ?>">
                <div class="card-header bg-white py-3 d-flex justify-content-between align-items-center">
                    <span class="fw-bold text-dark"><i class="bi bi-cash-coin me-2 text-primary"></i>Bước 5: Quyết Toán Lương (Trừ Tiền Tài Sản)</span>
                    <?= $offboarding['buoc_5_quyet_toan_luong'] ? '<span class="badge bg-success"><i class="bi bi-check-lg me-1"></i>Đã Quyết Toán</span>' : '<span class="badge bg-secondary">Chờ Duyệt Lương</span>' ?>
                </div>
                <div class="card-body p-3">
                    <p class="small text-muted mb-2">Tính toán lương P1, P2, P3, trợ cấp thôi việc (nếu có), và <strong>tự động trừ các khoản tài sản chưa hoàn trả</strong>.</p>
                    <?php if ($offboarding['buoc_5_quyet_toan_luong']): ?>
                        <div class="alert alert-success py-2 px-3 small mb-0">
                            <strong><i class="bi bi-person-check me-1"></i> Đã quyết toán bởi:</strong> <?= e($offboarding['nguoi_xac_nhan_luong']) ?> (<?= date('d/m/Y H:i', strtotime($offboarding['ngay_xac_nhan_luong'])) ?>)<br>
                            Thực lĩnh chuyển khoản: <strong class="text-success fs-6"><?= number_format($offboarding['thuc_linh_quyet_toan'], 0, ',', '.') ?> VNĐ</strong><br>
                            Đã khấu trừ tài sản nợ: <strong class="text-danger">-<?= number_format($offboarding['khau_tru_tai_san_da_tru'], 0, ',', '.') ?> VNĐ</strong>
                        </div>
                    <?php else: ?>
                        <form action="<?= url('offboarding/update_step') ?>" method="POST">
                            <?= csrf_field() ?>
                            <input type="hidden" name="offboarding_id" value="<?= $offboarding['id'] ?>">
                            <input type="hidden" name="step" value="5">
                            <div class="mb-2">
                                <label class="small fw-semibold text-muted">Tổng lương trước khấu trừ tài sản (VNĐ)</label>
                                <input type="number" name="settlement_amount" class="form-control form-control-sm" value="25000000" required>
                                <small class="text-muted">Hệ thống sẽ tự động trừ <?= number_format((float)($offboarding['tong_tien_khau_tru_ts'] ?? 0), 0, ',', '.') ?>đ tiền tài sản nợ.</small>
                            </div>
                            <button type="submit" class="btn btn-success btn-sm w-100 fw-semibold">
                                <i class="bi bi-cash me-1"></i> Duyệt Quyết Toán Lương
                            </button>
                        </form>
                    <?php endif; ?>
                </div>
            </div>
        </div>

        <!-- BƯỚC 6: CHỐT SỔ BHXH & HOÀN TẤT THÔI VIỆC -->
        <div class="col-md-6">
            <div class="card h-100 border shadow-sm <?= $offboarding['buoc_6_chot_bhxh'] ? 'border-success' : '' ?>">
                <div class="card-header bg-white py-3 d-flex justify-content-between align-items-center">
                    <span class="fw-bold text-dark"><i class="bi bi-journal-check me-2 text-primary"></i>Bước 6: Chốt Sổ BHXH & Quyết Định</span>
                    <?= $offboarding['buoc_6_chot_bhxh'] ? '<span class="badge bg-success"><i class="bi bi-check-lg me-1"></i>ĐÃ CHỐT BHXH</span>' : '<span class="badge bg-secondary">Chờ Cơ Quan BHXH</span>' ?>
                </div>
                <div class="card-body p-3">
                    <p class="small text-muted mb-2">Chốt sổ bảo hiểm xã hội tại Cơ quan BHXH quản lý và bàn giao sổ kèm quyết định thôi việc cho người lao động. Khi bước này hoàn thành, hồ sơ nhân sự chuyển sang trạng thái <strong>Nghỉ việc</strong>.</p>
                    <?php if ($offboarding['buoc_6_chot_bhxh']): ?>
                        <div class="alert alert-success py-2 px-3 small mb-0">
                            <strong><i class="bi bi-person-check me-1"></i> Đã chốt bởi:</strong> <?= e($offboarding['nguoi_xac_nhan_bhxh']) ?><br>
                            Số sổ BHXH: <strong class="font-monospace"><?= e($offboarding['so_so_bhxh']) ?></strong> • Ngày trả sổ: <?= format_date($offboarding['ngay_tra_so_bhxh']) ?><br>
                            <span class="text-success fw-bold"><i class="bi bi-check-all me-1"></i> QUY TRÌNH THÔI VIỆC ĐÃ HOÀN TẤT ĐỦ 6 BƯỚC!</span>
                        </div>
                    <?php else: ?>
                        <form action="<?= url('offboarding/update_step') ?>" method="POST">
                            <?= csrf_field() ?>
                            <input type="hidden" name="offboarding_id" value="<?= $offboarding['id'] ?>">
                            <input type="hidden" name="step" value="6">
                            <div class="row g-2 mb-2">
                                <div class="col-6">
                                    <label class="small fw-semibold text-muted">Số sổ BHXH</label>
                                    <input type="text" name="so_bhxh" class="form-control form-control-sm" placeholder="VD: 7912345678" required>
                                </div>
                                <div class="col-6">
                                    <label class="small fw-semibold text-muted">Ngày trả sổ</label>
                                    <input type="date" name="return_date" class="form-control form-control-sm" value="<?= date('Y-m-d') ?>" required>
                                </div>
                            </div>
                            <button type="submit" class="btn btn-danger btn-sm w-100 fw-semibold" onclick="return confirm('Xác nhận hoàn tất chốt sổ BHXH và đóng hồ sơ thôi việc? Trạng thái nhân sự sẽ chuyển thành Nghỉ việc!');">
                                <i class="bi bi-award me-1"></i> Chốt Sổ & Đóng Hồ Sơ Thôi Việc
                            </button>
                        </form>
                    <?php endif; ?>
                </div>
            </div>
        </div>
    </div>

    <!-- KHẢO SÁT LÝ DO THÔI VIỆC CHO MÔ HÌNH AI -->
    <div class="card border border-primary-subtle shadow-sm mb-4">
        <div class="card-header bg-light d-flex justify-content-between align-items-center py-3">
            <div>
                <h6 class="fw-bold text-dark mb-0">
                    <i class="bi bi-robot text-primary me-2"></i>Khảo Sát Lý Do Nghỉ Việc (Dữ Liệu Đào Tạo Mô Hình AI)
                </h6>
                <small class="text-muted">Dữ liệu phỏng vấn thôi việc (Exit Survey) được sử dụng để nuôi mô hình AI Turnover Prediction.</small>
            </div>
            <?php if ($survey): ?>
                <span class="badge bg-success"><i class="bi bi-check-circle me-1"></i> Đã Thu Thập Dữ Liệu AI</span>
            <?php else: ?>
                <span class="badge bg-warning text-dark"><i class="bi bi-hourglass-split me-1"></i> Chưa Thực Hiện</span>
            <?php endif; ?>
        </div>
        <div class="card-body p-4">
            <?php if ($survey): ?>
                <div class="row g-3 mb-3">
                    <div class="col-md-4">
                        <label class="small text-muted d-block">Lý do chính nghỉ việc</label>
                        <span class="badge bg-danger fs-6 mt-1"><?= e($survey['ly_do_chinh']) ?></span>
                    </div>
                    <div class="col-md-4">
                        <label class="small text-muted d-block">Loại hình DN mới</label>
                        <span class="fw-bold text-dark"><?= e($survey['cong_ty_moi_loai_hinh'] ?: 'Chưa tiết lộ') ?></span>
                    </div>
                    <div class="col-md-4">
                        <label class="small text-muted d-block">Mức lương mới tăng</label>
                        <span class="fw-bold text-success">+<?= (float)$survey['muc_luong_moi_tang_phan_tram'] ?>%</span>
                    </div>
                </div>
                <div class="row g-3 p-3 bg-light rounded border mb-3">
                    <div class="col-4 col-md-2 text-center">
                        <small class="text-muted d-block">Lương thưởng</small>
                        <span class="fw-bold fs-5 text-primary"><?= $survey['hai_long_luong_thuong'] ?>/5</span>
                    </div>
                    <div class="col-4 col-md-2 text-center">
                        <small class="text-muted d-block">Quản lý trực tiếp</small>
                        <span class="fw-bold fs-5 text-primary"><?= $survey['hai_long_quan_ly'] ?>/5</span>
                    </div>
                    <div class="col-4 col-md-2 text-center">
                        <small class="text-muted d-block">Thăng tiến</small>
                        <span class="fw-bold fs-5 text-primary"><?= $survey['hai_long_co_hoi_thang_tien'] ?>/5</span>
                    </div>
                    <div class="col-4 col-md-2 text-center">
                        <small class="text-muted d-block">Áp lực OT</small>
                        <span class="fw-bold fs-5 text-warning"><?= $survey['ap_luc_cong_viec_ot'] ?>/5</span>
                    </div>
                    <div class="col-4 col-md-2 text-center">
                        <small class="text-muted d-block">Môi trường</small>
                        <span class="fw-bold fs-5 text-primary"><?= $survey['hai_long_moi_truong_dong_nghiep'] ?>/5</span>
                    </div>
                    <div class="col-4 col-md-2 text-center">
                        <small class="text-muted d-block">Phúc lợi</small>
                        <span class="fw-bold fs-5 text-primary"><?= $survey['hai_long_chinh_sach_phuc_loi'] ?>/5</span>
                    </div>
                </div>
                <?php if (!empty($survey['dieu_gi_vnm_nen_cai_thien'])): ?>
                    <div class="p-3 bg-white border rounded">
                        <strong class="small text-muted d-block mb-1"><i class="bi bi-chat-quote text-primary me-1"></i> Ý kiến đóng góp cải thiện:</strong>
                        <em>"<?= e($survey['dieu_gi_vnm_nen_cai_thien']) ?>"</em>
                    </div>
                <?php endif; ?>
            <?php else: ?>
                <form action="<?= url('offboarding/save_survey') ?>" method="POST">
                    <?= csrf_field() ?>
                    <input type="hidden" name="offboarding_id" value="<?= $offboarding['id'] ?>">
                    <input type="hidden" name="ma_nv" value="<?= e($offboarding['ma_nv']) ?>">

                    <div class="row g-3 mb-3">
                        <div class="col-md-6">
                            <label class="form-label small fw-semibold">Lý do nghỉ việc chính <span class="text-danger">*</span></label>
                            <select name="ly_do_chinh" class="form-select form-select-sm" required>
                                <option value="LUONG_THAP">Mức lương và đãi ngộ thấp</option>
                                <option value="QUAN_LY_TRUC_TIEP">Quan hệ không tốt với quản lý trực tiếp</option>
                                <option value="KHONG_CO_CO_HOI_PHAT_TRIEN">Thiếu lộ trình thăng tiến và đào tạo</option>
                                <option value="AP_LUC_OT_CAO">Áp lực công việc cao, làm thêm giờ (OT) nhiều</option>
                                <option value="MOI_TRUONG_LAM_VIEC">Môi trường làm việc không phù hợp</option>
                                <option value="CO_HOI_VIEC_LAM_TOT_HON" selected>Có cơ hội nghề nghiệp / công ty khác tốt hơn</option>
                                <option value="LY_DO_CA_NHAN_GIA_DINH">Lý do cá nhân / Gia đình</option>
                                <option value="CHUYEN_CHO_O">Thay đổi nơi cư trú / Khoảng cách xa</option>
                                <option value="KHAC">Lý do khác</option>
                            </select>
                        </div>
                        <div class="col-md-3">
                            <label class="form-label small fw-semibold">Loại hình DN mới</label>
                            <input type="text" name="cong_ty_moi_loai_hinh" class="form-control form-control-sm" placeholder="VD: Tập đoàn FDI...">
                        </div>
                        <div class="col-md-3">
                            <label class="form-label small fw-semibold">Lương mới tăng (%)</label>
                            <input type="number" step="0.5" name="muc_luong_moi_tang_phan_tram" class="form-control form-control-sm" placeholder="VD: 30%">
                        </div>
                    </div>

                    <label class="form-label small fw-semibold mb-2">Đánh giá mức độ hài lòng (Thang điểm 1 - 5 sao)</label>
                    <div class="row g-2 mb-3">
                        <div class="col-md-2">
                            <label class="small text-muted">Lương thưởng</label>
                            <select name="hai_long_luong_thuong" class="form-select form-select-sm">
                                <option value="1">1 - Rất kém</option><option value="2">2 - Kém</option><option value="3" selected>3 - Tạm ổn</option><option value="4">4 - Tốt</option><option value="5">5 - Rất tốt</option>
                            </select>
                        </div>
                        <div class="col-md-2">
                            <label class="small text-muted">Quản lý</label>
                            <select name="hai_long_quan_ly" class="form-select form-select-sm">
                                <option value="1">1 - Rất kém</option><option value="2">2 - Kém</option><option value="3">3 - Tạm ổn</option><option value="4" selected>4 - Tốt</option><option value="5">5 - Rất tốt</option>
                            </select>
                        </div>
                        <div class="col-md-2">
                            <label class="small text-muted">Thăng tiến</label>
                            <select name="hai_long_co_hoi_thang_tien" class="form-select form-select-sm">
                                <option value="1">1 - Rất kém</option><option value="2">2 - Kém</option><option value="3" selected>3 - Tạm ổn</option><option value="4">4 - Tốt</option><option value="5">5 - Rất tốt</option>
                            </select>
                        </div>
                        <div class="col-md-2">
                            <label class="small text-muted">Áp lực OT</label>
                            <select name="ap_luc_cong_viec_ot" class="form-select form-select-sm">
                                <option value="1">1 - Rất ít OT</option><option value="2">2 - Bình thường</option><option value="3">3 - Khá mệt</option><option value="4" selected>4 - Rất căng thẳng</option><option value="5">5 - Quá tải</option>
                            </select>
                        </div>
                        <div class="col-md-2">
                            <label class="small text-muted">Môi trường</label>
                            <select name="hai_long_moi_truong_dong_nghiep" class="form-select form-select-sm">
                                <option value="1">1 - Rất kém</option><option value="2">2 - Kém</option><option value="3">3 - Tạm ổn</option><option value="4" selected>4 - Tốt</option><option value="5">5 - Rất tốt</option>
                            </select>
                        </div>
                        <div class="col-md-2">
                            <label class="small text-muted">Phúc lợi</label>
                            <select name="hai_long_chinh_sach_phuc_loi" class="form-select form-select-sm">
                                <option value="1">1 - Rất kém</option><option value="2">2 - Kém</option><option value="3">3 - Tạm ổn</option><option value="4" selected>4 - Tốt</option><option value="5">5 - Rất tốt</option>
                            </select>
                        </div>
                    </div>

                    <div class="mb-3">
                        <label class="form-label small fw-semibold">Vinamilk nên cải thiện điều gì nhất?</label>
                        <textarea name="dieu_gi_vnm_nen_cai_thien" class="form-control form-control-sm" rows="2" placeholder="Góp ý thẳng thắn..."></textarea>
                    </div>

                    <div class="d-flex justify-content-between align-items-center">
                        <div class="form-check">
                            <input class="form-check-input" type="checkbox" name="san_sang_quay_lai_vnm" value="1" id="checkReturnDetail" checked>
                            <label class="form-check-label small" for="checkReturnDetail">Sẵn sàng quay lại làm việc tại Vinamilk trong tương lai</label>
                        </div>
                        <button type="submit" class="btn btn-primary btn-sm fw-semibold" style="background: #0033A0;">
                            <i class="bi bi-send-check me-1"></i> Lưu Kết Quả Khảo Sát AI
                        </button>
                    </div>
                </form>
            <?php endif; ?>
        </div>
    </div>
</div>

<?php
require_once __DIR__ . '/../layouts/footer.php';
?>
