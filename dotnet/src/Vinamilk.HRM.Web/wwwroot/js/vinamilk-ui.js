/**
 * ============================================================================
 * VINAMILK ENTERPRISE HRM — UI/UX CORE INTERACTION ENGINE (Tabler UI + BS5)
 * ============================================================================
 * Handles:
 *  1. Light / Dark Theme Switcher (persisted in localStorage & cookie)
 *  2. Collapsible Sidebar (desktop collapse + mobile drawer)
 *  3. Global Search Command Palette (Ctrl+K / Cmd+K) with live API search
 *  4. Number Count-Up Animation ([data-vnm-countup]) respecting reduced-motion
 *  5. Button Ripple Effect ([data-vnm-ripple], .vnm-btn-primary)
 *  6. NProgress Top Loading Bar on AJAX / Fetch & Page Navigation
 *  7. Toast Notification System (VnmUI.toast)
 *  8. SweetAlert2 Confirmation Wrapper (VnmUI.confirm)
 *  9. AJAX Modal Form Loader & Submitter (VnmUI.openModal)
 * 10. Flatpickr Datepicker (dd/MM/yyyy Vietnamese locale)
 * 11. TomSelect Searchable Dropdowns
 * 12. Drag & Drop File Upload Zone
 * 13. SortableJS Kanban Board Initializer
 * ============================================================================
 */
