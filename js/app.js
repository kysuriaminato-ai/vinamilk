// ============================================
// VINAMILK HRIS - Main Application
// Router registration & initialization
// ============================================

(function() {
  'use strict';

  // Register routes
  Router.register('/dashboard', (container) => DashboardModule.render(container));
  Router.register('/orgchart', (container) => OrgChartModule.render(container));
  Router.register('/employees', (container, params) => EmployeesModule.render(container, params));
  Router.register('/recruitment', (container) => RecruitmentModule.render(container));
  Router.register('/attendance', (container) => AttendanceModule.render(container));
  Router.register('/payroll', (container, params) => PayrollModule.render(container, params));
  Router.register('/contracts', (container) => ContractsModule.render(container));
  Router.register('/training', (container) => TrainingModule.render(container));
  Router.register('/reports', (container) => ReportsModule.render(container));
  Router.register('/categories', (container, params) => CategoriesModule.render(container, params));
  Router.register('/roles', (container) => RolesModule.render(container));

  // Initialize Auth
  if (typeof Auth !== 'undefined') {
    Auth.init();
  }

  // Initialize router
  Router.init();

  // Set default route if none
  if (!window.location.hash || window.location.hash === '#') {
    window.location.hash = '/dashboard';
  }

  // Global search handler
  const globalSearch = document.getElementById('global-search');
  if (globalSearch) {
    globalSearch.addEventListener('input', Helpers.debounce((e) => {
      const query = e.target.value.trim();
      if (query.length >= 2) {
        const results = Helpers.searchFilter(EmployeesData, query, ['name', 'id', 'position', 'email', 'phone']);
        showSearchResults(results.slice(0, 5), query);
      } else {
        hideSearchResults();
      }
    }, 300));

    globalSearch.addEventListener('blur', () => {
      setTimeout(hideSearchResults, 200);
    });
  }

  function showSearchResults(results, query) {
    let dropdown = document.getElementById('search-dropdown');
    if (!dropdown) {
      dropdown = document.createElement('div');
      dropdown.id = 'search-dropdown';
      dropdown.className = 'dropdown-menu show';
      dropdown.style.cssText = 'position:absolute;top:100%;left:0;right:0;margin-top:4px;max-height:320px;overflow-y:auto;';
      const searchBox = document.querySelector('.header-search');
      searchBox.style.position = 'relative';
      searchBox.appendChild(dropdown);
    }

    if (results.length === 0) {
      dropdown.innerHTML = `<div style="padding:12px;text-align:center;color:var(--text-tertiary);font-size:var(--font-size-sm)">Không tìm thấy kết quả</div>`;
    } else {
      dropdown.innerHTML = results.map(emp => `
        <div class="dropdown-item" onclick="window.location.hash='/employees?id=${emp.id}'; document.getElementById('search-dropdown').remove();">
          <div style="display:flex;align-items:center;gap:8px;width:100%">
            ${Helpers.avatarHTML(emp.name, 28)}
            <div style="flex:1">
              <div style="font-weight:600;font-size:var(--font-size-base)">${emp.name}</div>
              <div style="font-size:var(--font-size-xs);color:var(--text-tertiary)">${emp.id} · ${emp.position}</div>
            </div>
            ${Helpers.statusBadge(emp.status)}
          </div>
        </div>
      `).join('');
    }
    dropdown.style.display = 'block';
  }

  function hideSearchResults() {
    const dropdown = document.getElementById('search-dropdown');
    if (dropdown) dropdown.style.display = 'none';
  }

  // Notification badge animation
  const notifDot = document.querySelector('.notification-dot');
  if (notifDot) {
    setInterval(() => {
      notifDot.style.animation = 'pulse 1s ease';
      setTimeout(() => notifDot.style.animation = '', 1000);
    }, 5000);
  }

  // Theme Switcher Engine (Sáng / Tối / Hệ thống)
  const themeButtons = document.querySelectorAll('[data-theme-mode]');
  function applyTheme(mode) {
    localStorage.setItem('vnm_theme', mode);
    document.documentElement.setAttribute('data-theme', mode);
    themeButtons.forEach(btn => {
      if (btn.getAttribute('data-theme-mode') === mode) {
        btn.classList.add('active');
      } else {
        btn.classList.remove('active');
      }
    });
  }

  const currentTheme = localStorage.getItem('vnm_theme') || 'light';
  applyTheme(currentTheme);

  themeButtons.forEach(btn => {
    btn.addEventListener('click', function() {
      applyTheme(this.getAttribute('data-theme-mode'));
    });
  });

  console.log(' VINAMILK HRIS v1.0 — Initialized');
  console.log(' Loaded', EmployeesData.length, 'employees');
})();

