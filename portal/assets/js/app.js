import htmx from 'htmx.org';
import Alpine from 'alpinejs';

// Expose HTMX globally
window.htmx = htmx;

// Auto-attach CSRF token to all HTMX requests
document.addEventListener('htmx:configRequest', (evt) => {
  const csrfToken = document.querySelector('meta[name="csrf-token"]')?.getAttribute('content');
  if (csrfToken) {
    evt.detail.headers['X-CSRF-TOKEN'] = csrfToken;
  }
});

// Alpine Component: Command Palette (Ctrl+K)
Alpine.data('commandPalette', () => ({
  isOpen: false,
  search: '',
  items: [
    { title: 'Dashboard Điều Hành', url: '/dashboard/executive', category: 'Điều Hành' },
    { title: 'Dashboard Đơn Vị', url: '/dashboard/unit', category: 'Đơn Vị' },
    { title: 'Hồ Sơ Nhân Sự', url: '/employees', category: 'Nhân Sự' },
    { title: 'Cơ Cấu Tổ Chức', url: '/org', category: 'Tổ Chức' },
    { title: 'Lịch Phân Ca & Công', url: '/attendance/matrix', category: 'Chấm Công' },
    { title: 'Hộp Duyệt Chung', url: '/approvals', category: 'Phê Duyệt' },
    { title: 'Quản Trị KPI', url: '/kpi/kanban', category: 'Hiệu Suất' },
    { title: 'Kỳ Lương & Phiếu Lương', url: '/payroll', category: 'Đãi Ngộ' },
    { title: 'Xét Tăng Lương', url: '/salary-review', category: 'Đãi Ngộ' },
    { title: 'Vị Thế Thị Trường & Dự Báo', url: '/analytics', category: 'Thị Trường' },
    { title: 'Cài Đặt Pháp Lý', url: '/settings', category: 'Hệ Thống' },
    { title: 'Cổng Nhân Viên (ESS)', url: '/self-service', category: 'Cá Nhân' }
  ],
  filteredItems() {
    if (!this.search.trim()) return this.items;
    const q = this.search.toLowerCase();
    return this.items.filter(i => i.title.toLowerCase().includes(q) || i.category.toLowerCase().includes(q));
  },
  init() {
    window.addEventListener('keydown', (e) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        this.isOpen = !this.isOpen;
      }
      if (e.key === 'Escape' && this.isOpen) {
        this.isOpen = false;
      }
    });
  }
}));

// Alpine Component: Theme Toggle (Dark / Light)
Alpine.data('themeToggle', () => ({
  theme: localStorage.getItem('vnm-theme') || 'light',
  init() {
    document.documentElement.setAttribute('data-theme', this.theme);
  },
  toggle() {
    this.theme = this.theme === 'light' ? 'dark' : 'light';
    localStorage.setItem('vnm-theme', this.theme);
    document.documentElement.setAttribute('data-theme', this.theme);
  }
}));

// Alpine Component: KPI Counter (Count up with reduced motion support)
Alpine.data('kpiCounter', (targetValue, formatFn) => ({
  current: 0,
  target: targetValue,
  init() {
    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (prefersReducedMotion) {
      this.current = this.target;
      return;
    }
    const duration = 1200;
    const startTime = performance.now();
    const update = (now) => {
      const elapsed = now - startTime;
      const progress = Math.min(elapsed / duration, 1);
      // easeOutExpo
      const ease = progress === 1 ? 1 : 1 - Math.pow(2, -10 * progress);
      this.current = Math.round(this.target * ease * 100) / 100;
      if (progress < 1) {
        requestAnimationFrame(update);
      } else {
        this.current = this.target;
      }
    };
    requestAnimationFrame(update);
  }
}));

// Alpine Component: Toast Notifications
Alpine.data('toastManager', () => ({
  toasts: [],
  addToast(message, type = 'info') {
    const id = Date.now();
    this.toasts.push({ id, message, type });
    setTimeout(() => {
      this.toasts = this.toasts.filter(t => t.id !== id);
    }, 3500);
  },
  init() {
    window.addEventListener('show-toast', (evt) => {
      this.addToast(evt.detail.message, evt.detail.type);
    });
  }
}));

// Start Alpine
window.Alpine = Alpine;
Alpine.start();

console.log('Vinamilk HRM Portal Asset Pipeline Loaded.');