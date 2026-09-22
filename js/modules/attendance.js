// ============================================
// VINAMILK HRIS - Attendance Module
// Multi-channel attendance tracking
// ============================================

const AttendanceModule = {
  render(container) {
    const stats = AttendanceData.getStatsOverview();
    const activeEmps = EmployeesHelper.getActive().slice(0, 15);

    container.innerHTML = `
      <div class="page-header">
        <div class="page-header-left">
          <h1 class="page-title">Chấm công Đa kênh</h1>
          <p class="page-subtitle">Tháng 09/2026 — Vân tay / Face ID / GPS / Bảng điện tử</p>
        </div>
        <div class="page-header-actions">
          <select class="form-select" style="width:160px">
            <option>Tháng 09/2026</option>
            <option>Tháng 08/2026</option>
            <option>Tháng 07/2026</option>
          </select>
          <button class="btn btn-primary">📥 Xuất Excel</button>
        </div>
      </div>

      <!-- Stats -->
      <div class="content-grid grid-cols-5" style="margin-bottom:var(--space-5)">
        <div class="stat-card stat-green animate-fade-in-up stagger-1">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${Helpers.formatPercent(stats.attendanceRate)}</div>
          <div class="stat-card-label">Tỷ lệ đi làm</div>
        </div>
        <div class="stat-card stat-orange animate-fade-in-up stagger-2">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${Helpers.formatPercent(stats.lateRate)}</div>
          <div class="stat-card-label">Đi muộn</div>
        </div>
        <div class="stat-card stat-red animate-fade-in-up stagger-3">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${Helpers.formatPercent(stats.absenceRate)}</div>
          <div class="stat-card-label">Vắng mặt</div>
        </div>
        <div class="stat-card stat-cyan animate-fade-in-up stagger-4">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${Helpers.formatNumber(stats.overtimeHours)}</div>
          <div class="stat-card-label">Giờ tăng ca (tổng)</div>
        </div>
        <div class="stat-card stat-purple animate-fade-in-up stagger-5">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.avgOvertimePerPerson}h</div>
          <div class="stat-card-label">TB tăng ca/người</div>
        </div>
      </div>

      <!-- Legend -->
      <div class="card animate-fade-in-up" style="margin-bottom:var(--space-5)">
        <div class="card-body" style="padding:var(--space-3) var(--space-5)">
          <div class="attendance-summary">
            ${Object.entries(AttendanceData.symbols).map(([sym, info]) => `
              <div class="attendance-symbol">
                <span class="attendance-symbol-dot ${info.class}">${sym}</span>
                <span>${info.label}</span>
              </div>
            `).join('')}
          </div>
        </div>
      </div>

      <!-- Channels info -->
      <div class="content-grid grid-cols-4" style="margin-bottom:var(--space-5)">
        ${Object.entries(AttendanceData.channels).map(([type, ch]) => `
          <div class="card animate-fade-in-up">
            <div class="card-body" style="text-align:center;padding:var(--space-4)">
              <div style="font-size:1.5rem;margin-bottom:var(--space-2)">${ch.icon}</div>
              <div style="font-weight:600;margin-bottom:2px">${ch.name}</div>
              <div style="font-size:var(--font-size-xs);color:var(--text-tertiary);text-transform:capitalize">${type === 'factory' ? 'Nhà máy' : type === 'farm' ? 'Trang trại' : type === 'sales' ? 'Kinh doanh' : 'Văn phòng'}</div>
            </div>
          </div>
        `).join('')}
      </div>

      <!-- Attendance Grid -->
      <div class="card animate-fade-in-up">
        <div class="card-header">
          <div class="card-header-title">📅 Bảng chấm công tháng 09/2026</div>
          <div style="font-size:var(--font-size-sm);color:var(--text-secondary)">Hiển thị ${activeEmps.length} nhân viên mẫu</div>
        </div>
        <div class="card-body" style="padding:0">
          <div class="attendance-grid">
            <table>
              <thead>
                <tr>
                  <th class="emp-name" style="text-align:left;min-width:180px">Nhân viên</th>
                  ${Array.from({length: 30}, (_, i) => {
                    const d = new Date(2026, 8, i + 1);
                    const dow = d.getDay();
                    const isSun = dow === 0;
                    return `<th style="${isSun ? 'color:var(--accent-red);background:#FFEBE6' : ''}">${i + 1}<br><span style="font-size:9px">${['CN','T2','T3','T4','T5','T6','T7'][dow]}</span></th>`;
                  }).join('')}
                  <th style="min-width:50px">Công</th>
                </tr>
              </thead>
              <tbody>
                ${activeEmps.map(emp => {
                  const attendance = AttendanceData.generateMonthData(emp.id);
                  const summary = AttendanceData.getSummary(attendance);
                  return `
                  <tr>
                    <td class="emp-name">
                      <div class="employee-cell">
                        ${Helpers.avatarHTML(emp.name, 28)}
                        <div>
                          <div style="font-size:var(--font-size-sm);font-weight:600">${emp.name.split(' ').slice(-2).join(' ')}</div>
                          <div style="font-size:10px;color:var(--text-tertiary)">${emp.id}</div>
                        </div>
                      </div>
                    </td>
                    ${attendance.map(day => {
                      const sym = AttendanceData.symbols[day] || { class: 'day-off' };
                      return `<td class="day-cell ${sym.class}" title="${sym.desc || day}">${day}</td>`;
                    }).join('')}
                    <td style="font-weight:700;text-align:center;background:var(--primary-50);color:var(--primary)">${summary.totalWork}</td>
                  </tr>`;
                }).join('')}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- Shift & OT Rules -->
      <div class="content-grid grid-cols-2" style="margin-top:var(--space-5)">
        <div class="card animate-fade-in-up">
          <div class="card-header"><div class="card-header-title">⏰ Ca làm việc</div></div>
          <div class="card-body" style="padding:0">
            <table class="data-table">
              <thead><tr><th>Ca</th><th>Giờ</th><th>Loại</th><th>Đêm</th></tr></thead>
              <tbody>
                ${AttendanceData.shifts.map(s => `
                  <tr>
                    <td><strong>${s.name}</strong></td>
                    <td>${s.start} - ${s.end}</td>
                    <td><span class="badge badge-info">${s.type}</span></td>
                    <td>${s.nightShift ? '<span class="badge badge-trial">🌙 Ca đêm</span>' : '—'}</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>
        <div class="card animate-fade-in-up">
          <div class="card-header"><div class="card-header-title">💹 Hệ số Tăng ca</div></div>
          <div class="card-body">
            <div style="display:grid;gap:var(--space-3)">
              <div style="display:flex;justify-content:space-between;align-items:center;padding:var(--space-2) 0;border-bottom:1px solid var(--border-light)">
                <span>Ngày thường</span>
                <span class="badge badge-info" style="font-size:var(--font-size-md)">× ${AttendanceData.overtimeRates.weekday}</span>
              </div>
              <div style="display:flex;justify-content:space-between;align-items:center;padding:var(--space-2) 0;border-bottom:1px solid var(--border-light)">
                <span>Thứ 7 / Chủ nhật</span>
                <span class="badge badge-trial" style="font-size:var(--font-size-md)">× ${AttendanceData.overtimeRates.sunday}</span>
              </div>
              <div style="display:flex;justify-content:space-between;align-items:center;padding:var(--space-2) 0;border-bottom:1px solid var(--border-light)">
                <span>Ngày lễ / tết</span>
                <span class="badge badge-danger" style="font-size:var(--font-size-md)">× ${AttendanceData.overtimeRates.holiday}</span>
              </div>
              <div style="display:flex;justify-content:space-between;align-items:center;padding:var(--space-2) 0">
                <span>Ca đêm (22h-06h)</span>
                <span class="badge badge-pending" style="font-size:var(--font-size-md)">× ${AttendanceData.overtimeRates.night}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    `;
  }
};
