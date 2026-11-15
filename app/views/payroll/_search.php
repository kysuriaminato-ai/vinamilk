<?php
/**
 * Ô tìm kiếm nhân viên dùng chung cho các trang lương.
 * Gõ mã NV, họ tên, đơn vị hoặc chức vụ -> chọn để mở trang Chi tiết tính lương.
 * Biến tùy chọn: $thang, $nam (giữ kỳ lương khi chuyển nhân viên)
 */
$searchPeriod = isset($thang, $nam) ? ('?thang=' . (int)$thang . '&nam=' . (int)$nam) : '';
?>
<style>
    .pls { position: relative; flex: 1; min-width: 260px; max-width: 520px; }
    .pls input { width: 100%; padding: 11px 14px 11px 40px; border: 2px solid var(--vnm-primary); border-radius: 10px; font-size: .95rem; background: var(--bg-card); color: var(--text-primary); }
    .pls input:focus { outline: none; box-shadow: 0 0 0 3px rgba(0, 86, 160, .18); }
    .pls .ico { position: absolute; left: 13px; top: 50%; transform: translateY(-50%); color: var(--vnm-primary); pointer-events: none; }
    .pls ul { position: absolute; z-index: 60; left: 0; right: 0; top: calc(100% + 4px); margin: 0; padding: 4px; list-style: none; background: var(--bg-card); border: 1px solid var(--border-color); border-radius: 10px; box-shadow: var(--shadow-lg); max-height: 360px; overflow-y: auto; }
    .pls li a { display: block; padding: 8px 10px; border-radius: 8px; color: var(--text-primary); text-decoration: none; }
    .pls li a:hover, .pls li a.active { background: var(--table-row-hover); }
    .pls li small { display: block; color: var(--text-secondary); font-size: .74rem; }
    .pls .empty { padding: 10px; color: var(--text-muted); font-size: .85rem; }
</style>
<div class="pls no-print" data-search-url="<?= url('luong/search') ?>" data-detail-url="<?= url('luong/detail/') ?>" data-period="<?= htmlspecialchars($searchPeriod) ?>">
    <svg class="ico" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" aria-hidden="true"><circle cx="11" cy="11" r="7"/><path d="M20 20l-3.5-3.5"/></svg>
    <input type="search" autocomplete="off" placeholder="Tìm nhân viên: mã NV, họ tên, đơn vị, chức vụ…" aria-label="Tìm nhân viên để xem chi tiết lương">
    <ul hidden></ul>
</div>
<script>
(function () {
    const box = document.currentScript.previousElementSibling;
    const input = box.querySelector('input'), list = box.querySelector('ul');
    let timer = null, items = [], active = -1;
    const esc = s => String(s ?? '').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
    const go = i => { if (items[i]) location.href = box.dataset.detailUrl + encodeURIComponent(items[i].MaNV) + box.dataset.period; };
    const status = t => ({ 1: '', 2: ' · thử việc', 3: ' · đã nghỉ việc', 4: ' · nghỉ hưu' }[t] ?? '');
    function render() {
        list.hidden = false;
        list.innerHTML = items.length
            ? items.map((e, i) => `<li><a href="#" data-i="${i}" class="${i === active ? 'active' : ''}"><strong>${esc(e.HoTen)}</strong> <code>${esc(e.MaNV)}</code><small>${esc(e.TenCV || '')} · ${esc(e.TenDV || '')}${status(e.TrangThai)}</small></a></li>`).join('')
            : '<li class="empty">Không tìm thấy nhân viên phù hợp</li>';
    }
    input.addEventListener('input', () => {
        clearTimeout(timer);
        const q = input.value.trim();
        if (!q) { list.hidden = true; items = []; return; }
        timer = setTimeout(async () => {
            try {
                const r = await fetch(box.dataset.searchUrl + '?q=' + encodeURIComponent(q), { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                items = (await r.json()).data || []; active = items.length ? 0 : -1; render();
            } catch (err) { items = []; list.hidden = false; list.innerHTML = '<li class="empty">Không tải được kết quả tìm kiếm</li>'; }
        }, 200);
    });
    input.addEventListener('keydown', e => {
        if (e.key === 'ArrowDown' || e.key === 'ArrowUp') { if (!items.length) return; e.preventDefault(); active = (active + (e.key === 'ArrowDown' ? 1 : -1) + items.length) % items.length; render(); }
        else if (e.key === 'Enter') { e.preventDefault(); go(active < 0 ? 0 : active); }
        else if (e.key === 'Escape') { list.hidden = true; }
    });
    list.addEventListener('mousedown', e => { const a = e.target.closest('a[data-i]'); if (a) { e.preventDefault(); go(+a.dataset.i); } });
    document.addEventListener('click', e => { if (!box.contains(e.target)) list.hidden = true; });
})();
</script>
