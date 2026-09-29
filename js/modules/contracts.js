// ============================================
// VINAMILK HRIS - Contracts Module
// Contract lifecycle management
// ============================================

const ContractsModule = {
  render(container) {
    const stats = ContractsData.getContractStats();
    const expiring = EmployeesHelper.getContractExpiring(90);
    const active = EmployeesHelper.getActive().filter(e => e.contractEnd);

    container.innerHTML = `
      <div class="page-header">
        <div class="page-header-left">
          <h1 class="page-title">Quản lý Hợp đồng</h1>
          <p class="page-subtitle">Vòng đời hợp đồng lao động — Tái ký & Offboarding</p>
        </div>
      </div>

      <!-- Stats -->
      <div class="content-grid grid-cols-5" style="margin-bottom:var(--space-5)">
        <div class="stat-card stat-orange animate-fade-in-up stagger-1">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.trial}</div>
          <div class="stat-card-label">Thử việc</div>
        </div>
        <div class="stat-card stat-cyan animate-fade-in-up stagger-2">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.oneYear}</div>
          <div class="stat-card-label">HĐ 1 năm</div>
        </div>
        <div class="stat-card stat-blue animate-fade-in-up stagger-3">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.threeYear}</div>
          <div class="stat-card-label">HĐ 3 năm</div>
        </div>
        <div class="stat-card stat-green animate-fade-in-up stagger-4">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.indefinite}</div>
          <div class="stat-card-label">Không thời hạn</div>
        </div>
        <div class="stat-card stat-red animate-fade-in-up stagger-5">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.expiringSoon}</div>
          <div class="stat-card-label">Sắp hết hạn (60d)</div>
        </div>
      </div>

      ${expiring.length > 0 ? `
      <!-- Expiring Soon Alert -->
      <div class="alert-banner alert-banner-warning animate-fade-in-up" style="margin-bottom:var(--space-5)">
        <span class="alert-banner-icon"></span>
        <div class="alert-banner-content">
          <div class="alert-banner-title">${expiring.length} hợp đồng sắp hết hạn trong 90 ngày tới</div>
          <div class="alert-banner-text">Cần đánh giá KPI và lịch sử công tác trước khi đề xuất tái ký hoặc chấm dứt.</div>
        </div>
      </div>` : ''}

      <!-- Contract types info -->
      <div class="content-grid grid-cols-4" style="margin-bottom:var(--space-5)">
        ${ContractsData.types.map(t => `
          <div class="card animate-fade-in-up">
            <div class="card-body" style="text-align:center">
              <div style="width:12px;height:12px;border-radius:50%;background:${t.color};margin:0 auto var(--space-2)"></div>
              <div style="font-weight:700;margin-bottom:2px">${t.name}</div>
              <div style="font-size:var(--font-size-sm);color:var(--text-secondary)">${t.duration}</div>
            </div>
          </div>
        `).join('')}
      </div>

      <!-- Contracts Table -->
      <div class="data-table-wrapper animate-fade-in-up" style="margin-bottom:var(--space-5)">
        <div class="table-toolbar">
          <div class="card-header-title"> Danh sách Hợp đồng có thời hạn</div>
        </div>
        <table class="data-table">
          <thead>
            <tr>
              <th>Nhân viên</th>
              <th>Loại HĐ</th>
              <th>Ngày ký</th>
              <th>Ngày hết hạn</th>
              <th>Còn lại</th>
              <th>Trạng thái</th>
              <th>Hành động</th>
            </tr>
          </thead>
          <tbody>
            ${active.sort((a, b) => new Date(a.contractEnd) - new Date(b.contractEnd)).map(emp => {
              const daysLeft = Helpers.daysBetween(new Date(), emp.contractEnd);
              const urgent = daysLeft <= 60;
              const critical = daysLeft <= 30;
              return `
              <tr style="${critical ? 'background:#FFEBE6' : urgent ? 'background:#FFF4E5' : ''}">
                <td>
                  <div class="employee-cell">
                    ${Helpers.avatarHTML(emp.name)}
                    <div>
                      <div class="employee-name">${emp.name}</div>
                      <div class="employee-id">${emp.id} · ${emp.position}</div>
                    </div>
                  </div>
                </td>
                <td><span class="badge badge-primary">${emp.contractType}</span></td>
                <td>${Helpers.formatDate(emp.joinDate)}</td>
                <td style="font-weight:${urgent ? '700' : '400'};color:${critical ? 'var(--accent-red)' : ''}">${Helpers.formatDate(emp.contractEnd)}</td>
                <td>
                  <span style="font-weight:700;color:${critical ? 'var(--accent-red)' : urgent ? 'var(--accent-orange)' : 'var(--text-primary)'}">${daysLeft} ngày</span>
                  ${critical ? '<br><span style="font-size:var(--font-size-xs);color:var(--accent-red);font-weight:600"> Khẩn cấp</span>' : ''}
                </td>
                <td>${Helpers.statusBadge(emp.status)}</td>
                <td>
                  <div style="display:flex;gap:var(--space-1)">
                    <button class="btn btn-success btn-sm" onclick="ContractsModule.showRenewModal('${emp.id}')" title="Tái ký"> Tái ký</button>
                    <button class="btn btn-ghost btn-sm" onclick="window.location.hash='/employees?id=${emp.id}'" title="Xem hồ sơ">️</button>
                  </div>
                </td>
              </tr>`;
            }).join('')}
          </tbody>
        </table>
      </div>

      <!-- Offboarding Checklist -->
      <div class="card animate-fade-in-up">
        <div class="card-header">
          <div class="card-header-title"> Quy trình Offboarding (Clearance)</div>
          <div class="card-header-subtitle">Checklist bàn giao khi nhân viên nghỉ việc</div>
        </div>
        <div class="card-body" style="padding:0">
          <table class="data-table">
            <thead><tr><th></th><th>Hạng mục</th><th>Bộ phận phụ trách</th><th>Trạng thái</th></tr></thead>
            <tbody>
              ${ContractsData.offboardingChecklist.map((item, i) => `
                <tr>
                  <td style="font-size:1.2rem;text-align:center">${item.icon}</td>
                  <td style="font-weight:500">${item.task}</td>
                  <td><span class="badge badge-info">${item.department}</span></td>
                  <td>${i < 3 ? '<span class="badge badge-active"><span class="badge-dot"></span>Mẫu sẵn sàng</span>' : '<span class="badge badge-pending"><span class="badge-dot"></span>Chờ xử lý</span>'}</td>
                </tr>
              `).join('')}
            </tbody>
          </table>
        </div>
      </div>
    `;
  },

  showRenewModal(empId) {
    const emp = EmployeesHelper.getById(empId);
    if (!emp) return;

    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
    overlay.innerHTML = `
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title"> Đề xuất Tái ký Hợp đồng</div>
          <button class="modal-close" onclick="this.closest('.modal-overlay').remove()">✕</button>
        </div>
        <div class="modal-body">
          <div style="display:flex;gap:var(--space-4);margin-bottom:var(--space-5);padding:var(--space-4);background:var(--bg-hover);border-radius:var(--radius-lg)">
            ${Helpers.avatarHTML(emp.name, 48)}
            <div>
              <div style="font-weight:700;font-size:var(--font-size-lg)">${emp.name}</div>
              <div style="color:var(--text-secondary)">${emp.id} · ${emp.position}</div>
              <div style="font-size:var(--font-size-sm);color:var(--text-tertiary)">HĐ ${emp.contractType} · Hết hạn ${Helpers.formatDate(emp.contractEnd)}</div>
            </div>
          </div>
          
          <div class="form-group">
            <label class="form-label">Trích xuất KPI & Đánh giá</label>
            <div style="display:grid;grid-template-columns:1fr 1fr;gap:var(--space-3)">
              <div style="background:var(--bg-hover);padding:var(--space-3);border-radius:var(--radius-lg)">
                <div style="font-size:var(--font-size-sm);color:var(--text-tertiary)">KPI trung bình</div>
                <div style="font-size:var(--font-size-xl);font-weight:800;color:var(--accent-green)">85%</div>
              </div>
              <div style="background:var(--bg-hover);padding:var(--space-3);border-radius:var(--radius-lg)">
                <div style="font-size:var(--font-size-sm);color:var(--text-tertiary)">Kỷ luật</div>
                <div style="font-size:var(--font-size-xl);font-weight:800;color:var(--accent-green)">0</div>
              </div>
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">Loại hợp đồng mới</label>
            <select class="form-select">
              <option>Có thời hạn 1 năm</option>
              <option selected>Có thời hạn 3 năm</option>
              <option>Không thời hạn</option>
            </select>
          </div>
          <div class="form-group">
            <label class="form-label">Ghi chú</label>
            <textarea class="form-textarea" placeholder="Nhận xét của quản lý trực tiếp...">Nhân viên có thành tích tốt, đề xuất tái ký HĐ 3 năm và xem xét nâng bậc lương.</textarea>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" onclick="this.closest('.modal-overlay').remove()">Hủy</button>
          <button class="btn btn-primary" onclick="alert('Demo: Đề xuất tái ký đã gửi CHRO phê duyệt!'); this.closest('.modal-overlay').remove()"> Gửi đề xuất</button>
        </div>
      </div>
    `;
    document.body.appendChild(overlay);
  }
};


