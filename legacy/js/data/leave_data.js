// ============================================
// VINAMILK HRIS - Leave Data
// Leave management records
// ============================================

const LeaveData = {
  leaveTypes: [
    { id: 'annual', name: 'Nghỉ phép năm', days: 12, color: '#0052CC' },
    { id: 'sick', name: 'Nghỉ ốm', days: 30, color: '#FF5630' },
    { id: 'maternity', name: 'Nghỉ thai sản', days: 180, color: '#E91E8C' },
    { id: 'wedding', name: 'Nghỉ kết hôn', days: 3, color: '#FFAB00' },
    { id: 'bereavement', name: 'Nghỉ tang', days: 3, color: '#5E6C84' },
    { id: 'unpaid', name: 'Nghỉ không lương', days: 0, color: '#8993A4' },
    { id: 'compensatory', name: 'Nghỉ bù', days: 0, color: '#00B8D9' }
  ],

  balances: [
    { empId: 'VNM-0001', annual: 15, used: 5, sick: 30, sickUsed: 1 },
    { empId: 'VNM-0002', annual: 12, used: 3, sick: 30, sickUsed: 2 },
    { empId: 'VNM-0003', annual: 12, used: 7, sick: 30, sickUsed: 0 },
    { empId: 'VNM-0004', annual: 12, used: 4, sick: 30, sickUsed: 1 },
    { empId: 'VNM-0006', annual: 14, used: 6, sick: 30, sickUsed: 3 },
    { empId: 'VNM-0007', annual: 12, used: 2, sick: 30, sickUsed: 0 },
    { empId: 'VNM-0008', annual: 13, used: 5, sick: 30, sickUsed: 1 },
    { empId: 'VNM-0009', annual: 15, used: 8, sick: 30, sickUsed: 2 },
    { empId: 'VNM-0010', annual: 12, used: 3, sick: 30, sickUsed: 0 },
    { empId: 'VNM-0014', annual: 15, used: 4, sick: 30, sickUsed: 0 },
    { empId: 'VNM-0020', annual: 18, used: 10, sick: 30, sickUsed: 1 },
    { empId: 'VNM-0021', annual: 14, used: 6, sick: 30, sickUsed: 2 },
    { empId: 'VNM-0024', annual: 18, used: 7, sick: 30, sickUsed: 0 },
    { empId: 'VNM-0026', annual: 14, used: 5, sick: 30, sickUsed: 1 },
    { empId: 'VNM-0028', annual: 15, used: 9, sick: 30, sickUsed: 3 },
    { empId: 'VNM-0030', annual: 13, used: 4, sick: 30, sickUsed: 0 },
    { empId: 'VNM-0032', annual: 14, used: 6, sick: 30, sickUsed: 1 },
    { empId: 'VNM-0038', annual: 13, used: 3, sick: 30, sickUsed: 2 },
    { empId: 'VNM-0041', annual: 12, used: 5, sick: 30, sickUsed: 0 },
    { empId: 'VNM-0046', annual: 13, used: 7, sick: 30, sickUsed: 1 }
  ],

  requests: [
    { id: 'LV-001', empId: 'VNM-0003', empName: 'Lê Thị Hương', type: 'annual', startDate: '2026-09-15', endDate: '2026-09-17', days: 3, reason: 'Về quê thăm gia đình', status: 'Chờ duyệt', createdDate: '2026-09-06' },
    { id: 'LV-002', empId: 'VNM-0031', empName: 'Trần Đức Hoàng', type: 'annual', startDate: '2026-09-22', endDate: '2026-09-24', days: 3, reason: 'Du lịch gia đình', status: 'Chờ duyệt', createdDate: '2026-09-07' },
    { id: 'LV-003', empId: 'VNM-0027', empName: 'Trịnh Thị Minh Châu', type: 'sick', startDate: '2026-09-08', endDate: '2026-09-09', days: 2, reason: 'Khám sức khỏe định kỳ', status: 'Đã duyệt', createdDate: '2026-09-05' },
    { id: 'LV-004', empId: 'VNM-0010', empName: 'Trần Thị Ngọc', type: 'annual', startDate: '2026-09-10', endDate: '2026-09-12', days: 3, reason: 'Việc cá nhân', status: 'Đã duyệt', createdDate: '2026-09-03' },
    { id: 'LV-005', empId: 'VNM-0045', empName: 'Đặng Thị Hạnh', type: 'wedding', startDate: '2026-10-10', endDate: '2026-10-12', days: 3, reason: 'Kết hôn', status: 'Đã duyệt', createdDate: '2026-08-25' },
    { id: 'LV-006', empId: 'VNM-0004', empName: 'Phạm Đức Thắng', type: 'annual', startDate: '2026-08-25', endDate: '2026-08-27', days: 3, reason: 'Nghỉ phép cá nhân', status: 'Hoàn thành', createdDate: '2026-08-15' },
    { id: 'LV-007', empId: 'VNM-0016', empName: 'Phan Thanh Tùng', type: 'sick', startDate: '2026-08-20', endDate: '2026-08-21', days: 2, reason: 'Ốm', status: 'Hoàn thành', createdDate: '2026-08-20' },
    { id: 'LV-008', empId: 'VNM-0043', empName: 'Lê Phương Anh', type: 'annual', startDate: '2026-09-29', endDate: '2026-10-01', days: 3, reason: 'Nghỉ lễ kết hợp phép', status: 'Chờ duyệt', createdDate: '2026-09-08' },
    { id: 'LV-009', empId: 'VNM-0015', empName: 'Hoàng Thị Mai Anh', type: 'compensatory', startDate: '2026-09-12', endDate: '2026-09-12', days: 1, reason: 'Nghỉ bù làm thêm cuối tuần', status: 'Đã duyệt', createdDate: '2026-09-06' },
    { id: 'LV-010', empId: 'VNM-0008', empName: 'Đặng Quốc Bảo', type: 'annual', startDate: '2026-09-18', endDate: '2026-09-19', days: 2, reason: 'Việc gia đình', status: 'Chờ duyệt', createdDate: '2026-09-08' }
  ],

  getBalance(empId) {
    return this.balances.find(b => b.empId === empId) || { annual: 12, used: 0, sick: 30, sickUsed: 0 };
  },

  getRequests(status) {
    if (status) return this.requests.filter(r => r.status === status);
    return this.requests;
  },

  getStats() {
    return {
      totalRequests: this.requests.length,
      pending: this.requests.filter(r => r.status === 'Chờ duyệt').length,
      approved: this.requests.filter(r => r.status === 'Đã duyệt').length,
      completed: this.requests.filter(r => r.status === 'Hoàn thành').length,
      avgDays: Math.round(this.requests.reduce((s, r) => s + r.days, 0) / this.requests.length * 10) / 10
    };
  }
};
