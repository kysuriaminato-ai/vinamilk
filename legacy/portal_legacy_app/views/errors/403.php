<div class="text-center py-5">
    <div class="display-1 fw-bold text-danger mb-3">403</div>
    <h3 class="fw-bold text-dark mb-2">Truy Cập Bị Từ Chối (Forbidden)</h3>
    <p class="text-muted mb-4" style="max-width: 500px; margin: 0 auto;">
        <?= e($message ?? 'Tài khoản của bạn không có vai trò hoặc quyền hạn phù hợp để truy cập vào khu vực này.') ?>
    </p>
    <a href="<?= url('dashboard') ?>" class="btn btn-primary fw-bold px-4" style="background: #002F6C; border-color: #002F6C;">
        <i class="bi bi-house-door-fill me-1"></i> Quay Lại Bảng Điều Khiển
    </a>
</div>
