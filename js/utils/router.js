// ============================================
// VINAMILK HRIS - SPA Router
// Hash-based client-side routing
// ============================================

const Router = {
  routes: {},
  currentRoute: null,

  register(path, handler) {
    this.routes[path] = handler;
  },

  navigate(path) {
    window.location.hash = path;
  },

  init() {
    window.addEventListener('hashchange', () => this._handleRoute());
    window.addEventListener('load', () => this._handleRoute());
  },

  _handleRoute() {
    const hash = window.location.hash.slice(1) || '/dashboard';
    const path = hash.split('?')[0];
    const params = this._parseParams(hash);

    // Update active nav
    document.querySelectorAll('.sidebar-menu-link').forEach(link => {
      link.classList.toggle('active', link.getAttribute('href') === '#' + path);
    });

    // Update breadcrumb
    this._updateBreadcrumb(path);

    const handler = this.routes[path];
    if (handler) {
      this.currentRoute = path;
      const container = document.getElementById('main-content');
      if (container) {
        container.style.opacity = '0';
        container.style.transform = 'translateY(8px)';
        setTimeout(() => {
          handler(container, params);
          requestAnimationFrame(() => {
            container.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
            container.style.opacity = '1';
            container.style.transform = 'translateY(0)';
          });
        }, 100);
      }
    }
  },

  _parseParams(hash) {
    const params = {};
    const queryString = hash.split('?')[1];
    if (queryString) {
      queryString.split('&').forEach(pair => {
        const [key, value] = pair.split('=');
        params[decodeURIComponent(key)] = decodeURIComponent(value || '');
      });
    }
    return params;
  },

  _updateBreadcrumb(path) {
    const breadcrumb = document.getElementById('header-breadcrumb');
    if (!breadcrumb) return;

    const routeNames = {
      '/dashboard': 'Tổng quan',
      '/orgchart': 'Sơ đồ tổ chức',
      '/employees': 'Nhân sự',
      '/recruitment': 'Tuyển dụng',
      '/attendance': 'Chấm công',
      '/payroll': 'Bảng lương',
      '/contracts': 'Hợp đồng',
      '/training': 'Đào tạo',
      '/reports': 'Báo cáo',
      '/roles': 'Phân quyền'
    };

    const name = routeNames[path] || path;
    breadcrumb.innerHTML = `
      <span class="header-breadcrumb-item">VINAMILK HRIS</span>
      <span class="header-breadcrumb-sep"></span>
      <span class="header-breadcrumb-item current">${name}</span>
    `;
  }
};

