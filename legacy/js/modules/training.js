// ============================================
// VINAMILK HRIS - Training Module
// E-Learning & Certificate tracking
// ============================================

const TrainingModule = {
  courses: [
    { id: 'TR-001', title: 'Hội nhập Vinamilk', category: 'Bắt buộc', icon: '', color: '#0052CC', duration: '8 giờ', target: 'Tất cả nhân viên mới', enrolled: 45, completed: 38, progress: 84 },
    { id: 'TR-002', title: 'An toàn Lao động', category: 'Bắt buộc', icon: '', color: '#FF8B00', duration: '16 giờ', target: 'Khối Sản xuất & Trang trại', enrolled: 520, completed: 485, progress: 93 },
    { id: 'TR-003', title: 'An toàn Vệ sinh Thực phẩm', category: 'Bắt buộc', icon: '', color: '#36B37E', duration: '12 giờ', target: 'Khối Sản xuất', enrolled: 380, completed: 350, progress: 92 },
    { id: 'TR-004', title: 'ISO 22000:2018', category: 'Chuyên môn', icon: '', color: '#6554C0', duration: '24 giờ', target: 'QA/QC & Quản lý', enrolled: 120, completed: 95, progress: 79 },
    { id: 'TR-005', title: 'Văn hóa Vinamilk', category: 'Bắt buộc', icon: '', color: '#00B8D9', duration: '4 giờ', target: 'Tất cả nhân viên', enrolled: 850, completed: 780, progress: 92 },
    { id: 'TR-006', title: 'Kỹ năng Lãnh đạo', category: 'Phát triển', icon: '', color: '#E91E8C', duration: '20 giờ', target: 'Quản lý cấp trung', enrolled: 65, completed: 40, progress: 62 },
    { id: 'TR-007', title: 'Kỹ thuật Chăn nuôi Bò sữa', category: 'Chuyên môn', icon: '', color: '#00C7B7', duration: '32 giờ', target: 'Khối Trang trại', enrolled: 180, completed: 150, progress: 83 },
    { id: 'TR-008', title: 'Digital Marketing', category: 'Kỹ năng', icon: '', color: '#FF5630', duration: '16 giờ', target: 'Khối Marketing & KD', enrolled: 90, completed: 55, progress: 61 },
    { id: 'TR-009', title: 'GMP - Thực hành Sản xuất tốt', category: 'Chuyên môn', icon: '', color: '#FFAB00', duration: '20 giờ', target: 'Khối Sản xuất', enrolled: 280, completed: 240, progress: 86 },
  ],

  certAlerts: [
    { employee: 'Hoàng Anh Tuấn', empId: 'VNM-0006', cert: 'An toàn lao động', expiry: '2026-10-15', daysLeft: 38 },
    { employee: 'Trần Thị Ngọc', empId: 'VNM-0010', cert: 'Thú y hành nghề', expiry: '2026-11-30', daysLeft: 84 },
    { employee: 'Lê Thị Hương', empId: 'VNM-0003', cert: 'ATVSTP', expiry: '2026-10-31', daysLeft: 54 },
    { employee: 'Bùi Thanh Sơn', empId: 'VNM-0009', cert: 'An toàn sinh học', expiry: '2026-12-15', daysLeft: 99 },
    { employee: 'Lý Quang Vinh', empId: 'VNM-0018', cert: 'Bằng lái C', expiry: '2027-01-20', daysLeft: 135 },
  ],

  render(container) {
    container.innerHTML = `
      <div class="page-header">
        <div class="page-header-left">
          <h1 class="page-title">Đào tạo & E-Learning</h1>
          <p class="page-subtitle">Quản lý khóa học, tiến độ học tập và chứng chỉ</p>
        </div>
        <div class="page-header-actions">
          <button class="btn btn-primary"> Tạo khóa học mới</button>
        </div>
      </div>

      <!-- Stats -->
      <div class="content-grid grid-cols-4" style="margin-bottom:var(--space-5)">
        <div class="stat-card stat-blue animate-fade-in-up stagger-1">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${this.courses.length}</div>
          <div class="stat-card-label">Khóa học đang mở</div>
        </div>
        <div class="stat-card stat-green animate-fade-in-up stagger-2">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${this.courses.reduce((s, c) => s + c.enrolled, 0)}</div>
          <div class="stat-card-label">Lượt đăng ký</div>
        </div>
        <div class="stat-card stat-cyan animate-fade-in-up stagger-3">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${Math.round(this.courses.reduce((s, c) => s + c.progress, 0) / this.courses.length)}%</div>
          <div class="stat-card-label">Tỷ lệ hoàn thành TB</div>
        </div>
        <div class="stat-card stat-orange animate-fade-in-up stagger-4">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${this.certAlerts.length}</div>
          <div class="stat-card-label">Chứng chỉ sắp hết hạn</div>
        </div>
      </div>

      <!-- Certificate Alerts -->
      ${this.certAlerts.length > 0 ? `
      <div class="alert-banner alert-banner-warning animate-fade-in-up" style="margin-bottom:var(--space-5); display: flex; justify-content: space-between; align-items: center;">
        <div style="display: flex; gap: 15px;">
          <span class="alert-banner-icon"></span>
          <div class="alert-banner-content">
            <div class="alert-banner-title">${this.certAlerts.length} chứng chỉ sắp hết hạn cần gia hạn</div>
            <div class="alert-banner-text">${this.certAlerts.map(a => `${a.employee} (${a.cert} - còn ${a.daysLeft} ngày)`).join(', ')}</div>
          </div>
        </div>
        <button class="btn btn-outline" style="background: white; border: 1px solid #FFAB00; color: #FF8B00;" onclick="alert('Đã gửi Email/Zalo nhắc nhở gia hạn chứng chỉ hàng loạt cho ${this.certAlerts.length} nhân viên!')">Gửi nhắc nhở hàng loạt</button>
      </div>` : ''}
      
      <!-- Gamification Leaderboard -->
      <div class="card animate-fade-in-up" style="margin-bottom:var(--space-5)">
        <div class="card-header">
          <div class="card-header-title"> Bảng xếp hạng Thi đua Học tập (Tháng này)</div>
        </div>
        <div class="card-body" style="display: flex; gap: 20px;">
          <div style="flex: 1; text-align: center; padding: 20px; background: linear-gradient(135deg, rgba(255,171,0,0.1), rgba(255,139,0,0.1)); border-radius: 8px; border: 1px solid rgba(255,171,0,0.3);">
            <div style="font-size: 40px; margin-bottom: 10px;">🏆</div>
            <div style="font-weight: 700; font-size: 16px; margin-bottom: 5px;">Nhà máy Tiên Sơn</div>
            <div style="color: #FF8B00; font-weight: 600;">98% Hoàn thành</div>
            <div style="font-size: 12px; color: var(--text-secondary); margin-top: 5px;">Hạng 1 - Khối Sản Xuất</div>
          </div>
          <div style="flex: 1; text-align: center; padding: 20px; background: linear-gradient(135deg, rgba(0,199,183,0.1), rgba(54,179,126,0.1)); border-radius: 8px; border: 1px solid rgba(54,179,126,0.3);">
            <div style="font-size: 40px; margin-bottom: 10px;">🥈</div>
            <div style="font-weight: 700; font-size: 16px; margin-bottom: 5px;">Phòng Marketing</div>
            <div style="color: #36B37E; font-weight: 600;">95% Hoàn thành</div>
            <div style="font-size: 12px; color: var(--text-secondary); margin-top: 5px;">Hạng 2 - Khối Văn Phòng</div>
          </div>
          <div style="flex: 1; text-align: center; padding: 20px; background: linear-gradient(135deg, rgba(0,184,217,0.1), rgba(0,82,204,0.1)); border-radius: 8px; border: 1px solid rgba(0,82,204,0.3);">
            <div style="font-size: 40px; margin-bottom: 10px;">🥉</div>
            <div style="font-weight: 700; font-size: 16px; margin-bottom: 5px;">Trang trại Nghệ An</div>
            <div style="color: #0052CC; font-weight: 600;">92% Hoàn thành</div>
            <div style="font-size: 12px; color: var(--text-secondary); margin-top: 5px;">Hạng 3 - Khối Nông Nghiệp</div>
          </div>
        </div>
      </div>

      <!-- Course Grid -->
      <div class="content-grid grid-cols-3 animate-fade-in-up" style="margin-bottom:var(--space-5)">
        ${this.courses.map((course, i) => `
          <div class="course-card stagger-${(i % 6) + 1}" style="animation: fadeInUp 0.4s ease both; animation-delay: ${i * 0.05}s">
            <div class="course-card-cover" style="background:linear-gradient(135deg, ${course.color}15, ${course.color}30)">
              <span style="font-size:3rem">${course.icon}</span>
            </div>
            <div class="course-card-body">
              <div class="course-card-category" style="color:${course.color}">${course.category}</div>
              <div class="course-card-title">${course.title}</div>
              <div class="course-card-meta">
                <span> ${course.duration}</span>
                <span> ${course.enrolled} học viên</span>
              </div>
              <div style="font-size:var(--font-size-xs);color:var(--text-tertiary);margin-bottom:var(--space-2)">Đối tượng: ${course.target}</div>
              <div class="course-card-progress">
                <div class="progress-label">
                  <span class="progress-label-text">Tiến độ</span>
                  <span class="progress-label-value">${course.completed}/${course.enrolled} (${course.progress}%)</span>
                </div>
                <div class="progress-bar">
                  <div class="progress-bar-fill" style="width:${course.progress}%;background:${course.color}"></div>
                </div>
              </div>
            </div>
          </div>
        `).join('')}
      </div>

      <!-- Cert Tracking Table -->
      <div class="data-table-wrapper animate-fade-in-up">
        <div class="table-toolbar">
          <div class="card-header-title"> Theo dõi Chứng chỉ sắp hết hạn</div>
        </div>
        <table class="data-table">
          <thead>
            <tr><th>Nhân viên</th><th>Chứng chỉ</th><th>Ngày hết hạn</th><th>Còn lại</th><th>Hành động</th></tr>
          </thead>
          <tbody>
            ${this.certAlerts.map(a => `
              <tr style="${a.daysLeft <= 60 ? 'background:#FFF4E5' : ''}">
                <td>
                  <div class="employee-cell">
                    ${Helpers.avatarHTML(a.employee)}
                    <div>
                      <div class="employee-name">${a.employee}</div>
                      <div class="employee-id">${a.empId}</div>
                    </div>
                  </div>
                </td>
                <td><strong>${a.cert}</strong></td>
                <td>${Helpers.formatDate(a.expiry)}</td>
                <td><span style="font-weight:700;color:${a.daysLeft <= 60 ? 'var(--accent-red)' : 'var(--accent-orange)'}">${a.daysLeft} ngày</span></td>
                <td><button class="btn btn-primary btn-sm"> Nhắc nhở</button></td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </div>
    `;
  }
};


