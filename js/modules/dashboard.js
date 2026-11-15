// ============================================
// VINAMILK HRIS - Dashboard Module (Live API & Chart.js)
// ============================================

const DashboardModule = {
  async render(container) {
    // 1. Loading State
    container.innerHTML = `
      <div class="page-header">
        <h1 class="page-title">Tổng quan Nhân sự (Analytics)</h1>
      </div>
      <div style="padding: 50px; text-align: center; color: #666;" id="dashboard-loading">
        <span style="font-size: 24px;">⏳</span> Đang phân tích dữ liệu hệ thống...
      </div>
      <div id="dashboard-content"></div>
    `;

    // 2. Fetch API Data
    const response = await Api.get('api-analytics/overview');
    if (!response || response.status !== 'success') {
      container.innerHTML += `<div style="color:red; text-align:center">Lỗi tải dữ liệu Dashboard!</div>`;
      return;
    }

    const data = response.data;
    const hc = data.headcount;
    const deptData = data.department_distribution;
    const turnover = data.turnover_rate_percent;

    document.getElementById('dashboard-loading').style.display = 'none';
    const content = document.getElementById('dashboard-content');

    // 3. Render HTML Layout
    content.innerHTML = `
      <!-- KPI Cards Row -->
      <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 20px; margin-bottom: 30px;">
        <!-- Card 1 -->
        <div class="card" style="background: #fff; padding: 20px; border-left: 4px solid #1976D2; border-radius: 8px; box-shadow: 0 2px 5px rgba(0,0,0,0.05);">
          <div style="color: #757575; font-size: 14px; margin-bottom: 5px;">Tổng nhân sự</div>
          <div style="font-size: 28px; font-weight: bold; color: #333;">${hc.total_active}</div>
        </div>
        
        <!-- Card 2 -->
        <div class="card" style="background: #fff; padding: 20px; border-left: 4px solid #2E7D32; border-radius: 8px; box-shadow: 0 2px 5px rgba(0,0,0,0.05);">
          <div style="color: #757575; font-size: 14px; margin-bottom: 5px;">Tuyển mới trong tháng</div>
          <div style="font-size: 28px; font-weight: bold; color: #333;">+${hc.new_hires_this_month}</div>
        </div>

        <!-- Card 3 -->
        <div class="card" style="background: #fff; padding: 20px; border-left: 4px solid #F6B800; border-radius: 8px; box-shadow: 0 2px 5px rgba(0,0,0,0.05);">
          <div style="color: #757575; font-size: 14px; margin-bottom: 5px;">Đang thử việc</div>
          <div style="font-size: 28px; font-weight: bold; color: #333;">${hc.on_probation}</div>
        </div>

        <!-- Card 4 -->
        <div class="card" style="background: #fff; padding: 20px; border-left: 4px solid #D32F2F; border-radius: 8px; box-shadow: 0 2px 5px rgba(0,0,0,0.05);">
          <div style="color: #757575; font-size: 14px; margin-bottom: 5px;">Tỉ lệ nghỉ việc (Tháng trước)</div>
          <div style="font-size: 28px; font-weight: bold; color: #333;">${turnover}%</div>
        </div>
      </div>

      <!-- Charts Row -->
      <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px;">
        <div class="card" style="background: #fff; padding: 20px; border-radius: 8px; box-shadow: 0 2px 5px rgba(0,0,0,0.05);">
          <h3 style="margin-top:0; color:#1B5E20; border-bottom: 1px solid #eee; padding-bottom: 10px;">Cơ cấu Phòng ban</h3>
          <canvas id="deptChart" height="250"></canvas>
        </div>
        
        <div class="card" style="background: #fff; padding: 20px; border-radius: 8px; box-shadow: 0 2px 5px rgba(0,0,0,0.05);">
          <h3 style="margin-top:0; color:#1B5E20; border-bottom: 1px solid #eee; padding-bottom: 10px;">Báo cáo Tuyển dụng & Thôi việc (Mô phỏng)</h3>
          <canvas id="trendChart" height="250"></canvas>
        </div>
      </div>
    `;

    // 4. Load Chart.js and Draw Charts
    this.loadChartJS(() => {
        this.drawDeptChart(deptData);
        this.drawTrendChart();
    });
  },

  loadChartJS(callback) {
    if (typeof Chart !== 'undefined') {
        callback();
        return;
    }
    const script = document.createElement('script');
    script.src = 'https://cdn.jsdelivr.net/npm/chart.js';
    script.onload = callback;
    document.head.appendChild(script);
  },

  drawDeptChart(deptData) {
    const ctx = document.getElementById('deptChart').getContext('2d');
    
    // Bóc tách Labels và Data từ API
    const labels = deptData.map(d => d.department_name || 'Khác');
    const data = deptData.map(d => d.employee_count);

    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: labels,
            datasets: [{
                data: data,
                backgroundColor: [
                    '#1B5E20', // Xanh đậm Vinamilk
                    '#4CAF50', // Xanh nhạt
                    '#F6B800', // Vàng Vinamilk
                    '#1976D2', // Xanh dương
                    '#9E9E9E'  // Xám
                ],
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { position: 'right' }
            }
        }
    });
  },

  drawTrendChart() {
    const ctx = document.getElementById('trendChart').getContext('2d');
    
    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: ['T1', 'T2', 'T3', 'T4', 'T5', 'T6'],
            datasets: [
                {
                    label: 'Tuyển mới',
                    data: [12, 19, 3, 5, 2, 8],
                    backgroundColor: '#1976D2'
                },
                {
                    label: 'Nghỉ việc',
                    data: [2, 3, 1, 4, 1, 2],
                    backgroundColor: '#D32F2F'
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: { beginAtZero: true }
            }
        }
    });
  }
};
