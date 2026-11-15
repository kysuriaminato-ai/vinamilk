// ============================================
// VINAMILK HRIS - Employees Module
// Replicates the requested "Danh sách cán bộ" & "Hồ sơ cán bộ" UI
// ============================================

const EmployeesModule = {
  render(container, params = {}) {
    if (params.id) {
      this.renderProfile(container, params.id);
    } else {
      this.renderList(container);
    }
  },

  currentPage: 1,
  currentFilters: { search: '', dept: '', status: '' },

  // ==========================================
  // LIST VIEW: Danh sách cán bộ (Dùng Fetch API + Phân trang)
  // ==========================================
  async renderList(container) {
    container.innerHTML = `
      <div class="page-header" style="margin-bottom: 20px;">
        <h1 class="page-title" style="border-left: 4px solid #F6B800; padding-left: 10px;">Danh sách cán bộ</h1>
      </div>
      <div style="padding: 50px; text-align: center; color: var(--text-secondary);" id="emp-loading">
        <span style="font-size: 24px;">🔄</span> Đang tải dữ liệu từ Server...
      </div>
      <div id="emp-content"></div>
    `;

    await this.loadDataAndRender(container.querySelector('#emp-content'));
  },

  async loadDataAndRender(contentDiv) {
    // Xây dựng Query String
    const params = new URLSearchParams({
      page: this.currentPage,
      search: this.currentFilters.search,
      department: this.currentFilters.dept,
      status: this.currentFilters.status
    });

    const response = await Api.get(`api-employee/index?${params.toString()}`);
    
    if (!response || response.status !== 'success') {
        contentDiv.innerHTML = `<div style="color: red; text-align: center;">Lỗi không thể tải dữ liệu!</div>`;
        document.getElementById('emp-loading').style.display = 'none';
        return;
    }

    const employees = response.data;
    const meta = response.meta;

    document.getElementById('emp-loading').style.display = 'none';

    contentDiv.innerHTML = `
      <!-- Filters -->
      <div class="card" style="margin-bottom: 20px; padding: 15px; display: flex; gap: 15px; align-items: center; background: #fff;">
        <input type="text" class="input" placeholder="Tìm theo họ tên hoặc mã..." style="flex:1" id="emp-search" value="${this.currentFilters.search}">
        <select class="input" style="width: 250px" id="emp-dept">
          <option value="">-- Tất cả Tổ/Bộ phận --</option>
          <option value="NM_TS" ${this.currentFilters.dept === 'NM_TS' ? 'selected' : ''}>Nhà máy Tiên Sơn</option>
          <option value="PB_NS" ${this.currentFilters.dept === 'PB_NS' ? 'selected' : ''}>Phòng Nhân sự</option>
        </select>
        <select class="input" style="width: 200px" id="emp-status">
          <option value="">-- Trạng thái --</option>
          <option value="Đang làm việc" ${this.currentFilters.status === 'Đang làm việc' ? 'selected' : ''}>Đang công tác</option>
          <option value="Đã nghỉ việc" ${this.currentFilters.status === 'Đã nghỉ việc' ? 'selected' : ''}>Đã nghỉ việc</option>
        </select>
        <button class="btn btn-primary" style="background: #1B5E20; border: none; padding: 10px 20px;" onclick="EmployeesModule.applyFilters()">
           Lọc
        </button>
      </div>

      <!-- Table Header & Results Count -->
      <div class="card" style="background: #fff; border: 1px solid var(--border-light);">
        <div style="padding: 15px; background: #E8F5E9; border-bottom: 1px solid #C8E6C9; display: flex; justify-content: space-between; align-items: center;">
          <div style="color: #2E7D32; font-weight: 600;"> Tổng kết quả: ${meta.total_records} cán bộ</div>
          <div style="display: flex; gap: 10px;">
            <button class="btn btn-success" style="background: #2E7D32; border: none;">
              + Thêm cán bộ
            </button>
          </div>
        </div>

        <div class="table-responsive">
          <table class="table">
            <thead>
              <tr style="background: #f8f9fa;">
                <th>Mã CB</th>
                <th>Họ và tên</th>
                <th>Giới tính</th>
                <th>Ngày sinh</th>
                <th>Tổ/Bộ phận</th>
                <th>Chức danh</th>
                <th>Trạng thái</th>
                <th style="text-align:center">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              ${employees.map(emp => `
                <tr>
                  <td>${emp.id}</td>
                  <td><a href="#/employees?id=${emp.id}" style="color:#1976D2;font-weight:500;">${emp.name}</a></td>
                  <td>${emp.gender || '-'}</td>
                  <td>${emp.birth_date ? emp.birth_date : '-'}</td>
                  <td>${emp.department_name || emp.department_id || '-'}</td>
                  <td>${emp.position || '-'}</td>
                  <td>
                    <span class="badge ${emp.status === 'Đang làm việc' ? 'badge-success' : 'badge-danger'}" style="background: ${emp.status === 'Đang làm việc' ? '#1B5E20' : '#D32F2F'}; color: white;">
                      ${emp.status}
                    </span>
                  </td>
                  <td style="text-align:center">
                    <button class="btn btn-icon btn-text" style="color:#1976D2;">Sửa</button>
                  </td>
                </tr>
              `).join('')}
              ${employees.length === 0 ? '<tr><td colspan="8" style="text-align:center;padding:20px;">Không tìm thấy dữ liệu</td></tr>' : ''}
            </tbody>
          </table>
        </div>

        <!-- Pagination Controls -->
        ${meta.total_pages > 1 ? `
        <div style="padding: 15px; border-top: 1px solid #eee; display: flex; justify-content: flex-end; align-items: center; gap: 10px;">
           <span style="color: #757575; font-size: 14px;">Trang ${meta.current_page} / ${meta.total_pages}</span>
           <button class="btn btn-outline" ${meta.current_page === 1 ? 'disabled' : ''} onclick="EmployeesModule.changePage(${meta.current_page - 1})">Trước</button>
           <button class="btn btn-outline" ${meta.current_page === meta.total_pages ? 'disabled' : ''} onclick="EmployeesModule.changePage(${meta.current_page + 1})">Sau</button>
        </div>
        ` : ''}
      </div>
    `;

    // Gắn sự kiện "Enter" cho ô tìm kiếm
    const searchInput = document.getElementById('emp-search');
    if (searchInput) {
        searchInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                EmployeesModule.applyFilters();
            }
        });
    }
  },

  applyFilters() {
    this.currentFilters = {
        search: document.getElementById('emp-search').value,
        dept: document.getElementById('emp-dept').value,
        status: document.getElementById('emp-status').value
    };
    this.currentPage = 1; // Reset về trang 1 khi lọc
    
    // Tạo hiệu ứng loading mờ form
    const contentDiv = document.getElementById('emp-content');
    contentDiv.style.opacity = '0.5';
    this.loadDataAndRender(contentDiv);
  },

  changePage(page) {
    this.currentPage = page;
    const contentDiv = document.getElementById('emp-content');
    contentDiv.style.opacity = '0.5';
    this.loadDataAndRender(contentDiv);
  },

  // ==========================================
  // PROFILE VIEW: Hồ sơ cán bộ
  // ==========================================
  renderProfile(container, id) {
    const employees = typeof EmployeesData !== 'undefined' ? EmployeesData : [];
    const emp = employees.find(e => e.id === id);
    if (!emp) {
      container.innerHTML = `<div class="card"><div class="card-body">Không tìm thấy cán bộ</div></div>`;
      return;
    }

    container.innerHTML = `
      <div class="page-header" style="margin-bottom: 20px; display: flex; justify-content: space-between; align-items: center;">
        <h1 class="page-title" style="border-left: 4px solid #F6B800; padding-left: 10px;">Hồ sơ cán bộ: ${emp.name}</h1>
        <div style="display: flex; gap: 10px;">
          <button class="btn btn-outline" style="border: 1px solid #1976D2; color: #1976D2;" onclick="window.print()">️ In hồ sơ</button>
          <button class="btn btn-secondary" onclick="window.history.back()">← Quay lại</button>
        </div>
      </div>

      <div class="grid-cols-12" style="display: grid; grid-template-columns: repeat(12, 1fr); gap: 20px;">
        <!-- Left Sidebar: Avatar & Quick Actions -->
        <div style="grid-column: span 3;">
          <div class="card" style="text-align: center; padding: 20px; border: 1px solid var(--border-light); background: #fff;">
            <div style="width: 150px; height: 180px; margin: 0 auto 15px; border: 1px solid #E0E0E0; display: flex; align-items: center; justify-content: center; background: #FAFAFA; border-radius: 4px; overflow: hidden;">
              <div style="font-size: 60px; color: #BDBDBD;"></div>
            </div>
            <h3 style="margin-bottom: 5px; font-weight: 700;">${emp.name}</h3>
            <div style="color: var(--text-secondary); margin-bottom: 10px;">${emp.id}</div>
            <div style="margin-bottom: 15px;">
              <span class="badge" style="background: #1B5E20; color: white; font-size: 12px;">Đang công tác</span>
            </div>
            <button class="btn btn-block" style="background: #fff; border: 1px solid #1976D2; color: #1976D2;">
               Sửa hồ sơ
            </button>
          </div>
        </div>

        <div style="grid-column: span 9; background: #fff; border: 1px solid var(--border-light); border-radius: 4px; box-shadow: 0 1px 3px rgba(0,0,0,0.05);">
          <!-- Tabs Header -->
          <div style="display: flex; border-bottom: 1px solid var(--border-light); overflow-x: auto; font-size: 14px;">
            <div onclick="EmployeesModule.switchTab(this, 'tab-chitiet')" style="padding: 15px 20px; border-bottom: 2px solid #1976D2; color: #1976D2; font-weight: 600; cursor: pointer; white-space: nowrap;">Chi tiết</div>
            <div onclick="EmployeesModule.switchTab(this, 'tab-phucloi')" style="padding: 15px 20px; color: var(--text-secondary); cursor: pointer; white-space: nowrap;">Phúc lợi</div>
            <div onclick="EmployeesModule.switchTab(this, 'tab-hoso')" style="padding: 15px 20px; color: var(--text-secondary); cursor: pointer; white-space: nowrap;">Hồ sơ</div>
            <div onclick="EmployeesModule.switchTab(this, 'tab-nangluc')" style="padding: 15px 20px; color: var(--text-secondary); cursor: pointer; white-space: nowrap;">Năng lực</div>
            <div onclick="EmployeesModule.switchTab(this, 'tab-daotao')" style="padding: 15px 20px; color: var(--text-secondary); cursor: pointer; white-space: nowrap;">Đào tạo</div>
            <div onclick="EmployeesModule.switchTab(this, 'tab-taisan')" style="padding: 15px 20px; color: var(--text-secondary); cursor: pointer; white-space: nowrap;">Tài sản</div>
            <div onclick="EmployeesModule.switchTab(this, 'tab-phanquyen')" style="padding: 15px 20px; color: var(--text-secondary); cursor: pointer; white-space: nowrap;">Phân quyền</div>
            <div onclick="EmployeesModule.switchTab(this, 'tab-congviec')" style="padding: 15px 20px; color: var(--text-secondary); cursor: pointer; white-space: nowrap;">Công việc</div>
            <div style="padding: 15px 20px; color: var(--text-secondary); cursor: pointer; white-space: nowrap;" onclick="EmployeesModule.showTransferModal('${emp.id}')">Thuyên chuyển</div>
          </div>
          
          <!-- Tab Body (Chi tiết) -->
          <div id="tab-chitiet" class="tab-body" style="padding: 20px; height: 600px; overflow-y: auto; display: block;">
            
            <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 15px; margin-bottom: 25px;">
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Cấp bậc chức vụ</label>
                <select class="input" style="width:100%"><option>${emp.position || 'Chọn'}</option><option>Quản lý</option></select>
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Mã phân ca</label>
                <input type="text" class="input" style="width:100%; background: #f5f5f5;" value="Ca hành chính" readonly>
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Mã chấm công</label>
                <input type="text" class="input" style="width:100%" value="${emp.id.replace('VNM-', 'CC')}">
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Nơi làm việc</label>
                <select class="input" style="width:100%"><option>${this._getDeptName(emp.department)}</option></select>
              </div>
            </div>

            <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 15px; margin-bottom: 25px;">
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Nơi sinh</label>
                <input type="text" class="input" style="width:100%" value="${emp.address || ''}">
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Tên thường gọi</label>
                <input type="text" class="input" style="width:100%" value="${emp.name}">
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Ngày bắt đầu tính thâm niên</label>
                <input type="date" class="input" style="width:100%" value="${emp.joinDate || ''}">
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Ngày thôi việc</label>
                <input type="date" class="input" style="width:100%; background: #f5f5f5;" readonly>
              </div>
            </div>

            <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 15px; margin-bottom: 25px;">
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Cư trú</label>
                <select class="input" style="width:100%"><option>Nội trú</option><option>Ngoại trú</option></select>
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Dân tộc</label>
                <select class="input" style="width:100%"><option>Kinh</option><option>Khác</option></select>
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Tôn giáo</label>
                <select class="input" style="width:100%"><option>Không</option><option>Phật giáo</option></select>
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Quốc tịch</label>
                <select class="input" style="width:100%"><option>Việt Nam</option><option>Khác</option></select>
              </div>
            </div>

            <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 15px; margin-bottom: 25px;">
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Số CMND/Thẻ căn cước</label>
                <input type="text" class="input" style="width:100%" value="001099123456">
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Ngày cấp</label>
                <input type="date" class="input" style="width:100%" value="2020-05-15">
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Nơi cấp</label>
                <input type="text" class="input" style="width:100%" value="Cục cảnh sát QLHC về TTXH">
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Học vấn</label>
                <select class="input" style="width:100%"><option>${emp.education || 'Đại học'}</option></select>
              </div>
            </div>

            <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 15px; margin-bottom: 25px;">
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Hình thức chi lương</label>
                <select class="input" style="width:100%"><option>Chuyển khoản</option><option>Tiền mặt</option></select>
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Ngân hàng</label>
                <select class="input" style="width:100%"><option>Vietcombank</option><option>MB Bank</option></select>
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Số tài khoản</label>
                <input type="text" class="input" style="width:100%" value="0123456789">
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Mã số thuế</label>
                <input type="text" class="input" style="width:100%" value="812349129">
              </div>
            </div>
            
            <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 15px; margin-bottom: 25px;">
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Tỉnh thường trú</label>
                <select class="input" style="width:100%"><option>Hà Nội</option><option>TP.HCM</option></select>
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Quận thường trú</label>
                <select class="input" style="width:100%"><option>Cầu Giấy</option></select>
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Phường/Xã thường trú</label>
                <select class="input" style="width:100%"><option>Dịch Vọng</option></select>
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Địa chỉ thường trú</label>
                <input type="text" class="input" style="width:100%" value="Số 10, Ngõ 1, Xuân Thủy">
              </div>
            </div>

            <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 15px; margin-bottom: 25px;">
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Tỉnh tạm trú</label>
                <select class="input" style="width:100%"><option>Hà Nội</option><option>TP.HCM</option></select>
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Quận tạm trú</label>
                <select class="input" style="width:100%"><option>Nam Từ Liêm</option></select>
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Phường/Xã tạm trú</label>
                <select class="input" style="width:100%"><option>Mễ Trì</option></select>
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Địa chỉ tạm trú</label>
                <input type="text" class="input" style="width:100%" value="Chung cư The Manor">
              </div>
            </div>

            <h4 style="color: var(--text-primary); margin: 30px 0 15px; border-bottom: 1px solid var(--border-light); padding-bottom: 5px; text-transform: uppercase;">Thông tin công tác & Tuyển dụng (Trang 1)</h4>
            <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 15px; margin-bottom: 25px;">
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Bí danh</label>
                <input type="text" class="input" style="width:100%">
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Số hiệu CC, VC</label>
                <input type="text" class="input" style="width:100%" value="CC-${emp.id}">
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Cơ quan tuyển dụng</label>
                <input type="text" class="input" style="width:100%">
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Vị trí tuyển dụng</label>
                <input type="text" class="input" style="width:100%">
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Công việc đảm nhiệm</label>
                <input type="text" class="input" style="width:100%" value="${emp.position || ''}">
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Mã ngạch hiện nay</label>
                <input type="text" class="input" style="width:100%" value="01.001">
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Lĩnh vực công tác</label>
                <input type="text" class="input" style="width:100%">
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Đại biểu</label>
                <select class="input" style="width:100%"><option>Không</option><option>Quốc hội</option><option>HĐND cấp tỉnh</option></select>
              </div>
            </div>

            <h4 style="color: var(--text-primary); margin: 30px 0 15px; border-bottom: 1px solid var(--border-light); padding-bottom: 5px; text-transform: uppercase;">Sức khỏe & Đặc biệt (Trang 2)</h4>
            <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 15px; margin-bottom: 25px;">
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Tình trạng sức khỏe</label>
                <select class="input" style="width:100%"><option>Loại 1 (Tốt)</option><option>Loại 2</option><option>Loại 3</option></select>
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Chiều cao / Cân nặng</label>
                <div style="display: flex; gap: 5px;">
                  <input type="text" class="input" style="width:50%" placeholder="cm">
                  <input type="text" class="input" style="width:50%" placeholder="kg">
                </div>
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Nhóm máu</label>
                <select class="input" style="width:100%"><option>Chưa rõ</option><option>O</option><option>A</option><option>B</option><option>AB</option></select>
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Thương binh / Khuyết tật</label>
                <input type="text" class="input" style="width:100%">
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Năng lực sở trường</label>
                <input type="text" class="input" style="width:100%">
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Việc làm lâu nhất</label>
                <input type="text" class="input" style="width:100%">
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Khen thưởng cao nhất</label>
                <input type="text" class="input" style="width:100%">
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Kỷ luật cao nhất</label>
                <input type="text" class="input" style="width:100%">
              </div>
              <div style="grid-column: span 4;">
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Lịch sử vi phạm (Bị bắt, làm việc cho chế độ cũ, vv...)</label>
                <textarea class="input" style="width:100%; height: 50px; padding: 10px;"></textarea>
              </div>
            </div>

            <h4 style="color: var(--text-primary); margin: 30px 0 15px; border-bottom: 1px solid var(--border-light); padding-bottom: 5px; text-transform: uppercase;">Quan hệ Nước ngoài (Trang 3)</h4>
            <div style="display: grid; grid-template-columns: repeat(1, 1fr); gap: 15px; margin-bottom: 25px;">
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Tham gia tổ chức nước ngoài</label>
                <textarea class="input" style="width:100%; height: 50px; padding: 10px;"></textarea>
              </div>
              <div>
                <label style="display:block; margin-bottom: 5px; font-size: 13px; color: var(--text-secondary);">Có thân nhân ở nước ngoài</label>
                <textarea class="input" style="width:100%; height: 50px; padding: 10px;"></textarea>
              </div>
            </div>

            <div style="display: flex; justify-content: flex-end; margin-top: 20px; border-top: 1px solid var(--border-light); padding-top: 20px;">
              <button class="btn btn-secondary" style="margin-right: 10px;">Làm mới</button>
              <button class="btn btn-primary" onclick="alert('Đã lưu thông tin hồ sơ thành công!')">Lưu thông tin</button>
            </div>
            
          </div>
          
          <!-- Tab Body (Phúc lợi) -->
          <div id="tab-phucloi" class="tab-body" style="padding: 20px; height: 600px; overflow-y: auto; display: none;">
            <h4 style="color: var(--text-primary); margin-bottom: 15px;">Thông tin Phúc lợi & BHXH</h4>
            <div style="background: #F8FAFC; padding: 20px; border-radius: 8px; border: 1px dashed var(--border-medium); text-align: center; color: var(--text-secondary);">
              Chưa có dữ liệu phúc lợi cho cán bộ này.
            </div>
          </div>

          <!-- Tab Body (Hồ sơ) -->
          <div id="tab-hoso" class="tab-body" style="padding: 20px; height: 600px; overflow-y: auto; display: none;">
            <h4 style="color: var(--text-primary); margin-bottom: 15px;">Quản lý Hồ sơ đính kèm</h4>
            <div style="background: #F8FAFC; padding: 20px; border-radius: 8px; border: 1px dashed var(--border-medium); text-align: center; color: var(--text-secondary);">
              Chưa có tệp tin đính kèm.
            </div>
          </div>

          <!-- Tab Body (Năng lực) -->
          <div id="tab-nangluc" class="tab-body" style="padding: 20px; height: 600px; overflow-y: auto; display: none;">
            <h4 style="color: var(--text-primary); margin-bottom: 15px;">Đánh giá Năng lực (KPI)</h4>
            <div style="background: #F8FAFC; padding: 20px; border-radius: 8px; border: 1px dashed var(--border-medium); text-align: center; color: var(--text-secondary);">
              Module đánh giá KPI đang được cập nhật.
            </div>
          </div>

          <!-- Tab Body (Đào tạo) -->
          <div id="tab-daotao" class="tab-body" style="padding: 20px; height: 600px; overflow-y: auto; display: none;">
            <h4 style="color: var(--text-primary); margin-bottom: 15px;">Lịch sử Đào tạo & Khóa học</h4>
            <div style="background: #F8FAFC; padding: 20px; border-radius: 8px; border: 1px dashed var(--border-medium); text-align: center; color: var(--text-secondary);">
              Không có khóa học nào gần đây.
            </div>
          </div>

          <!-- Tab Body (Tài sản) -->
          <div id="tab-taisan" class="tab-body" style="padding: 20px; height: 600px; overflow-y: auto; display: none;">
            <h4 style="color: var(--text-primary); margin-bottom: 15px;">Tài sản cấp phát</h4>
            <div style="background: #F8FAFC; padding: 20px; border-radius: 8px; border: 1px dashed var(--border-medium); text-align: center; color: var(--text-secondary);">
              Chưa có tài sản nào được ghi nhận giao cho nhân viên.
            </div>
          </div>

          <!-- Tab Body (Phân quyền) -->
          <div id="tab-phanquyen" class="tab-body" style="padding: 20px; height: 600px; overflow-y: auto; display: none;">
            <h4 style="color: var(--text-primary); margin-bottom: 15px;">Phân quyền & Tài khoản hệ thống</h4>
            <div style="background: #F8FAFC; padding: 20px; border-radius: 8px; border: 1px dashed var(--border-medium); text-align: center; color: var(--text-secondary);">
              Tài khoản hệ thống hiện tại đang hoạt động bình thường.
            </div>
          </div>

          <!-- Tab Body (Công việc) -->
          <div id="tab-congviec" class="tab-body" style="padding: 20px; height: 600px; overflow-y: auto; display: none;">
            <h4 style="color: var(--text-primary); margin-bottom: 15px;">Kế hoạch Công việc (Giao việc)</h4>
            <div style="background: #F8FAFC; padding: 20px; border-radius: 8px; border: 1px dashed var(--border-medium); text-align: center; color: var(--text-secondary);">
              Không có công việc nào đang được giao trên hệ thống quản lý công việc.
            </div>
          </div>

        </div>
      </div>
    `;
  },

  _getDeptName(code) {
    // Quick map for display
    const depts = {
      'nm_ts': 'Nhà máy Tiên Sơn',
      'nm_na': 'Nhà máy Nghệ An',
      'nm_bd': 'Nhà máy Bình Dương',
      'pb_nhansu': 'Phòng Nhân sự',
      'pb_taichinh': 'Phòng Tài chính',
      'pb_it': 'Phòng CNTT',
      'cn_nam': 'Chi nhánh miền Nam',
      'cn_bac': 'Chi nhánh miền Bắc',
      'tt_green1': 'Trang trại Green Farm 1'
    };
    return depts[code] || code || 'Chưa phân bổ';
  },

  // ==========================================
  // ADVANCED SEARCH MODAL
  // ==========================================
  showAdvancedSearch() {
    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
    overlay.innerHTML = `
      <div class="modal" style="max-width: 600px;">
        <div class="modal-header" style="background: #E8F5E9; border-bottom: 1px solid #C8E6C9;">
          <div class="modal-title" style="color: #2E7D32;"> Tra cứu thông tin cán bộ (Nâng cao)</div>
          <button class="modal-close" onclick="this.closest('.modal-overlay').remove()">✕</button>
        </div>
        <div class="modal-body" style="background: #F1F8E9;">
          <div style="display: flex; gap: 10px; margin-bottom: 15px;">
            <select class="input" style="flex: 2">
              <option>Trình độ CM</option>
              <option>Ngoại ngữ</option>
              <option>Bậc lương</option>
              <option>Tuổi</option>
            </select>
            <select class="input" style="flex: 1">
              <option>=</option>
              <option>></option>
              <option><</option>
            </select>
            <input type="text" class="input" style="flex: 3" placeholder="Giá trị...">
          </div>
          <div style="display: flex; gap: 10px; margin-bottom: 15px;">
            <button class="btn btn-outline">VÀ (AND)</button>
            <button class="btn btn-outline">HOẶC (OR)</button>
            <button class="btn btn-outline">(</button>
            <button class="btn btn-outline">)</button>
          </div>
          <textarea class="input" style="width: 100%; height: 100px; margin-bottom: 15px;" placeholder="Biểu thức tra cứu..."></textarea>
          <div style="display: flex; gap: 10px;">
            <button class="btn btn-secondary">Thêm điều kiện</button>
            <button class="btn btn-secondary">Xóa điều kiện</button>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" onclick="this.closest('.modal-overlay').remove()">Quay ra</button>
          <button class="btn btn-primary" style="background: #2E7D32;" onclick="alert('Đang thực hiện truy vấn...'); this.closest('.modal-overlay').remove()"> Thực hiện tra cứu</button>
        </div>
      </div>
    `;
    document.body.appendChild(overlay);
  },

  // ==========================================
  // TRANSFER MODAL
  // ==========================================
  showTransferModal(empId) {
    const employees = typeof EmployeesData !== 'undefined' ? EmployeesData : [];
    const emp = employees.find(e => e.id === empId);
    if (!emp) return;

    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
    overlay.innerHTML = `
      <div class="modal" style="max-width: 600px;">
        <div class="modal-header" style="background: #000080; color: white;">
          <div class="modal-title">Thuyên chuyển cán bộ</div>
          <button class="modal-close" style="color: white;" onclick="this.closest('.modal-overlay').remove()">✕</button>
        </div>
        <div class="modal-body" style="background: #E8F5E9;">
          <div style="margin-bottom: 15px;">
            <strong>Cán bộ:</strong> ${emp.name} (${emp.id})
            <br>
            <strong>Đơn vị hiện tại:</strong> ${this._getDeptName(emp.department)}
          </div>
          <div class="form-group" style="background: #C8E6C9; padding: 15px; border-radius: 4px;">
            <strong style="display: block; margin-bottom: 10px;">Phương án thuyên chuyển</strong>
            <div style="display: flex; gap: 15px; margin-bottom: 15px;">
              <label><input type="radio" name="transfer_type" value="noibo" checked> Chuyển nội bộ</label>
              <label><input type="radio" name="transfer_type" value="khac"> Chuyển đi nơi khác</label>
              <label><input type="radio" name="transfer_type" value="nghihuu"> Nghỉ hưu</label>
              <label><input type="radio" name="transfer_type" value="thoiviec"> Thôi việc</label>
            </div>
            
            <div style="margin-bottom: 15px;">
              <label class="form-label">Đơn vị nơi chuyển đến (Nếu nội bộ):</label>
              <select class="input" style="width: 100%" id="transfer-dept">
                <option value="nm_ts">Nhà máy Tiên Sơn</option>
                <option value="pb_nhansu">Phòng Nhân sự</option>
                <option value="pb_taichinh">Phòng Tài chính</option>
                <option value="pb_it">Phòng CNTT</option>
                <option value="khac">Đơn vị khác...</option>
              </select>
            </div>
            
            <div>
              <label class="form-label">Ngày chuyển:</label>
              <input type="date" class="input" id="transfer-date" value="2026-09-29">
            </div>
          </div>
        </div>
        <div class="modal-footer" style="background: #C8E6C9;">
          <button class="btn btn-secondary" onclick="this.closest('.modal-overlay').remove()">Quay ra</button>
          <button class="btn btn-primary" onclick="alert('Đã lưu dữ liệu thuyên chuyển!'); this.closest('.modal-overlay').remove()">Lưu Thuyên chuyển</button>
        </div>
      </div>
    `;
    document.body.appendChild(overlay);
  },

  switchTab(el, tabId) {
    const tabs = el.parentElement.children;
    for (let t of tabs) {
      if (t.innerText !== 'Thuyên chuyển') {
        t.style.borderBottom = 'none';
        t.style.color = 'var(--text-secondary)';
        t.style.fontWeight = 'normal';
      }
    }
    el.style.borderBottom = '2px solid #1976D2';
    el.style.color = '#1976D2';
    el.style.fontWeight = '600';

    const bodies = el.parentElement.parentElement.querySelectorAll('.tab-body');
    for (let b of bodies) {
      b.style.display = 'none';
    }
    document.getElementById(tabId).style.display = 'block';
  }
};

