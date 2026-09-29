/**
 * =====================================================================
 * VINAMILK HRM - Interactive JavaScript
 * =====================================================================
 */

document.addEventListener('DOMContentLoaded', function () {
    // 1. Password Toggle Eye Icon
    const togglePasswordButtons = document.querySelectorAll('.toggle-password');
    togglePasswordButtons.forEach(btn => {
        btn.addEventListener('click', function () {
            const input = this.previousElementSibling;
            if (input && (input.type === 'password' || input.type === 'text')) {
                if (input.type === 'password') {
                    input.type = 'text';
                    this.textContent = '️';
                } else {
                    input.type = 'password';
                    this.textContent = '';
                }
            }
        });
    });

    // 2. Select All Checkboxes in Dynamic Permission Matrix
    const selectAllBtn = document.getElementById('btn-select-all-perms');
    const deselectAllBtn = document.getElementById('btn-deselect-all-perms');
    const permCheckboxes = document.querySelectorAll('.matrix-checkbox');

    if (selectAllBtn) {
        selectAllBtn.addEventListener('click', function () {
            permCheckboxes.forEach(cb => cb.checked = true);
        });
    }

    if (deselectAllBtn) {
        deselectAllBtn.addEventListener('click', function () {
            permCheckboxes.forEach(cb => cb.checked = false);
        });
    }

    // 3. Tự động ẩn các thông báo Alert thành công sau 5 giây
    const successAlerts = document.querySelectorAll('.alert-success');
    successAlerts.forEach(alert => {
        setTimeout(() => {
            alert.style.transition = 'opacity 0.5s ease';
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 500);
        }, 5000);
    // 4. Theme Switcher Engine (Sáng / Tối / Hệ thống)
    const themeButtons = document.querySelectorAll('[data-theme-mode]');
    
    function applyTheme(mode) {
        localStorage.setItem('vnm_theme', mode);
        document.documentElement.setAttribute('data-theme', mode);
        
        // Cập nhật trạng thái active cho nút bấm
        themeButtons.forEach(btn => {
            if (btn.getAttribute('data-theme-mode') === mode) {
                btn.classList.add('active');
            } else {
                btn.classList.remove('active');
            }
        });
    }

    const currentSavedTheme = localStorage.getItem('vnm_theme') || 'system';
    applyTheme(currentSavedTheme);

    themeButtons.forEach(btn => {
        btn.addEventListener('click', function () {
            const mode = this.getAttribute('data-theme-mode');
            applyTheme(mode);
        });
    });

    // Lắng nghe thay đổi hệ điều hành khi ở chế độ 'system'
    if (window.matchMedia) {
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
            if ((localStorage.getItem('vnm_theme') || 'system') === 'system') {
                document.documentElement.setAttribute('data-theme', 'system');
            }
        });
    }
});

