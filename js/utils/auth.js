// ============================================
// VINAMILK HRIS - Auth Module
// ============================================

const Auth = {
  // Mocked logged in user for demonstration
  currentUser: {
    id: 'NV001',
    name: 'Nguyễn T. Thanh Huyền',
    role: 'Admin', // Roles: Admin, HR_Manager, Employee
    department: 'Ban Giám Đốc'
  },
  
  // Define permissions mapping
  permissions: {
    'Admin': ['all'],
    'HR_Manager': ['view_dashboard', 'manage_employees', 'manage_recruitment', 'manage_contracts', 'manage_training', 'manage_attendance', 'manage_payroll', 'view_reports', 'manage_categories'],
    'Employee': ['view_dashboard', 'view_orgchart', 'view_own_profile', 'view_own_attendance', 'view_own_payroll']
  },

  getCurrentUser() {
    return this.currentUser;
  },

  hasRole(roleName) {
    return this.currentUser.role === roleName;
  },

  hasPermission(permission) {
    if (this.currentUser.role === 'Admin') return true;
    const userPermissions = this.permissions[this.currentUser.role] || [];
    return userPermissions.includes(permission) || userPermissions.includes('all');
  },

  init() {
    console.log(`🔑 Logged in as: ${this.currentUser.name} (${this.currentUser.role})`);
    this.applyUIPermissions();
  },

  applyUIPermissions() {
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
