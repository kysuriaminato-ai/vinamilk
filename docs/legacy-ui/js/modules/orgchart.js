// ============================================
// VINAMILK HRIS - Org Chart Module
// Interactive organization tree
// ============================================

const OrgChartModule = {
  expandedNodes: new Set(['vinamilk', 'ban_tgd']),

  render(container) {
    container.innerHTML = `
      <div class="page-header">
        <div class="page-header-left">
          <h1 class="page-title">Sơ đồ Tổ chức</h1>
          <p class="page-subtitle">Cơ cấu tổ chức Công ty CP Sữa Việt Nam — ${Helpers.formatNumber(10156)} nhân viên</p>
        </div>
        <div class="page-header-actions">
          <button class="btn btn-secondary" onclick="OrgChartModule.expandAll()"> Mở rộng tất cả</button>
          <button class="btn btn-secondary" onclick="OrgChartModule.collapseAll()"> Thu gọn</button>
        </div>
      </div>

      <div class="card">
        <div class="card-body">
          <div class="org-chart-container" id="org-chart-tree">
            ${this.renderNode(DepartmentsData.orgStructure, true)}
          </div>
        </div>
      </div>
    `;
  },

  renderNode(node, isRoot = false) {
    const hasChildren = node.children && node.children.length > 0;
    const isExpanded = this.expandedNodes.has(node.id);
    const cssClass = node.cssClass || '';
    const rootClass = isRoot ? 'root' : (node.type === 'Khối' ? 'level-1' : '');

    let childrenHTML = '';
    if (hasChildren) {
      childrenHTML = `
        <div class="org-children ${isExpanded ? 'expanded' : ''}" id="org-children-${node.id}">
          ${node.children.map(child => `
            <div class="org-branch">
              ${this.renderNode(child)}
            </div>
          `).join('')}
        </div>
      `;
    }

    return `
      <div class="org-branch" style="display:flex;flex-direction:column;align-items:center;">
        <div class="org-node ${rootClass} ${cssClass}" onclick="OrgChartModule.onNodeClick('${node.id}')" title="${node.name}">
          <div class="org-node-icon">${node.icon || ''}</div>
          <div class="org-node-title">${node.shortName || node.name}</div>
          <div class="org-node-type">${node.type}</div>
          <div class="org-node-count"> ${Helpers.formatNumber(node.headcount)} người</div>
          ${hasChildren ? `
            <button class="org-expand-btn" onclick="event.stopPropagation(); OrgChartModule.toggleNode('${node.id}')" title="${isExpanded ? 'Thu gọn' : 'Mở rộng'}">
              ${isExpanded ? '−' : '+'}
            </button>
          ` : ''}
        </div>
        ${childrenHTML}
      </div>
    `;
  },

  toggleNode(nodeId) {
    if (this.expandedNodes.has(nodeId)) {
      this.expandedNodes.delete(nodeId);
    } else {
      this.expandedNodes.add(nodeId);
    }
    this.rerender();
  },

  onNodeClick(nodeId) {
    const node = DepartmentsData.getDepartment(nodeId);
    if (!node) return;
    
    const employees = EmployeesHelper.getByDepartment(nodeId);
    
    if (employees.length > 0) {
      // Show employee list for this department
      this.showDeptDetail(node, employees);
    } else if (node.children && node.children.length > 0) {
      this.toggleNode(nodeId);
    }
  },

  showDeptDetail(node, employees) {
    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };

    overlay.innerHTML = `
      <div class="modal modal-lg">
        <div class="modal-header">
          <div>
            <div class="modal-title">${node.icon} ${node.name}</div>
            <div style="font-size:var(--font-size-sm);color:var(--text-secondary);margin-top:4px">${node.type} — ${employees.length} nhân viên trong hệ thống mẫu</div>
          </div>
          <button class="modal-close" onclick="this.closest('.modal-overlay').remove()">✕</button>
        </div>
        <div class="modal-body">
          <table class="data-table">
            <thead>
              <tr>
                <th>Nhân viên</th>
                <th>Chức danh</th>
                <th>Trạng thái</th>
                <th>Ngày vào</th>
              </tr>
            </thead>
            <tbody>
              ${employees.map(emp => `
                <tr style="cursor:pointer" onclick="this.closest('.modal-overlay').remove(); window.location.hash='/employees?id=${emp.id}'">
                  <td>
                    <div class="employee-cell">
                      ${Helpers.avatarHTML(emp.name)}
                      <div>
                        <div class="employee-name">${emp.name}</div>
                        <div class="employee-id">${emp.id}</div>
                      </div>
                    </div>
                  </td>
                  <td>${emp.position}</td>
                  <td>${Helpers.statusBadge(emp.status)}</td>
                  <td>${Helpers.formatDate(emp.joinDate)}</td>
                </tr>
              `).join('')}
            </tbody>
          </table>
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" onclick="this.closest('.modal-overlay').remove()">Đóng</button>
          <button class="btn btn-primary" onclick="this.closest('.modal-overlay').remove(); window.location.hash='/employees'">Xem tất cả nhân sự</button>
        </div>
      </div>
    `;

    document.body.appendChild(overlay);
  },

  expandAll() {
    const addAll = (node) => {
      this.expandedNodes.add(node.id);
      if (node.children) node.children.forEach(c => addAll(c));
    };
    addAll(DepartmentsData.orgStructure);
    this.rerender();
  },

  collapseAll() {
    this.expandedNodes.clear();
    this.expandedNodes.add('vinamilk');
    this.rerender();
  },

  rerender() {
    const container = document.getElementById('main-content');
    if (container) this.render(container);
  }
};

