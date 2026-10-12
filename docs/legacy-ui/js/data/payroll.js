// ============================================
// VINAMILK HRIS - Payroll & Contracts Data
// 3P salary structure and contract management
// ============================================

const PayrollData = {
  // Insurance rates (employer + employee)
  insuranceRates: {
    bhxh_employee: 0.08,
    bhyt_employee: 0.015,
    bhtn_employee: 0.01,
    bhxh_employer: 0.175,
    bhyt_employer: 0.03,
    bhtn_employer: 0.01,
    union_employee: 0.01,
    union_employer: 0.02
  },

  // Tax brackets (Vietnam PIT)
  taxBrackets: [
    { min: 0, max: 5000000, rate: 0.05 },
    { min: 5000000, max: 10000000, rate: 0.10 },
    { min: 10000000, max: 18000000, rate: 0.15 },
    { min: 18000000, max: 32000000, rate: 0.20 },
    { min: 32000000, max: 52000000, rate: 0.25 },
    { min: 52000000, max: 80000000, rate: 0.30 },
    { min: 80000000, max: Infinity, rate: 0.35 }
  ],

  personalDeduction: 11000000,
  dependentDeduction: 4400000,

  // Calculate payroll for an employee
  calculatePayroll(emp) {
    const grossSalary = emp.p1 + emp.p2 + emp.p3 + emp.allowances;
    
    // Insurance deductions (based on insurance salary)
    const insSalary = emp.insuranceSalary || 0;
    const bhxh = Math.round(insSalary * this.insuranceRates.bhxh_employee);
    const bhyt = Math.round(insSalary * this.insuranceRates.bhyt_employee);
    const bhtn = Math.round(insSalary * this.insuranceRates.bhtn_employee);
    const unionFee = Math.round(insSalary * this.insuranceRates.union_employee);
    const totalInsurance = bhxh + bhyt + bhtn + unionFee;

    // Taxable income
    const deductions = this.personalDeduction + (emp.dependents || 0) * this.dependentDeduction;
    const taxableIncome = Math.max(0, grossSalary - totalInsurance - deductions);
    
    // Calculate PIT
    let tax = 0;
    let remaining = taxableIncome;
    for (const bracket of this.taxBrackets) {
      if (remaining <= 0) break;
      const taxable = Math.min(remaining, bracket.max - bracket.min);
      tax += taxable * bracket.rate;
      remaining -= taxable;
    }
    tax = Math.round(tax);

    const netSalary = grossSalary - totalInsurance - tax;

    return {
      employeeId: emp.id,
      name: emp.name,
      department: emp.department,
      position: emp.position,
      p1: emp.p1,
      p2: emp.p2,
      p3: emp.p3,
      allowances: emp.allowances,
      grossSalary,
      insuranceSalary: insSalary,
      bhxh, bhyt, bhtn, unionFee,
      totalInsurance,
      taxableIncome,
      tax,
      netSalary,
      dependents: emp.dependents || 0
    };
  },

  // Generate payroll for all active employees
  generatePayroll() {
    return EmployeesData
      .filter(e => e.status === 'Đang làm việc' || e.status === 'Thử việc')
      .map(e => this.calculatePayroll(e));
  },

  // Payroll summary
  getPayrollSummary() {
    const payroll = this.generatePayroll();
    return {
      totalEmployees: payroll.length,
      totalGross: payroll.reduce((s, p) => s + p.grossSalary, 0),
      totalP1: payroll.reduce((s, p) => s + p.p1, 0),
      totalP2: payroll.reduce((s, p) => s + p.p2, 0),
      totalP3: payroll.reduce((s, p) => s + p.p3, 0),
      totalAllowances: payroll.reduce((s, p) => s + p.allowances, 0),
      totalInsurance: payroll.reduce((s, p) => s + p.totalInsurance, 0),
      totalTax: payroll.reduce((s, p) => s + p.tax, 0),
      totalNet: payroll.reduce((s, p) => s + p.netSalary, 0),
      avgGross: Math.round(payroll.reduce((s, p) => s + p.grossSalary, 0) / payroll.length),
      avgNet: Math.round(payroll.reduce((s, p) => s + p.netSalary, 0) / payroll.length)
    };
  }
};