(function (window, document) {
    'use strict';

    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    const VnmUI = {
        /* --------------------------------------------------------------------
         * 1. THEME MANAGEMENT (LIGHT / DARK)
         * -------------------------------------------------------------------- */
        initTheme: function () {
            const savedTheme = localStorage.getItem('vnm_hrm_theme') || 'light';
            this.setTheme(savedTheme, false);

            document.querySelectorAll('[data-vnm-theme-toggle]').forEach(btn => {
                btn.addEventListener('click', (e) => {
                    e.preventDefault();
                    const current = document.documentElement.getAttribute('data-bs-theme') || 'light';
                    const next = current === 'dark' ? 'light' : 'dark';
                    this.setTheme(next, true);
                });
            });
        },

        setTheme: function (theme, notify) {
            document.documentElement.setAttribute('data-bs-theme', theme);
            localStorage.setItem('vnm_hrm_theme', theme);
            document.cookie = `vnm_hrm_theme=${theme};path=/;max-age=31536000;SameSite=Lax`;

            document.querySelectorAll('[data-vnm-theme-icon]').forEach(icon => {
                icon.className = theme === 'dark' ? 'ti ti-sun text-warning fs-4' : 'ti ti-moon fs-4';
            });
            document.querySelectorAll('[data-vnm-theme-label]').forEach(lbl => {
                lbl.textContent = theme === 'dark' ? 'Giao diện Sáng' : 'Giao diện Tối';
            });

            if (notify) {
                this.toast(
                    theme === 'dark' ? 'Đã bật chế độ Ban đêm (Dark Mode)' : 'Đã bật chế độ Ban ngày (Light Mode)',
                    'info'
                );
            }
            window.dispatchEvent(new CustomEvent('vnm:theme-changed', { detail: { theme } }));
        },

        /* --------------------------------------------------------------------
         * 2. SIDEBAR COLLAPSE & MOBILE DRAWER
         * -------------------------------------------------------------------- */
        initSidebar: function () {
            const sidebar = document.getElementById('vnmSidebar');
            const backdrop = document.getElementById('vnmSidebarBackdrop');
            if (!sidebar) return;

            const isCollapsed = localStorage.getItem('vnm_hrm_sidebar_collapsed') === 'true';
            if (isCollapsed && window.innerWidth >= 992) {
                sidebar.classList.add('collapsed');
            }

            // Desktop collapse toggle
            document.querySelectorAll('[data-vnm-sidebar-collapse]').forEach(btn => {
                btn.addEventListener('click', (e) => {
                    e.preventDefault();
                    if (window.innerWidth < 992) {
                        sidebar.classList.toggle('mobile-open');
                        if (backdrop) backdrop.classList.toggle('show');
                    } else {
                        sidebar.classList.toggle('collapsed');
                        localStorage.setItem('vnm_hrm_sidebar_collapsed', sidebar.classList.contains('collapsed'));
                    }
                });
            });

            // Mobile close on backdrop click
            if (backdrop) {
                backdrop.addEventListener('click', () => {
                    sidebar.classList.remove('mobile-open');
                    backdrop.classList.remove('show');
                });
            }
        },

        /* --------------------------------------------------------------------
         * 3. GLOBAL COMMAND PALETTE (CTRL+K / CMD+K)
         * -------------------------------------------------------------------- */
        initCommandPalette: function () {
            const modalEl = document.getElementById('vnmSearchModal');
            const inputEl = document.getElementById('vnmGlobalSearchInput');
            const resultsEl = document.getElementById('vnmGlobalSearchResults');
            if (!modalEl || !inputEl || !resultsEl) return;

            let bsModal = null;
            const openPalette = () => {
                if (!bsModal && window.bootstrap) {
                    bsModal = new bootstrap.Modal(modalEl);
                }
                if (bsModal) {
                    bsModal.show();
                    setTimeout(() => inputEl.focus(), 150);
                }
            };

            // Keyboard shortcut Ctrl+K / Cmd+K
            document.addEventListener('keydown', (e) => {
                if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
                    e.preventDefault();
                    openPalette();
                }
            });

            document.querySelectorAll('[data-vnm-open-search]').forEach(trigger => {
                trigger.addEventListener('click', (e) => {
                    e.preventDefault();
                    openPalette();
                });
            });

            let debounceTimer = null;
            inputEl.addEventListener('input', () => {
                clearTimeout(debounceTimer);
                const query = inputEl.value.trim();
                debounceTimer = setTimeout(() => this.performGlobalSearch(query, resultsEl), 220);
            });
        },

        performGlobalSearch: async function (query, container) {
            if (!query || query.length < 1) {
                container.innerHTML = `
                    <div class="p-3">
                        <div class="text-uppercase text-muted fw-bold fs-7 mb-2 px-2">Lối tắt điều hướng nhanh</div>
                        <a href="/" class="d-flex align-items-center justify-content-between p-2 rounded text-decoration-none text-body hover-bg-light">
                            <span><i class="ti ti-layout-dashboard me-2 text-primary"></i>Bảng điều khiển Executive HRM</span>
                            <kbd class="vnm-kbd">G + D</kbd>
                        </a>
                        <a href="/dev/styleguide" class="d-flex align-items-center justify-content-between p-2 rounded text-decoration-none text-body hover-bg-light">
                            <span><i class="ti ti-palette me-2 text-primary"></i>Vinamilk HRM Design System & Style Guide</span>
                            <kbd class="vnm-kbd">G + S</kbd>
                        </a>
                        <a href="/dev/styleguide/print-preview" target="_blank" class="d-flex align-items-center justify-content-between p-2 rounded text-decoration-none text-body hover-bg-light">
                            <span><i class="ti ti-printer me-2 text-primary"></i>Xem mẫu in Quyết định nhân sự A4 chuẩn Quốc hiệu</span>
                            <kbd class="vnm-kbd">G + P</kbd>
                        </a>
                        <a href="/swagger" target="_blank" class="d-flex align-items-center justify-content-between p-2 rounded text-decoration-none text-body hover-bg-light">
                            <span><i class="ti ti-api me-2 text-primary"></i>OpenAPI / Swagger UI (/api/v1)</span>
                            <kbd class="vnm-kbd">API</kbd>
                        </a>
                    </div>`;
                return;
            }

            container.innerHTML = `
                <div class="p-4 text-center text-muted">
                    <div class="spinner-border spinner-border-sm text-primary me-2" role="status"></div>
                    Đang tìm kiếm "${this.escapeHtml(query)}" trong toàn hệ thống...
                </div>`;

            try {
                const res = await fetch(`/api/v1/system/search?q=${encodeURIComponent(query)}`);
                if (!res.ok) throw new Error('Search request failed');
                const data = await res.json();

                let html = '<div class="p-2">';

                if (data.employees && data.employees.length > 0) {
                    html += `<div class="text-uppercase text-muted fw-bold fs-7 mt-1 mb-1 px-2">Hồ sơ Nhân sự (${data.employees.length})</div>`;
                    data.employees.forEach(emp => {
                        html += `
                        <a href="/?emp=${encodeURIComponent(emp.employeeCode)}" class="d-flex align-items-center justify-content-between p-2 rounded text-decoration-none text-body">
                            <div class="d-flex align-items-center gap-2">
                                <span class="badge bg-primary-subtle text-primary font-monospace">${this.escapeHtml(emp.employeeCode)}</span>
                                <div>
                                    <div class="fw-semibold">${this.escapeHtml(emp.fullName)}</div>
                                    <div class="text-muted fs-7">${this.escapeHtml(emp.positionName)} • ${this.escapeHtml(emp.unitName)}</div>
                                </div>
                            </div>
                            <span class="vnm-badge vnm-badge-success"><span class="vnm-badge-dot"></span>${this.escapeHtml(emp.status)}</span>
                        </a>`;
                    });
                }

                if (data.units && data.units.length > 0) {
                    html += `<div class="text-uppercase text-muted fw-bold fs-7 mt-3 mb-1 px-2">Đơn vị / Phòng ban (${data.units.length})</div>`;
                    data.units.forEach(u => {
                        html += `
                        <a href="/?unit=${encodeURIComponent(u.code)}" class="d-flex align-items-center justify-content-between p-2 rounded text-decoration-none text-body">
                            <div>
                                <i class="ti ti-building-Skyscraper text-primary me-2"></i>
                                <span class="fw-semibold">${this.escapeHtml(u.name)}</span>
                            </div>
                            <span class="badge bg-secondary-subtle text-secondary font-monospace">${this.escapeHtml(u.code)}</span>
                        </a>`;
                    });
                }

                if ((!data.employees || !data.employees.length) && (!data.units || !data.units.length)) {
                    html += `
                        <div class="vnm-empty-state py-4">
                            <i class="ti ti-search-off fs-1 text-muted mb-2"></i>
                            <div class="fw-semibold">Không tìm thấy kết quả phù hợp</div>
                            <div class="text-muted fs-7">Thử tìm theo Mã NV (VD: VNM-0001), Họ tên hoặc Tên phòng ban.</div>
                        </div>`;
                }

                html += '</div>';
                container.innerHTML = html;
            } catch (err) {
                container.innerHTML = `<div class="p-3 text-danger"><i class="ti ti-alert-triangle me-1"></i>Không thể tải kết quả tìm kiếm.</div>`;
            }
        },

        /* --------------------------------------------------------------------
         * 4. NUMBER COUNT-UP ANIMATION ([data-vnm-countup])
         * -------------------------------------------------------------------- */
        initCountUp: function () {
            const elements = document.querySelectorAll('[data-vnm-countup]');
            elements.forEach(el => {
                const targetRaw = el.getAttribute('data-vnm-countup') || '0';
                const target = parseFloat(targetRaw.replace(/,/g, ''));
                const prefix = el.getAttribute('data-vnm-prefix') || '';
                const suffix = el.getAttribute('data-vnm-suffix') || '';
                const decimals = parseInt(el.getAttribute('data-vnm-decimals') || '0', 10);

                if (isNaN(target)) return;

                if (prefersReducedMotion) {
                    el.textContent = prefix + target.toLocaleString('vi-VN', {
                        minimumFractionDigits: decimals,
                        maximumFractionDigits: decimals
                    }) + suffix;
                    return;
                }

                const duration = 1100;
                const startTime = performance.now();

                const step = (currentTime) => {
                    const elapsed = currentTime - startTime;
                    const progress = Math.min(elapsed / duration, 1);
                    // easeOutQuart
                    const ease = 1 - Math.pow(1 - progress, 4);
                    const currentVal = target * ease;

                    el.textContent = prefix + currentVal.toLocaleString('vi-VN', {
                        minimumFractionDigits: decimals,
                        maximumFractionDigits: decimals
                    }) + suffix;

                    if (progress < 1) {
                        requestAnimationFrame(step);
                    }
                };
                requestAnimationFrame(step);
            });
        },

        /* --------------------------------------------------------------------
         * 5. BUTTON RIPPLE EFFECT
         * -------------------------------------------------------------------- */
        initRipple: function () {
            if (prefersReducedMotion) return;
            document.addEventListener('click', (e) => {
                const btn = e.target.closest('.vnm-btn-primary, [data-vnm-ripple]');
                if (!btn) return;

                const rect = btn.getBoundingClientRect();
                const ripple = document.createElement('span');
                const size = Math.max(rect.width, rect.height);
                ripple.className = 'vnm-ripple-wave';
                ripple.style.width = ripple.style.height = `${size}px`;
                ripple.style.left = `${e.clientX - rect.left - size / 2}px`;
                ripple.style.top = `${e.clientY - rect.top - size / 2}px`;

                btn.appendChild(ripple);
                setTimeout(() => ripple.remove(), 650);
            });
        },

        /* --------------------------------------------------------------------
         * 6. NPROGRESS LOADING BAR ON AJAX / FETCH
         * -------------------------------------------------------------------- */
        initNProgress: function () {
            if (!window.NProgress) return;
            NProgress.configure({ showSpinner: false, speed: 350, minimum: 0.12 });

            const origFetch = window.fetch;
            let activeRequests = 0;

            window.fetch = async function (...args) {
                if (activeRequests === 0) NProgress.start();
                activeRequests++;
                try {
                    return await origFetch.apply(this, args);
                } finally {
                    activeRequests--;
                    if (activeRequests <= 0) {
                        activeRequests = 0;
                        NProgress.done();
                    }
                }
            };
        },

        /* --------------------------------------------------------------------
         * 7. TOAST NOTIFICATION SYSTEM
         * -------------------------------------------------------------------- */
        toast: function (message, type = 'success', title = null) {
            let container = document.getElementById('vnmToastContainer');
            if (!container) {
                container = document.createElement('div');
                container.id = 'vnmToastContainer';
                container.className = 'toast-container position-fixed top-0 end-0 p-3';
                container.style.zIndex = '1095';
                document.body.appendChild(container);
            }

            const config = {
                success: { icon: 'ti-circle-check', color: 'var(--vnm-success)', defaultTitle: 'Thành công' },
                warning: { icon: 'ti-alert-triangle', color: 'var(--vnm-warning)', defaultTitle: 'Cảnh báo' },
                danger:  { icon: 'ti-Xbox-x', color: 'var(--vnm-danger)', defaultTitle: 'Lỗi nghiệp vụ' },
                info:    { icon: 'ti-info-circle', color: 'var(--vnm-info)', defaultTitle: 'Thông báo hệ thống' }
            }[type] || { icon: 'ti-info-circle', color: 'var(--vnm-primary)', defaultTitle: 'Thông báo' };

            const toastEl = document.createElement('div');
            toastEl.className = 'toast show vnm-toast-enter border-0 mb-2';
            toastEl.setAttribute('role', 'status');
            toastEl.setAttribute('aria-live', 'polite');
            toastEl.style.background = 'var(--vnm-bg-surface)';
            toastEl.style.boxShadow = 'var(--vnm-shadow-lg)';
            toastEl.style.borderLeft = `4px solid ${config.color}`;
            toastEl.style.borderRadius = 'var(--vnm-radius-md)';

            toastEl.innerHTML = `
                <div class="d-flex align-items-start p-3 gap-2">
                    <i class="ti ${config.icon} fs-3" style="color: ${config.color};"></i>
                    <div class="flex-grow-1">
                        <div class="fw-bold fs-6">${this.escapeHtml(title || config.defaultTitle)}</div>
                        <div class="text-secondary fs-6 mt-1">${this.escapeHtml(message)}</div>
                    </div>
                    <button type="button" class="btn-close ms-2" aria-label="Đóng"></button>
                </div>`;

            toastEl.querySelector('.btn-close').addEventListener('click', () => toastEl.remove());
            container.appendChild(toastEl);

            setTimeout(() => {
                if (toastEl.parentNode) {
                    toastEl.style.opacity = '0';
                    toastEl.style.transition = 'opacity 0.25s ease';
                    setTimeout(() => toastEl.remove(), 260);
                }
            }, 4200);
        },

        /* --------------------------------------------------------------------
         * 8. SWEETALERT2 CONFIRMATION DIALOG
         * -------------------------------------------------------------------- */
        confirm: function (options = {}) {
            const defaults = {
                title: 'Xác nhận thao tác?',
                text: 'Hành động này sẽ được ghi nhận vào Nhật ký Kiểm toán (AuditLog).',
                icon: 'warning',
                confirmButtonText: 'Đồng ý thực hiện',
                cancelButtonText: 'Hủy bỏ',
                confirmButtonColor: '#0213B0',
                cancelButtonColor: '#64748B',
                onConfirm: null
            };
            const cfg = Object.assign({}, defaults, options);

            if (window.Swal) {
                Swal.fire({
                    title: cfg.title,
                    text: cfg.text,
                    icon: cfg.icon,
                    showCancelButton: true,
                    confirmButtonColor: cfg.confirmButtonColor,
                    cancelButtonColor: cfg.cancelButtonColor,
                    confirmButtonText: `<i class="ti ti-check me-1"></i>${cfg.confirmButtonText}`,
                    cancelButtonText: cfg.cancelButtonText,
                    reverseButtons: true
                }).then((result) => {
                    if (result.isConfirmed && typeof cfg.onConfirm === 'function') {
                        cfg.onConfirm();
                    }
                });
            } else if (window.confirm(`${cfg.title}\n${cfg.text}`)) {
                if (typeof cfg.onConfirm === 'function') cfg.onConfirm();
            }
        },

        /* --------------------------------------------------------------------
         * 9. FLATPICKR DATEPICKER & TOMSELECT DROPDOWNS
         * -------------------------------------------------------------------- */
        initFormWidgets: function (root = document) {
            if (window.flatpickr) {
                root.querySelectorAll('[data-vnm-datepicker]').forEach(input => {
                    flatpickr(input, {
                        dateFormat: 'd/m/Y',
                        allowInput: true,
                        locale: {
                            firstDayOfWeek: 1,
                            weekdays: {
                                shorthand: ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'],
                                longhand: ['Chủ nhật', 'Thứ hai', 'Thứ ba', 'Thứ tư', 'Thứ năm', 'Thứ sáu', 'Thứ bảy']
                            },
                            months: {
                                shorthand: ['Th1', 'Th2', 'Th3', 'Th4', 'Th5', 'Th6', 'Th7', 'Th8', 'Th9', 'Th10', 'Th11', 'Th12'],
                                longhand: ['Tháng 1', 'Tháng 2', 'Tháng 3', 'Tháng 4', 'Tháng 5', 'Tháng 6', 'Tháng 7', 'Tháng 8', 'Tháng 9', 'Tháng 10', 'Tháng 11', 'Tháng 12']
                            }
                        }
                    });
                });
            }

            if (window.TomSelect) {
                root.querySelectorAll('[data-vnm-select]').forEach(select => {
                    if (select.tomselect) return;
                    new TomSelect(select, {
                        create: false,
                        sortField: { field: 'text', direction: 'asc' },
                        placeholder: select.getAttribute('placeholder') || 'Chọn một mục...'
                    });
                });
            }
        },

        /* --------------------------------------------------------------------
         * 10. DRAG & DROP UPLOAD ZONE & KANBAN SORTABLE
         * -------------------------------------------------------------------- */
        initDropzones: function () {
            document.querySelectorAll('.vnm-dropzone').forEach(zone => {
                const fileInput = zone.querySelector('input[type="file"]');
                const statusText = zone.querySelector('[data-vnm-dropzone-status]');

                zone.addEventListener('click', (e) => {
                    if (e.target !== fileInput && fileInput) fileInput.click();
                });

                ['dragenter', 'dragover'].forEach(evt => {
                    zone.addEventListener(evt, (e) => {
                        e.preventDefault();
                        e.stopPropagation();
                        zone.classList.add('dragover');
                    });
                });

                ['dragleave', 'drop'].forEach(evt => {
                    zone.addEventListener(evt, (e) => {
                        e.preventDefault();
                        e.stopPropagation();
                        zone.classList.remove('dragover');
                    });
                });

                const handleFiles = (files) => {
                    if (!files || !files.length) return;
                    const names = Array.from(files).map(f => `${f.name} (${(f.size / 1024).toFixed(1)} KB)`).join(', ');
                    if (statusText) {
                        statusText.innerHTML = `<span class="text-success fw-semibold"><i class="ti ti-circle-check me-1"></i>Đã chọn: ${this.escapeHtml(names)}</span>`;
                    }
                    this.toast(`Đã tải tệp "${files[0].name}" vào bộ nhớ đệm`, 'success');
                };

                zone.addEventListener('drop', (e) => handleFiles(e.dataTransfer.files));
                if (fileInput) {
                    fileInput.addEventListener('change', () => handleFiles(fileInput.files));
                }
            });
        },

        initKanban: function () {
            if (!window.Sortable) return;
            document.querySelectorAll('[data-vnm-kanban-list]').forEach(list => {
                new Sortable(list, {
                    group: 'vnm-recruitment-kanban',
                    animation: prefersReducedMotion ? 0 : 180,
                    ghostClass: 'sortable-ghost',
                    onEnd: (evt) => {
                        if (evt.from !== evt.to) {
                            const candidateName = evt.item.getAttribute('data-candidate-name') || 'Ứng viên';
                            const targetStage = evt.to.getAttribute('data-stage-name') || 'Vòng mới';
                            this.toast(`Đã chuyển "${candidateName}" sang giai đoạn [${targetStage}]`, 'info', 'Kanban Tuyển dụng (ATS)');
                        }
                    }
                });
            });
        },

        escapeHtml: function (str) {
            if (str === null || str === undefined) return '';
            return String(str)
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&#039;');
        },

        initAll: function () {
            this.initTheme();
            this.initSidebar();
            this.initCommandPalette();
            this.initCountUp();
            this.initRipple();
            this.initNProgress();
            this.initFormWidgets();
            this.initDropzones();
            this.initKanban();
            if (window.AOS && !prefersReducedMotion) {
                AOS.init({ duration: 450, once: true, offset: 25 });
            }
        }
    };

    window.VnmUI = VnmUI;
    document.addEventListener('DOMContentLoaded', () => VnmUI.initAll());
})(window, document);

