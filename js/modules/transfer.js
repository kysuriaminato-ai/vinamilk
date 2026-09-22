// ============================================
// VINAMILK HRIS - Transfer Module
// Department transfer management
// ============================================

const TransferModule = {
  currentPage: 1,
  perPage: 10,
  filterStatus: '',

  render(container) {
    const stats = TransfersHelper.getStats();
    let transfers = TransfersData;

    if (this.filterStatus) {
      transfers = TransfersHelper.getByStatus(this.filterStatus);
    }

    const paged = Helpers.paginate(transfers, this.currentPage, this.perPage);

    container.innerHTML = `
      <div class="page-header">
        <div class="page-header-left">
          <h1 class="page-title">Thuyên chuyển Công tác</h1>
          <p class="page-subtitle">Quản lý điều chuyển nhân sự giữa các đơn vị</p>
        </div>
        <div class="page-header-actions">
          <button class="btn btn-primary" onclick="TransferModule.showCreateModal()">
            <span>🔄</span> Tạo quyết định
          </button>
        </div>
      </div>

      <!-- Stats -->
      <div class="content-grid grid-cols-4" style="margin-bottom:var(--space-5)">
        <div class="stat-card stat-blue animate-fade-in-up stagger-1" style="cursor:pointer" onclick="TransferModule.setFilter('')">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.total}</div>
          <div class="stat-card-label">Tổng thuyên chuyển</div>
        </div>
        <div class="stat-card stat-orange animate-fade-in-up stagger-2" style="cursor:pointer" onclick="TransferModule.setFilter('Chờ duyệt')">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.pending}</div>
          <div class="stat-card-label">Chờ duyệt</div>
        </div>
        <div class="stat-card stat-green animate-fade-in-up stagger-3" style="cursor:pointer" onclick="TransferModule.setFilter('Đã duyệt')">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.approved}</div>
          <div class="stat-card-label">Đã duyệt</div>
        </div>
        <div class="stat-card stat-purple animate-fade-in-up stagger-4" style="cursor:pointer" onclick="TransferModule.setFilter('Hoàn thành')">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.completed}</div>
          <div class="stat-card-label">Hoàn thành</div>
        </div>
      </div>

      <!-- Transfer Table -->
      <div class="data-table-wrapper animate-fade-in-up">
        <div class="table-toolbar">
          <div class="table-toolbar-left">
            <select class="form-select" style="width:180px;min-height:36px" onchange="TransferModule.setFilter(this.value)">
              <option value="" ${this.filterStatus === '' ? 'selected' : ''}>Tất cả trạng thái</option>
              <option value="Chờ duyệt" ${this.filterStatus === 'Chờ duyệt' ? 'selected' : ''}>Chờ duyệt</option>
              <option value="Đã duyệt" ${this.filterStatus === 'Đã duyệt' ? 'selected' : ''}>Đã duyệt</option>
              <option value="Hoàn thành" ${this.filterStatus === 'Hoàn thành' ? 'selected' : ''}>Hoàn thành</option>
            </select>
          </div>
          <div class="table-toolbar-right">
            <span style="font-size:var(--font-size-sm);color:var(--text-secondary)">${paged.total} kết quả</span>
          </div>
        </div>

        <table class="data-table">
          <thead>
            <tr>
              <th>Mã QĐ</th>
              <th>Nhân viên</th>
              <th>Từ đơn vị</th>
              <th>Đến đơn vị</th>
              <th>Chức danh mới</th>
              <th>Ngày hiệu lực</th>
              <th>Trạng thái</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            ${paged.items.map(t => {
              const fromDept = DepartmentsData.getDepartment(t.fromDept);
              const toDept = DepartmentsData.getDepartment(t.toDept);
              const statusMap = { 'Chờ duyệt': 'badge-pending', 'Đã duyệt': 'badge-info', 'Hoàn thành': 'badge-active' };
              return `
              <tr>
                <td><code style="font-size:var(--font-size-sm);color:var(--primary)">${t.id}</code></td>
                <td>
                  <div class="employee-cell">
                    ${Helpers.avatarHTML(t.empName)}
                    <div>
                      <div class="employee-name">${t.empName}</div>
                      <div class="employee-id">${t.empId}</div>
                    </div>
                  </div>
                </td>
                <td><span style="font-size:var(--font-size-sm)">${fromDept ? fromDept.name : t.fromDept}</span></td>
                <td>
                  <span style="font-size:var(--font-size-sm);font-weight:600;color:var(--primary)">→ ${toDept ? toDept.name : t.toDept}</span>
                </td>
                <td><span style="font-size:var(--font-size-sm)">${t.toPosition}</span></td>
                <td>${Helpers.formatDate(t.effectiveDate)}</td>
                <td><span class="badge ${statusMap[t.status] || 'badge-info'}"><span class="badge-dot"></span>${t.status}</span></td>
                <td>
                  <button class="btn btn-ghost btn-sm" onclick="TransferModule.showDetail('${t.id}')" title="Chi tiết">👁️</button>
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
          this.render(container);
        }
      });
    });
  },

  setFilter(status) {
    this.filterStatus = status;
    this.currentPage = 1;
    this.render(document.getElementById('main-content'));
  },

  showDetail(transferId) {
    const t = TransfersData.find(tr => tr.id === transferId);
    if (!t) return;
    const fromDept = DepartmentsData.getDepartment(t.fromDept);
    const toDept = DepartmentsData.getDepartment(t.toDept);

    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
    overlay.innerHTML = `
      <div class="modal modal-lg">
        <div class="modal-header">
          <div>
            <div class="modal-title">🔄 Quyết định Thuyên chuyển ${t.id}</div>
            <div style="font-size:var(--font-size-sm);color:var(--text-secondary);margin-top:4px">Ngày tạo: ${Helpers.formatDate(t.createdDate)}</div>
          </div>
          <button class="modal-close" onclick="this.closest('.modal-overlay').remove()">✕</button>
        </div>
        <div class="modal-body">
          <div class="profile-header" style="margin-bottom:var(--space-5)">
            <div class="profile-avatar" style="width:60px;height:60px;font-size:var(--font-size-xl)">${Helpers.getInitials(t.empName)}</div>
            <div class="profile-info">
              <div class="profile-name" style="font-size:var(--font-size-xl)">${t.empName}</div>
              <div class="profile-position">${t.empId}</div>
            </div>
          </div>

          <div class="content-grid grid-cols-2" style="margin-bottom:var(--space-4)">
            <div class="card" style="border-left:4px solid var(--accent-red)">
              <div class="card-body">
                <div style="font-size:var(--font-size-sm);color:var(--text-secondary);margin-bottom:var(--space-2)">📍 Đơn vị cũ</div>
                <div style="font-weight:700;margin-bottom:var(--space-1)">${fromDept ? fromDept.name : t.fromDept}</div>
                <div style="font-size:var(--font-size-sm);color:var(--text-secondary)">Chức danh: ${t.fromPosition}</div>
              </div>
            </div>
            <div class="card" style="border-left:4px solid var(--accent-green)">
              <div class="card-body">
                <div style="font-size:var(--font-size-sm);color:var(--text-secondary);margin-bottom:var(--space-2)">📍 Đơn vị mới</div>
                <div style="font-weight:700;color:var(--primary);margin-bottom:var(--space-1)">${toDept ? toDept.name : t.toDept}</div>
                <div style="font-size:var(--font-size-sm);color:var(--text-secondary)">Chức danh: ${t.toPosition}</div>
              </div>
            </div>
          </div>

          <div class="form-group"><div class="form-label">Ngày hiệu lực</div><div style="font-weight:600">${Helpers.formatDate(t.effectiveDate)}</div></div>
          <div class="form-group"><div class="form-label">Lý do thuyên chuyển</div><div>${t.reason}</div></div>
          ${t.notes ? `<div class="form-group"><div class="form-label">Ghi chú</div><div style="font-size:var(--font-size-sm);color:var(--text-secondary)">${t.notes}</div></div>` : ''}
          ${t.approvedBy ? `<div class="form-group" style="margin:0"><div class="form-label">Người phê duyệt</div><div style="font-weight:500">${t.approvedBy}</div></div>` : ''}
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" onclick="this.closest('.modal-overlay').remove()">Đóng</button>
          ${t.status === 'Chờ duyệt' ? '<button class="btn btn-primary" onclick="alert(\'Demo: Đã phê duyệt!\'); this.closest(\'.modal-overlay\').remove()">✅ Phê duyệt</button>' : ''}
        </div>
      </div>
    `;
    document.body.appendChild(overlay);
  },

  showCreateModal() {
    const depts = DepartmentsData.getDepartmentList();
    const activeEmps = EmployeesHelper.getActive();
    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
    overlay.innerHTML = `
      <div class="modal modal-lg">
        <div class="modal-header">
          <div class="modal-title">🔄 Tạo Quyết định Thuyên chuyển</div>
          <button class="modal-close" onclick="this.closest('.modal-overlay').remove()">✕</button>
        </div>
        <div class="modal-body">
          <div class="alert-banner alert-banner-info" style="margin-bottom:var(--space-5)">
            <span class="alert-banner-icon">ℹ️</span>
            <div class="alert-banner-content">
              <div class="alert-banner-title">Chế độ Demo</div>
              <div class="alert-banner-text">Quyết định sẽ không được lưu vĩnh viễn trong bản demo.</div>
            </div>
          </div>
          <div class="form-group">
            <label class="form-label">Nhân viên <span class="required">*</span></label>
            <select class="form-select">
              ${activeEmps.map(e => `<option value="${e.id}">${e.id} - ${e.name} (${e.position})</option>`).join('')}
            </select>
          </div>
          <div class="form-row" style="margin-bottom:var(--space-4)">
            <div class="form-group" style="margin:0">
              <label class="form-label">Từ đơn vị</label>
              <select class="form-select">${depts.map(d => `<option value="${d.id}">${d.name}</option>`).join('')}</select>
            </div>
            <div class="form-group" style="margin:0">
              <label class="form-label">Đến đơn vị <span class="required">*</span></label>
              <select class="form-select">${depts.map(d => `<option value="${d.id}">${d.name}</option>`).join('')}</select>
            </div>
          </div>
          <div class="form-row" style="margin-bottom:var(--space-4)">
            <div class="form-group" style="margin:0">
              <label class="form-label">Chức danh mới</label>
              <input class="form-input" placeholder="Chức danh tại đơn vị mới">
            </div>
            <div class="form-group" style="margin:0">
              <label class="form-label">Ngày hiệu lực <span class="required">*</span></label>
              <input class="form-input" type="date">
            </div>
          </div>
          <div class="form-group">
            <label class="form-label">Lý do thuyên chuyển <span class="required">*</span></label>
            <textarea class="form-input" rows="3" placeholder="Nhập lý do..."></textarea>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" onclick="this.closest('.modal-overlay').remove()">Hủy</button>
          <button class="btn btn-primary" onclick="alert('Demo: Quyết định đã được tạo!'); this.closest('.modal-overlay').remove()">💾 Tạo quyết định</button>
        </div>
      </div>
    `;
    document.body.appendChild(overlay);
  }
};
