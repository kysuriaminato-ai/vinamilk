// ============================================
// VINAMILK HRIS - Auth Module
// Login, logout, session, role-based access
// ============================================

const Auth = {
  // Demo user accounts database
  accounts: [
    {
      username: 'admin',
      password: 'admin123',
      id: 'NV001',
      name: 'Nguyễn T. Thanh Huyền',
      role: 'Admin',
      department: 'Ban Giám Đốc',
      title: 'CHRO'
    },
    {
      username: 'hr_manager',
      password: 'hr123',
      id: 'NV003',
      name: 'Trần Minh Đức',
      role: 'HR_Manager',
      department: 'Phòng Nhân sự',
      title: 'Trưởng phòng NS'
    },
    {
      username: 'nv001',
      password: 'nv123',
      id: 'VNM-0025',
      name: 'Phạm Thị Lan Anh',
      role: 'Employee',
      department: 'NM Sữa Bình Dương',
      title: 'Nhân viên QC'
    }
  ],

  currentUser: null,

  // Define permissions mapping
  permissions: {
    'Admin': ['all'],
    'HR_Manager': ['view_dashboard', 'manage_employees', 'manage_recruitment', 'manage_contracts', 'manage_training', 'manage_attendance', 'manage_payroll', 'view_reports', 'manage_categories'],
    'Employee': ['view_dashboard', 'view_orgchart', 'view_own_profile', 'view_own_attendance', 'view_own_payroll']
  },

  // Initialize auth - check session
  init() {
    const savedSession = localStorage.getItem('vnm_session');
    if (savedSession) {
      try {
        const session = JSON.parse(savedSession);
        // Validate session (check if account still exists)
        const account = this.accounts.find(a => a.username === session.username);
        if (account) {
          this.currentUser = {
            id: account.id,
            name: account.name,
            role: account.role,
            department: account.department,
            title: account.title,
            username: account.username
          };
          this.showApp();
          console.log(` Session restored: ${this.currentUser.name} (${this.currentUser.role})`);
          return;
        }
      } catch (e) {
        localStorage.removeItem('vnm_session');
      }
    }
    // No valid session — show login
    this.showLogin();
    this.bindLoginEvents();
  },

  // Show login screen, hide app
  showLogin() {
    const loginScreen = document.getElementById('login-screen');
    const appShell = document.getElementById('app-shell');
    const header = document.getElementById('header');

    if (loginScreen) loginScreen.classList.remove('hidden');
    if (loginScreen) loginScreen.style.display = '';
    if (appShell) appShell.style.display = 'none';
    if (header) header.style.display = 'none';
  },

  // Show app, hide login
  showApp() {
    const loginScreen = document.getElementById('login-screen');
    const appShell = document.getElementById('app-shell');
    const header = document.getElementById('header');

    if (loginScreen) loginScreen.style.display = 'none';
    if (appShell) appShell.style.display = '';
    if (header) header.style.display = '';

    // Update sidebar user info
    this.updateSidebarUser();
    this.applyUIPermissions();
  },

  // Update sidebar user display
  updateSidebarUser() {
    if (!this.currentUser) return;
    const avatar = document.getElementById('sidebar-avatar');
    const username = document.getElementById('sidebar-username');
    const role = document.getElementById('sidebar-role');

    if (avatar) {
      const initials = this.currentUser.name
        .split(' ')
        .filter(w => w.length > 0)
        .slice(-2)
        .map(w => w[0])
        .join('')
        .toUpperCase();
      avatar.textContent = initials;
    }
    if (username) username.textContent = this.currentUser.name;
    if (role) role.textContent = `${this.currentUser.title} · ${this.currentUser.role}`;
  },

  // Bind login form events
  bindLoginEvents() {
    const form = document.getElementById('login-form');
    const togglePass = document.getElementById('toggle-password');

    if (form) {
      form.addEventListener('submit', (e) => {
        e.preventDefault();
        this.handleLogin();
      });
    }

    if (togglePass) {
      togglePass.addEventListener('click', () => {
        const passInput = document.getElementById('login-password');
        if (passInput.type === 'password') {
          passInput.type = 'text';
          togglePass.textContent = '';
        } else {
          passInput.type = 'password';
          togglePass.textContent = '️';
        }
      });
    }

    // Enter key on inputs
    const usernameInput = document.getElementById('login-username');
    const passwordInput = document.getElementById('login-password');
    if (usernameInput) {
      usernameInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
          e.preventDefault();
          passwordInput.focus();
        }
      });
    }
  },

  // Handle login attempt
  handleLogin() {
    const usernameInput = document.getElementById('login-username');
    const passwordInput = document.getElementById('login-password');
    const errorDiv = document.getElementById('login-error');
    const btn = document.getElementById('login-btn');
    const remember = document.getElementById('login-remember');

    const username = usernameInput.value.trim().toLowerCase();
    const password = passwordInput.value;

    // Clear previous errors
    errorDiv.textContent = '';
    usernameInput.classList.remove('error');
    passwordInput.classList.remove('error');

    // Validation
    if (!username) {
      errorDiv.textContent = ' Vui lòng nhập tên đăng nhập';
      usernameInput.classList.add('error');
      usernameInput.focus();
      return;
    }
    if (!password) {
      errorDiv.textContent = ' Vui lòng nhập mật khẩu';
      passwordInput.classList.add('error');
      passwordInput.focus();
      return;
    }

    // Show loading state
    btn.disabled = true;
    btn.querySelector('.login-submit-text').style.display = 'none';
    btn.querySelector('.login-submit-loading').style.display = 'inline';

    // Simulate network delay for realism
    setTimeout(() => {
      // Authenticate
      const account = this.accounts.find(
        a => a.username === username && a.password === password
      );

      if (account) {
        // Success
        this.currentUser = {
          id: account.id,
          name: account.name,
          role: account.role,
          department: account.department,
          title: account.title,
          username: account.username
        };

        // Save session
        if (remember && remember.checked) {
          localStorage.setItem('vnm_session', JSON.stringify({
            username: account.username,
            loginTime: Date.now()
          }));
        }

        console.log(` Login success: ${account.name} (${account.role})`);

        // Transition to app
        this.showApp();

        // Re-init router if needed
        if (typeof Router !== 'undefined') {
          Router.init();
          if (!window.location.hash || window.location.hash === '#') {
            window.location.hash = '/dashboard';
          }
        }

      } else {
        // Failure
        errorDiv.textContent = '❌ Sai tên đăng nhập hoặc mật khẩu!';
        passwordInput.classList.add('error');
        usernameInput.classList.add('error');
        passwordInput.value = '';
        passwordInput.focus();

        // Shake animation
        errorDiv.style.animation = 'none';
        void errorDiv.offsetWidth; // Trigger reflow
        errorDiv.style.animation = 'loginShake 0.4s ease';
      }

      // Reset button
      btn.disabled = false;
      btn.querySelector('.login-submit-text').style.display = 'inline';
      btn.querySelector('.login-submit-loading').style.display = 'none';
    }, 800);
  },

  // Fill demo credentials
  fillDemo(username, password) {
    const usernameInput = document.getElementById('login-username');
    const passwordInput = document.getElementById('login-password');
    if (usernameInput) usernameInput.value = username;
    if (passwordInput) passwordInput.value = password;
    // Clear errors
    const errorDiv = document.getElementById('login-error');
    if (errorDiv) errorDiv.textContent = '';
    usernameInput.classList.remove('error');
    passwordInput.classList.remove('error');
    usernameInput.focus();
  },

  // Logout
  logout() {
    if (!confirm('Bạn có chắc muốn đăng xuất khỏi hệ thống?')) return;

    this.currentUser = null;
    localStorage.removeItem('vnm_session');

    // Reset login form
    const form = document.getElementById('login-form');
    if (form) form.reset();
    const errorDiv = document.getElementById('login-error');
    if (errorDiv) errorDiv.textContent = '';

    // Show login
    this.showLogin();
    this.bindLoginEvents();

    console.log(' Logged out successfully');
  },

  // Permission helpers
  getCurrentUser() {
    return this.currentUser;
  },

  hasRole(roleName) {
    return this.currentUser && this.currentUser.role === roleName;
  },

  hasPermission(permission) {
    if (!this.currentUser) return false;
    if (this.currentUser.role === 'Admin') return true;
    const userPermissions = this.permissions[this.currentUser.role] || [];
    return userPermissions.includes(permission) || userPermissions.includes('all');
  },

  applyUIPermissions() {
    if (!this.currentUser) return;
    const role = this.currentUser.role;
    // Hide/show elements with data-roles attribute
    document.querySelectorAll('[data-roles]').forEach(el => {
      const allowedRoles = el.getAttribute('data-roles').split(',').map(r => r.trim());
      if (!allowedRoles.includes(role) && !allowedRoles.includes('All')) {
        el.style.display = 'none';
      } else {
        el.style.display = ''; // Restore default
      }
    });
  }
};