// Contract types and status
const ContractsData = {
  types: [
    { code: 'TV', name: 'Thử việc', duration: '2 tháng', color: '#FFAB00' },
    { code: '1Y', name: 'Có thời hạn 1 năm', duration: '12 tháng', color: '#00B8D9' },
    { code: '3Y', name: 'Có thời hạn 3 năm', duration: '36 tháng', color: '#0052CC' },
    { code: 'KTH', name: 'Không thời hạn', duration: 'Vô thời hạn', color: '#36B37E' }
  ],

  getContractStats() {
    const active = EmployeesData.filter(e => e.status === 'Đang làm việc' || e.status === 'Thử việc');
    return {
      trial: active.filter(e => e.contractType === 'Thử việc').length,
      oneYear: active.filter(e => e.contractType === '1 năm').length,
      threeYear: active.filter(e => e.contractType === '3 năm').length,
      indefinite: active.filter(e => e.contractType === 'Không thời hạn').length,
      expiringSoon: EmployeesHelper.getContractExpiring(60).length,
      expired: EmployeesHelper.getContractExpiring(0).length
    };
  },

  // Contract history for an employee
  getHistory(employeeId) {
    const histories = {
      'VNM-0001': [
        { type: 'Thử việc', start: '2002-06-01', end: '2002-07-31', status: 'Hoàn thành' },
        { type: '1 năm', start: '2002-08-01', end: '2003-07-31', status: 'Hoàn thành' },
        { type: '3 năm', start: '2003-08-01', end: '2006-07-31', status: 'Hoàn thành' },
        { type: 'Không thời hạn', start: '2006-08-01', end: null, status: 'Hiện tại' }
      ],
      'VNM-0002': [
        { type: 'Thử việc', start: '2010-03-15', end: '2010-05-14', status: 'Hoàn thành' },
        { type: '1 năm', start: '2010-05-15', end: '2011-05-14', status: 'Hoàn thành' },
        { type: '3 năm', start: '2011-05-15', end: '2014-05-14', status: 'Hoàn thành' },
        { type: '3 năm', start: '2014-05-15', end: '2017-05-14', status: 'Hoàn thành' },
        { type: '3 năm', start: '2017-05-15', end: '2020-05-14', status: 'Hoàn thành' },
        { type: '3 năm', start: '2024-03-15', end: '2027-03-14', status: 'Hiện tại' }
      ]
    };
    return histories[employeeId] || [
      { type: 'Đang hoạt động', start: EmployeesData.find(e => e.id === employeeId)?.joinDate, end: null, status: 'Hiện tại' }
    ];
  },

  // Offboarding checklist
  offboardingChecklist: [
    { id: 'OB-01', task: 'Thu hồi thẻ nhân viên & thẻ ra vào', department: 'Hành chính', icon: '' },
    { id: 'OB-02', task: 'Thu hồi laptop/thiết bị IT', department: 'CNTT', icon: '' },
    { id: 'OB-03', task: 'Vô hiệu hóa tài khoản email & hệ thống', department: 'CNTT', icon: '' },
    { id: 'OB-04', task: 'Thanh toán công nợ/tạm ứng', department: 'Tài chính', icon: '' },
    { id: 'OB-05', task: 'Bàn giao công việc', department: 'Quản lý trực tiếp', icon: '' },
    { id: 'OB-06', task: 'Chốt sổ BHXH', department: 'Nhân sự', icon: '' },
    { id: 'OB-07', task: 'Hoàn thành Exit Interview', department: 'Nhân sự', icon: '' },
    { id: 'OB-08', task: 'Chuyển hồ sơ sang Archive', department: 'Nhân sự', icon: '' }
  ]
};

