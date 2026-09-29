// ============================================
// VINAMILK HRIS - Roles Management Module
// ============================================

const RolesModule = {
  render(container) {
    if (typeof Auth !== 'undefined' && !Auth.hasRole('Admin')) {
      container.innerHTML = `
        <div class="empty-state">
          <div class="empty-icon" style="font-size: 3rem">⛔</div>
          <h3 class="empty-title">Không có quyền truy cập</h3>
          <p class="empty-desc">Bạn cần quyền Admin để truy cập trang này.</p>
        </div>
      `;
      return;
    }

    container.innerHTML = `
      <div class="page-header">
        <div>
          <h1 class="page-title">Quản lý Phân quyền</h1>
          <p class="page-subtitle">Thiết lập vai trò và quyền hạn cho nhân viên</p>
        </div>
        <div class="header-actions">
          <button class="btn btn-primary" onclick="RolesModule.saveRoles()">
            <span class="btn-icon"></span> Lưu thay đổi
          </button>
        </div>
      </div>
      
      <div class="card">
        <div class="table-responsive">
          <table class="table" id="roles-table">
            <thead>
              <tr>
                <th>Nhân viên</th>
                <th>Phòng ban</th>
                <th>Chức vụ</th>
                <th>Vai trò hệ thống</th>
              </tr>
            </thead>
            <tbody>
              <!-- Renders from EmployeesData -->
            </tbody>
          </table>
        </div>
      </div>
    `;

    this.renderTable();
  },

  renderTable() {
    const tbody = document.querySelector('#roles-table tbody');
    if (!tbody || typeof EmployeesData === 'undefined') return;

    // We take top 10 employees to demo
    const demoEmployees = EmployeesData.slice(0, 10);
    
    // Default roles mapping just for UI simulation
    const simulateRoles = {
      'NV001': 'Admin',
      'NV003': 'HR_Manager',
    };

    tbody.innerHTML = demoEmployees.map(emp => {
      const currentRole = simulateRoles[emp.id] || 'Employee';
      return `
        <tr>
          <td>
            <div style="display:flex;align-items:center;gap:12px;">
              ${Helpers.avatarHTML(emp.name, 36)}
              <div>
                <div style="font-weight:500">${emp.name}</div>
                <div style="font-size:var(--font-size-xs);color:var(--text-tertiary)">${emp.id}</div>
              </div>
            </div>
          </td>
          <td>${emp.department}</td>
          <td>${emp.position}</td>
          <td>
            <select class="form-control" style="width: 200px" data-emp-id="${emp.id}">
              <option value="Admin" ${currentRole === 'Admin' ? 'selected' : ''}>Admin (Quản trị hệ thống)</option>
              <option value="HR_Manager" ${currentRole === 'HR_Manager' ? 'selected' : ''}>HR Manager (Quản lý Nhân sự)</option>
              <option value="Employee" ${currentRole === 'Employee' ? 'selected' : ''}>Employee (Nhân viên)</option>
            </select>
          </td>
        </tr>
      `;
    }).join('');
  },

  saveRoles() {
    Helpers.showToast('Đã lưu thiết lập phân quyền thành công!', 'success');
  }
};

