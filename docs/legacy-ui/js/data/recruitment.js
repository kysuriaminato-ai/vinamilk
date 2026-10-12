// ============================================
// VINAMILK HRIS - Recruitment Data
// ATS candidates and job openings
// ============================================

const RecruitmentData = {
  jobs: [
    { id: 'JOB-001', title: 'Kỹ sư QA/QC', department: 'nm_bd', level: 'Chuyên viên', quantity: 2, status: 'Đang tuyển', postedDate: '2026-07-15', deadline: '2026-09-30', salaryRange: '16-22 triệu', source: 'VietnamWorks', requester: 'VNM-0006', inBudget: true },
    { id: 'JOB-002', title: 'Bác sĩ Thú y', department: 'tt_organic2', level: 'Chuyên viên', quantity: 1, status: 'Đang tuyển', postedDate: '2026-08-01', deadline: '2026-10-15', salaryRange: '18-25 triệu', source: 'LinkedIn', requester: 'VNM-0009', inBudget: true },
    { id: 'JOB-003', title: 'Nhân viên Bán hàng', department: 'cn_nam', level: 'Nhân viên', quantity: 5, status: 'Đang tuyển', postedDate: '2026-08-10', deadline: '2026-09-20', salaryRange: '10-15 triệu', source: 'TopCV', requester: 'VNM-0014', inBudget: true },
    { id: 'JOB-004', title: 'Lập trình viên Full-stack', department: 'pb_it', level: 'Chuyên viên', quantity: 2, status: 'Đang tuyển', postedDate: '2026-08-05', deadline: '2026-10-05', salaryRange: '22-35 triệu', source: 'ITviec', requester: 'VNM-0030', inBudget: true },
    { id: 'JOB-005', title: 'Trưởng ca Sản xuất', department: 'nm_na', level: 'Quản lý', quantity: 1, status: 'Đang tuyển', postedDate: '2026-07-20', deadline: '2026-09-15', salaryRange: '18-24 triệu', source: 'Nội bộ', requester: 'VNM-0046', inBudget: false },
    { id: 'JOB-006', title: 'Chuyên viên Marketing Digital', department: 'pb_marketing', level: 'Chuyên viên', quantity: 1, status: 'Hoàn thành', postedDate: '2026-06-01', deadline: '2026-08-01', salaryRange: '14-20 triệu', source: 'LinkedIn', requester: 'VNM-0026', inBudget: true },
  ],

  candidates: [
    { id: 'UV-001', name: 'Nguyễn Hoàng Phúc', phone: '0911223344', email: 'phuc.nh@gmail.com', jobId: 'JOB-001', stage: 'Phỏng vấn', matchScore: 88, appliedDate: '2026-08-05', source: 'VietnamWorks', healthCheck: null, notes: 'Có 3 năm kinh nghiệm QA tại công ty FMCG', interviewScore: 82, education: 'ĐH Bách Khoa TP.HCM' },
    { id: 'UV-002', name: 'Trần Thị Mỹ Hạnh', phone: '0922334455', email: 'hanh.ttm@gmail.com', jobId: 'JOB-001', stage: 'Sàng lọc CV', matchScore: 72, appliedDate: '2026-08-12', source: 'VietnamWorks', healthCheck: null, notes: 'Fresh graduate, có thực tập tại Nestlé', interviewScore: null, education: 'ĐH Nông Lâm TP.HCM' },
    { id: 'UV-003', name: 'Lê Minh Khang', phone: '0933445566', email: 'khang.lm@gmail.com', jobId: 'JOB-001', stage: 'Kiểm tra sức khỏe', matchScore: 91, appliedDate: '2026-07-28', source: 'Giới thiệu', healthCheck: 'Đạt', notes: 'Kỹ sư cao cấp, 5 năm kinh nghiệm', interviewScore: 90, education: 'ĐH Bách Khoa HN' },
    { id: 'UV-004', name: 'Phạm Thùy Dung', phone: '0944556677', email: 'dung.pt@gmail.com', jobId: 'JOB-002', stage: 'Offer', matchScore: 95, appliedDate: '2026-08-08', source: 'LinkedIn', healthCheck: 'Đạt', notes: 'Bác sĩ thú y 7 năm, từng làm tại TH True Milk', interviewScore: 93, education: 'ĐH Nông nghiệp HN' },
    { id: 'UV-005', name: 'Đỗ Quang Minh', phone: '0955667788', email: 'minh.dq@gmail.com', jobId: 'JOB-003', stage: 'Đề xuất', matchScore: 65, appliedDate: '2026-08-20', source: 'TopCV', healthCheck: null, notes: 'Sinh viên mới tốt nghiệp', interviewScore: null, education: 'ĐH Tài chính Marketing' },
    { id: 'UV-006', name: 'Hoàng Thị Lan Chi', phone: '0966778899', email: 'chi.htl@gmail.com', jobId: 'JOB-003', stage: 'Phỏng vấn', matchScore: 78, appliedDate: '2026-08-15', source: 'TopCV', healthCheck: null, notes: '2 năm sales FMCG tại Unilever', interviewScore: 75, education: 'ĐH Thương mại' },
    { id: 'UV-007', name: 'Vũ Đức Trung', phone: '0977889900', email: 'trung.vd@gmail.com', jobId: 'JOB-003', stage: 'Sàng lọc CV', matchScore: 70, appliedDate: '2026-08-22', source: 'Giới thiệu', healthCheck: null, notes: 'Sales route tại Pepsi, am hiểu kênh MT', interviewScore: null, education: 'ĐH Kinh tế TP.HCM' },
    { id: 'UV-008', name: 'Nguyễn Thanh Tùng', phone: '0988990011', email: 'tung.nt@gmail.com', jobId: 'JOB-004', stage: 'Phỏng vấn', matchScore: 85, appliedDate: '2026-08-10', source: 'ITviec', healthCheck: null, notes: '4 năm React/Node.js, từng làm tại FPT', interviewScore: 80, education: 'ĐH Công nghệ - ĐHQG HN' },
    { id: 'UV-009', name: 'Lê Thị Kiều Trang', phone: '0999001122', email: 'trang.ltk@gmail.com', jobId: 'JOB-004', stage: 'Kiểm tra sức khỏe', matchScore: 92, appliedDate: '2026-08-03', source: 'LinkedIn', healthCheck: 'Chờ kết quả', notes: 'Senior dev 6 năm, full-stack Python/React', interviewScore: 88, education: 'ĐH KHTN TP.HCM' },
    { id: 'UV-010', name: 'Phan Quốc Đạt', phone: '0900112233', email: 'dat.pq@gmail.com', jobId: 'JOB-005', stage: 'Sàng lọc CV', matchScore: 75, appliedDate: '2026-08-18', source: 'Nội bộ', healthCheck: null, notes: 'Nội bộ - Tổ trưởng NM Nghệ An, muốn thăng tiến', interviewScore: null, education: 'ĐH Vinh' },
    { id: 'UV-011', name: 'Trương Minh Hiếu', phone: '0911224455', email: 'hieu.tm@gmail.com', jobId: 'JOB-003', stage: 'Onboarding', matchScore: 82, appliedDate: '2026-07-10', source: 'VietnamWorks', healthCheck: 'Đạt', notes: 'Đã nhận offer, bắt đầu 01/10', interviewScore: 85, education: 'ĐH Kinh tế Quốc dân' },
    { id: 'UV-012', name: 'Bùi Thị Hồng Nhung', phone: '0922335566', email: 'nhung.bth@gmail.com', jobId: 'JOB-006', stage: 'Onboarding', matchScore: 89, appliedDate: '2026-06-15', source: 'LinkedIn', healthCheck: 'Đạt', notes: 'Đã onboard 01/08 thành công', interviewScore: 87, education: 'ĐH RMIT' },
  ],

  stages: ['Đề xuất', 'Đăng tuyển', 'Sàng lọc CV', 'Phỏng vấn', 'Kiểm tra sức khỏe', 'Offer', 'Onboarding'],
  
  stageColors: {
    'Đề xuất': '#8993A4',
    'Đăng tuyển': '#6554C0',
    'Sàng lọc CV': '#00B8D9',
    'Phỏng vấn': '#FFAB00',
    'Kiểm tra sức khỏe': '#FF8B00',
    'Offer': '#36B37E',
    'Onboarding': '#0052CC'
  },

  getByStage(stage) {
    return this.candidates.filter(c => c.stage === stage);
  },

  getByJob(jobId) {
    return this.candidates.filter(c => c.jobId === jobId);
  },

  getStats() {
    return {
      totalJobs: this.jobs.filter(j => j.status === 'Đang tuyển').length,
      totalCandidates: this.candidates.length,
      inInterview: this.candidates.filter(c => c.stage === 'Phỏng vấn').length,
      offers: this.candidates.filter(c => c.stage === 'Offer').length,
      avgMatchScore: Math.round(this.candidates.reduce((s, c) => s + c.matchScore, 0) / this.candidates.length),
      avgTimeToHire: 32
    };
  }
};
