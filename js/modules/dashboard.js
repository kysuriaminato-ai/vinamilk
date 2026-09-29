// ============================================
// VINAMILK HRIS - Dashboard Module
// KPI cards, charts, alerts
// ============================================

const DashboardModule = {
  render(container) {
    const stats = EmployeesHelper.getStats();
    const payrollSummary = PayrollData.getPayrollSummary();
    const recruitStats = RecruitmentData.getStats();
    const expiring = EmployeesHelper.getContractExpiring(60);
    const attendStats = AttendanceData.getStatsOverview();

    container.innerHTML = `
      <div class="page-header">
        <div class="page-header-left">
          <h1 class="page-title">Tổng quan Nhân sự</h1>
          <p class="page-subtitle">Dashboard BI — Cập nhật: ${new Date().toLocaleDateString('vi-VN')}</p>
        </div>
        <div class="page-header-actions">
          <button class="btn btn-secondary" onclick="window.location.hash='/reports'">
            <span></span> Xem báo cáo
          </button>
        </div>
      </div>

      <!-- KPI Cards -->
      <div class="content-grid grid-cols-4" style="margin-bottom: var(--space-5)">
        <div class="stat-card stat-blue animate-fade-in-up stagger-1">
          <div class="stat-card-top">
            <div class="stat-card-icon"></div>
            <div class="stat-card-trend up">↑ 2.3%</div>
          </div>
          <div class="stat-card-value">${Helpers.formatNumber(10156)}</div>
          <div class="stat-card-label">Tổng nhân sự</div>
          <div class="stat-card-sparkline"><canvas id="spark-total"></canvas></div>
        </div>
        <div class="stat-card stat-green animate-fade-in-up stagger-2">
          <div class="stat-card-top">
            <div class="stat-card-icon"></div>
            <div class="stat-card-trend up">↑ 5.1%</div>
          </div>
          <div class="stat-card-value">${(payrollSummary.totalGross / 1000000000).toFixed(1)}B</div>
          <div class="stat-card-label">Chi phí lương tháng</div>
          <div class="stat-card-sparkline"><canvas id="spark-payroll"></canvas></div>
        </div>
        <div class="stat-card stat-orange animate-fade-in-up stagger-3">
          <div class="stat-card-top">
            <div class="stat-card-icon"></div>
            <div class="stat-card-trend down">↓ 0.5%</div>
          </div>
          <div class="stat-card-value">${recruitStats.totalJobs}</div>
          <div class="stat-card-label">Vị trí đang tuyển</div>
          <div class="stat-card-sparkline"><canvas id="spark-recruit"></canvas></div>
        </div>
        <div class="stat-card stat-red animate-fade-in-up stagger-4">
          <div class="stat-card-top">
            <div class="stat-card-icon"></div>
            <div class="stat-card-trend down">↓ 1.2%</div>
          </div>
          <div class="stat-card-value">4.8%</div>
          <div class="stat-card-label">Tỷ lệ nghỉ việc (YTD)</div>
          <div class="stat-card-sparkline"><canvas id="spark-turnover"></canvas></div>
        </div>
      </div>

      <!-- Charts Row 1 -->
      <div class="content-grid grid-cols-2" style="margin-bottom: var(--space-5)">
        <div class="chart-card animate-fade-in-up stagger-3">
          <div class="chart-card-header">
            <div>
              <div class="chart-card-title">
                Biến động nhân sự 
                <select id="headcount-year" style="border:none; font-family:inherit; font-weight:inherit; font-size:inherit; color:var(--primary); outline:none; background:transparent; cursor:pointer;" onchange="DashboardModule.updateHeadcount(this.value)">
                  <option value="2026" selected>2026</option>
                  <option value="2025">2025</option>
                  <option value="2024">2024</option>
                </select>
              </div>
              <div style="font-size:var(--font-size-sm);color:var(--text-secondary);margin-top:2px">Tuyển mới vs. Nghỉ việc theo tháng</div>
            </div>
            <div class="filter-bar">
              <span class="badge badge-primary"><span class="badge-dot"></span>Tuyển mới</span>
              <span class="badge badge-danger"><span class="badge-dot" style="background:#FF5630"></span>Nghỉ việc</span>
            </div>
          </div>
          <div class="chart-card-body" style="height:280px">
            <canvas id="chart-headcount"></canvas>
          </div>
        </div>

        <div class="chart-card animate-fade-in-up stagger-4">
          <div class="chart-card-header">
            <div>
              <div class="chart-card-title">Phân bổ nhân sự theo Khối</div>
              <div style="font-size:var(--font-size-sm);color:var(--text-secondary);margin-top:2px">Tổng ${Helpers.formatNumber(10156)} nhân viên</div>
            </div>
          </div>
          <div class="chart-card-body" style="display:flex;align-items:center;gap:var(--space-6)">
            <div style="flex:0 0 200px">
              <canvas id="chart-distribution" width="200" height="200"></canvas>
            </div>
            <div class="chart-legend" style="flex:1">
              <div class="chart-legend-item">
                <div class="chart-legend-color" style="background:#0052CC"></div>
                <span class="chart-legend-label">Khối Sản xuất</span>
                <span class="chart-legend-value">5.420 (53%)</span>
              </div>
              <div class="chart-legend-item">
                <div class="chart-legend-color" style="background:#00C7B7"></div>
                <span class="chart-legend-label">Khối Trang trại</span>
                <span class="chart-legend-value">1.850 (18%)</span>
              </div>
              <div class="chart-legend-item">
                <div class="chart-legend-color" style="background:#FF8B00"></div>
                <span class="chart-legend-label">Khối Kinh doanh</span>
                <span class="chart-legend-value">1.980 (20%)</span>
              </div>
              <div class="chart-legend-item">
                <div class="chart-legend-color" style="background:#6554C0"></div>
                <span class="chart-legend-label">Khối Văn phòng</span>
                <span class="chart-legend-value">906 (9%)</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Charts Row 2 -->
      <div class="content-grid grid-cols-2" style="margin-bottom: var(--space-5)">
        <div class="chart-card animate-fade-in-up stagger-5">
          <div class="chart-card-header">
            <div>
              <div class="chart-card-title">Chi phí lương theo Quý</div>
              <div style="font-size:var(--font-size-sm);color:var(--text-secondary);margin-top:2px">Đơn vị: Tỷ VNĐ</div>
            </div>
          </div>
          <div class="chart-card-body" style="height:280px">
            <canvas id="chart-payroll-trend"></canvas>
          </div>
        </div>

        <div class="card animate-fade-in-up stagger-6">
          <div class="card-header">
            <div>
              <div class="card-header-title">Nhắc việc & Cảnh báo</div>
              <div class="card-header-subtitle">Nhiều mục cần xử lý</div>
            </div>
          </div>
          <div class="card-body" style="padding:0">
            <div class="dashboard-alerts">
              <div class="dashboard-alert-item" onclick="window.location.hash='/contracts'">
                <div class="dashboard-alert-icon warning"></div>
                <div class="dashboard-alert-content">
                  <div class="dashboard-alert-title">Nhân viên đến tuổi nghỉ hưu</div>
                  <div class="dashboard-alert-desc">Sắp nghỉ hưu trong 6 tháng tới</div>
                </div>
                <div class="dashboard-alert-count">2</div>
              </div>
              <div class="dashboard-alert-item">
                <div class="dashboard-alert-icon danger"></div>
                <div class="dashboard-alert-content">
                  <div class="dashboard-alert-title">Giấy tờ hết hạn</div>
                  <div class="dashboard-alert-desc">CMND/CCCD, Visa, Thẻ xanh</div>
                </div>
                <div class="dashboard-alert-count">8</div>
              </div>
              <div class="dashboard-alert-item">
                <div class="dashboard-alert-icon info"></div>
                <div class="dashboard-alert-content">
                  <div class="dashboard-alert-title">Sinh nhật nhân viên</div>
                  <div class="dashboard-alert-desc">Trong tuần này</div>
                </div>
                <div class="dashboard-alert-count">15</div>
              </div>
              <div class="dashboard-alert-item">
                <div class="dashboard-alert-icon success"></div>
                <div class="dashboard-alert-content">
                  <div class="dashboard-alert-title">Kỷ niệm ngày vào làm</div>
                  <div class="dashboard-alert-desc">Tháng này</div>
                </div>
                <div class="dashboard-alert-count">1</div>
              </div>
              <div class="dashboard-alert-item">
                <div class="dashboard-alert-icon warning"></div>
                <div class="dashboard-alert-content">
                  <div class="dashboard-alert-title">Chưa ký hợp đồng</div>
                  <div class="dashboard-alert-desc">Nhân viên mới qua thử việc</div>
                </div>
                <div class="dashboard-alert-count">5</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Charts Row 3 -->
      <div class="content-grid grid-cols-2" style="margin-bottom: var(--space-5)">
        <div class="chart-card animate-fade-in-up stagger-7">
          <div class="chart-card-header">
            <div>
              <div class="chart-card-title">Thống kê hợp đồng theo thời hạn</div>
              <div style="font-size:var(--font-size-sm);color:var(--text-secondary);margin-top:2px">Tất cả đơn vị - Đến ngày hôm nay</div>
            </div>
          </div>
          <div class="chart-card-body" style="display:flex;align-items:center;gap:var(--space-6)">
            <div style="flex:0 0 200px">
              <canvas id="chart-contracts" width="200" height="200"></canvas>
            </div>
            <div class="chart-legend" style="flex:1">
              <div class="chart-legend-item">
                <div class="chart-legend-color" style="background:#4CAF50"></div>
                <span class="chart-legend-label">Không xác định thời hạn</span>
                <span class="chart-legend-value">17 (53.13%)</span>
              </div>
              <div class="chart-legend-item">
                <div class="chart-legend-color" style="background:#F44336"></div>
                <span class="chart-legend-label">1 Năm</span>
                <span class="chart-legend-value">14 (43.75%)</span>
              </div>
              <div class="chart-legend-item">
                <div class="chart-legend-color" style="background:#2196F3"></div>
                <span class="chart-legend-label">6 Tháng</span>
                <span class="chart-legend-value">1 (3.13%)</span>
              </div>
            </div>
          </div>
        </div>
        
        <div class="chart-card animate-fade-in-up stagger-8">
          <div class="chart-card-header">
            <div>
              <div class="chart-card-title">Tình hình nghỉ theo phòng ban</div>
              <div style="font-size:var(--font-size-sm);color:var(--text-secondary);margin-top:2px">Số ngày nghỉ / Tháng này</div>
            </div>
          </div>
          <div class="chart-card-body" style="height:250px">
            <canvas id="chart-leaves"></canvas>
          </div>
        </div>
      </div>

      <!-- Quick Stats Row -->
      <div class="content-grid grid-cols-5" style="margin-bottom: var(--space-5)">
        <div class="stat-card stat-cyan animate-fade-in-up stagger-5">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${Helpers.formatPercent(attendStats.attendanceRate)}</div>
          <div class="stat-card-label">Tỷ lệ đi làm</div>
        </div>
        <div class="stat-card stat-purple animate-fade-in-up stagger-6">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.trial}</div>
          <div class="stat-card-label">Đang thử việc</div>
        </div>
        <div class="stat-card stat-green animate-fade-in-up stagger-7">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.avgAge}</div>
          <div class="stat-card-label">Tuổi trung bình</div>
        </div>
        <div class="stat-card stat-blue animate-fade-in-up stagger-8">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.male}/${stats.female}</div>
          <div class="stat-card-label">Nam / Nữ</div>
        </div>
        <div class="stat-card stat-orange animate-fade-in-up stagger-8">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${recruitStats.avgTimeToHire}d</div>
          <div class="stat-card-label">TB ngày tuyển</div>
        </div>
      </div>

      <!-- Recent activities -->
      <div class="card animate-fade-in-up">
        <div class="card-header">
          <div class="card-header-title"> Hoạt động gần đây</div>
        </div>
        <div class="card-body">
          <div class="timeline">
            <div class="timeline-item">
              <div class="timeline-dot green"></div>
              <div class="timeline-date">Hôm nay, 09:30</div>
              <div class="timeline-title">Nhân viên mới bắt đầu thử việc</div>
              <div class="timeline-desc">Cao Minh Khôi (VNM-0051) - Công nhân Chế biến tại NM Tiên Sơn</div>
            </div>
            <div class="timeline-item">
              <div class="timeline-dot blue"></div>
              <div class="timeline-date">Hôm qua, 15:45</div>
              <div class="timeline-title">Phê duyệt yêu cầu tuyển dụng</div>
              <div class="timeline-desc">JOB-004: Lập trình viên Full-stack - Phòng CNTT (2 vị trí)</div>
            </div>
            <div class="timeline-item">
              <div class="timeline-dot orange"></div>
              <div class="timeline-date">05/09/2026, 10:00</div>
              <div class="timeline-title">Cảnh báo hợp đồng sắp hết hạn</div>
              <div class="timeline-desc">Võ Minh Tâm (VNM-0007) - HĐ 3 năm hết hạn 31/08/2026</div>
            </div>
            <div class="timeline-item">
              <div class="timeline-dot purple"></div>
              <div class="timeline-date">04/09/2026, 14:00</div>
              <div class="timeline-title">Hoàn thành đào tạo ISO 22000</div>
              <div class="timeline-desc">15 nhân viên NM Bình Dương đạt chứng nhận</div>
            </div>
            <div class="timeline-item">
              <div class="timeline-dot red"></div>
              <div class="timeline-date">03/09/2026, 11:30</div>
              <div class="timeline-title">Chạy bảng lương tháng 08/2026</div>
              <div class="timeline-desc">Tổng chi phí: ${Helpers.formatCurrency(payrollSummary.totalGross)} cho ${payrollSummary.totalEmployees} nhân viên</div>
            </div>
          </div>
        </div>
      </div>
    `;

    // Render charts after DOM is ready
    setTimeout(() => {
      this.renderCharts();
    }, 200);
  },

  renderCharts() {
    // Sparklines
    Charts.sparkline('spark-total', [9820, 9850, 9900, 9950, 10020, 10080, 10100, 10120, 10156], '#0052CC');
    Charts.sparkline('spark-payroll', [180, 185, 188, 192, 195, 198, 200, 205, 210], '#36B37E');
    Charts.sparkline('spark-recruit', [8, 12, 10, 7, 5, 8, 6, 4, 5], '#FF8B00');
    Charts.sparkline('spark-turnover', [5.8, 5.5, 5.2, 5.0, 4.9, 5.1, 4.7, 4.5, 4.8], '#FF5630');

    // Headcount bar chart
    this.updateHeadcount('2026');

    // Distribution donut
    Charts.donut('chart-distribution', [
      { value: 5420, color: '#0052CC', label: 'Sản xuất' },
      { value: 1850, color: '#00C7B7', label: 'Trang trại' },
      { value: 1980, color: '#FF8B00', label: 'Kinh doanh' },
      { value: 906, color: '#6554C0', label: 'Văn phòng' }
    ], { size: 200, centerText: '10.156', centerSubtext: 'Nhân viên' });

    // Contracts donut
    if (document.getElementById('chart-contracts')) {
      Charts.donut('chart-contracts', [
        { value: 17, color: '#4CAF50', label: 'Không thời hạn' },
        { value: 14, color: '#F44336', label: '1 Năm' },
        { value: 1, color: '#2196F3', label: '6 Tháng' }
      ], { size: 200, centerText: '32', centerSubtext: 'Hợp đồng' });
    }

    // Leaves bar chart
    if (document.getElementById('chart-leaves')) {
      Charts.bar('chart-leaves', 
        ['C.Ty CP', 'Văn phòng', 'P.Kinh doanh', 'Chi nhánh', 'P.Kế toán', 'IT'],
        [
          { data: [160, 42, 19, 11, 8, 7], color: '#6554C0', label: 'Số ngày nghỉ' }
        ]
      );
    }

    // Payroll trend line chart
    Charts.line('chart-payroll-trend',
      ['Q1/25', 'Q2/25', 'Q3/25', 'Q4/25', 'Q1/26', 'Q2/26', 'Q3/26'],
      [
        { data: [580, 595, 610, 640, 615, 630, 650], color: '#0052CC', fill: true, label: 'Tổng chi phí' }
      ],
      { formatY: (v) => (v / 1).toFixed(0) }
    );
  },

  updateHeadcount(year) {
    const dataByYear = {
      '2026': {
        labels: ['T1', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'T8', 'T9', 'T10', 'T11', 'T12'],
        hired: [45, 38, 52, 60, 48, 55, 42, 50, 35, 40, 30, 20],
        resigned: [20, 25, 18, 22, 30, 15, 28, 20, 12, 10, 8, 15]
      },
      '2025': {
        labels: ['T1', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'T8', 'T9', 'T10', 'T11', 'T12'],
        hired: [30, 40, 35, 45, 50, 42, 38, 44, 40, 32, 28, 25],
        resigned: [15, 18, 12, 20, 25, 18, 22, 15, 10, 12, 9, 14]
      },
      '2024': {
        labels: ['T1', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'T8', 'T9', 'T10', 'T11', 'T12'],
        hired: [25, 28, 30, 35, 40, 38, 32, 36, 42, 45, 38, 30],
        resigned: [10, 12, 8, 15, 20, 14, 18, 12, 16, 20, 15, 12]
      }
    };

    const d = dataByYear[year] || dataByYear['2026'];
    Charts.bar('chart-headcount',
      d.labels,
      [
        { data: d.hired, color: '#0052CC', label: 'Tuyển mới' },
        { data: d.resigned, color: '#FF5630', label: 'Nghỉ việc' }
      ]
    );
  }
};


