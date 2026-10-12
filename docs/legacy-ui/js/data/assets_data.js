// ============================================
// VINAMILK HRIS - Assets Data
// Employee asset assignment records
// ============================================

const AssetsData = {
  categories: [
    { id: 'laptop', name: 'Laptop/Máy tính', icon: '' },
    { id: 'phone', name: 'Điện thoại', icon: '' },
    { id: 'card', name: 'Thẻ nhân viên', icon: '' },
    { id: 'uniform', name: 'Đồng phục', icon: '' },
    { id: 'key', name: 'Chìa khóa/Thẻ từ', icon: '' },
    { id: 'vehicle', name: 'Phương tiện', icon: '' },
    { id: 'tools', name: 'Dụng cụ/Thiết bị', icon: '' },
    { id: 'other', name: 'Khác', icon: '' }
  ],

  items: [
    { id: 'AST-001', name: 'MacBook Pro 14" M3', category: 'laptop', serial: 'MBP-2025-0042', empId: 'VNM-0030', empName: 'Nguyễn Minh Phúc', assignDate: '2025-03-01', status: 'Đang sử dụng', value: 52000000, condition: 'Tốt' },
    { id: 'AST-002', name: 'Dell Latitude 5540', category: 'laptop', serial: 'DL5-2024-0118', empId: 'VNM-0031', empName: 'Trần Đức Hoàng', assignDate: '2024-06-15', status: 'Đang sử dụng', value: 28000000, condition: 'Tốt' },
    { id: 'AST-003', name: 'iPhone 15 Pro', category: 'phone', serial: 'IP15-2024-0023', empId: 'VNM-0020', empName: 'Nguyễn Thị Thanh Huyền', assignDate: '2024-01-10', status: 'Đang sử dụng', value: 32000000, condition: 'Tốt' },
    { id: 'AST-004', name: 'Dell Latitude 5540', category: 'laptop', serial: 'DL5-2024-0119', empId: 'VNM-0049', empName: 'Trịnh Minh Đức', assignDate: '2024-06-15', status: 'Đang sử dụng', value: 28000000, condition: 'Tốt' },
    { id: 'AST-005', name: 'Thẻ nhân viên RFID', category: 'card', serial: 'CARD-VNM-0001', empId: 'VNM-0001', empName: 'Nguyễn Văn Hùng', assignDate: '2020-01-01', status: 'Đang sử dụng', value: 50000, condition: 'Tốt' },
    { id: 'AST-006', name: 'Đồng phục Quản lý (Bộ 3)', category: 'uniform', serial: 'UNI-MGR-0014', empId: 'VNM-0014', empName: 'Vũ Đình Khoa', assignDate: '2025-06-01', status: 'Đang sử dụng', value: 2500000, condition: 'Tốt' },
    { id: 'AST-007', name: 'Toyota Innova 2023', category: 'vehicle', serial: 'VEH-2023-003', empId: 'VNM-0018', empName: 'Lý Quang Vinh', assignDate: '2023-09-01', status: 'Đang sử dụng', value: 850000000, condition: 'Tốt' },
    { id: 'AST-008', name: 'Bộ dụng cụ Thú y', category: 'tools', serial: 'VET-KIT-005', empId: 'VNM-0010', empName: 'Trần Thị Ngọc', assignDate: '2024-01-15', status: 'Đang sử dụng', value: 15000000, condition: 'Tốt' },
    { id: 'AST-009', name: 'HP ProBook 450 G10', category: 'laptop', serial: 'HP450-2024-0035', empId: 'VNM-0022', empName: 'Trần Hoàng Long', assignDate: '2024-03-01', status: 'Đang sử dụng', value: 22000000, condition: 'Tốt' },
    { id: 'AST-010', name: 'Samsung Galaxy A54', category: 'phone', serial: 'SGA54-2024-0012', empId: 'VNM-0015', empName: 'Hoàng Thị Mai Anh', assignDate: '2024-02-01', status: 'Đang sử dụng', value: 9000000, condition: 'Tốt' },
    { id: 'AST-011', name: 'Thẻ từ phòng Server', category: 'key', serial: 'KEY-SVR-003', empId: 'VNM-0049', empName: 'Trịnh Minh Đức', assignDate: '2023-11-01', status: 'Đang sử dụng', value: 200000, condition: 'Tốt' },
    { id: 'AST-012', name: 'Đồng phục Công nhân (Bộ 5)', category: 'uniform', serial: 'UNI-WRK-0048', empId: 'VNM-0048', empName: 'Nguyễn Tiến Dũng', assignDate: '2025-01-01', status: 'Đang sử dụng', value: 1500000, condition: 'Khá' },
    { id: 'AST-013', name: 'Dell Latitude 3540', category: 'laptop', serial: 'DL3-2023-0067', empId: 'VNM-0037', empName: 'Đoàn Thị Mỹ Linh', assignDate: '2023-03-01', status: 'Đã thu hồi', value: 18000000, condition: 'Tốt' },
    { id: 'AST-014', name: 'Bộ thiết bị đo lường QC', category: 'tools', serial: 'QC-MEAS-008', empId: 'VNM-0052', empName: 'Phạm Thị Thanh Tâm', assignDate: '2023-05-01', status: 'Đang sử dụng', value: 35000000, condition: 'Tốt' },
    { id: 'AST-015', name: 'HP EliteBook 840 G9', category: 'laptop', serial: 'HPE840-2024-0009', empId: 'VNM-0026', empName: 'Ngô Quang Hải', assignDate: '2024-04-01', status: 'Đang sử dụng', value: 32000000, condition: 'Tốt' }
  ],

  getByEmployee(empId) {
    return this.items.filter(a => a.empId === empId);
  },

  getByCategory(category) {
    return this.items.filter(a => a.category === category);
  },

  getByStatus(status) {
    return this.items.filter(a => a.status === status);
  },

  getStats() {
    const active = this.items.filter(a => a.status === 'Đang sử dụng');
    return {
      totalItems: this.items.length,
      inUse: active.length,
      returned: this.items.filter(a => a.status === 'Đã thu hồi').length,
      totalValue: active.reduce((s, a) => s + a.value, 0),
      byCategory: this.categories.map(c => ({
        ...c,
        count: active.filter(a => a.category === c.id).length,
        value: active.filter(a => a.category === c.id).reduce((s, a) => s + a.value, 0)
      })).filter(c => c.count > 0)
    };
  }
};

