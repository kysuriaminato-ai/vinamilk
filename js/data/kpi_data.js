// ============================================
// VINAMILK HRIS - KPI Data
// KPI/Performance evaluation records
// ============================================

const KPIData = {
  periods: [
    { id: 'Q3-2026', name: 'Quý 3/2026', startDate: '2026-07-01', endDate: '2026-09-30', status: 'Đang đánh giá' },
    { id: 'Q2-2026', name: 'Quý 2/2026', startDate: '2026-04-01', endDate: '2026-06-30', status: 'Hoàn thành' },
    { id: 'Q1-2026', name: 'Quý 1/2026', startDate: '2026-01-01', endDate: '2026-03-31', status: 'Hoàn thành' },
    { id: 'Q4-2025', name: 'Quý 4/2025', startDate: '2025-10-01', endDate: '2025-12-31', status: 'Hoàn thành' }
  ],

  criteria: [
    { id: 'productivity', name: 'Năng suất làm việc', weight: 25, icon: '⚡' },
    { id: 'quality', name: 'Chất lượng công việc', weight: 25, icon: '✅' },
    { id: 'teamwork', name: 'Làm việc nhóm', weight: 15, icon: '🤝' },
    { id: 'initiative', name: 'Sáng kiến & Đổi mới', weight: 15, icon: '💡' },
    { id: 'discipline', name: 'Kỷ luật & Tác phong', weight: 10, icon: '📋' },
    { id: 'learning', name: 'Học hỏi & Phát triển', weight: 10, icon: '📚' }
  ],

  evaluations: [
    { empId: 'VNM-0001', period: 'Q2-2026', scores: { productivity: 92, quality: 95, teamwork: 88, initiative: 90, discipline: 95, learning: 85 }, rating: 'Xuất sắc', notes: 'Lãnh đạo xuất sắc, vượt KPI sản lượng 8%' },
    { empId: 'VNM-0002', period: 'Q2-2026', scores: { productivity: 88, quality: 85, teamwork: 90, initiative: 78, discipline: 92, learning: 80 }, rating: 'Tốt', notes: 'Quản lý tốt phân xưởng' },
    { empId: 'VNM-0003', period: 'Q2-2026', scores: { productivity: 82, quality: 88, teamwork: 85, initiative: 70, discipline: 90, learning: 75 }, rating: 'Tốt', notes: 'Ổn định, cần cải thiện sáng kiến' },
    { empId: 'VNM-0006', period: 'Q2-2026', scores: { productivity: 90, quality: 92, teamwork: 85, initiative: 82, discipline: 88, learning: 78 }, rating: 'Tốt', notes: 'Vận hành ca đêm hiệu quả' },
    { empId: 'VNM-0009', period: 'Q2-2026', scores: { productivity: 95, quality: 90, teamwork: 92, initiative: 88, discipline: 95, learning: 90 }, rating: 'Xuất sắc', notes: 'GĐ trang trại xuất sắc, đàn bò đạt năng suất cao nhất' },
    { empId: 'VNM-0014', period: 'Q2-2026', scores: { productivity: 88, quality: 85, teamwork: 82, initiative: 92, discipline: 90, learning: 85 }, rating: 'Tốt', notes: 'Mở rộng thị trường miền Bắc tốt' },
    { empId: 'VNM-0020', period: 'Q2-2026', scores: { productivity: 93, quality: 95, teamwork: 95, initiative: 90, discipline: 98, learning: 92 }, rating: 'Xuất sắc', notes: 'CHRO xuất sắc, triển khai HRIS thành công' },
    { empId: 'VNM-0024', period: 'Q2-2026', scores: { productivity: 90, quality: 92, teamwork: 85, initiative: 88, discipline: 95, learning: 88 }, rating: 'Xuất sắc', notes: 'CFO quản lý tài chính hiệu quả' },
    { empId: 'VNM-0026', period: 'Q2-2026', scores: { productivity: 85, quality: 88, teamwork: 80, initiative: 92, discipline: 85, learning: 90 }, rating: 'Tốt', notes: 'Chiến dịch marketing hiệu quả' },
    { empId: 'VNM-0028', period: 'Q2-2026', scores: { productivity: 92, quality: 95, teamwork: 88, initiative: 95, discipline: 90, learning: 95 }, rating: 'Xuất sắc', notes: 'Ra mắt 3 sản phẩm mới' },
    { empId: 'VNM-0030', period: 'Q2-2026', scores: { productivity: 88, quality: 90, teamwork: 85, initiative: 85, discipline: 92, learning: 88 }, rating: 'Tốt', notes: 'Hệ thống IT ổn định 99.9%' },
    { empId: 'VNM-0032', period: 'Q2-2026', scores: { productivity: 90, quality: 95, teamwork: 88, initiative: 82, discipline: 95, learning: 85 }, rating: 'Xuất sắc', notes: 'Không có sự cố chất lượng' },
    { empId: 'VNM-0016', period: 'Q2-2026', scores: { productivity: 75, quality: 72, teamwork: 70, initiative: 65, discipline: 70, learning: 68 }, rating: 'Trung bình', notes: 'Cần cải thiện KPI bán hàng' },
    { empId: 'VNM-0004', period: 'Q2-2026', scores: { productivity: 70, quality: 78, teamwork: 75, initiative: 60, discipline: 65, learning: 62 }, rating: 'Trung bình', notes: 'Đi muộn nhiều, cần coaching' },
    { empId: 'VNM-0033', period: 'Q2-2026', scores: { productivity: 80, quality: 82, teamwork: 85, initiative: 68, discipline: 88, learning: 72 }, rating: 'Khá', notes: 'Hoàn thành nhiệm vụ đều đặn' }
  ],

  deptScores: [
    { deptId: 'nm_ts', deptName: 'NM Tiên Sơn', avgScore: 86, trend: 'up', prevScore: 83 },
    { deptId: 'nm_bd', deptName: 'NM Bình Dương', avgScore: 84, trend: 'stable', prevScore: 84 },
    { deptId: 'nm_sg', deptName: 'NM Sài Gòn', avgScore: 82, trend: 'down', prevScore: 85 },
    { deptId: 'tt_green1', deptName: 'TT Tây Ninh', avgScore: 91, trend: 'up', prevScore: 87 },
    { deptId: 'cn_bac', deptName: 'CN miền Bắc', avgScore: 85, trend: 'up', prevScore: 82 },
    { deptId: 'cn_nam', deptName: 'CN miền Nam', avgScore: 78, trend: 'down', prevScore: 81 },
    { deptId: 'pb_nhansu', deptName: 'P. Nhân sự', avgScore: 92, trend: 'up', prevScore: 88 },
    { deptId: 'pb_rd', deptName: 'TT R&D', avgScore: 90, trend: 'up', prevScore: 86 },
    { deptId: 'pb_it', deptName: 'P. CNTT', avgScore: 87, trend: 'stable', prevScore: 87 },
    { deptId: 'pb_taichinh', deptName: 'P. Tài chính', avgScore: 89, trend: 'up', prevScore: 85 }
  ],

  getRatingColor(rating) {
    const map = { 'Xuất sắc': '#36B37E', 'Tốt': '#0052CC', 'Khá': '#00B8D9', 'Trung bình': '#FFAB00', 'Yếu': '#FF5630' };
    return map[rating] || '#8993A4';
  },

  getRatingBadge(rating) {
    const map = { 'Xuất sắc': 'badge-active', 'Tốt': 'badge-info', 'Khá': 'badge-trial', 'Trung bình': 'badge-pending', 'Yếu': 'badge-danger' };
    return map[rating] || 'badge-info';
  },

  getAvgScore(scores) {
    const criteria = this.criteria;
    let total = 0, weightSum = 0;
    criteria.forEach(c => {
      if (scores[c.id] !== undefined) {
        total += scores[c.id] * c.weight;
        weightSum += c.weight;
      }
    });
    return Math.round(total / weightSum * 10) / 10;
  },

  getStats() {
    const q2 = this.evaluations.filter(e => e.period === 'Q2-2026');
    return {
      totalEvaluated: q2.length,
      excellent: q2.filter(e => e.rating === 'Xuất sắc').length,
      good: q2.filter(e => e.rating === 'Tốt').length,
      average: q2.filter(e => e.rating === 'Trung bình' || e.rating === 'Khá').length,
      poor: q2.filter(e => e.rating === 'Yếu').length,
      avgCompanyScore: Math.round(q2.reduce((s, e) => s + this.getAvgScore(e.scores), 0) / q2.length * 10) / 10
    };
  }
};
