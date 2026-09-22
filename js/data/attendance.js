// ============================================
// VINAMILK HRIS - Attendance Data
// Multi-channel attendance tracking
// ============================================

const AttendanceData = {
  // Current month: September 2026
  currentMonth: '2026-09',
  
  // Attendance channels by department type
  channels: {
    'factory': { name: 'Vân tay / Face ID', icon: '🔒' },
    'farm': { name: 'Bảng công điện tử', icon: '📋' },
    'sales': { name: 'GPS Check-in', icon: '📍' },
    'office': { name: 'Vân tay / Face ID', icon: '🔒' }
  },

  // Attendance symbols
  symbols: {
    'X': { label: 'Đi làm', class: 'day-full', desc: 'Đủ công' },
    'X/2': { label: 'Nửa ngày', class: 'day-half', desc: 'Nửa công' },
    'V': { label: 'Vắng', class: 'day-absent', desc: 'Vắng mặt' },
    'P': { label: 'Nghỉ phép', class: 'day-leave', desc: 'Nghỉ phép năm' },
    'Ô': { label: 'Ốm', class: 'day-leave', desc: 'Nghỉ ốm' },
    'TS': { label: 'Thai sản', class: 'day-leave', desc: 'Nghỉ thai sản' },
    'T': { label: 'Tăng ca', class: 'day-ot', desc: 'Tăng ca' },
    'CN': { label: 'Chủ nhật', class: 'day-off', desc: 'Nghỉ CN' },
    'L': { label: 'Lễ', class: 'day-off', desc: 'Nghỉ lễ' },
    '-': { label: 'Nghỉ', class: 'day-off', desc: 'Ngày nghỉ' }
  },

  // Generate attendance for an employee for current month
  generateMonthData(employeeId) {
    const seed = employeeId.charCodeAt(employeeId.length - 1);
    const days = [];
    const daysInMonth = 30; // September
    
    for (let d = 1; d <= daysInMonth; d++) {
      const date = new Date(2026, 8, d); // September 2026
      const dow = date.getDay(); // 0=Sun
      
      if (dow === 0) {
        // Random overtime on Sundays
        days.push((seed + d) % 5 === 0 ? 'T' : 'CN');
      } else if (d > 7) { // Future days in September
        days.push('-');
      } else {
        // Working days
        const rand = (seed * d) % 20;
        if (rand === 0) days.push('V');
        else if (rand === 1) days.push('P');
        else if (rand === 2) days.push('X/2');
        else if (rand === 3) days.push('Ô');
        else days.push('X');
      }
    }
    return days;
  },

  // Get attendance summary for an employee
  getSummary(attendance) {
    return {
      workDays: attendance.filter(d => d === 'X').length,
      halfDays: attendance.filter(d => d === 'X/2').length,
      absent: attendance.filter(d => d === 'V').length,
      leave: attendance.filter(d => d === 'P' || d === 'Ô' || d === 'TS').length,
      overtime: attendance.filter(d => d === 'T').length,
      totalWork: attendance.filter(d => d === 'X').length + attendance.filter(d => d === 'X/2').length * 0.5
    };
  },

  // Leave balance
  leaveBalances: {
    'VNM-0001': { annual: 15, used: 5, remaining: 10 },
    'VNM-0002': { annual: 14, used: 8, remaining: 6 },
    'VNM-0003': { annual: 12, used: 3, remaining: 9 },
    'VNM-0006': { annual: 14, used: 7, remaining: 7 },
    'VNM-0007': { annual: 12, used: 4, remaining: 8 },
    'VNM-0009': { annual: 16, used: 6, remaining: 10 },
    'VNM-0010': { annual: 12, used: 2, remaining: 10 },
    'VNM-0014': { annual: 16, used: 10, remaining: 6 },
    'VNM-0020': { annual: 18, used: 8, remaining: 10 },
    'VNM-0030': { annual: 14, used: 5, remaining: 9 },
  },

  // Overtime rules
  overtimeRates: {
    weekday: 1.5,
    saturday: 2.0,
    sunday: 2.0,
    holiday: 3.0,
    night: 1.3 // 22h-06h
  },

  // Shift definitions
  shifts: [
    { id: 'S1', name: 'Ca sáng', start: '06:00', end: '14:00', type: 'Sản xuất' },
    { id: 'S2', name: 'Ca chiều', start: '14:00', end: '22:00', type: 'Sản xuất' },
    { id: 'S3', name: 'Ca đêm', start: '22:00', end: '06:00', type: 'Sản xuất', nightShift: true },
    { id: 'HC', name: 'Hành chính', start: '08:00', end: '17:00', type: 'Văn phòng' },
    { id: 'TT', name: 'Trang trại', start: '05:30', end: '16:30', type: 'Trang trại' }
  ],

  getStatsOverview() {
    return {
      attendanceRate: 96.5,
      lateRate: 3.2,
      absenceRate: 1.8,
      overtimeHours: 12450,
      avgOvertimePerPerson: 8.2
    };
  }
};
