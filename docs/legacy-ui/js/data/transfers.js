// ============================================
// VINAMILK HRIS - Transfer Data
// Department transfer records
// ============================================

const TransfersData = [
  {
    id: 'TR-001',
    empId: 'VNM-0015',
    empName: 'Hoàng Thị Mai Anh',
    fromDept: 'cn_nam',
    toDept: 'cn_bac',
    fromPosition: 'Nhân viên Bán hàng',
    toPosition: 'Giám sát bán hàng',
    effectiveDate: '2016-05-10',
    reason: 'Thăng chức và luân chuyển theo yêu cầu kinh doanh',
    status: 'Hoàn thành',
    approvedBy: 'Vũ Đình Khoa',
    createdDate: '2016-04-20',
    notes: 'Đạt KPI xuất sắc 3 quý liên tiếp'
  },
  {
    id: 'TR-002',
    empId: 'VNM-0006',
    empName: 'Hoàng Anh Tuấn',
    fromDept: 'nm_sg',
    toDept: 'nm_bd',
    fromPosition: 'Công nhân Vận hành',
    toPosition: 'Trưởng ca Sản xuất',
    effectiveDate: '2012-05-15',
    reason: 'Điều chuyển theo nhu cầu sản xuất, thăng chức',
    status: 'Hoàn thành',
    approvedBy: 'Nguyễn Văn Hùng',
    createdDate: '2012-04-28',
    notes: 'Có kinh nghiệm quản lý tốt'
  },
  {
    id: 'TR-003',
    empId: 'VNM-0022',
    empName: 'Trần Hoàng Long',
    fromDept: 'pb_taichinh',
    toDept: 'pb_nhansu',
    fromPosition: 'Kế toán viên',
    toPosition: 'Chuyên viên C&B',
    effectiveDate: '2016-03-15',
    reason: 'Chuyển đổi vị trí phù hợp chuyên môn C&B',
    status: 'Hoàn thành',
    approvedBy: 'Nguyễn Thị Thanh Huyền',
    createdDate: '2016-02-25',
    notes: 'Có chứng chỉ C&B Management'
  },
  {
    id: 'TR-004',
    empId: 'VNM-0016',
    empName: 'Phan Thanh Tùng',
    fromDept: 'cn_trung',
    toDept: 'cn_nam',
    fromPosition: 'Nhân viên Bán hàng',
    toPosition: 'Nhân viên Bán hàng',
    effectiveDate: '2022-01-01',
    reason: 'Luân chuyển theo nguyện vọng cá nhân, gần gia đình',
    status: 'Hoàn thành',
    approvedBy: 'Vũ Đình Khoa',
    createdDate: '2021-12-10',
    notes: ''
  },
  {
    id: 'TR-005',
    empId: 'VNM-0042',
    empName: 'Tạ Quốc Huy',
    fromDept: 'nm_sg',
    toDept: 'nm_dn',
    fromPosition: 'Kỹ sư Cơ điện',
    toPosition: 'Kỹ sư Cơ điện',
    effectiveDate: '2020-07-01',
    reason: 'Hỗ trợ vận hành nhà máy mới Đà Nẵng',
    status: 'Hoàn thành',
    approvedBy: 'Nguyễn Văn Hùng',
    createdDate: '2020-06-15',
    notes: 'Có phụ cấp đi xa'
  },
  {
    id: 'TR-006',
    empId: 'VNM-0003',
    empName: 'Lê Thị Hương',
    fromDept: 'nm_ts',
    toDept: 'nm_bd',
    fromPosition: 'Tổ trưởng Đóng gói',
    toPosition: 'Tổ trưởng Đóng gói',
    effectiveDate: '2026-10-01',
    reason: 'Luân chuyển định kỳ giữa các nhà máy',
    status: 'Chờ duyệt',
    approvedBy: '',
    createdDate: '2026-09-05',
    notes: 'Đang chờ phê duyệt từ GĐ Nhà máy'
  },
  {
    id: 'TR-007',
    empId: 'VNM-0031',
    empName: 'Trần Đức Hoàng',
    fromDept: 'pb_it',
    toDept: 'pb_rd',
    fromPosition: 'Lập trình viên',
    toPosition: 'Lập trình viên R&D',
    effectiveDate: '2026-11-01',
    reason: 'Chuyển sang phát triển phần mềm R&D nội bộ',
    status: 'Đã duyệt',
    approvedBy: 'Nguyễn Minh Phúc',
    createdDate: '2026-09-01',
    notes: 'Có kỹ năng AI/ML phù hợp'
  },
  {
    id: 'TR-008',
    empId: 'VNM-0050',
    empName: 'Lê Thị Ngọc Diệp',
    fromDept: 'pb_marketing',
    toDept: 'cn_tmdt',
    fromPosition: 'Nhân viên Truyền thông',
    toPosition: 'Chuyên viên Content E-commerce',
    effectiveDate: '2026-12-01',
    reason: 'Phát triển kênh thương mại điện tử',
    status: 'Chờ duyệt',
    approvedBy: '',
    createdDate: '2026-09-08',
    notes: 'Có kinh nghiệm Content Marketing'
  }
];

const TransfersHelper = {
  getAll() { return TransfersData; },
  getByEmployee(empId) { return TransfersData.filter(t => t.empId === empId); },
  getByStatus(status) { return TransfersData.filter(t => t.status === status); },
  getPending() { return TransfersData.filter(t => t.status === 'Chờ duyệt'); },
  getStats() {
    return {
      total: TransfersData.length,
      pending: TransfersData.filter(t => t.status === 'Chờ duyệt').length,
      approved: TransfersData.filter(t => t.status === 'Đã duyệt').length,
      completed: TransfersData.filter(t => t.status === 'Hoàn thành').length
    };
  }
};
