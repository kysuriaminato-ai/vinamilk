// ============================================
// VINAMILK HRIS - Helpers
// Format, date, currency utilities
// ============================================

const Helpers = {
  // Format currency in VND
  formatCurrency(amount) {
    if (amount === null || amount === undefined) return '—';
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
      maximumFractionDigits: 0
    }).format(amount);
  },

  // Format number with thousand separator
  formatNumber(num) {
    if (num === null || num === undefined) return '—';
    return new Intl.NumberFormat('vi-VN').format(num);
  },

  // Format percentage
  formatPercent(value, decimals = 1) {
    if (value === null || value === undefined) return '—';
    return value.toFixed(decimals) + '%';
  },

  // Format date
  formatDate(dateStr) {
    if (!dateStr) return '—';
    const date = new Date(dateStr);
    return date.toLocaleDateString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  },

  // Format date with month name
  formatDateLong(dateStr) {
    if (!dateStr) return '—';
    const date = new Date(dateStr);
    return date.toLocaleDateString('vi-VN', {
      day: '2-digit',
      month: 'long',
      year: 'numeric'
    });
  },

  // Get relative time
  timeAgo(dateStr) {
    const date = new Date(dateStr);
    const now = new Date();
    const diff = now - date;
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(diff / 3600000);
    const days = Math.floor(diff / 86400000);

    if (minutes < 1) return 'Vừa xong';
    if (minutes < 60) return `${minutes} phút trước`;
    if (hours < 24) return `${hours} giờ trước`;
    if (days < 30) return `${days} ngày trước`;
    return this.formatDate(dateStr);
  },

  // Days between two dates
  daysBetween(date1, date2) {
    const d1 = new Date(date1);
    const d2 = new Date(date2);
    return Math.floor((d2 - d1) / 86400000);
  },

  // Get initials from name
  getInitials(name) {
    if (!name) return '??';
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return (parts[parts.length - 2][0] + parts[parts.length - 1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  },

  // Generate random avatar color based on name
  getAvatarColor(name) {
    const colors = [
      '#0052CC', '#00B8D9', '#36B37E', '#FFAB00',
      '#FF8B00', '#FF5630', '#6554C0', '#E91E8C',
      '#008DA6', '#006644', '#403294', '#BF2600'
    ];
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
      hash = name.charCodeAt(i) + ((hash << 5) - hash);
    }
    return colors[Math.abs(hash) % colors.length];
  },

  // Generate employee avatar HTML
  avatarHTML(name, size = 36) {
    const initials = this.getInitials(name);
    const color = this.getAvatarColor(name);
    return `<div class="employee-avatar" style="width:${size}px;height:${size}px;background:${color}">${initials}</div>`;
  },

  // Truncate text
  truncate(text, maxLen = 50) {
    if (!text || text.length <= maxLen) return text || '';
    return text.substring(0, maxLen) + '…';
  },

  // Generate unique ID
  uid() {
    return Date.now().toString(36) + Math.random().toString(36).substr(2, 5);
  },

  // Calculate age from birth date
  calcAge(birthDate) {
    const today = new Date();
    const birth = new Date(birthDate);
    let age = today.getFullYear() - birth.getFullYear();
    const m = today.getMonth() - birth.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < birth.getDate())) age--;
    return age;
  },

  // Calculate seniority in years
  calcSeniority(joinDate) {
    return this.calcAge(joinDate);
  },

  // Status badge HTML
  statusBadge(status) {
    const map = {
      'Đang làm việc': 'badge-active',
      'Thử việc': 'badge-trial',
      'Đã nghỉ việc': 'badge-resigned',
      'Nghỉ hưu': 'badge-retired',
      'Chờ xử lý': 'badge-pending',
      'Hoàn thành': 'badge-active',
      'Đang tuyển': 'badge-info',
      'Hết hạn': 'badge-danger'
    };
    const cls = map[status] || 'badge-info';
    return `<span class="badge ${cls}"><span class="badge-dot"></span>${status}</span>`;
  },

  // Debounce function
  debounce(func, wait = 300) {
    let timer;
    return function (...args) {
      clearTimeout(timer);
      timer = setTimeout(() => func.apply(this, args), wait);
    };
  },

  // Sort array by key
  sortBy(arr, key, dir = 'asc') {
    return [...arr].sort((a, b) => {
      const va = a[key], vb = b[key];
      if (typeof va === 'string') {
        return dir === 'asc' ? va.localeCompare(vb, 'vi') : vb.localeCompare(va, 'vi');
      }
      return dir === 'asc' ? va - vb : vb - va;
    });
  },

  // Search/filter
  searchFilter(items, query, fields) {
    if (!query) return items;
    const q = query.toLowerCase().trim();
    return items.filter(item =>
      fields.some(f => (item[f] || '').toString().toLowerCase().includes(q))
    );
  },

  // Paginate
  paginate(items, page = 1, perPage = 10) {
    const start = (page - 1) * perPage;
    return {
      items: items.slice(start, start + perPage),
      total: items.length,
      totalPages: Math.ceil(items.length / perPage),
      page,
      perPage
    };
  },

  // Render pagination HTML
  paginationHTML(total, page, perPage) {
    const totalPages = Math.ceil(total / perPage);
    const start = (page - 1) * perPage + 1;
    const end = Math.min(page * perPage, total);

    let btns = '';
    btns += `<button class="table-pagination-btn" data-page="${page-1}" ${page <= 1 ? 'disabled' : ''}>‹</button>`;
    
    const range = [];
    if (totalPages <= 7) {
      for (let i = 1; i <= totalPages; i++) range.push(i);
    } else {
      range.push(1);
      if (page > 3) range.push('...');
      for (let i = Math.max(2, page - 1); i <= Math.min(totalPages - 1, page + 1); i++) {
        range.push(i);
      }
      if (page < totalPages - 2) range.push('...');
      range.push(totalPages);
    }

    range.forEach(p => {
      if (p === '...') {
        btns += `<span class="table-pagination-btn" style="cursor:default">…</span>`;
      } else {
        btns += `<button class="table-pagination-btn ${p === page ? 'active' : ''}" data-page="${p}">${p}</button>`;
      }
    });

    btns += `<button class="table-pagination-btn" data-page="${page+1}" ${page >= totalPages ? 'disabled' : ''}>›</button>`;

    return `
      <div class="table-pagination">
        <span class="table-pagination-info">Hiển thị ${start}–${end} trong ${Helpers.formatNumber(total)} kết quả</span>
        <div class="table-pagination-controls">${btns}</div>
      </div>
    `;
  }
};
