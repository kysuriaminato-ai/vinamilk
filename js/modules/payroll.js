// ============================================
// VINAMILK HRIS - Payroll Module
// 3P Salary calculation & Payslip
// ============================================

const PayrollModule = {
  currentPage: 1,
  perPage: 10,
  showPayslip: null,

  render(container, params = {}) {
    if (params.id) {
      this.showPayslip = params.id;
    }

    const payroll = PayrollData.generatePayroll();
    const summary = PayrollData.getPayrollSummary();
    const paged = Helpers.paginate(payroll, this.currentPage, this.perPage);

    container.innerHTML = `
      <div class="page-header">
        <div class="page-header-left">
          <h1 class="page-title">Bảng Lương 3P</h1>
          <p class="page-subtitle">Tính lương tháng 09/2026 — Mô hình P1 + P2 + P3</p>
        </div>
        <div class="page-header-actions">
          <button class="btn btn-secondary"> Xuất Excel</button>
          <button class="btn btn-success" onclick="PayrollModule.runPayroll()">⚡ Chạy bảng lương</button>
        </div>
      </div>

      <!-- Summary Cards -->
      <div class="payroll-header animate-fade-in-up">
        <div class="payroll-summary-card">
          <div class="payroll-summary-label">P1 — Lương vị trí</div>
          <div class="payroll-summary-value payroll-p1">${Helpers.formatCurrency(summary.totalP1)}</div>
        </div>
        <div class="payroll-summary-card">
          <div class="payroll-summary-label">P2 — Lương năng lực</div>
          <div class="payroll-summary-value payroll-p2">${Helpers.formatCurrency(summary.totalP2)}</div>
        </div>
        <div class="payroll-summary-card">
          <div class="payroll-summary-label">P3 — Lương hiệu suất</div>
          <div class="payroll-summary-value payroll-p3">${Helpers.formatCurrency(summary.totalP3)}</div>
        </div>
        <div class="payroll-summary-card" style="border:2px solid var(--primary)">
          <div class="payroll-summary-label">Tổng chi lương</div>
          <div class="payroll-summary-value payroll-total">${Helpers.formatCurrency(summary.totalGross)}</div>
        </div>
      </div>

      <!-- Additional Stats -->
      <div class="content-grid grid-cols-4" style="margin-bottom:var(--space-5)">
        <div class="stat-card stat-green animate-fade-in-up stagger-1">
          <div class="stat-card-value" style="font-size:var(--font-size-xl)">${Helpers.formatCurrency(summary.avgGross)}</div>
          <div class="stat-card-label">Lương gross TB</div>
        </div>
        <div class="stat-card stat-blue animate-fade-in-up stagger-2">
          <div class="stat-card-value" style="font-size:var(--font-size-xl)">${Helpers.formatCurrency(summary.avgNet)}</div>
          <div class="stat-card-label">Lương net TB</div>
        </div>
        <div class="stat-card stat-orange animate-fade-in-up stagger-3">
          <div class="stat-card-value" style="font-size:var(--font-size-xl)">${Helpers.formatCurrency(summary.totalInsurance)}</div>
          <div class="stat-card-label">Tổng BHXH+BHYT+BHTN</div>
        </div>
        <div class="stat-card stat-red animate-fade-in-up stagger-4">
          <div class="stat-card-value" style="font-size:var(--font-size-xl)">${Helpers.formatCurrency(summary.totalTax)}</div>
          <div class="stat-card-label">Tổng thuế TNCN</div>
        </div>
      </div>

      <!-- Payroll Table -->
      <div class="data-table-wrapper animate-fade-in-up">
        <div class="table-toolbar">
          <div class="table-toolbar-left">
            <div class="card-header-title"> Chi tiết bảng lương</div>
          </div>
          <div class="table-toolbar-right">
            <span style="font-size:var(--font-size-sm);color:var(--text-secondary)">${summary.totalEmployees} nhân viên</span>
          </div>
        </div>
        <div style="overflow-x:auto">
          <table class="data-table" style="min-width:1200px">
            <thead>
              <tr>
                <th>Nhân viên</th>
                <th style="text-align:right;color:var(--primary)">P1</th>
                <th style="text-align:right;color:#36B37E">P2</th>
                <th style="text-align:right;color:#FF8B00">P3</th>
                <th style="text-align:right">Phụ cấp</th>
                <th style="text-align:right;font-weight:800">Gross</th>
                <th style="text-align:right;color:var(--accent-red)">BHXH</th>
                <th style="text-align:right;color:var(--accent-red)">Thuế</th>
                <th style="text-align:right;font-weight:800;color:var(--primary)">Net</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              ${paged.items.map(p => `
                <tr>
                  <td>
                    <div class="employee-cell">
                      ${Helpers.avatarHTML(p.name)}
                      <div>
                        <div class="employee-name">${p.name}</div>
                        <div class="employee-id">${p.employeeId} · ${p.position}</div>
                      </div>
                    </div>
                  </td>
                  <td style="text-align:right;font-size:var(--font-size-sm)">${Helpers.formatCurrency(p.p1)}</td>
                  <td style="text-align:right;font-size:var(--font-size-sm)">${Helpers.formatCurrency(p.p2)}</td>
                  <td style="text-align:right;font-size:var(--font-size-sm)">${Helpers.formatCurrency(p.p3)}</td>
                  <td style="text-align:right;font-size:var(--font-size-sm)">${Helpers.formatCurrency(p.allowances)}</td>
                  <td style="text-align:right;font-weight:700">${Helpers.formatCurrency(p.grossSalary)}</td>
                  <td style="text-align:right;font-size:var(--font-size-sm);color:var(--accent-red)">-${Helpers.formatCurrency(p.totalInsurance)}</td>
                  <td style="text-align:right;font-size:var(--font-size-sm);color:var(--accent-red)">-${Helpers.formatCurrency(p.tax)}</td>
                  <td style="text-align:right;font-weight:800;color:var(--primary)">${Helpers.formatCurrency(p.netSalary)}</td>
                  <td><button class="btn btn-ghost btn-sm" onclick="PayrollModule.viewPayslip('${p.employeeId}')" title="Xem phiếu lương"></button></td>
                </tr>
              `).join('')}
            </tbody>
          </table>
        </div>
        ${Helpers.paginationHTML(paged.total, paged.page, paged.perPage)}
      </div>
    `;

    // Pagination
    container.querySelectorAll('.table-pagination-btn[data-page]').forEach(btn => {
      btn.addEventListener('click', () => {
        const page = parseInt(btn.dataset.page);
        if (page >= 1) { this.currentPage = page; this.render(container); }
      });
    });

    // Auto-show payslip if requested
    if (this.showPayslip) {
      setTimeout(() => this.viewPayslip(this.showPayslip), 300);
      this.showPayslip = null;
    }
  },

  viewPayslip(empId) {
    const emp = EmployeesHelper.getById(empId);
    if (!emp) return;
    const p = PayrollData.calculatePayroll(emp);
    const dept = DepartmentsData.getDepartment(emp.department);

    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
    overlay.innerHTML = `
      <div class="modal modal-lg">
        <div class="modal-header">
          <div class="modal-title"> Phiếu lương tháng 09/2026</div>
          <button class="modal-close" onclick="this.closest('.modal-overlay').remove()">✕</button>
        </div>
        <div class="modal-body">
          <div class="payslip">
            <div class="payslip-header">
              <div>
                <div class="payslip-company"> VINAMILK</div>
                <div style="font-size:var(--font-size-sm);color:var(--text-secondary)">Công ty CP Sữa Việt Nam</div>
              </div>
              <div style="text-align:right">
                <div style="font-weight:600">Phiếu lương</div>
                <div style="font-size:var(--font-size-sm);color:var(--text-secondary)">Tháng 09/2026</div>
              </div>
            </div>

            <div class="payslip-info">
              <div><span class="payslip-info-label">Họ tên:</span> <span class="payslip-info-value">${emp.name}</span></div>
              <div><span class="payslip-info-label">Mã NV:</span> <span class="payslip-info-value">${emp.id}</span></div>
              <div><span class="payslip-info-label">Chức danh:</span> <span class="payslip-info-value">${emp.position}</span></div>
              <div><span class="payslip-info-label">Đơn vị:</span> <span class="payslip-info-value">${dept ? dept.name : emp.department}</span></div>
              <div><span class="payslip-info-label">Ngân hàng:</span> <span class="payslip-info-value">${emp.bank}</span></div>
              <div><span class="payslip-info-label">STK:</span> <span class="payslip-info-value">${emp.bankAccount}</span></div>
            </div>

            <table class="payslip-table">
              <thead><tr><th colspan="2">THU NHẬP</th><th class="amount">Số tiền (VNĐ)</th></tr></thead>
              <tbody>
                <tr><td>1</td><td>P1 — Lương vị trí (Position)</td><td class="amount">${Helpers.formatCurrency(p.p1)}</td></tr>
                <tr><td>2</td><td>P2 — Lương năng lực (Person)</td><td class="amount">${Helpers.formatCurrency(p.p2)}</td></tr>
                <tr><td>3</td><td>P3 — Lương hiệu suất (Performance)</td><td class="amount">${Helpers.formatCurrency(p.p3)}</td></tr>
                <tr><td>4</td><td>Phụ cấp (ca đêm, độc hại, xa nhà...)</td><td class="amount">${Helpers.formatCurrency(p.allowances)}</td></tr>
                <tr class="total-row"><td></td><td><strong>TỔNG THU NHẬP</strong></td><td class="amount">${Helpers.formatCurrency(p.grossSalary)}</td></tr>
              </tbody>
            </table>

            <table class="payslip-table">
              <thead><tr><th colspan="2">KHẤU TRỪ</th><th class="amount">Số tiền (VNĐ)</th></tr></thead>
              <tbody>
                <tr><td>5</td><td>BHXH (8% × ${Helpers.formatCurrency(p.insuranceSalary)})</td><td class="amount" style="color:var(--accent-red)">-${Helpers.formatCurrency(p.bhxh)}</td></tr>
                <tr><td>6</td><td>BHYT (1.5%)</td><td class="amount" style="color:var(--accent-red)">-${Helpers.formatCurrency(p.bhyt)}</td></tr>
                <tr><td>7</td><td>BHTN (1%)</td><td class="amount" style="color:var(--accent-red)">-${Helpers.formatCurrency(p.bhtn)}</td></tr>
                <tr><td>8</td><td>Công đoàn (1%)</td><td class="amount" style="color:var(--accent-red)">-${Helpers.formatCurrency(p.unionFee)}</td></tr>
                <tr><td>9</td><td>Thuế TNCN (${p.dependents} người phụ thuộc)</td><td class="amount" style="color:var(--accent-red)">-${Helpers.formatCurrency(p.tax)}</td></tr>
                <tr class="total-row"><td></td><td><strong>TỔNG KHẤU TRỪ</strong></td><td class="amount" style="color:var(--accent-red)">-${Helpers.formatCurrency(p.totalInsurance + p.tax)}</td></tr>
              </tbody>
            </table>

            <div class="payslip-net-pay">
              <span class="payslip-net-label">THỰC LĨNH</span>
              <span class="payslip-net-value">${Helpers.formatCurrency(p.netSalary)}</span>
            </div>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" onclick="this.closest('.modal-overlay').remove()">Đóng</button>
          <button class="btn btn-primary">️ In phiếu lương</button>
        </div>
      </div>
    `;
    document.body.appendChild(overlay);
  },

  runPayroll() {
    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.innerHTML = `
      <div class="modal" style="max-width:480px">
        <div class="modal-body" style="text-align:center;padding:var(--space-8)">
          <div style="font-size:3rem;margin-bottom:var(--space-4)" class="animate-spin"></div>
          <div style="font-size:var(--font-size-lg);font-weight:700;margin-bottom:var(--space-2)">Đang chạy bảng lương...</div>
          <div style="color:var(--text-secondary);margin-bottom:var(--space-4)">Tính toán lương 3P cho ${EmployeesHelper.getActive().length} nhân viên</div>
          <div class="progress-bar" style="margin-bottom:var(--space-2)">
            <div class="progress-bar-fill striped" id="payroll-progress" style="width:0%"></div>
          </div>
          <div id="payroll-status" style="font-size:var(--font-size-sm);color:var(--text-tertiary)">Đang xử lý...</div>
        </div>
      </div>
    `;
    document.body.appendChild(overlay);

    let progress = 0;
    const interval = setInterval(() => {
      progress += Math.random() * 15 + 5;
      if (progress >= 100) {
        progress = 100;
        clearInterval(interval);
        document.getElementById('payroll-progress').style.width = '100%';
        document.getElementById('payroll-status').textContent = ' Hoàn thành!';
        setTimeout(() => {
          overlay.remove();
          const container = document.getElementById('main-content');
          if (container) this.render(container);
        }, 1000);
      } else {
        document.getElementById('payroll-progress').style.width = progress + '%';
        const steps = ['Tải dữ liệu chấm công...', 'Tính P1 (Lương vị trí)...', 'Tính P2 (Lương năng lực)...', 'Tính P3 (KPI/OKR)...', 'Tính phụ cấp & khấu trừ...', 'Tính thuế TNCN...', 'Xuất bảng lương...'];
        document.getElementById('payroll-status').textContent = steps[Math.floor(progress / 15)] || 'Đang xử lý...';
      }
    }, 300);
  }
};


