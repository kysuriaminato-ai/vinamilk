// ============================================
// VINAMILK HRIS - Departments Data
// Organizational structure
// ============================================

const DepartmentsData = {
  orgStructure: {
    id: 'vinamilk',
    name: 'CÔNG TY CP SỮA VIỆT NAM',
    shortName: 'VINAMILK',
    type: 'Tổng công ty',
    headcount: 10156,
    icon: '',
    children: [
      {
        id: 'hdqt',
        name: 'Hội đồng Quản trị',
        type: 'Quản trị',
        headcount: 9,
        icon: '',
        children: []
      },
      {
        id: 'ban_tgd',
        name: 'Ban Tổng Giám đốc',
        type: 'Điều hành',
        headcount: 5,
        icon: '',
        children: [
          {
            id: 'khoi_sx',
            name: 'Khối Sản xuất',
            type: 'Khối',
            headcount: 5420,
            icon: '',
            cssClass: 'factory',
            children: [
              { id: 'nm_ts', name: 'Nhà máy Sữa Tiên Sơn', type: 'Nhà máy', headcount: 820, icon: '', cssClass: 'factory' },
              { id: 'nm_na', name: 'Nhà máy Sữa Nghệ An', type: 'Nhà máy', headcount: 650, icon: '', cssClass: 'factory' },
              { id: 'nm_bd', name: 'Nhà máy Sữa Bình Dương', type: 'Nhà máy', headcount: 780, icon: '', cssClass: 'factory' },
              { id: 'nm_dn', name: 'Nhà máy Sữa Đà Nẵng', type: 'Nhà máy', headcount: 520, icon: '', cssClass: 'factory' },
              { id: 'nm_sg', name: 'Nhà máy Sữa Sài Gòn', type: 'Nhà máy', headcount: 900, icon: '', cssClass: 'factory' },
              { id: 'nm_tl', name: 'Nhà máy Sữa Thống Nhất', type: 'Nhà máy', headcount: 680, icon: '', cssClass: 'factory' },
              { id: 'nm_ct', name: 'Nhà máy Sữa Cần Thơ', type: 'Nhà máy', headcount: 540, icon: '', cssClass: 'factory' },
              { id: 'nm_dl', name: 'Nhà máy Sữa Đà Lạt', type: 'Nhà máy', headcount: 530, icon: '', cssClass: 'factory' }
            ]
          },
          {
            id: 'khoi_tt',
            name: 'Khối Trang trại',
            type: 'Khối',
            headcount: 1850,
            icon: '',
            cssClass: 'farm',
            children: [
              { id: 'tt_green1', name: 'Trang trại Green Farm Tây Ninh', type: 'Trang trại', headcount: 320, icon: '', cssClass: 'farm' },
              { id: 'tt_green2', name: 'Trang trại Green Farm Hà Tĩnh', type: 'Trang trại', headcount: 280, icon: '', cssClass: 'farm' },
              { id: 'tt_organic1', name: 'Trang trại Organic Đà Lạt', type: 'Trang trại', headcount: 350, icon: '', cssClass: 'farm' },
              { id: 'tt_organic2', name: 'Trang trại Organic Lâm Đồng', type: 'Trang trại', headcount: 300, icon: '', cssClass: 'farm' },
              { id: 'tt_reseda', name: 'Trang trại Reseda Thanh Hóa', type: 'Trang trại', headcount: 280, icon: '', cssClass: 'farm' },
              { id: 'tt_tq', name: 'Trang trại Tuyên Quang', type: 'Trang trại', headcount: 320, icon: '', cssClass: 'farm' }
            ]
          },
          {
            id: 'khoi_kd',
            name: 'Khối Kinh doanh',
            type: 'Khối',
            headcount: 1980,
            icon: '',
            cssClass: 'sales',
            children: [
              { id: 'cn_bac', name: 'Chi nhánh miền Bắc', type: 'Chi nhánh', headcount: 580, icon: '', cssClass: 'sales' },
              { id: 'cn_trung', name: 'Chi nhánh miền Trung', type: 'Chi nhánh', headcount: 420, icon: '', cssClass: 'sales' },
              { id: 'cn_nam', name: 'Chi nhánh miền Nam', type: 'Chi nhánh', headcount: 620, icon: '', cssClass: 'sales' },
              { id: 'cn_xuatkhau', name: 'Phòng Xuất khẩu', type: 'Phòng', headcount: 180, icon: '', cssClass: 'sales' },
              { id: 'cn_tmdt', name: 'Phòng Thương mại điện tử', type: 'Phòng', headcount: 180, icon: '', cssClass: 'sales' }
            ]
          },
          {
            id: 'khoi_vp',
            name: 'Khối Văn phòng',
            type: 'Khối',
            headcount: 906,
            icon: '️',
            cssClass: 'office',
            children: [
              { id: 'pb_nhansu', name: 'Phòng Nhân sự', type: 'Phòng', headcount: 85, icon: '', cssClass: 'office' },
              { id: 'pb_taichinh', name: 'Phòng Tài chính - Kế toán', type: 'Phòng', headcount: 120, icon: '', cssClass: 'office' },
              { id: 'pb_marketing', name: 'Phòng Marketing', type: 'Phòng', headcount: 95, icon: '', cssClass: 'office' },
              { id: 'pb_rd', name: 'Trung tâm R&D', type: 'Trung tâm', headcount: 180, icon: '', cssClass: 'office' },
              { id: 'pb_it', name: 'Phòng Công nghệ thông tin', type: 'Phòng', headcount: 110, icon: '', cssClass: 'office' },
              { id: 'pb_phapluat', name: 'Phòng Pháp luật', type: 'Phòng', headcount: 45, icon: '⚖️', cssClass: 'office' },
              { id: 'pb_hanhchinh', name: 'Phòng Hành chính', type: 'Phòng', headcount: 75, icon: '', cssClass: 'office' },
              { id: 'pb_qlcl', name: 'Phòng Quản lý Chất lượng', type: 'Phòng', headcount: 96, icon: '', cssClass: 'office' }
            ]
          }
        ]
      }
    ]
  },

  // Flat department list for dropdowns
  getDepartmentList() {
    const list = [];
    const traverse = (node, prefix = '') => {
      list.push({ id: node.id, name: prefix + node.name, type: node.type, headcount: node.headcount });
      if (node.children) node.children.forEach(c => traverse(c, '  '));
    };
    this.orgStructure.children.forEach(c => {
      if (c.children) c.children.forEach(cc => traverse(cc));
      else traverse(c);
    });
    return list;
  },

  // Get department by ID
  getDepartment(id) {
    const find = (node) => {
      if (node.id === id) return node;
      if (node.children) {
        for (const child of node.children) {
          const result = find(child);
          if (result) return result;
        }
      }
      return null;
    };
    return find(this.orgStructure);
  }
};


