<?php
/**
 * View: Cơ cấu tổ chức Vinamilk (dm_donvi)
 */
?>
<div class="container-fluid px-0">
    <div class="d-flex justify-content-between align-items-center mb-4 pb-2 border-bottom">
        <div>
            <h4 class="fw-bold text-navy mb-1" style="color: var(--vnm-primary);">
                <i class="bi bi-diagram-3-fill me-2"></i>Cơ Cấu Tổ Chức & Cây Đơn Vị Vinamilk
            </h4>
            <p class="text-muted small mb-0">Hệ thống phân cấp 5 tầng: Trụ sở chính TP.HCM &rarr; Khối chức năng &rarr; Nhà máy / Trang trại / Chi nhánh &rarr; Phòng / Ban / Xưởng &rarr; Tổ / Ca</p>
        </div>
        <button class="btn btn-outline-secondary btn-sm" onclick="window.print()">
            <i class="bi bi-printer me-1"></i> In sơ đồ (A4)
        </button>
    </div>

    <div class="row g-4">
        <div class="col-12">
            <div class="card card-custom">
                <div class="card-header bg-white py-3 d-flex justify-content-between align-items-center border-bottom">
                    <h6 class="fw-bold mb-0 text-primary">
                        <i class="bi bi-folder2-open me-2"></i>Danh mục Đơn vị Thành viên (dm_donvi)
                    </h6>
                    <span class="badge bg-primary rounded-pill"><?= count($units) ?> Đơn vị</span>
                </div>
                <div class="table-responsive">
                    <table class="table table-hover align-middle mb-0">
                        <thead class="table-light">
                            <tr>
                                <th style="width: 140px;">Mã Đơn Vị</th>
                                <th>Tên Đơn Vị / Phòng Ban</th>
                                <th style="width: 120px;">Cấp</th>
                                <th style="width: 150px;">Phân Loại</th>
                                <th style="width: 180px;">Đơn Vị Cấp Trên</th>
                                <th class="text-end" style="width: 130px;">Nhân Sự</th>
                            </tr>
                        </thead>
                        <tbody>
                            <?php foreach ($units as $u): 
                                $capBadge = match((int)$u['CapDV']) {
                                    1 => 'bg-danger',
                                    2 => 'bg-primary',
                                    3 => 'bg-success',
                                    4 => 'bg-info text-dark',
                                    5 => 'bg-secondary',
                                    default => 'bg-light text-dark'
                                };
                                $capName = match((int)$u['CapDV']) {
                                    1 => 'Cấp 1 - Trụ sở',
                                    2 => 'Cấp 2 - Khối',
                                    3 => 'Cấp 3 - Nhà máy / Chi nhánh',
                                    4 => 'Cấp 4 - Phòng / Xưởng',
                                    5 => 'Cấp 5 - Tổ / Ca',
                                    default => 'Cấp ' . $u['CapDV']
                                };
                                $indent = str_repeat('&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;', max(0, (int)$u['CapDV'] - 1));
                            ?>
                            <tr>
                                <td class="font-monospace fw-bold text-navy"><?= e($u['MaDV']) ?></td>
                                <td>
                                    <?= $indent ?>
                                    <?php if ($u['CapDV'] > 1): ?>
                                        <i class="bi bi-arrow-return-right text-muted me-1"></i>
                                    <?php else: ?>
                                        <i class="bi bi-building text-danger me-1"></i>
                                    <?php endif; ?>
                                    <span class="<?= $u['CapDV'] <= 2 ? 'fw-bold' : '' ?>"><?= e($u['TenDV']) ?></span>
                                </td>
                                <td>
                                    <span class="badge <?= $capBadge ?>"><?= $capName ?></span>
                                </td>
                                <td>
                                    <span class="badge bg-light text-dark border"><?= e($u['LoaiDV']) ?></span>
                                </td>
                                <td class="text-muted small">
                                    <?= e($u['TenDV_Cha'] ?: '-') ?>
                                </td>
                                <td class="text-end fw-bold">
                                    <span class="badge bg-light text-primary border px-2 py-1">
                                        <?= number_format($headcountMap[$u['MaDV']] ?? 0) ?> người
                                    </span>
                                </td>
                            </tr>
                            <?php endforeach; ?>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>
</div>
