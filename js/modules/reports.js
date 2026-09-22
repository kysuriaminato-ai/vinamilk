// ============================================
// VINAMILK HRIS - Reports & Analytics Module
// BI dashboards, charts, AI predictions
// ============================================

const ReportsModule = {
  render(container) {
    container.innerHTML = `
      <div class="page-header">
        <div class="page-header-left">
          <h1 class="page-title">Báo cáo & Analytics</h1>
          <p class="page-subtitle">BI Dashboard — Phân tích nhân sự & Dự báo AI</p>
        </div>
        <div class="page-header-actions">
          <select class="form-select" style="width:140px">
            <option>Năm 2026</option>
            <option>Năm 2025</option>
          </select>
          <button class="btn btn-primary">📥 Xuất báo cáo</button>
        </div>
      </div>

      <!-- Report Cards -->
      <div class="content-grid grid-cols-2" style="margin-bottom:var(--space-5)">
        <!-- Turnover Analysis -->
        <div class="chart-card animate-fade-in-up stagger-1">
          <div class="chart-card-header">
            <div class="chart-card-title">📈 Biến động Nhân sự theo Tháng</div>
          </div>
          <div class="chart-card-body" style="height:280px">
            <canvas id="report-turnover"></canvas>
          </div>
        </div>

        <!-- Cost Analysis -->
        <div class="chart-card animate-fade-in-up stagger-2">
          <div class="chart-card-header">
            <div class="chart-card-title">💰 Chi phí Lương theo P1/P2/P3</div>
          </div>
          <div class="chart-card-body" style="height:280px">
            <canvas id="report-salary-breakdown"></canvas>
          </div>
        </div>
      </div>

      <div class="content-grid grid-cols-2" style="margin-bottom:var(--space-5)">
        <!-- Department Distribution -->
        <div class="chart-card animate-fade-in-up stagger-3">
          <div class="chart-card-header">
            <div class="chart-card-title">👥 Phân bổ Nhân sự theo Đơn vị</div>
          </div>
          <div class="chart-card-body" style="height:280px">
            <canvas id="report-dept-dist"></canvas>
          </div>
        </div>

        <!-- Recruitment Funnel -->
        <div class="chart-card animate-fade-in-up stagger-4">
          <div class="chart-card-header">
            <div class="chart-card-title">🎯 Funnel Tuyển dụng 2026</div>
          </div>
          <div class="chart-card-body" style="height:280px">
            <canvas id="report-funnel"></canvas>
          </div>
        </div>
      </div>

      <!-- AI Predictions Section -->
      <div class="card animate-fade-in-up" style="margin-bottom:var(--space-5)">
        <div class="card-header" style="background:linear-gradient(135deg, #403294 0%, #6554C0 100%);border-radius:var(--radius-xl) var(--radius-xl) 0 0">
          <div>
            <div class="card-header-title" style="color:white">🤖 Dự báo AI (Mô phỏng)</div>
            <div style="font-size:var(--font-size-sm);color:rgba(255,255,255,0.7);margin-top:2px">Machine Learning predictions - DSS Level 3</div>
          </div>
        </div>
        <div class="card-body">
          <div class="content-grid grid-cols-3">
            <div style="padding:var(--space-4);background:var(--bg-hover);border-radius:var(--radius-xl)">
              <div style="font-size:1.5rem;margin-bottom:var(--space-2)">📉</div>
              <div style="font-weight:700;margin-bottom:var(--space-1)">Dự báo nghỉ việc Q4/2026</div>
              <div style="font-size:var(--font-size-3xl);font-weight:800;color:var(--accent-red);margin-bottom:var(--space-2)">4.2%</div>
              <div style="font-size:var(--font-size-sm);color:var(--text-secondary)">
                Giảm 0.6% so với Q3. Nhóm rủi ro cao: Sales miền Nam (8 nhân viên), Công nhân NM Sài Gòn (5 nhân viên).
              </div>
              <div class="progress-bar" style="margin-top:var(--space-3)">
                <div class="progress-bar-fill" style="width:42%;background:linear-gradient(90deg,#36B37E,#FF5630)"></div>
              </div>
            </div>
            <div style="padding:var(--space-4);background:var(--bg-hover);border-radius:var(--radius-xl)">
              <div style="font-size:1.5rem;margin-bottom:var(--space-2)">📊</div>
              <div style="font-weight:700;margin-bottom:var(--space-1)">Dự báo nhu cầu tuyển dụng</div>
              <div style="font-size:var(--font-size-3xl);font-weight:800;color:var(--primary);margin-bottom:var(--space-2)">85</div>
              <div style="font-size:var(--font-size-sm);color:var(--text-secondary)">
                Vị trí cần tuyển trong Q4/2026. Chủ yếu: Công nhân SX (45), Sales (20), R&D (8), IT (6), Khác (6).
              </div>
            </div>
            <div style="padding:var(--space-4);background:var(--bg-hover);border-radius:var(--radius-xl)">
              <div style="font-size:1.5rem;margin-bottom:var(--space-2)">💰</div>
              <div style="font-weight:700;margin-bottom:var(--space-1)">Dự báo chi phí lương Q4</div>
              <div style="font-size:var(--font-size-3xl);font-weight:800;color:var(--accent-green);margin-bottom:var(--space-2)">680B</div>
              <div style="font-size:var(--font-size-sm);color:var(--text-secondary)">
                Tăng 4.6% so với Q3 do điều chỉnh lương cuối năm và thưởng KPI. Budget dự phòng: 720B.
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Risk Scores -->
      <div class="data-table-wrapper animate-fade-in-up">
        <div class="table-toolbar">
          <div class="card-header-title">🎯 Risk Score — Nhân viên có nguy cơ nghỉ việc cao (AI)</div>
        </div>
        <table class="data-table">
          <thead>
            <tr><th>Nhân viên</th><th>Đơn vị</th><th>Thâm niên</th><th>Risk Score</th><th>Yếu tố rủi ro</th><th>Đề xuất</th></tr>
          </thead>
          <tbody>
            ${[
              { emp: EmployeesData[15], risk: 78, factors: 'Lương dưới thị trường, Overtime cao', suggest: 'Tăng P3, giảm OT' },
              { emp: EmployeesData[16], risk: 72, factors: 'Thâm niên thấp, Không thăng tiến', suggest: 'Lộ trình phát triển' },
              { emp: EmployeesData[3], risk: 65, factors: 'Đi muộn thường xuyên', suggest: 'Coaching, đổi ca' },
              { emp: EmployeesData[11], risk: 60, factors: 'Xa nhà, chưa đào tạo nâng cao', suggest: 'Phụ cấp, E-learning' },
              { emp: EmployeesData[47], risk: 55, factors: 'Hợp đồng ngắn hạn', suggest: 'Xem xét tái ký sớm' },
            ].map(item => `
              <tr>
                <td>
                  <div class="employee-cell">
                    ${Helpers.avatarHTML(item.emp.name)}
                    <div>
                      <div class="employee-name">${item.emp.name}</div>
                      <div class="employee-id">${item.emp.id}</div>
                    </div>
                  </div>
                </td>
                <td style="font-size:var(--font-size-sm)">${DepartmentsData.getDepartment(item.emp.department)?.name || item.emp.department}</td>
                <td>${Helpers.calcSeniority(item.emp.joinDate)} năm</td>
                <td>
                  <div style="display:flex;align-items:center;gap:var(--space-2)">
                    <div class="progress-bar" style="width:80px;height:6px">
                      <div class="progress-bar-fill" style="width:${item.risk}%;background:${item.risk >= 70 ? '#FF5630' : item.risk >= 50 ? '#FFAB00' : '#36B37E'}"></div>
                    </div>
                    <span style="font-weight:700;color:${item.risk >= 70 ? 'var(--accent-red)' : 'var(--accent-orange)'}">${item.risk}%</span>
                  </div>
                </td>
                <td style="font-size:var(--font-size-sm);color:var(--text-secondary)">${item.factors}</td>
                <td><span class="badge badge-info">${item.suggest}</span></td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </div>
    `;

    setTimeout(() => this.renderCharts(), 200);
  },

  renderCharts() {
    // Turnover by month
    Charts.line('report-turnover',
      ['T1', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'T8', 'T9'],
      [
        { data: [45, 38, 52, 60, 48, 55, 42, 50, 35], color: '#0052CC', fill: true, label: 'Tuyển vào' },
        { data: [20, 25, 18, 22, 30, 15, 28, 20, 12], color: '#FF5630', fill: false, label: 'Nghỉ việc' }
      ]
    );

    // Salary breakdown by P
    Charts.bar('report-salary-breakdown',
      ['SX', 'TT', 'KD', 'VP'],
      [
        { data: [180, 60, 58, 75], color: '#0052CC', label: 'P1' },
        { data: [95, 35, 40, 45], color: '#36B37E', label: 'P2' },
        { data: [70, 25, 55, 30], color: '#FF8B00', label: 'P3' }
      ]
    );

    // Department distribution horizontal bar
    Charts.horizontalBar('report-dept-dist', [
      { label: 'Sản xuất', value: 5420, color: '#0052CC' },
      { label: 'Kinh doanh', value: 1980, color: '#FF8B00' },
      { label: 'Trang trại', value: 1850, color: '#00C7B7' },
      { label: 'Văn phòng', value: 906, color: '#6554C0' }
    ]);

    // Recruitment funnel
    Charts.horizontalBar('report-funnel', [
      { label: 'Ứng tuyển', value: 450, color: '#8993A4' },
      { label: 'Sàng lọc', value: 280, color: '#00B8D9' },
      { label: 'Phỏng vấn', value: 120, color: '#FFAB00' },
      { label: 'Sức khỏe', value: 85, color: '#FF8B00' },
      { label: 'Offer', value: 60, color: '#36B37E' },
      { label: 'Onboard', value: 48, color: '#0052CC' }
    ]);
  }
};
