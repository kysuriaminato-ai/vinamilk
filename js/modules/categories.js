// ============================================
// VINAMILK HRIS - Categories Module
// Business categories management (Danh mục nghiệp vụ)
// ============================================

const CategoriesModule = {
  currentCategory: null,
  searchQuery: '',
  currentPage: 1,
  perPage: 15,

  render(container, params = {}) {
    if (params.id) {
      this.renderCategoryDetail(container, params.id);
    } else {
      this.renderCategoryGrid(container);
    }
  },

  // =============================================
  // MAIN GRID VIEW — All categories by group
  // =============================================
  renderCategoryGrid(container) {
    const allCategories = CategoriesData.getAll();
    const systemCats = allCategories.filter(c => c.group === 'system');
    const profCats = allCategories.filter(c => c.group === 'professional');

    const totalItems = allCategories.reduce((sum, c) => sum + c.items.length, 0);

    container.innerHTML = `
      <div class="page-header">
        <div class="page-header-left">
          <h1 class="page-title">Danh mục Nghiệp vụ</h1>
          <p class="page-subtitle">Hệ thống ${allCategories.length} danh mục — ${Helpers.formatNumber(totalItems)} mục dữ liệu</p>
        </div>
        <div class="page-header-actions">
          <button class="btn btn-secondary" onclick="CategoriesModule.resetData()">
            <span>🔄</span> Khôi phục mặc định
          </button>
        </div>
      </div>

      <!-- Stats -->
      <div class="content-grid grid-cols-4" style="margin-bottom:var(--space-5)">
        <div class="stat-card stat-blue animate-fade-in-up stagger-1">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${allCategories.length}</div>
          <div class="stat-card-label">Tổng danh mục</div>
        </div>
        <div class="stat-card stat-green animate-fade-in-up stagger-2">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${systemCats.length}</div>
          <div class="stat-card-label">DM Hệ thống</div>
        </div>
        <div class="stat-card stat-purple animate-fade-in-up stagger-3">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${profCats.length}</div>
          <div class="stat-card-label">DM Chuyên môn</div>
        </div>
        <div class="stat-card stat-orange animate-fade-in-up stagger-4">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${Helpers.formatNumber(totalItems)}</div>
          <div class="stat-card-label">Tổng mục dữ liệu</div>
        </div>
      </div>

      <!-- Group 1: System Categories -->
      <div class="card animate-fade-in-up" style="margin-bottom:var(--space-5)">
        <div class="card-header">
          <div class="card-header-title">
            <span style="margin-right:var(--space-2)">🏛️</span>
            Hệ thống các Danh mục
          </div>
          <span style="font-size:var(--font-size-sm);color:var(--text-secondary)">${systemCats.length} danh mục</span>
        </div>
        <div class="card-body">
          <div class="cat-grid">
            ${systemCats.map((cat, i) => this._categoryCard(cat, i)).join('')}
          </div>
        </div>
      </div>

      <!-- Group 2: Professional Categories -->
      <div class="card animate-fade-in-up" style="margin-bottom:var(--space-5)">
        <div class="card-header">
          <div class="card-header-title">
            <span style="margin-right:var(--space-2)">🎓</span>
            Danh mục Chuyên môn
          </div>
          <span style="font-size:var(--font-size-sm);color:var(--text-secondary)">${profCats.length} danh mục</span>
        </div>
        <div class="card-body">
          <div class="cat-grid">
            ${profCats.map((cat, i) => this._categoryCard(cat, i)).join('')}
          </div>
        </div>
      </div>
    `;
  },

  _categoryCard(cat, index) {
    const activeCount = cat.items.filter(i => i.active).length;
    const totalCount = cat.items.length;
    return `
      <div class="cat-card animate-fade-in-up stagger-${(index % 4) + 1}" 
           onclick="window.location.hash='/categories?id=${cat.id}'" 
           title="${cat.description}">
        <div class="cat-card-icon">${cat.icon}</div>
        <div class="cat-card-content">
          <div class="cat-card-name">${cat.name}</div>
          <div class="cat-card-count">
            <span class="cat-card-count-number">${totalCount}</span> mục
            ${totalCount !== activeCount ? `<span class="cat-card-count-inactive">(${totalCount - activeCount} ẩn)</span>` : ''}
          </div>
        </div>
        <div class="cat-card-arrow">›</div>
      </div>
    `;
  },

  // =============================================
  // DETAIL VIEW — Single category items
  // =============================================
  renderCategoryDetail(container, catId) {
    const cat = CategoriesData.getCategory(catId);
    if (!cat) {
      container.innerHTML = '<div class="empty-state"><div class="empty-state-icon">🔍</div><div class="empty-state-title">Không tìm thấy danh mục</div></div>';
      return;
    }

    this.currentCategory = cat;
    let items = [...cat.items];

    // Search filter
    if (this.searchQuery) {
      const q = this.searchQuery.toLowerCase();
      items = items.filter(item =>
        item.name.toLowerCase().includes(q) ||
        item.code.toLowerCase().includes(q) ||
        (item.description || '').toLowerCase().includes(q)
      );
    }

    const paged = Helpers.paginate(items, this.currentPage, this.perPage);
    const activeCount = cat.items.filter(i => i.active).length;
    const inactiveCount = cat.items.length - activeCount;

    container.innerHTML = `
      <div class="page-header">
        <div class="page-header-left">
          <button class="btn btn-ghost" onclick="window.location.hash='/categories'" style="margin-bottom:var(--space-2)">← Quay lại danh mục</button>
          <h1 class="page-title">${cat.icon} ${cat.name}</h1>
          <p class="page-subtitle">${cat.description} — Mã: <code>${cat.code}</code></p>
        </div>
        <div class="page-header-actions">
          <button class="btn btn-primary" onclick="CategoriesModule.showAddItemModal('${cat.id}')">
            <span>➕</span> Thêm mục mới
          </button>
        </div>
      </div>

      <!-- Quick stats -->
      <div class="content-grid grid-cols-3" style="margin-bottom:var(--space-5)">
        <div class="stat-card stat-blue animate-fade-in-up stagger-1">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${cat.items.length}</div>
          <div class="stat-card-label">Tổng số mục</div>
        </div>
        <div class="stat-card stat-green animate-fade-in-up stagger-2">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${activeCount}</div>
          <div class="stat-card-label">Đang hoạt động</div>
        </div>
        <div class="stat-card stat-red animate-fade-in-up stagger-3">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${inactiveCount}</div>
          <div class="stat-card-label">Đã ẩn</div>
        </div>
      </div>

      <!-- Data Table -->
      <div class="data-table-wrapper animate-fade-in-up">
        <div class="table-toolbar">
          <div class="table-toolbar-left">
            <div class="table-search">
              <span>🔍</span>
              <input type="text" placeholder="Tìm theo tên, mã..." 
                     value="${this.searchQuery}" 
                     oninput="CategoriesModule.onSearch(this.value)" id="cat-search">
            </div>
          </div>
          <div class="table-toolbar-right">
            <span style="font-size:var(--font-size-sm);color:var(--text-secondary)">${paged.total} kết quả</span>
          </div>
        </div>

        <table class="data-table">
          <thead>
            <tr>
              <th style="width:80px">STT</th>
              <th style="width:120px">Mã</th>
              <th>Tên</th>
              <th>Mô tả</th>
              <th style="width:120px">Trạng thái</th>
              <th style="width:160px">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            ${paged.items.length > 0 ? paged.items.map((item, idx) => `
              <tr class="${!item.active ? 'row-inactive' : ''}">
                <td style="text-align:center;color:var(--text-tertiary)">${(paged.page - 1) * paged.perPage + idx + 1}</td>
                <td><code style="font-size:var(--font-size-sm);color:var(--primary)">${item.code}</code></td>
                <td style="font-weight:var(--font-weight-semibold)">${item.name}</td>
                <td style="font-size:var(--font-size-sm);color:var(--text-secondary)">${item.description || '—'}</td>
                <td>
                  ${item.active 
                    ? '<span class="badge badge-active"><span class="badge-dot"></span>Hoạt động</span>' 
                    : '<span class="badge badge-retired"><span class="badge-dot"></span>Đã ẩn</span>'}
                </td>
                <td>
                  <div style="display:flex;gap:var(--space-1)">
                    <button class="btn btn-ghost btn-sm" onclick="CategoriesModule.showEditItemModal('${cat.id}', '${item.id}')" title="Sửa">✏️</button>
                    <button class="btn btn-ghost btn-sm" onclick="CategoriesModule.toggleItem('${cat.id}', '${item.id}')" title="${item.active ? 'Ẩn' : 'Hiện'}">
                      ${item.active ? '🔒' : '🔓'}
                    </button>
                    <button class="btn btn-ghost btn-sm" onclick="CategoriesModule.confirmDelete('${cat.id}', '${item.id}', '${item.name.replace(/'/g, "\\'")}')" title="Xóa" style="color:var(--accent-red)">🗑️</button>
                  </div>
                </td>
              </tr>
            `).join('') : `
              <tr>
                <td colspan="6" style="text-align:center;padding:var(--space-8);color:var(--text-tertiary)">
                  <div style="font-size:2rem;margin-bottom:var(--space-2)">📭</div>
                  Không tìm thấy mục nào ${this.searchQuery ? 'phù hợp' : 'trong danh mục'}
                </td>
              </tr>
            `}
          </tbody>
        </table>
        ${paged.total > paged.perPage ? Helpers.paginationHTML(paged.total, paged.page, paged.perPage) : ''}
      </div>
    `;

    // Bind pagination
    container.querySelectorAll('.table-pagination-btn[data-page]').forEach(btn => {
      btn.addEventListener('click', () => {
        const page = parseInt(btn.dataset.page);
        if (page >= 1 && page <= paged.totalPages) {
          this.currentPage = page;
          this.renderCategoryDetail(container, catId);
        }
      });
    });
  },

  // =============================================
  // SEARCH
  // =============================================
  onSearch: Helpers.debounce(function(query) {
    CategoriesModule.searchQuery = query;
    CategoriesModule.currentPage = 1;
    if (CategoriesModule.currentCategory) {
      CategoriesModule.renderCategoryDetail(
        document.getElementById('main-content'),
        CategoriesModule.currentCategory.id
      );
    }
  }, 300),

  // =============================================
  // CRUD OPERATIONS
  // =============================================
  showAddItemModal(catId) {
    const cat = CategoriesData.getCategory(catId);
    if (!cat) return;

    // Generate next code
    const lastCode = cat.items.length > 0 
      ? cat.items[cat.items.length - 1].code 
      : cat.code + '00';
    const prefix = lastCode.replace(/\d+$/, '');
    const lastNum = parseInt(lastCode.replace(/\D+/g, '') || '0');
    const nextCode = prefix + String(lastNum + 1).padStart(2, '0');

    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
    overlay.innerHTML = `
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title">➕ Thêm mục mới — ${cat.name}</div>
          <button class="modal-close" onclick="this.closest('.modal-overlay').remove()">✕</button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label class="form-label">Mã <span class="required">*</span></label>
            <input class="form-input" id="add-item-code" placeholder="${nextCode}" value="${nextCode}">
          </div>
          <div class="form-group">
            <label class="form-label">Tên <span class="required">*</span></label>
            <input class="form-input" id="add-item-name" placeholder="Nhập tên mục">
          </div>
          <div class="form-group" style="margin:0">
            <label class="form-label">Mô tả</label>
            <textarea class="form-input" id="add-item-desc" rows="3" placeholder="Nhập mô tả (tùy chọn)"></textarea>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" onclick="this.closest('.modal-overlay').remove()">Hủy</button>
          <button class="btn btn-primary" onclick="CategoriesModule.addItem('${catId}')">💾 Lưu</button>
        </div>
      </div>
    `;
    document.body.appendChild(overlay);

    // Focus on name field
    setTimeout(() => document.getElementById('add-item-name')?.focus(), 100);
  },

  addItem(catId) {
    const code = document.getElementById('add-item-code')?.value.trim();
    const name = document.getElementById('add-item-name')?.value.trim();
    const desc = document.getElementById('add-item-desc')?.value.trim();

    if (!code || !name) {
      alert('Vui lòng nhập Mã và Tên!');
      return;
    }

    CategoriesData.addItem(catId, { code, name, description: desc || '' });
    document.querySelector('.modal-overlay')?.remove();
    this._showToast('✅ Đã thêm mục mới thành công!');
    this.renderCategoryDetail(document.getElementById('main-content'), catId);
  },

  showEditItemModal(catId, itemId) {
    const cat = CategoriesData.getCategory(catId);
    if (!cat) return;
    const item = cat.items.find(i => i.id === itemId);
    if (!item) return;

    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
    overlay.innerHTML = `
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title">✏️ Chỉnh sửa — ${item.name}</div>
          <button class="modal-close" onclick="this.closest('.modal-overlay').remove()">✕</button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label class="form-label">Mã <span class="required">*</span></label>
            <input class="form-input" id="edit-item-code" value="${item.code}">
          </div>
          <div class="form-group">
            <label class="form-label">Tên <span class="required">*</span></label>
            <input class="form-input" id="edit-item-name" value="${item.name}">
          </div>
          <div class="form-group" style="margin:0">
            <label class="form-label">Mô tả</label>
            <textarea class="form-input" id="edit-item-desc" rows="3">${item.description || ''}</textarea>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" onclick="this.closest('.modal-overlay').remove()">Hủy</button>
          <button class="btn btn-primary" onclick="CategoriesModule.updateItem('${catId}', '${itemId}')">💾 Cập nhật</button>
        </div>
      </div>
    `;
    document.body.appendChild(overlay);
  },

  updateItem(catId, itemId) {
    const code = document.getElementById('edit-item-code')?.value.trim();
    const name = document.getElementById('edit-item-name')?.value.trim();
    const desc = document.getElementById('edit-item-desc')?.value.trim();

    if (!code || !name) {
      alert('Vui lòng nhập Mã và Tên!');
      return;
    }

    CategoriesData.updateItem(catId, itemId, { code, name, description: desc || '' });
    document.querySelector('.modal-overlay')?.remove();
    this._showToast('✅ Đã cập nhật thành công!');
    this.renderCategoryDetail(document.getElementById('main-content'), catId);
  },

  toggleItem(catId, itemId) {
    CategoriesData.toggleItem(catId, itemId);
    this.renderCategoryDetail(document.getElementById('main-content'), catId);
  },

  confirmDelete(catId, itemId, itemName) {
    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
    overlay.innerHTML = `
      <div class="modal" style="max-width:440px">
        <div class="modal-header">
          <div class="modal-title">⚠️ Xác nhận xóa</div>
          <button class="modal-close" onclick="this.closest('.modal-overlay').remove()">✕</button>
        </div>
        <div class="modal-body" style="text-align:center;padding:var(--space-6)">
          <div style="font-size:3rem;margin-bottom:var(--space-3)">🗑️</div>
          <div style="font-size:var(--font-size-md);font-weight:var(--font-weight-semibold);margin-bottom:var(--space-2)">
            Bạn có chắc chắn muốn xóa?
          </div>
          <div style="color:var(--text-secondary);font-size:var(--font-size-sm)">
            Mục "<strong>${itemName}</strong>" sẽ bị xóa vĩnh viễn và không thể khôi phục.
          </div>
        </div>
        <div class="modal-footer" style="justify-content:center">
          <button class="btn btn-secondary" onclick="this.closest('.modal-overlay').remove()">Hủy</button>
          <button class="btn btn-primary" style="background:var(--accent-red)" onclick="CategoriesModule.deleteItem('${catId}', '${itemId}')">🗑️ Xóa</button>
        </div>
      </div>
    `;
    document.body.appendChild(overlay);
  },

  deleteItem(catId, itemId) {
    CategoriesData.deleteItem(catId, itemId);
    document.querySelector('.modal-overlay')?.remove();
    this._showToast('🗑️ Đã xóa mục thành công!');
    this.renderCategoryDetail(document.getElementById('main-content'), catId);
  },

  resetData() {
    if (confirm('Bạn có chắc muốn khôi phục tất cả danh mục về mặc định? Mọi thay đổi sẽ bị mất.')) {
      CategoriesData.resetToDefaults();
      this._showToast('🔄 Đã khôi phục dữ liệu mặc định!');
      this.renderCategoryGrid(document.getElementById('main-content'));
    }
  },

  // =============================================
  // TOAST HELPER
  // =============================================
  _showToast(message) {
    const container = document.getElementById('toast-container');
    if (!container) return;
    const toast = document.createElement('div');
    toast.className = 'toast toast-success';
    toast.innerHTML = `<span>${message}</span>`;
    toast.style.cssText = 'background:var(--bg-card);border:1px solid var(--border-light);padding:12px 20px;border-radius:var(--radius-lg);box-shadow:var(--shadow-lg);font-size:var(--font-size-sm);font-weight:var(--font-weight-medium);animation:fadeInUp 0.3s ease;';
    container.appendChild(toast);
    setTimeout(() => {
      toast.style.opacity = '0';
      toast.style.transform = 'translateY(-10px)';
      toast.style.transition = 'all 0.3s ease';
      setTimeout(() => toast.remove(), 300);
    }, 3000);
  }
};
