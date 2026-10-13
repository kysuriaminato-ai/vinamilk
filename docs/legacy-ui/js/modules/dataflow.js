// ============================================
// VINAMILK HRIS - Data Flow Diagram Module
// DFD visualization (Context & Level 1)
// ============================================

const DataFlowModule = {
  currentLevel: 0,

  render(container) {
    container.innerHTML = `
      <div class="page-header">
        <div class="page-header-left">
          <h1 class="page-title">Biểu đồ Luồng Dữ liệu (DFD)</h1>
          <p class="page-subtitle">Data Flow Diagram — Hệ thống HRIS Vinamilk</p>
        </div>
        <div class="page-header-actions">
          <button class="btn ${this.currentLevel === 0 ? 'btn-primary' : 'btn-secondary'}" onclick="DataFlowModule.setLevel(0)"> Context (Level 0)</button>
          <button class="btn ${this.currentLevel === 1 ? 'btn-primary' : 'btn-secondary'}" onclick="DataFlowModule.setLevel(1)"> Level 1</button>
        </div>
      </div>

      <!-- Legend -->
      <div class="card animate-fade-in-up" style="margin-bottom:var(--space-5)">
        <div class="card-body" style="padding:var(--space-3) var(--space-5)">
          <div style="display:flex;align-items:center;gap:var(--space-6);flex-wrap:wrap">
            <span style="font-weight:600;color:var(--text-secondary);font-size:var(--font-size-sm)">Ký hiệu:</span>
            <div style="display:flex;align-items:center;gap:var(--space-2)">
              <div style="width:36px;height:36px;border-radius:50%;border:2px solid #0052CC;display:flex;align-items:center;justify-content:center;font-size:12px;background:#E6F0FF">P</div>
              <span style="font-size:var(--font-size-sm)">Tiến trình (Process)</span>
            </div>
            <div style="display:flex;align-items:center;gap:var(--space-2)">
              <div style="width:36px;height:24px;border-radius:4px;border:2px solid #FF8B00;display:flex;align-items:center;justify-content:center;font-size:10px;background:#FFF4E5">EE</div>
              <span style="font-size:var(--font-size-sm)">Thực thể ngoài (External Entity)</span>
            </div>
            <div style="display:flex;align-items:center;gap:var(--space-2)">
              <div style="width:36px;height:24px;border-bottom:2px solid #36B37E;display:flex;align-items:center;justify-content:center;font-size:10px;background:#E3FCEF;border-top:2px solid #36B37E">DS</div>
              <span style="font-size:var(--font-size-sm)">Kho dữ liệu (Data Store)</span>
            </div>
            <div style="display:flex;align-items:center;gap:var(--space-2)">
              <div style="width:36px;height:2px;background:#5E6C84;position:relative"><div style="position:absolute;right:-4px;top:-4px;border:5px solid transparent;border-left:6px solid #5E6C84"></div></div>
              <span style="font-size:var(--font-size-sm)">Luồng dữ liệu (Data Flow)</span>
            </div>
          </div>
        </div>
      </div>

      <!-- DFD Canvas -->
      <div class="card animate-fade-in-up">
        <div class="card-body" style="padding:0;overflow:hidden">
          <canvas id="dfd-canvas" style="width:100%;cursor:grab"></canvas>
        </div>
      </div>

      <!-- Flow Description -->
      <div class="card animate-fade-in-up" style="margin-top:var(--space-5)">
        <div class="card-header"><div class="card-header-title"> Mô tả Luồng Dữ liệu</div></div>
        <div class="card-body" id="dfd-description"></div>
      </div>
    `;

    setTimeout(() => this.drawDFD(), 200);
  },

  setLevel(level) {
    this.currentLevel = level;
    this.render(document.getElementById('main-content'));
  },

  drawDFD() {
    const canvas = document.getElementById('dfd-canvas');
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    const dpr = window.devicePixelRatio || 1;
    const w = canvas.parentElement.offsetWidth;
    const h = this.currentLevel === 0 ? 520 : 680;
    canvas.width = w * dpr;
    canvas.height = h * dpr;
    canvas.style.width = w + 'px';
    canvas.style.height = h + 'px';
    ctx.scale(dpr, dpr);

    // Background
    ctx.fillStyle = '#FAFBFC';
    ctx.fillRect(0, 0, w, h);

    if (this.currentLevel === 0) {
      this.drawContextDiagram(ctx, w, h);
    } else {
      this.drawLevel1(ctx, w, h);
    }

    this.renderDescription();
  },

  drawContextDiagram(ctx, w, h) {
    const cx = w / 2, cy = h / 2;

    // Central process
    this.drawProcess(ctx, cx, cy, 80, '0', 'Hệ thống\nHRIS\nVinamilk');

    // External entities
    const entities = [
      { x: cx - 320, y: 80, label: 'Nhân viên', icon: '' },
      { x: cx + 320, y: 80, label: 'Ban Lãnh đạo', icon: '' },
      { x: cx - 320, y: h - 80, label: 'Cơ quan BHXH', icon: '️' },
      { x: cx + 320, y: h - 80, label: 'Cơ quan Thuế', icon: '' },
      { x: cx, y: 60, label: 'Ứng viên', icon: '' },
      { x: cx, y: h - 60, label: 'Ngân hàng', icon: '' }
    ];

    entities.forEach(e => this.drawExternalEntity(ctx, e.x, e.y, e.label, e.icon));

    // Data flows
    const flows = [
      { from: { x: cx - 320, y: 80 }, to: { x: cx - 80, y: cy - 20 }, label: 'Hồ sơ, Chấm công', dir: 'right' },
      { from: { x: cx + 80, y: cy - 20 }, to: { x: cx + 320, y: 80 }, label: 'Báo cáo, KPI', dir: 'right' },
      { from: { x: cx - 80, y: cy + 20 }, to: { x: cx - 320, y: h - 80 }, label: 'Dữ liệu BH', dir: 'left' },
      { from: { x: cx + 80, y: cy + 20 }, to: { x: cx + 320, y: h - 80 }, label: 'Dữ liệu thuế TNCN', dir: 'right' },
      { from: { x: cx, y: 90 }, to: { x: cx, y: cy - 80 }, label: 'Đơn ứng tuyển', dir: 'down' },
      { from: { x: cx, y: cy + 80 }, to: { x: cx, y: h - 90 }, label: 'Lệnh chi lương', dir: 'down' },
      { from: { x: cx - 80, y: cy }, to: { x: cx - 320, y: 110 }, label: 'Phiếu lương, Thông báo', dir: 'left' },
      { from: { x: cx + 320, y: 110 }, to: { x: cx + 80, y: cy }, label: 'Phê duyệt, Chính sách', dir: 'left' }
    ];

    flows.forEach(f => this.drawFlow(ctx, f.from.x, f.from.y, f.to.x, f.to.y, f.label));
  },

  drawLevel1(ctx, w, h) {
    const processes = [
      { x: w * 0.2, y: 120, id: '1.0', label: 'Quản lý\nTuyển dụng' },
      { x: w * 0.5, y: 120, id: '2.0', label: 'Quản lý\nHồ sơ NV' },
      { x: w * 0.8, y: 120, id: '3.0', label: 'Quản lý\nHợp đồng' },
      { x: w * 0.15, y: 320, id: '4.0', label: 'Chấm công\n& Nghỉ phép' },
      { x: w * 0.42, y: 320, id: '5.0', label: 'Tính lương\n3P' },
      { x: w * 0.7, y: 320, id: '6.0', label: 'Đào tạo\n& KPI' },
      { x: w * 0.3, y: 520, id: '7.0', label: 'Thuyên chuyển\nCông tác' },
      { x: w * 0.6, y: 520, id: '8.0', label: 'Báo cáo\n& Thống kê' },
      { x: w * 0.85, y: 520, id: '9.0', label: 'Dashboard\nChiến lược' }
    ];

    const stores = [
      { x: w * 0.08, y: 210, label: 'D1  Hồ sơ Ứng viên' },
      { x: w * 0.5, y: 230, label: 'D2  Hồ sơ Nhân viên' },
      { x: w * 0.88, y: 210, label: 'D3  Hợp đồng LĐ' },
      { x: w * 0.08, y: 420, label: 'D4  Chấm công' },
      { x: w * 0.42, y: 435, label: 'D5  Bảng lương' },
      { x: w * 0.75, y: 420, label: 'D6  Đào tạo/KPI' },
      { x: w * 0.3, y: 620, label: 'D7  Lịch sử Thuyên chuyển' }
    ];

    // Draw data stores
    stores.forEach(s => this.drawDataStore(ctx, s.x, s.y, s.label));

    // Draw processes
    processes.forEach(p => this.drawProcess(ctx, p.x, p.y, 50, p.id, p.label));

    // Key flows between processes
    const flows = [
      { from: processes[0], to: processes[1], label: 'Hồ sơ mới' },
      { from: processes[1], to: processes[2], label: 'Thông tin NV' },
      { from: processes[1], to: processes[3], label: 'Danh sách NV' },
      { from: processes[3], to: processes[4], label: 'Dữ liệu công' },
      { from: processes[1], to: processes[5], label: 'NV đào tạo' },
      { from: processes[1], to: processes[6], label: 'NV thuyên chuyển' },
      { from: processes[4], to: processes[7], label: 'Dữ liệu lương' },
      { from: processes[5], to: processes[7], label: 'KPI, Đánh giá' },
      { from: processes[7], to: processes[8], label: 'Báo cáo tổng hợp' }
    ];

    flows.forEach(f => {
      this.drawFlow(ctx, f.from.x, f.from.y + 50, f.to.x, f.to.y - 50, f.label);
    });

    // External entities
    this.drawExternalEntity(ctx, w * 0.05, 50, 'Ứng viên', '');
    this.drawExternalEntity(ctx, w * 0.95, 50, 'Lãnh đạo', '');
    this.drawExternalEntity(ctx, w * 0.95, 620, 'Ban GĐ', '');

    // EE flows
    this.drawFlow(ctx, w * 0.05, 80, w * 0.2, 80, 'Đơn ứng tuyển');
    this.drawFlow(ctx, w * 0.85, 520, w * 0.95, 590, 'Dashboard');
  },

  drawProcess(ctx, x, y, r, id, label) {
    // Circle
    const gradient = ctx.createRadialGradient(x, y, r * 0.2, x, y, r);
    gradient.addColorStop(0, '#E6F0FF');
    gradient.addColorStop(1, '#CCE0FF');
    ctx.beginPath();
    ctx.arc(x, y, r, 0, Math.PI * 2);
    ctx.fillStyle = gradient;
    ctx.fill();
    ctx.strokeStyle = '#0052CC';
    ctx.lineWidth = 2.5;
    ctx.stroke();

    // ID
    ctx.fillStyle = '#0052CC';
    ctx.font = "bold 11px 'Be Vietnam Pro', sans-serif";
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(id, x, y - r * 0.55);

    // Separator line
    ctx.beginPath();
    ctx.moveTo(x - r * 0.6, y - r * 0.35);
    ctx.lineTo(x + r * 0.6, y - r * 0.35);
    ctx.strokeStyle = '#0052CC';
    ctx.lineWidth = 1;
    ctx.stroke();

    // Label
    ctx.fillStyle = '#172B4D';
    ctx.font = "600 11px 'Be Vietnam Pro', sans-serif";
    const lines = label.split('\n');
    lines.forEach((line, i) => {
      ctx.fillText(line, x, y - r * 0.05 + i * 16);
    });
  },

  drawExternalEntity(ctx, x, y, label, icon) {
    const w = 120, h = 44;
    ctx.fillStyle = '#FFF4E5';
    ctx.strokeStyle = '#FF8B00';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.roundRect(x - w / 2, y - h / 2, w, h, 6);
    ctx.fill();
    ctx.stroke();

    ctx.fillStyle = '#172B4D';
    ctx.font = "600 12px 'Be Vietnam Pro', sans-serif";
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(`${icon} ${label}`, x, y);
  },

  drawDataStore(ctx, x, y, label) {
    const w = 180, h = 28;
    ctx.fillStyle = '#E3FCEF';
    ctx.fillRect(x - w / 2, y - h / 2, w, h);
    ctx.strokeStyle = '#36B37E';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(x - w / 2, y - h / 2);
    ctx.lineTo(x + w / 2, y - h / 2);
    ctx.moveTo(x - w / 2, y + h / 2);
    ctx.lineTo(x + w / 2, y + h / 2);
    ctx.stroke();

    ctx.fillStyle = '#006644';
    ctx.font = "600 11px 'Be Vietnam Pro', sans-serif";
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(label, x, y);
  },

  drawFlow(ctx, x1, y1, x2, y2, label) {
    // Line
    ctx.beginPath();
    ctx.moveTo(x1, y1);
    ctx.lineTo(x2, y2);
    ctx.strokeStyle = '#5E6C84';
    ctx.lineWidth = 1.5;
    ctx.stroke();

    // Arrow head
    const angle = Math.atan2(y2 - y1, x2 - x1);
    const headLen = 10;
    ctx.beginPath();
    ctx.moveTo(x2, y2);
    ctx.lineTo(x2 - headLen * Math.cos(angle - Math.PI / 6), y2 - headLen * Math.sin(angle - Math.PI / 6));
    ctx.lineTo(x2 - headLen * Math.cos(angle + Math.PI / 6), y2 - headLen * Math.sin(angle + Math.PI / 6));
    ctx.closePath();
    ctx.fillStyle = '#5E6C84';
    ctx.fill();

    // Label
    if (label) {
      const mx = (x1 + x2) / 2;
      const my = (y1 + y2) / 2;
      ctx.save();
      ctx.fillStyle = 'white';
      const textWidth = ctx.measureText(label).width + 10;
      ctx.fillRect(mx - textWidth / 2, my - 9, textWidth, 18);
      ctx.fillStyle = '#5E6C84';
      ctx.font = "500 10px 'Be Vietnam Pro', sans-serif";
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      ctx.fillText(label, mx, my);
      ctx.restore();
    }
  },

  renderDescription() {
    const descEl = document.getElementById('dfd-description');
    if (!descEl) return;

    if (this.currentLevel === 0) {
      descEl.innerHTML = `
        <div class="content-grid grid-cols-2">
          <div>
            <h3 style="font-size:var(--font-size-md);margin-bottom:var(--space-3)"> Luồng dữ liệu vào</h3>
            <div style="display:flex;flex-direction:column;gap:var(--space-2)">
              <div style="padding:var(--space-2) var(--space-3);background:var(--bg-hover);border-radius:var(--radius-md);font-size:var(--font-size-sm)">
                <strong>Nhân viên →</strong> Hồ sơ cá nhân, Chấm công hàng ngày, Đơn nghỉ phép
              </div>
              <div style="padding:var(--space-2) var(--space-3);background:var(--bg-hover);border-radius:var(--radius-md);font-size:var(--font-size-sm)">
                <strong>Ứng viên →</strong> Đơn ứng tuyển, CV, Hồ sơ xin việc
              </div>
              <div style="padding:var(--space-2) var(--space-3);background:var(--bg-hover);border-radius:var(--radius-md);font-size:var(--font-size-sm)">
                <strong>Ban Lãnh đạo →</strong> Phê duyệt, Chính sách nhân sự, KPI mục tiêu
              </div>
            </div>
          </div>
          <div>
            <h3 style="font-size:var(--font-size-md);margin-bottom:var(--space-3)"> Luồng dữ liệu ra</h3>
            <div style="display:flex;flex-direction:column;gap:var(--space-2)">
              <div style="padding:var(--space-2) var(--space-3);background:var(--bg-hover);border-radius:var(--radius-md);font-size:var(--font-size-sm)">
                <strong>→ Nhân viên:</strong> Phiếu lương, Thông báo, Kết quả KPI
              </div>
              <div style="padding:var(--space-2) var(--space-3);background:var(--bg-hover);border-radius:var(--radius-md);font-size:var(--font-size-sm)">
                <strong>→ BHXH:</strong> Danh sách đóng bảo hiểm, Thông báo tăng/giảm
              </div>
              <div style="padding:var(--space-2) var(--space-3);background:var(--bg-hover);border-radius:var(--radius-md);font-size:var(--font-size-sm)">
                <strong>→ Cơ quan Thuế:</strong> Dữ liệu thuế TNCN, Quyết toán thuế
              </div>
              <div style="padding:var(--space-2) var(--space-3);background:var(--bg-hover);border-radius:var(--radius-md);font-size:var(--font-size-sm)">
                <strong>→ Ngân hàng:</strong> Lệnh chi lương, Danh sách chuyển khoản
              </div>
              <div style="padding:var(--space-2) var(--space-3);background:var(--bg-hover);border-radius:var(--radius-md);font-size:var(--font-size-sm)">
                <strong>→ Ban Lãnh đạo:</strong> Báo cáo tổng hợp, Dashboard chiến lược
              </div>
            </div>
          </div>
        </div>
      `;
    } else {
      descEl.innerHTML = `
        <div class="content-grid grid-cols-3">
          ${[
            { id: '1.0', name: 'Quản lý Tuyển dụng', desc: 'Tiếp nhận đơn ứng tuyển → Sàng lọc → Phỏng vấn → Onboard', store: 'D1 Hồ sơ Ứng viên' },
            { id: '2.0', name: 'Quản lý Hồ sơ NV', desc: 'E-Profile đầy đủ: cá nhân, pháp lý, đào tạo, công tác', store: 'D2 Hồ sơ Nhân viên' },
            { id: '3.0', name: 'Quản lý Hợp đồng', desc: 'Tạo, theo dõi, cảnh báo hết hạn hợp đồng lao động', store: 'D3 Hợp đồng LĐ' },
            { id: '4.0', name: 'Chấm công & Nghỉ phép', desc: 'Ghi nhận công hàng ngày, quản lý đơn nghỉ phép', store: 'D4 Chấm công' },
            { id: '5.0', name: 'Tính lương 3P', desc: 'P1 (Vị trí) + P2 (Năng lực) + P3 (Hiệu suất) + Phụ cấp', store: 'D5 Bảng lương' },
            { id: '6.0', name: 'Đào tạo & KPI', desc: 'Quản lý khóa học, đánh giá KPI cá nhân và phòng ban', store: 'D6 Đào tạo/KPI' },
            { id: '7.0', name: 'Thuyên chuyển', desc: 'Điều chuyển NV giữa các phòng/đơn vị, lưu lịch sử', store: 'D7 Lịch sử TC' },
            { id: '8.0', name: 'Báo cáo & Thống kê', desc: 'Tổng hợp dữ liệu → Biểu đồ, báo cáo cho quản lý', store: 'Tất cả Data Store' },
            { id: '9.0', name: 'Dashboard Chiến lược', desc: 'BI Dashboard cho Ban GĐ: KPI, xu hướng, SWOT, đề xuất AI', store: 'Tất cả Data Store' }
          ].map(p => `
            <div style="padding:var(--space-3);background:var(--bg-hover);border-radius:var(--radius-lg);border-left:3px solid var(--primary)">
              <div style="font-weight:700;color:var(--primary);margin-bottom:var(--space-1)">${p.id} ${p.name}</div>
              <div style="font-size:var(--font-size-sm);color:var(--text-secondary);margin-bottom:var(--space-2)">${p.desc}</div>
              <div style="font-size:var(--font-size-xs);color:var(--accent-green)"> ${p.store}</div>
            </div>
          `).join('')}
        </div>
      `;
    }
  }
};

