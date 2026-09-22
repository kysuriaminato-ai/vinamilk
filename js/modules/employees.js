// ============================================
// VINAMILK HRIS - Employees Module  
// Core HR, E-Profile, search/filter
// ============================================

const EmployeesModule = {
  currentPage: 1,
  perPage: 10,
  searchQuery: '',
  filterStatus: '',
  filterDept: '',
  sortKey: 'id',
  sortDir: 'asc',

  render(container, params = {}) {
    if (params.id) {
      this.renderProfile(container, params.id);
      return;
    }
    this.renderList(container);
  },

  renderList(container) {
    // Filters using new Helper
    let employees = EmployeesHelper.search(this.searchQuery, this.filterStatus || 'All', this.filterDept || 'All');

    // Sort
    employees = Helpers.sortBy(employees, this.sortKey, this.sortDir);

    // Paginate
    const paged = Helpers.paginate(employees, this.currentPage, this.perPage);
    const stats = EmployeesHelper.getStats();
    const depts = DepartmentsData.getDepartmentList();

    container.innerHTML = `
      <div class="page-header">
        <div class="page-header-left">
          <h1 class="page-title">Quản lý Nhân sự</h1>
          <p class="page-subtitle">Hồ sơ E-Profile — ${Helpers.formatNumber(stats.total)} nhân viên đang hoạt động</p>
        </div>
        <div class="page-header-actions">
          <button class="btn btn-secondary" onclick="EmployeesHelper.backupData()">
            <span>💾</span> Sao lưu
          </button>
          <button class="btn btn-primary" onclick="EmployeesModule.showAddModal()">
            <span>➕</span> Thêm nhân viên
          </button>
        </div>
      </div>

      <!-- Quick Stats -->
      <div class="content-grid grid-cols-4" style="margin-bottom:var(--space-5)">
        <div class="stat-card stat-blue animate-fade-in-up stagger-1" style="cursor:pointer" onclick="EmployeesModule.setFilter('status','Đang làm việc')">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.total - stats.trial}</div>
          <div class="stat-card-label">Chính thức</div>
        </div>
        <div class="stat-card stat-orange animate-fade-in-up stagger-2" style="cursor:pointer" onclick="EmployeesModule.setFilter('status','Thử việc')">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.trial}</div>
          <div class="stat-card-label">Thử việc</div>
        </div>
        <div class="stat-card stat-red animate-fade-in-up stagger-3" style="cursor:pointer" onclick="EmployeesModule.setFilter('status','Đã nghỉ việc')">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.resigned}</div>
          <div class="stat-card-label">Đã nghỉ việc</div>
        </div>
        <div class="stat-card stat-purple animate-fade-in-up stagger-4" style="cursor:pointer" onclick="EmployeesModule.setFilter('status','Nghỉ hưu')">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.retired}</div>
          <div class="stat-card-label">Nghỉ hưu</div>
        </div>
      </div>

      <!-- Data Table -->
      <div class="data-table-wrapper animate-fade-in-up">
        <div class="table-toolbar">
          <div class="table-toolbar-left">
            <div class="table-search">
              <span>🔍</span>
              <input type="text" placeholder="Tìm theo tên, mã NV, chức danh..." 
                     value="${this.searchQuery}" 
                     oninput="EmployeesModule.onSearch(this.value)" id="emp-search">
            </div>
            <select class="form-select" style="width:180px;min-height:36px" onchange="EmployeesModule.setFilter('status', this.value)">
              <option value="">Tất cả trạng thái</option>
              <option value="Đang làm việc" ${this.filterStatus === 'Đang làm việc' ? 'selected' : ''}>Đang làm việc</option>
              <option value="Thử việc" ${this.filterStatus === 'Thử việc' ? 'selected' : ''}>Thử việc</option>
              <option value="Đã nghỉ việc" ${this.filterStatus === 'Đã nghỉ việc' ? 'selected' : ''}>Đã nghỉ việc</option>
              <option value="Nghỉ hưu" ${this.filterStatus === 'Nghỉ hưu' ? 'selected' : ''}>Nghỉ hưu</option>
            </select>
            <select class="form-select" style="width:220px;min-height:36px" onchange="EmployeesModule.setFilter('dept', this.value)">
              <option value="">Tất cả đơn vị</option>
              ${depts.map(d => `<option value="${d.id}" ${this.filterDept === d.id ? 'selected' : ''}>${d.name}</option>`).join('')}
            </select>
          </div>
          <div class="table-toolbar-right">
            <span style="font-size:var(--font-size-sm);color:var(--text-secondary)">${paged.total} kết quả</span>
          </div>
        </div>

        <table class="data-table">
          <thead>
            <tr>
              <th onclick="EmployeesModule.sort('id')" class="${this.sortKey==='id'?'sorted':''}">Mã NV <span class="sort-icon">${this.sortKey==='id'?(this.sortDir==='asc'?'↑':'↓'):'↕'}</span></th>
              <th onclick="EmployeesModule.sort('name')" class="${this.sortKey==='name'?'sorted':''}">Nhân viên <span class="sort-icon">${this.sortKey==='name'?(this.sortDir==='asc'?'↑':'↓'):'↕'}</span></th>
              <th>Chức danh</th>
              <th>Đơn vị</th>
              <th onclick="EmployeesModule.sort('status')">Trạng thái <span class="sort-icon">↕</span></th>
              <th>Loại HĐ</th>
              <th onclick="EmployeesModule.sort('joinDate')">Ngày vào <span class="sort-icon">↕</span></th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            ${paged.items.map(emp => {
              const dept = DepartmentsData.getDepartment(emp.department);
              return `
              <tr onclick="window.location.hash='/employees?id=${emp.id}'" style="cursor:pointer">
                <td><code style="font-size:var(--font-size-sm);color:var(--primary)">${emp.id}</code></td>
                <td>
                  <div class="employee-cell">
                    ${Helpers.avatarHTML(emp.name)}
                    <div>
                      <div class="employee-name">${emp.name}</div>
                      <div class="employee-id">${emp.email}</div>
                    </div>
                  </div>
                </td>
                <td>${emp.position}</td>
                <td><span style="font-size:var(--font-size-sm)">${dept ? dept.name : emp.department}</span></td>
                <td>${Helpers.statusBadge(emp.status)}</td>
                <td><span style="font-size:var(--font-size-sm)">${emp.contractType || '—'}</span></td>
                <td>${Helpers.formatDate(emp.joinDate)}</td>
                <td>
                  <button class="btn btn-ghost btn-sm" onclick="event.stopPropagation(); window.location.hash='/employees?id=${emp.id}'" title="Xem chi tiết">👁️</button>
                </td>
              </tr>`;
            }).join('')}
          </tbody>
        </table>
        ${Helpers.paginationHTML(paged.total, paged.page, paged.perPage)}
      </div>
    `;

    // Bind pagination
    container.querySelectorAll('.table-pagination-btn[data-page]').forEach(btn => {
      btn.addEventListener('click', () => {
        const page = parseInt(btn.dataset.page);
        if (page >= 1 && page <= paged.totalPages) {
          this.currentPage = page;
          this.renderList(container);
        }
      });
    });
  },

  renderProfile(container, empId) {
    const emp = EmployeesHelper.getById(empId);
    if (!emp) {
      container.innerHTML = '<div class="empty-state"><div class="empty-state-icon">🔍</div><div class="empty-state-title">Không tìm thấy nhân viên</div></div>';
      return;
    }
    const dept = DepartmentsData.getDepartment(emp.department);
    const age = Helpers.calcAge(emp.birthDate);
    const seniority = Helpers.calcSeniority(emp.joinDate);

    container.innerHTML = `
      <div class="page-header">
        <div class="page-header-left">
          <button class="btn btn-ghost" onclick="window.location.hash='/employees'" style="margin-bottom:var(--space-2)">← Quay lại danh sách</button>
          <h1 class="page-title">Hồ sơ Nhân viên</h1>
        </div>
        <div class="page-header-actions">
          <button class="btn btn-secondary" onclick="EmployeesModule.showPrintModal('${emp.id}')">🖨️ In hồ sơ</button>
          <button class="btn btn-secondary" onclick="EmployeesModule.showTransferModal('${emp.id}')">🔄 Thuyên chuyển</button>
          <button class="btn btn-primary" onclick="window.location.hash='/payroll?id=${emp.id}'">💰 Xem phiếu lương</button>
        </div>
      </div>

      <!-- Profile Header -->
      <div class="profile-header animate-fade-in-up">
        <div class="profile-avatar">${Helpers.getInitials(emp.name)}</div>
        <div class="profile-info">
          <div class="profile-name">${emp.name}</div>
          <div class="profile-position">${emp.position} — ${dept ? dept.name : emp.department}</div>
          <div class="profile-meta">
            <span class="profile-meta-item">🆔 ${emp.id}</span>
            <span class="profile-meta-item">📧 ${emp.email}</span>
            <span class="profile-meta-item">📞 ${emp.phone}</span>
          </div>
        </div>
        <div class="profile-actions">
          ${Helpers.statusBadge(emp.status)}
          <span class="badge badge-info" style="margin-top:4px">${emp.contractType || 'N/A'}</span>
        </div>
      </div>

      <!-- Tabs -->
      <div class="tabs" id="profile-tabs">
        <div class="tab active" data-tab="personal" onclick="EmployeesModule.switchTab('personal')">👤 Cá nhân & Nhân thân</div>
        <div class="tab" data-tab="legal" onclick="EmployeesModule.switchTab('legal')">📋 Pháp lý & Chế độ</div>
        <div class="tab" data-tab="education" onclick="EmployeesModule.switchTab('education')">🎓 Trình độ & Chứng chỉ</div>
        <div class="tab" data-tab="work" onclick="EmployeesModule.switchTab('work')">💼 Công tác & Xếp lương</div>
      </div>

      <!-- Tab: Personal -->
      <div class="tab-content active" id="tab-personal">
        <div class="content-grid grid-cols-2">
          <div class="card">
            <div class="card-header"><div class="card-header-title">Thông tin cá nhân</div></div>
            <div class="card-body">
              <div class="form-row" style="margin-bottom:var(--space-4)">
                <div class="form-group" style="margin:0"><div class="form-label">Họ tên</div><div style="font-weight:600">${emp.name}</div></div>
                <div class="form-group" style="margin:0"><div class="form-label">Giới tính</div><div>${emp.gender}</div></div>
              </div>
              <div class="form-row" style="margin-bottom:var(--space-4)">
                <div class="form-group" style="margin:0"><div class="form-label">Ngày sinh</div><div>${Helpers.formatDate(emp.birthDate)} (${age} tuổi)</div></div>
                <div class="form-group" style="margin:0"><div class="form-label">CCCD</div><div><code>${emp.cccd}</code></div></div>
              </div>
              <div class="form-row" style="margin-bottom:var(--space-4)">
                <div class="form-group" style="margin:0"><div class="form-label">Số điện thoại</div><div>${emp.phone}</div></div>
                <div class="form-group" style="margin:0"><div class="form-label">Email</div><div>${emp.email}</div></div>
              </div>
              <div class="form-group" style="margin:0"><div class="form-label">Địa chỉ</div><div>${emp.address}</div></div>
            </div>
          </div>
          <div class="card">
            <div class="card-header"><div class="card-header-title">Nhân thân & Liên hệ khẩn cấp</div></div>
            <div class="card-body">
              <div class="form-group"><div class="form-label">Liên hệ khẩn cấp</div><div style="font-weight:500">${emp.emergencyContact}</div></div>
              <div class="form-group"><div class="form-label">Số người phụ thuộc</div><div>${emp.dependents} người</div></div>
              <div class="form-group" style="margin:0"><div class="form-label">Vị trí hồ sơ giấy</div><div><code style="background:var(--bg-hover);padding:4px 8px;border-radius:4px">${emp.fileLocation}</code></div></div>
            </div>
          </div>
        </div>
      </div>

      <!-- Tab: Legal -->
      <div class="tab-content" id="tab-legal">
        <div class="content-grid grid-cols-2">
          <div class="card">
            <div class="card-header"><div class="card-header-title">Bảo hiểm & Thuế</div></div>
            <div class="card-body">
              <div class="form-row" style="margin-bottom:var(--space-4)">
                <div class="form-group" style="margin:0"><div class="form-label">Mức đóng BHXH</div><div style="font-weight:600;color:var(--primary)">${Helpers.formatCurrency(emp.insuranceSalary)}</div></div>
                <div class="form-group" style="margin:0"><div class="form-label">Mã số thuế</div><div><code>${emp.taxId}</code></div></div>
              </div>
              <div class="form-row">
                <div class="form-group" style="margin:0"><div class="form-label">Tài khoản NH</div><div><code>${emp.bankAccount}</code></div></div>
                <div class="form-group" style="margin:0"><div class="form-label">Ngân hàng</div><div>${emp.bank}</div></div>
              </div>
            </div>
          </div>
          <div class="card">
            <div class="card-header"><div class="card-header-title">Đoàn thể</div></div>
            <div class="card-body">
              <div class="form-row">
                <div class="form-group" style="margin:0">
                  <div class="form-label">Đảng viên</div>
                  <div>${emp.partyMember ? '<span class="badge badge-active"><span class="badge-dot"></span>Có</span>' : '<span class="badge badge-retired"><span class="badge-dot"></span>Không</span>'}</div>
                </div>
                <div class="form-group" style="margin:0">
                  <div class="form-label">Công đoàn viên</div>
                  <div>${emp.unionMember ? '<span class="badge badge-active"><span class="badge-dot"></span>Có</span>' : '<span class="badge badge-retired"><span class="badge-dot"></span>Không</span>'}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Tab: Education -->
      <div class="tab-content" id="tab-education">
        <div class="content-grid grid-cols-2">
          <div class="card">
            <div class="card-header"><div class="card-header-title">Học vấn</div></div>
            <div class="card-body">
              <div class="form-group"><div class="form-label">Trình độ</div><div style="font-weight:600">${emp.education}</div></div>
              ${emp.major ? `<div class="form-group"><div class="form-label">Chuyên ngành</div><div>${emp.major}</div></div>` : ''}
              ${emp.university ? `<div class="form-group" style="margin:0"><div class="form-label">Trường</div><div>${emp.university}</div></div>` : ''}
            </div>
          </div>
          <div class="card">
            <div class="card-header"><div class="card-header-title">Kho chứng chỉ</div></div>
            <div class="card-body">
              ${emp.certifications.length > 0 ? 
                emp.certifications.map(cert => `
                  <div style="display:flex;align-items:center;gap:var(--space-2);padding:var(--space-2) 0;border-bottom:1px solid var(--border-light)">
                    <span>📜</span>
                    <span style="flex:1;font-weight:500">${cert}</span>
                    <span class="badge badge-active"><span class="badge-dot"></span>Còn hạn</span>
                  </div>
                `).join('') :
                '<div style="color:var(--text-tertiary);text-align:center;padding:var(--space-4)">Chưa có chứng chỉ</div>'
              }
            </div>
          </div>
        </div>
      </div>

      <!-- Tab: Work -->
      <div class="tab-content" id="tab-work">
        <div class="content-grid grid-cols-2">
          <div class="card">
            <div class="card-header"><div class="card-header-title">Thông tin công tác</div></div>
            <div class="card-body">
              <div class="form-row" style="margin-bottom:var(--space-4)">
                <div class="form-group" style="margin:0"><div class="form-label">Đơn vị</div><div style="font-weight:600">${dept ? dept.name : emp.department}</div></div>
                <div class="form-group" style="margin:0"><div class="form-label">Chức danh</div><div style="font-weight:600">${emp.position}</div></div>
              </div>
              <div class="form-row" style="margin-bottom:var(--space-4)">
                <div class="form-group" style="margin:0"><div class="form-label">Cấp bậc</div><div>${emp.level}</div></div>
                <div class="form-group" style="margin:0"><div class="form-label">Ngày vào công ty</div><div>${Helpers.formatDate(emp.joinDate)} (${seniority} năm)</div></div>
              </div>
              <div class="form-row">
                <div class="form-group" style="margin:0"><div class="form-label">Loại hợp đồng</div><div>${emp.contractType || '—'}</div></div>
                <div class="form-group" style="margin:0"><div class="form-label">Hết hạn HĐ</div><div>${emp.contractEnd ? Helpers.formatDate(emp.contractEnd) : 'Không thời hạn'}</div></div>
              </div>
            </div>
          </div>
          <div class="card">
            <div class="card-header"><div class="card-header-title">Cơ cấu lương 3P</div></div>
            <div class="card-body">
              <div style="margin-bottom:var(--space-4)">
                <div class="progress-label"><span class="progress-label-text">P1 — Lương vị trí</span><span class="progress-label-value payroll-p1">${Helpers.formatCurrency(emp.p1)}</span></div>
                <div class="progress-bar"><div class="progress-bar-fill" style="width:${emp.p1 / (emp.p1 + emp.p2 + emp.p3) * 100}%;background:linear-gradient(90deg,#0052CC,#4C9AFF)"></div></div>
              </div>
              <div style="margin-bottom:var(--space-4)">
                <div class="progress-label"><span class="progress-label-text">P2 — Lương năng lực</span><span class="progress-label-value payroll-p2">${Helpers.formatCurrency(emp.p2)}</span></div>
                <div class="progress-bar"><div class="progress-bar-fill" style="width:${emp.p2 / (emp.p1 + emp.p2 + emp.p3) * 100}%;background:linear-gradient(90deg,#006644,#36B37E)"></div></div>
              </div>
              <div style="margin-bottom:var(--space-4)">
                <div class="progress-label"><span class="progress-label-text">P3 — Lương hiệu suất</span><span class="progress-label-value payroll-p3">${Helpers.formatCurrency(emp.p3)}</span></div>
                <div class="progress-bar"><div class="progress-bar-fill" style="width:${emp.p3 / (emp.p1 + emp.p2 + emp.p3) * 100}%;background:linear-gradient(90deg,#FF8B00,#FFAB00)"></div></div>
              </div>
              <div style="display:flex;justify-content:space-between;padding-top:var(--space-3);border-top:2px solid var(--primary)">
                <span style="font-weight:700">Tổng lương cơ bản</span>
                <span style="font-weight:800;color:var(--primary);font-size:var(--font-size-lg)">${Helpers.formatCurrency(emp.p1 + emp.p2 + emp.p3)}</span>
              </div>
              <div style="display:flex;justify-content:space-between;padding-top:var(--space-2)">
                <span style="color:var(--text-secondary)">+ Phụ cấp</span>
                <span style="font-weight:600">${Helpers.formatCurrency(emp.allowances)}</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Timeline -->
        <div class="card" style="margin-top:var(--space-5)">
          <div class="card-header"><div class="card-header-title">📜 Quá trình Công tác (History)</div></div>
          <div class="card-body">
            <div class="timeline">
              ${(emp.history && emp.history.length > 0) ? emp.history.slice().reverse().map(h => {
                const toDeptName = DepartmentsData.getDepartment(h.toDept)?.name || h.toDept;
                const fromDeptName = DepartmentsData.getDepartment(h.fromDept)?.name || h.fromDept;
                return `
                <div class="timeline-item">
                  <div class="timeline-dot ${h.type.includes('Tuyển dụng') ? 'green' : 'blue'}"></div>
                  <div class="timeline-date">${Helpers.formatDate(h.date)}</div>
                  <div class="timeline-title">${h.type}</div>
                  <div class="timeline-desc">
                    ${(h.type === 'Tuyển dụng' || h.type === 'Tuyển dụng mới') ? `Gia nhập: ${toDeptName} — ${h.toPos}` : `Từ: ${fromDeptName} (${h.fromPos}) ➡️ Đến: ${toDeptName} (${h.toPos})`}
                    <br><i style="color:var(--text-tertiary)">Ghi chú: ${h.note || ''}</i>
                  </div>
                </div>
              `}).join('') : `
                <div class="timeline-item">
                  <div class="timeline-dot green"></div>
                  <div class="timeline-date">${Helpers.formatDate(emp.joinDate)}</div>
                  <div class="timeline-title">Bắt đầu làm việc</div>
                  <div class="timeline-desc">Gia nhập ${dept ? dept.name : 'Vinamilk'} — Chức danh: ${emp.position}</div>
                </div>
              `}
            </div>
          </div>
        </div>
      </div>
    `;
  },

  switchTab(tabId) {
    document.querySelectorAll('#profile-tabs .tab').forEach(t => t.classList.remove('active'));
    document.querySelector(`#profile-tabs .tab[data-tab="${tabId}"]`).classList.add('active');
    document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
    document.getElementById(`tab-${tabId}`).classList.add('active');
  },

  onSearch: Helpers.debounce(function(query) {
    EmployeesModule.searchQuery = query;
    EmployeesModule.currentPage = 1;
    EmployeesModule.renderList(document.getElementById('main-content'));
  }, 300),

  setFilter(type, value) {
    if (type === 'status') this.filterStatus = value;
    if (type === 'dept') this.filterDept = value;
    this.currentPage = 1;
    this.renderList(document.getElementById('main-content'));
  },

  sort(key) {
    if (this.sortKey === key) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortKey = key;
      this.sortDir = 'asc';
    }
    this.renderList(document.getElementById('main-content'));
  },

  showAddModal() {
    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
    overlay.innerHTML = `
      <div class="modal modal-lg">
        <div class="modal-header">
          <div class="modal-title">➕ Nhập hồ sơ nhân viên mới</div>
          <button class="modal-close" onclick="this.closest('.modal-overlay').remove()">✕</button>
        </div>
        <div class="modal-body" id="add-emp-form">
          <div class="form-row" style="margin-bottom:var(--space-4)">
            <div class="form-group" style="margin:0"><label class="form-label">Họ tên <span class="required">*</span></label><input class="form-input" id="add-name" required placeholder="Ví dụ: Nguyễn Văn A"></div>
            <div class="form-group" style="margin:0"><label class="form-label">Giới tính</label><select class="form-select" id="add-gender"><option>Nam</option><option>Nữ</option></select></div>
          </div>
          <div class="form-row" style="margin-bottom:var(--space-4)">
            <div class="form-group" style="margin:0"><label class="form-label">CCCD <span class="required">*</span></label><input class="form-input" id="add-cccd" required placeholder="079xxx"></div>
            <div class="form-group" style="margin:0"><label class="form-label">Số ĐT <span class="required">*</span></label><input class="form-input" id="add-phone" required placeholder="09xxxx"></div>
          </div>
          <div class="form-row" style="margin-bottom:var(--space-4)">
            <div class="form-group" style="margin:0"><label class="form-label">Đơn vị tiếp nhận <span class="required">*</span></label><select class="form-select" id="add-dept">${DepartmentsData.getDepartmentList().map(d => `<option value="${d.id}">${d.name}</option>`).join('')}</select></div>
            <div class="form-group" style="margin:0"><label class="form-label">Chức danh <span class="required">*</span></label><input class="form-input" id="add-pos" required placeholder="Nhân viên"></div>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" onclick="this.closest('.modal-overlay').remove()">Hủy</button>
          <button class="btn btn-primary" onclick="EmployeesModule.submitAddEmployee(this)">Lưu hồ sơ</button>
        </div>
      </div>
    `;
    document.body.appendChild(overlay);
  },

  submitAddEmployee(btn) {
    const name = document.getElementById('add-name').value.trim();
    const cccd = document.getElementById('add-cccd').value.trim();
    const phone = document.getElementById('add-phone').value.trim();
    const pos = document.getElementById('add-pos').value.trim();
    if (!name || !cccd || !phone || !pos) {
      alert("Vui lòng nhập đầy đủ thông tin bắt buộc!");
      return;
    }
    
    EmployeesHelper.addEmployee({
      name, cccd, phone, position: pos,
      gender: document.getElementById('add-gender').value,
      department: document.getElementById('add-dept').value,
      status: 'Thử việc',
      joinDate: new Date().toISOString().split('T')[0],
      p1: 10000000, p2: 0, p3: 0, allowances: 0, insuranceSalary: 10000000,
      certifications: [], email: 'new.emp@vinamilk.com.vn',
      level: 'Nhân viên', address: '', emergencyContact: '', dependents: 0,
      education: '', fileLocation: 'Chưa lưu'
    });
    
    btn.closest('.modal-overlay').remove();
    Helpers.showToast('Đã thêm hồ sơ nhân viên mới thành công!', 'success');
    this.renderList(document.getElementById('main-content'));
  },

  showTransferModal(empId) {
    const emp = EmployeesHelper.getById(empId);
    if (!emp) return;

    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
    overlay.innerHTML = `
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title">🔄 Thuyên chuyển công tác</div>
          <button class="modal-close" onclick="this.closest('.modal-overlay').remove()">✕</button>
        </div>
        <div class="modal-body">
          <div style="margin-bottom:var(--space-4);padding:var(--space-3);background:var(--bg-hover);border-radius:var(--radius-md)">
            <strong>Nhân viên:</strong> ${emp.name} (${emp.id})<br>
            <strong>Đơn vị cũ:</strong> ${DepartmentsData.getDepartment(emp.department)?.name || emp.department}<br>
            <strong>Chức danh cũ:</strong> ${emp.position}
          </div>
          <div class="form-group">
            <label class="form-label">Đơn vị mới</label>
            <select class="form-select" id="trans-dept">
              ${DepartmentsData.getDepartmentList().map(d => `<option value="${d.id}" ${d.id === emp.department ? 'selected' : ''}>${d.name}</option>`).join('')}
            </select>
          </div>
          <div class="form-group">
            <label class="form-label">Chức danh mới</label>
            <input class="form-input" id="trans-pos" value="${emp.position}">
          </div>
          <div class="form-group">
            <label class="form-label">Lý do / Quyết định số</label>
            <textarea class="form-input" id="trans-note" rows="2" placeholder="Điều động theo quyết định số..."></textarea>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" onclick="this.closest('.modal-overlay').remove()">Hủy</button>
          <button class="btn btn-primary" onclick="EmployeesModule.submitTransfer('${empId}', this)">Thực hiện</button>
        </div>
      </div>
    `;
    document.body.appendChild(overlay);
  },

  submitTransfer(empId, btn) {
    const newDept = document.getElementById('trans-dept').value;
    const newPos = document.getElementById('trans-pos').value.trim();
    const note = document.getElementById('trans-note').value.trim();

    if (EmployeesHelper.transferEmployee(empId, newDept, newPos, note)) {
      btn.closest('.modal-overlay').remove();
      Helpers.showToast('Thuyên chuyển thành công!', 'success');
      this.renderProfile(document.getElementById('main-content'), empId);
    }
  },

  showPrintModal(empId) {
    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
    overlay.innerHTML = `
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title">🖨️ Chọn trang hồ sơ cần in</div>
          <button class="modal-close" onclick="this.closest('.modal-overlay').remove()">✕</button>
        </div>
        <div class="modal-body">
          <div class="content-grid grid-cols-2" style="gap:var(--space-2);background:var(--bg-hover);padding:var(--space-4);border-radius:var(--radius-md);">
            <label style="display:flex;align-items:center;gap:8px;cursor:pointer">
              <input type="checkbox" id="print-cover" checked> Trang bìa
            </label>
            <label style="display:flex;align-items:center;gap:8px;cursor:pointer">
              <input type="checkbox" id="print-p1" checked> Trang 1 (Thông tin chung)
            </label>
            <label style="display:flex;align-items:center;gap:8px;cursor:pointer">
              <input type="checkbox" id="print-p2" checked> Trang 2 (Công tác & Lương)
            </label>
            <label style="display:flex;align-items:center;gap:8px;cursor:pointer">
              <input type="checkbox" id="print-p3" checked> Trang 3 (Quá trình công tác)
            </label>
          </div>
        </div>
        <div class="modal-footer" style="justify-content:center;gap:var(--space-3)">
          <button class="btn btn-primary" onclick="EmployeesModule.executePrint('${empId}', this)">Xem trước & In</button>
          <button class="btn btn-secondary" onclick="this.closest('.modal-overlay').remove()">Hủy</button>
        </div>
      </div>
    `;
    document.body.appendChild(overlay);
  },

  executePrint(empId, btn) {
    const emp = EmployeesHelper.getById(empId);
    if (!emp) return;

    const printCover = document.getElementById('print-cover').checked;
    const printP1 = document.getElementById('print-p1').checked;
    const printP2 = document.getElementById('print-p2').checked;
    const printP3 = document.getElementById('print-p3').checked;

    const dept = DepartmentsData.getDepartment(emp.department);
    const deptName = dept ? dept.name : emp.department;

    let html = '';

    // Cover Page
    if (printCover) {
      html += \`
        <div class="print-page" style="display:flex;flex-direction:column;justify-content:center;align-items:center;height:100vh;">
          <div style="text-align:center;margin-bottom:100px;">
            <div style="font-size:14pt;font-weight:bold;">TẬP ĐOÀN VINAMILK</div>
            <div style="font-size:13pt;text-decoration:underline;">\${deptName.toUpperCase()}</div>
          </div>
          <div style="text-align:center;flex:1;display:flex;flex-direction:column;justify-content:center;">
            <h1 style="font-size:36pt;margin:0;">HỒ SƠ CÁN BỘ</h1>
            <h2 style="font-size:24pt;font-weight:normal;margin-top:20px;">Họ và tên: <strong style="font-size:28pt">\${emp.name.toUpperCase()}</strong></h2>
            <div style="font-size:16pt;margin-top:10px;">Số hiệu: \${emp.id}</div>
          </div>
        </div>
      \`;
    }

    // Page 1: General Info
    if (printP1) {
      html += \`
        <div class="print-page">
          <div class="print-header">
            <div class="print-header-left">
              Cơ quan quản lý: <strong>VINAMILK</strong><br>
              Đơn vị: <strong>\${deptName}</strong>
            </div>
            <div class="print-header-right">
              CỘNG HOÀ XÃ HỘI CHỦ NGHĨA VIỆT NAM<br>
              Độc lập - Tự do - Hạnh phúc<br>
              --------------------
            </div>
          </div>
          
          <div class="print-title">PHIẾU CÁN BỘ, CÔNG CHỨC, VIÊN CHỨC</div>
          
          <div class="print-section-title">I. THÔNG TIN CHUNG:</div>
          <div style="display:flex;">
            <div class="print-col-avatar">Ảnh<br>4x6</div>
            <div class="print-col-info">
              <div class="print-row">
                <span class="print-label">1. Họ và tên khai sinh:</span>
                <span class="print-value" style="font-weight:bold;">\${emp.name}</span>
                <div class="dotted-line"></div>
              </div>
              <div class="print-row">
                <span class="print-label">Sinh ngày:</span>
                <span class="print-value">\${Helpers.formatDate(emp.birthDate)}</span>
                <span class="print-label" style="margin-left:20px">Giới tính:</span>
                <span class="print-value">\${emp.gender}</span>
              </div>
              <div class="print-row">
                <span class="print-label">2. Số hiệu cán bộ:</span>
                <span class="print-value">\${emp.id}</span>
              </div>
              <div class="print-row">
                <span class="print-label">3. Số CCCD:</span>
                <span class="print-value">\${emp.cccd}</span>
              </div>
              <div class="print-row">
                <span class="print-label">4. Điện thoại:</span>
                <span class="print-value">\${emp.phone}</span>
                <span class="print-label" style="margin-left:20px">Email:</span>
                <span class="print-value" style="text-transform:none">\${emp.email}</span>
              </div>
            </div>
          </div>
          
          <div class="print-row" style="margin-top:10px">
            <span class="print-label">5. Nơi đăng ký HKTT:</span>
            <span class="print-value">\${emp.address}</span>
            <div class="dotted-line"></div>
          </div>
          <div class="print-row">
            <span class="print-label">6. Chỗ ở hiện nay:</span>
            <span class="print-value">\${emp.address}</span>
            <div class="dotted-line"></div>
          </div>
          <div class="print-row">
            <span class="print-label">7. Trình độ giáo dục phổ thông:</span>
            <span class="print-value">12/12</span>
          </div>
          <div class="print-row">
            <span class="print-label">8. Trình độ chuyên môn cao nhất:</span>
            <span class="print-value">\${emp.education} - \${emp.major || 'N/A'}</span>
            <div class="dotted-line"></div>
          </div>
        </div>
      \`;
    }

    // Page 2: Work & Salary
    if (printP2) {
      html += \`
        <div class="print-page">
          <div class="print-section-title">II. CÔNG TÁC:</div>
          <div class="print-row">
            <span class="print-label">9. Ngày tuyển dụng:</span>
            <span class="print-value">\${Helpers.formatDate(emp.joinDate)}</span>
          </div>
          <div class="print-row">
            <span class="print-label">10. Cơ quan/đơn vị tuyển dụng:</span>
            <span class="print-value">Công ty CP Sữa Việt Nam (Vinamilk)</span>
          </div>
          <div class="print-row">
            <span class="print-label">11. Chức danh công tác:</span>
            <span class="print-value">\${emp.position}</span>
            <div class="dotted-line"></div>
          </div>
          <div class="print-row">
            <span class="print-label">12. Đơn vị đang công tác:</span>
            <span class="print-value">\${deptName}</span>
            <div class="dotted-line"></div>
          </div>
          
          <div class="print-section-title">III. LƯƠNG, PHỤ CẤP:</div>
          <div class="print-row">
            <span class="print-label">13. Tổng mức lương cơ bản:</span>
            <span class="print-value">\${Helpers.formatCurrency(emp.p1 + emp.p2 + emp.p3)}</span>
            <div class="dotted-line"></div>
          </div>
          <div class="print-row">
            <span class="print-label">14. Phụ cấp hiện hưởng:</span>
            <span class="print-value">\${Helpers.formatCurrency(emp.allowances)}</span>
            <div class="dotted-line"></div>
          </div>
          <div class="print-row">
            <span class="print-label">15. Mức đóng BHXH:</span>
            <span class="print-value">\${Helpers.formatCurrency(emp.insuranceSalary)}</span>
            <div class="dotted-line"></div>
          </div>
          <div class="print-row">
            <span class="print-label">16. Tài khoản ngân hàng:</span>
            <span class="print-value">\${emp.bankAccount} (\${emp.bank})</span>
            <div class="dotted-line"></div>
          </div>
          <div class="print-row">
            <span class="print-label">17. Mã số thuế TNCN:</span>
            <span class="print-value">\${emp.taxId}</span>
            <div class="dotted-line"></div>
          </div>
        </div>
      \`;
    }

    // Page 3: History
    if (printP3) {
      let historyRows = '';
      if (emp.history && emp.history.length > 0) {
        historyRows = emp.history.map(h => {
          const toDeptStr = DepartmentsData.getDepartment(h.toDept)?.name || h.toDept;
          return \`
            <tr>
              <td>\${Helpers.formatDate(h.date)}</td>
              <td>\${h.type}</td>
              <td>\${toDeptStr} - \${h.toPos}</td>
              <td>\${h.note || ''}</td>
            </tr>
          \`;
        }).join('');
      } else {
        historyRows = \`<tr><td colspan="4" style="text-align:center">Chưa có thay đổi</td></tr>\`;
      }

      html += \`
        <div class="print-page">
          <div class="print-section-title">IV. LỊCH SỬ CÔNG TÁC & THUYÊN CHUYỂN:</div>
          <table class="print-table">
            <thead>
              <tr>
                <th width="15%">Ngày/Tháng</th>
                <th width="20%">Loại QĐ</th>
                <th width="40%">Nội dung (Đơn vị - Chức vụ)</th>
                <th width="25%">Ghi chú</th>
              </tr>
            </thead>
            <tbody>
              \${historyRows}
            </tbody>
          </table>
          
          <div style="display:flex;justify-content:space-between;margin-top:50px;">
            <div style="text-align:center;width:40%">
              <br><strong>Người khai</strong><br>(Ký, ghi rõ họ tên)
            </div>
            <div style="text-align:center;width:50%">
              <em>Ngày \${new Date().getDate()} tháng \${new Date().getMonth() + 1} năm \${new Date().getFullYear()}</em><br>
              <strong>Thủ trưởng cơ quan, đơn vị</strong><br>(Ký tên, đóng dấu)
            </div>
          </div>
        </div>
      \`;
    }

    // Inject to print container
    const printContainer = document.getElementById('print-container');
    printContainer.innerHTML = html;

    // Remove modal
    btn.closest('.modal-overlay').remove();

    // Trigger print
    window.print();

    // Cleanup after print dialog is closed (setTimeout as a basic fallback)
    setTimeout(() => {
      printContainer.innerHTML = '';
    }, 1000);
  }
};
