<?php
/**
 * =====================================================================
 * VINAMILK HRM - GIAO DIỆN ĐỔI MẬT KHẨU (CHANGE PASSWORD VIEW)
 * =====================================================================
 * Tuân thủ WCAG AA: Đầy đủ label, focus outline rõ ràng, thông báo trực quan.
 */
?>
<div class="container py-4">
    <div class="row justify-content-center">
        <div class="col-12 col-md-8 col-lg-6">
            <div class="card-vnm p-4 p-md-5">
                <div class="text-center mb-4">
                    <div class="rounded-circle bg-primary bg-opacity-10 text-primary d-inline-flex align-items-center justify-content-center mb-3" style="width: 64px; height: 64px; font-size: 1.75rem;">
                        <i class="bi bi-shield-lock-fill" aria-hidden="true"></i>
                    </div>
                    <h1 class="h4 fw-bold text-navy mb-1" style="color: var(--vnm-primary);">
                        <?= !empty($isFirstTime) ? 'Đổi Mật Khẩu Lần Đầu' : 'Cập Nhật Mật Khẩu' ?>
                    </h1>
                    <p class="text-muted small">
                        <?= !empty($isFirstTime) 
                            ? 'Vì lý do bảo mật, bạn bắt buộc phải đổi mật khẩu mặc định trong lần đăng nhập đầu tiên.' 
                            : 'Vui lòng thiết lập mật khẩu mạnh để bảo vệ tài khoản của bạn.' ?>
                    </p>
                </div>

                <form action="<?= url('auth/change_password') ?>" method="POST">
                    <?= csrf_field() ?>

                    <!-- MẬT KHẨU HIỆN TẠI -->
                    <div class="mb-3">
                        <label for="current_password" class="form-label fw-semibold text-dark small">
                            Mật khẩu hiện tại <span class="text-danger" aria-hidden="true">*</span>
                        </label>
                        <div class="input-group">
                            <span class="input-group-text bg-white border-end-0">
                                <i class="bi bi-key text-muted" aria-hidden="true"></i>
                            </span>
                            <input type="password" 
                                   class="form-control border-start-0" 
                                   id="current_password" 
                                   name="current_password" 
                                   required 
                                   autocomplete="current-password"
                                   placeholder="Nhập mật khẩu đang sử dụng">
                        </div>
                    </div>

                    <!-- MẬT KHẨU MỚI -->
                    <div class="mb-3">
                        <label for="new_password" class="form-label fw-semibold text-dark small">
                            Mật khẩu mới <span class="text-danger" aria-hidden="true">*</span>
                        </label>
                        <div class="input-group">
                            <span class="input-group-text bg-white border-end-0">
                                <i class="bi bi-lock text-muted" aria-hidden="true"></i>
                            </span>
                            <input type="password" 
                                   class="form-control border-start-0" 
                                   id="new_password" 
                                   name="new_password" 
                                   required 
                                   autocomplete="new-password"
                                   aria-describedby="passwordHelp"
                                   placeholder="Tối thiểu 8 ký tự, gồm hoa, thường, số, ký tự đặc biệt">
                        </div>
                        <div id="passwordHelp" class="form-text text-muted small">
                            Mật khẩu phải có ít nhất 8 ký tự, gồm chữ hoa (A-Z), chữ thường (a-z), chữ số (0-9) và ký tự đặc biệt (@$!%*?&).
                        </div>
                    </div>

                    <!-- XÁC NHẬN MẬT KHẨU MỚI -->
                    <div class="mb-4">
                        <label for="confirm_password" class="form-label fw-semibold text-dark small">
                            Xác nhận mật khẩu mới <span class="text-danger" aria-hidden="true">*</span>
                        </label>
                        <div class="input-group">
                            <span class="input-group-text bg-white border-end-0">
                                <i class="bi bi-shield-check text-muted" aria-hidden="true"></i>
                            </span>
                            <input type="password" 
                                   class="form-control border-start-0" 
                                   id="confirm_password" 
                                   name="confirm_password" 
                                   required 
                                   autocomplete="new-password"
                                   placeholder="Nhập lại chính xác mật khẩu mới">
                        </div>
                    </div>

                    <div class="d-grid gap-2">
                        <button type="submit" class="btn btn-vnm-primary py-2 fw-semibold">
                            <i class="bi bi-check-circle me-1" aria-hidden="true"></i> Lưu Mật Khẩu Mới
                        </button>
                        <?php if (empty($isFirstTime)): ?>
                            <a href="<?= url('dashboard') ?>" class="btn btn-outline-secondary py-2">
                                Hủy Bỏ
                            </a>
                        <?php endif; ?>
                    </div>
                </form>
            </div>
        </div>
    </div>
</div>
