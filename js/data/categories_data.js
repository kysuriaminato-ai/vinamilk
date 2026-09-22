// ============================================
// VINAMILK HRIS - Categories Data
// All business categories for HR management
// ============================================

const CategoriesData = {
  // Storage key prefix
  _storageKey: 'vinamilk_hris_categories',

  // Get categories with localStorage override
  getAll() {
    const saved = Storage.get('categories');
    if (saved) return saved;
    return this._defaultCategories;
  },

  // Get a single category by id
  getCategory(catId) {
    const all = this.getAll();
    return all.find(c => c.id === catId) || null;
  },

  // Add item to category
  addItem(catId, item) {
    const all = this.getAll();
    const cat = all.find(c => c.id === catId);
    if (!cat) return false;
    item.id = 'item_' + Date.now().toString(36);
    item.active = true;
    cat.items.push(item);
    Storage.set('categories', all);
    return true;
  },

  // Update item in category
  updateItem(catId, itemId, updates) {
    const all = this.getAll();
    const cat = all.find(c => c.id === catId);
    if (!cat) return false;
    const item = cat.items.find(i => i.id === itemId);
    if (!item) return false;
    Object.assign(item, updates);
    Storage.set('categories', all);
    return true;
  },

  // Delete item from category
  deleteItem(catId, itemId) {
    const all = this.getAll();
    const cat = all.find(c => c.id === catId);
    if (!cat) return false;
    cat.items = cat.items.filter(i => i.id !== itemId);
    Storage.set('categories', all);
    return true;
  },

  // Toggle item active status
  toggleItem(catId, itemId) {
    const all = this.getAll();
    const cat = all.find(c => c.id === catId);
    if (!cat) return false;
    const item = cat.items.find(i => i.id === itemId);
    if (!item) return false;
    item.active = !item.active;
    Storage.set('categories', all);
    return true;
  },

  // Reset to defaults
  resetToDefaults() {
    Storage.remove('categories');
  },

  // =============================================
  // DEFAULT CATEGORIES DATA
  // =============================================
  _defaultCategories: [
    // ===== NHÓM 1: HỆ THỐNG CÁC DANH MỤC =====
    {
      id: 'dm_don_vi',
      code: 'DM01',
      name: 'Đơn vị quản lý',
      group: 'system',
      icon: '🏢',
      description: 'Danh mục các đơn vị, phòng ban trong tổ chức',
      items: [
        { id: 'dv01', code: 'DV01', name: 'Hội đồng Quản trị', description: 'Cơ quan quản trị cao nhất', active: true },
        { id: 'dv02', code: 'DV02', name: 'Ban Tổng Giám đốc', description: 'Ban điều hành', active: true },
        { id: 'dv03', code: 'DV03', name: 'Phòng Nhân sự', description: 'Quản lý nhân sự toàn công ty', active: true },
        { id: 'dv04', code: 'DV04', name: 'Phòng Tài chính - Kế toán', description: 'Quản lý tài chính, kế toán', active: true },
        { id: 'dv05', code: 'DV05', name: 'Phòng Marketing', description: 'Tiếp thị, truyền thông', active: true },
        { id: 'dv06', code: 'DV06', name: 'Trung tâm R&D', description: 'Nghiên cứu và phát triển sản phẩm', active: true },
        { id: 'dv07', code: 'DV07', name: 'Phòng Công nghệ thông tin', description: 'Quản lý hệ thống CNTT', active: true },
        { id: 'dv08', code: 'DV08', name: 'Phòng Pháp luật', description: 'Tư vấn pháp lý nội bộ', active: true },
        { id: 'dv09', code: 'DV09', name: 'Phòng Hành chính', description: 'Công tác hành chính, lễ tân', active: true },
        { id: 'dv10', code: 'DV10', name: 'Phòng Quản lý Chất lượng', description: 'Đảm bảo chất lượng sản phẩm', active: true },
        { id: 'dv11', code: 'DV11', name: 'Nhà máy Sữa Tiên Sơn', description: 'Nhà máy sản xuất tại Bắc Ninh', active: true },
        { id: 'dv12', code: 'DV12', name: 'Nhà máy Sữa Nghệ An', description: 'Nhà máy sản xuất tại Nghệ An', active: true },
        { id: 'dv13', code: 'DV13', name: 'Nhà máy Sữa Bình Dương', description: 'Nhà máy sản xuất tại Bình Dương', active: true },
        { id: 'dv14', code: 'DV14', name: 'Nhà máy Sữa Sài Gòn', description: 'Nhà máy sản xuất tại TP.HCM', active: true },
        { id: 'dv15', code: 'DV15', name: 'Chi nhánh miền Bắc', description: 'Kinh doanh khu vực miền Bắc', active: true },
        { id: 'dv16', code: 'DV16', name: 'Chi nhánh miền Trung', description: 'Kinh doanh khu vực miền Trung', active: true },
        { id: 'dv17', code: 'DV17', name: 'Chi nhánh miền Nam', description: 'Kinh doanh khu vực miền Nam', active: true },
        { id: 'dv18', code: 'DV18', name: 'Trang trại Green Farm Tây Ninh', description: 'Trang trại bò sữa Tây Ninh', active: true }
      ]
    },
    {
      id: 'dm_hon_nhan',
      code: 'DM02',
      name: 'Tình trạng hôn nhân',
      group: 'system',
      icon: '💍',
      description: 'Phân loại tình trạng hôn nhân của cán bộ',
      items: [
        { id: 'hn01', code: 'HN01', name: 'Độc thân', description: 'Chưa kết hôn', active: true },
        { id: 'hn02', code: 'HN02', name: 'Đã kết hôn', description: 'Đang có vợ/chồng', active: true },
        { id: 'hn03', code: 'HN03', name: 'Ly hôn', description: 'Đã ly hôn', active: true },
        { id: 'hn04', code: 'HN04', name: 'Góa', description: 'Vợ/chồng đã mất', active: true },
        { id: 'hn05', code: 'HN05', name: 'Ly thân', description: 'Đang ly thân', active: true }
      ]
    },
    {
      id: 'dm_dan_toc',
      code: 'DM03',
      name: 'Dân tộc',
      group: 'system',
      icon: '🏛️',
      description: 'Danh mục 54 dân tộc Việt Nam',
      items: [
        { id: 'dt01', code: 'DT01', name: 'Kinh', description: 'Dân tộc Kinh (Việt)', active: true },
        { id: 'dt02', code: 'DT02', name: 'Tày', description: 'Dân tộc Tày', active: true },
        { id: 'dt03', code: 'DT03', name: 'Thái', description: 'Dân tộc Thái', active: true },
        { id: 'dt04', code: 'DT04', name: 'Mường', description: 'Dân tộc Mường', active: true },
        { id: 'dt05', code: 'DT05', name: 'Khmer', description: 'Dân tộc Khmer', active: true },
        { id: 'dt06', code: 'DT06', name: 'Hoa', description: 'Dân tộc Hoa', active: true },
        { id: 'dt07', code: 'DT07', name: 'Nùng', description: 'Dân tộc Nùng', active: true },
        { id: 'dt08', code: 'DT08', name: 'Hmông', description: 'Dân tộc Hmông', active: true },
        { id: 'dt09', code: 'DT09', name: 'Dao', description: 'Dân tộc Dao', active: true },
        { id: 'dt10', code: 'DT10', name: 'Gia Rai', description: 'Dân tộc Gia Rai', active: true },
        { id: 'dt11', code: 'DT11', name: 'Ê Đê', description: 'Dân tộc Ê Đê', active: true },
        { id: 'dt12', code: 'DT12', name: 'Ba Na', description: 'Dân tộc Ba Na', active: true },
        { id: 'dt13', code: 'DT13', name: 'Sán Chay', description: 'Dân tộc Sán Chay', active: true },
        { id: 'dt14', code: 'DT14', name: 'Chăm', description: 'Dân tộc Chăm', active: true },
        { id: 'dt15', code: 'DT15', name: 'Cơ Ho', description: 'Dân tộc Cơ Ho', active: true },
        { id: 'dt16', code: 'DT16', name: 'Xơ Đăng', description: 'Dân tộc Xơ Đăng', active: true },
        { id: 'dt17', code: 'DT17', name: 'Sán Dìu', description: 'Dân tộc Sán Dìu', active: true },
        { id: 'dt18', code: 'DT18', name: 'Hrê', description: 'Dân tộc Hrê', active: true },
        { id: 'dt19', code: 'DT19', name: 'Ra Glai', description: 'Dân tộc Ra Glai', active: true },
        { id: 'dt20', code: 'DT20', name: 'Mnông', description: 'Dân tộc Mnông', active: true }
      ]
    },
    {
      id: 'dm_ton_giao',
      code: 'DM04',
      name: 'Tôn giáo',
      group: 'system',
      icon: '🛕',
      description: 'Danh mục các tôn giáo',
      items: [
        { id: 'tg01', code: 'TG01', name: 'Không', description: 'Không theo tôn giáo', active: true },
        { id: 'tg02', code: 'TG02', name: 'Phật giáo', description: 'Đạo Phật', active: true },
        { id: 'tg03', code: 'TG03', name: 'Công giáo', description: 'Đạo Công giáo (Thiên Chúa giáo)', active: true },
        { id: 'tg04', code: 'TG04', name: 'Tin lành', description: 'Đạo Tin lành', active: true },
        { id: 'tg05', code: 'TG05', name: 'Cao Đài', description: 'Đạo Cao Đài', active: true },
        { id: 'tg06', code: 'TG06', name: 'Hòa Hảo', description: 'Phật giáo Hòa Hảo', active: true },
        { id: 'tg07', code: 'TG07', name: 'Hồi giáo', description: 'Đạo Hồi', active: true },
        { id: 'tg08', code: 'TG08', name: 'Bà La Môn', description: 'Đạo Bà La Môn (Ấn Độ giáo)', active: true }
      ]
    },
    {
      id: 'dm_xuat_than',
      code: 'DM05',
      name: 'Thành phần xuất thân',
      group: 'system',
      icon: '👤',
      description: 'Thành phần gia đình, xuất thân',
      items: [
        { id: 'xt01', code: 'XT01', name: 'Công nhân', description: 'Gia đình công nhân', active: true },
        { id: 'xt02', code: 'XT02', name: 'Nông dân', description: 'Gia đình nông dân', active: true },
        { id: 'xt03', code: 'XT03', name: 'Trí thức', description: 'Gia đình trí thức', active: true },
        { id: 'xt04', code: 'XT04', name: 'Quân nhân', description: 'Gia đình quân nhân', active: true },
        { id: 'xt05', code: 'XT05', name: 'Viên chức', description: 'Gia đình viên chức nhà nước', active: true },
        { id: 'xt06', code: 'XT06', name: 'Tiểu thương', description: 'Gia đình tiểu thương, buôn bán', active: true },
        { id: 'xt07', code: 'XT07', name: 'Doanh nhân', description: 'Gia đình doanh nhân', active: true }
      ]
    },
    {
      id: 'dm_chinh_sach',
      code: 'DM06',
      name: 'Đối tượng hưởng chính sách NN',
      group: 'system',
      icon: '🎖️',
      description: 'Đối tượng hưởng chính sách nhà nước, người có công',
      items: [
        { id: 'cs01', code: 'CS01', name: 'Thương binh', description: 'Người bị thương trong chiến đấu', active: true },
        { id: 'cs02', code: 'CS02', name: 'Bệnh binh', description: 'Quân nhân mắc bệnh do phục vụ', active: true },
        { id: 'cs03', code: 'CS03', name: 'Con liệt sĩ', description: 'Con của liệt sĩ', active: true },
        { id: 'cs04', code: 'CS04', name: 'Gia đình có công cách mạng', description: 'Gia đình có công với cách mạng', active: true },
        { id: 'cs05', code: 'CS05', name: 'Người nhiễm chất độc da cam', description: 'Nạn nhân chất độc da cam/dioxin', active: true },
        { id: 'cs06', code: 'CS06', name: 'Con thương binh', description: 'Con của thương binh', active: true },
        { id: 'cs07', code: 'CS07', name: 'Dân tộc thiểu số', description: 'Người dân tộc thiểu số', active: true },
        { id: 'cs08', code: 'CS08', name: 'Không thuộc diện', description: 'Không thuộc đối tượng chính sách', active: true }
      ]
    },
    {
      id: 'dm_chuc_danh',
      code: 'DM07',
      name: 'Chức danh, chức vụ',
      group: 'system',
      icon: '👔',
      description: 'Danh mục chức danh và chức vụ trong tổ chức',
      items: [
        { id: 'cd01', code: 'CD01', name: 'Tổng Giám đốc', description: 'Người đứng đầu công ty', active: true },
        { id: 'cd02', code: 'CD02', name: 'Phó Tổng Giám đốc', description: 'Phó người đứng đầu', active: true },
        { id: 'cd03', code: 'CD03', name: 'Giám đốc', description: 'Giám đốc đơn vị', active: true },
        { id: 'cd04', code: 'CD04', name: 'Phó Giám đốc', description: 'Phó giám đốc đơn vị', active: true },
        { id: 'cd05', code: 'CD05', name: 'Trưởng phòng', description: 'Trưởng phòng ban', active: true },
        { id: 'cd06', code: 'CD06', name: 'Phó Trưởng phòng', description: 'Phó trưởng phòng ban', active: true },
        { id: 'cd07', code: 'CD07', name: 'Trưởng ca', description: 'Trưởng ca sản xuất', active: true },
        { id: 'cd08', code: 'CD08', name: 'Quản đốc', description: 'Quản đốc phân xưởng', active: true },
        { id: 'cd09', code: 'CD09', name: 'Tổ trưởng', description: 'Tổ trưởng sản xuất', active: true },
        { id: 'cd10', code: 'CD10', name: 'Chuyên viên', description: 'Chuyên viên nghiệp vụ', active: true },
        { id: 'cd11', code: 'CD11', name: 'Nhân viên', description: 'Nhân viên thường', active: true },
        { id: 'cd12', code: 'CD12', name: 'Công nhân', description: 'Công nhân sản xuất', active: true },
        { id: 'cd13', code: 'CD13', name: 'Chuyên gia', description: 'Chuyên gia cố vấn', active: true },
        { id: 'cd14', code: 'CD14', name: 'Kế toán trưởng', description: 'Kế toán trưởng đơn vị', active: true }
      ]
    },
    {
      id: 'dm_chuc_vu_dang',
      code: 'DM08',
      name: 'Chức vụ Đảng',
      group: 'system',
      icon: '⭐',
      description: 'Chức vụ trong tổ chức Đảng',
      items: [
        { id: 'cvd01', code: 'CVD01', name: 'Bí thư Đảng ủy', description: 'Bí thư Đảng ủy cơ sở', active: true },
        { id: 'cvd02', code: 'CVD02', name: 'Phó Bí thư Đảng ủy', description: 'Phó Bí thư Đảng ủy', active: true },
        { id: 'cvd03', code: 'CVD03', name: 'Ủy viên Ban Chấp hành', description: 'Ủy viên BCH Đảng bộ', active: true },
        { id: 'cvd04', code: 'CVD04', name: 'Bí thư Chi bộ', description: 'Bí thư chi bộ', active: true },
        { id: 'cvd05', code: 'CVD05', name: 'Phó Bí thư Chi bộ', description: 'Phó Bí thư chi bộ', active: true },
        { id: 'cvd06', code: 'CVD06', name: 'Đảng viên', description: 'Đảng viên thường', active: true },
        { id: 'cvd07', code: 'CVD07', name: 'Đảng viên dự bị', description: 'Đảng viên dự bị', active: true }
      ]
    },
    {
      id: 'dm_chuc_vu_doan',
      code: 'DM09',
      name: 'Chức vụ Đoàn',
      group: 'system',
      icon: '🌟',
      description: 'Chức vụ trong tổ chức Đoàn Thanh niên',
      items: [
        { id: 'cvdn01', code: 'CVDN01', name: 'Bí thư Đoàn', description: 'Bí thư Đoàn cơ sở', active: true },
        { id: 'cvdn02', code: 'CVDN02', name: 'Phó Bí thư Đoàn', description: 'Phó Bí thư Đoàn', active: true },
        { id: 'cvdn03', code: 'CVDN03', name: 'Ủy viên BCH Đoàn', description: 'Ủy viên Ban Chấp hành Đoàn', active: true },
        { id: 'cvdn04', code: 'CVDN04', name: 'Chi đoàn trưởng', description: 'Chi đoàn trưởng', active: true },
        { id: 'cvdn05', code: 'CVDN05', name: 'Đoàn viên', description: 'Đoàn viên thường', active: true }
      ]
    },
    {
      id: 'dm_cong_viec_cm',
      code: 'DM10',
      name: 'Công việc chuyên môn đảm nhiệm',
      group: 'system',
      icon: '💼',
      description: 'Loại công việc chuyên môn đang đảm nhiệm',
      items: [
        { id: 'cv01', code: 'CV01', name: 'Quản lý', description: 'Công tác quản lý, điều hành', active: true },
        { id: 'cv02', code: 'CV02', name: 'Kỹ thuật', description: 'Công tác kỹ thuật chuyên môn', active: true },
        { id: 'cv03', code: 'CV03', name: 'Nghiên cứu', description: 'Nghiên cứu khoa học', active: true },
        { id: 'cv04', code: 'CV04', name: 'Giảng dạy', description: 'Giảng dạy, đào tạo', active: true },
        { id: 'cv05', code: 'CV05', name: 'Hành chính - Văn phòng', description: 'Công tác hành chính', active: true },
        { id: 'cv06', code: 'CV06', name: 'Kế toán - Tài chính', description: 'Công tác kế toán, tài chính', active: true },
        { id: 'cv07', code: 'CV07', name: 'Kinh doanh - Bán hàng', description: 'Kinh doanh thương mại', active: true },
        { id: 'cv08', code: 'CV08', name: 'Sản xuất', description: 'Trực tiếp sản xuất', active: true },
        { id: 'cv09', code: 'CV09', name: 'Bảo vệ - An ninh', description: 'Bảo vệ, an ninh', active: true },
        { id: 'cv10', code: 'CV10', name: 'Lái xe', description: 'Lái xe, vận tải', active: true }
      ]
    },
    {
      id: 'dm_nghe_truoc_td',
      code: 'DM11',
      name: 'Ngành nghề trước tuyển dụng',
      group: 'system',
      icon: '🔧',
      description: 'Nghề nghiệp trước khi được tuyển dụng',
      items: [
        { id: 'nn01', code: 'NN01', name: 'Sinh viên mới tốt nghiệp', description: 'Mới ra trường, chưa có kinh nghiệm', active: true },
        { id: 'nn02', code: 'NN02', name: 'Công nhân', description: 'Công nhân các ngành', active: true },
        { id: 'nn03', code: 'NN03', name: 'Kỹ sư', description: 'Kỹ sư kỹ thuật', active: true },
        { id: 'nn04', code: 'NN04', name: 'Nhân viên văn phòng', description: 'Làm việc văn phòng', active: true },
        { id: 'nn05', code: 'NN05', name: 'Giáo viên', description: 'Giáo viên, giảng viên', active: true },
        { id: 'nn06', code: 'NN06', name: 'Bộ đội xuất ngũ', description: 'Quân nhân xuất ngũ', active: true },
        { id: 'nn07', code: 'NN07', name: 'Nông dân', description: 'Nông dân, lao động nông thôn', active: true },
        { id: 'nn08', code: 'NN08', name: 'Tự do', description: 'Lao động tự do', active: true },
        { id: 'nn09', code: 'NN09', name: 'Kinh doanh tự do', description: 'Tự kinh doanh', active: true }
      ]
    },
    {
      id: 'dm_hoc_van_pt',
      code: 'DM12',
      name: 'Trình độ học vấn phổ thông',
      group: 'system',
      icon: '📚',
      description: 'Trình độ học vấn phổ thông',
      items: [
        { id: 'hv01', code: 'HV01', name: '12/12', description: 'Tốt nghiệp THPT (lớp 12)', active: true },
        { id: 'hv02', code: 'HV02', name: '10/12', description: 'Học đến lớp 10', active: true },
        { id: 'hv03', code: 'HV03', name: '9/12', description: 'Tốt nghiệp THCS (lớp 9)', active: true },
        { id: 'hv04', code: 'HV04', name: '5/12', description: 'Tốt nghiệp Tiểu học (lớp 5)', active: true },
        { id: 'hv05', code: 'HV05', name: 'Chưa tốt nghiệp tiểu học', description: 'Chưa hoàn thành tiểu học', active: true },
        { id: 'hv06', code: 'HV06', name: 'Bổ túc THPT', description: 'Bổ túc văn hóa cấp 3', active: true }
      ]
    },

    // ===== NHÓM 2: DANH MỤC CHUYÊN MÔN =====
    {
      id: 'dm_trinh_do_cm',
      code: 'DM13',
      name: 'Trình độ chuyên môn',
      group: 'professional',
      icon: '🎓',
      description: 'Trình độ chuyên môn được đào tạo',
      items: [
        { id: 'cm01', code: 'CM01', name: 'Tiến sĩ', description: 'Tiến sĩ khoa học', active: true },
        { id: 'cm02', code: 'CM02', name: 'Thạc sĩ', description: 'Thạc sĩ', active: true },
        { id: 'cm03', code: 'CM03', name: 'Đại học', description: 'Cử nhân, Kỹ sư', active: true },
        { id: 'cm04', code: 'CM04', name: 'Cao đẳng', description: 'Cao đẳng nghề', active: true },
        { id: 'cm05', code: 'CM05', name: 'Trung cấp', description: 'Trung cấp chuyên nghiệp', active: true },
        { id: 'cm06', code: 'CM06', name: 'Sơ cấp nghề', description: 'Sơ cấp nghề', active: true },
        { id: 'cm07', code: 'CM07', name: 'Chưa qua đào tạo', description: 'Chưa qua đào tạo chuyên môn', active: true },
        { id: 'cm08', code: 'CM08', name: 'Tiến sĩ Khoa học', description: 'Tiến sĩ Khoa học (TSKH)', active: true }
      ]
    },
    {
      id: 'dm_nganh_dao_tao',
      code: 'DM14',
      name: 'Ngành đào tạo',
      group: 'professional',
      icon: '📖',
      description: 'Các ngành nghề đào tạo chuyên môn',
      items: [
        { id: 'ndt01', code: 'NDT01', name: 'Công nghệ Thực phẩm', description: 'Ngành Công nghệ Thực phẩm', active: true },
        { id: 'ndt02', code: 'NDT02', name: 'Công nghệ thông tin', description: 'Ngành CNTT, phần mềm', active: true },
        { id: 'ndt03', code: 'NDT03', name: 'Kế toán', description: 'Ngành Kế toán - Kiểm toán', active: true },
        { id: 'ndt04', code: 'NDT04', name: 'Quản trị Kinh doanh', description: 'Ngành QTKD', active: true },
        { id: 'ndt05', code: 'NDT05', name: 'Quản trị Nhân lực', description: 'Ngành Quản trị Nhân sự', active: true },
        { id: 'ndt06', code: 'NDT06', name: 'Kỹ thuật Cơ khí', description: 'Ngành Cơ khí chế tạo', active: true },
        { id: 'ndt07', code: 'NDT07', name: 'Kỹ thuật Điện', description: 'Ngành Kỹ thuật Điện - Điện tử', active: true },
        { id: 'ndt08', code: 'NDT08', name: 'Thú y', description: 'Ngành Thú y', active: true },
        { id: 'ndt09', code: 'NDT09', name: 'Chăn nuôi', description: 'Ngành Chăn nuôi', active: true },
        { id: 'ndt10', code: 'NDT10', name: 'Marketing', description: 'Ngành Marketing, truyền thông', active: true },
        { id: 'ndt11', code: 'NDT11', name: 'Luật', description: 'Ngành Luật', active: true },
        { id: 'ndt12', code: 'NDT12', name: 'Tài chính - Ngân hàng', description: 'Ngành Tài chính', active: true },
        { id: 'ndt13', code: 'NDT13', name: 'Hóa học', description: 'Ngành Hóa học, Hóa Phân tích', active: true },
        { id: 'ndt14', code: 'NDT14', name: 'Sinh học', description: 'Ngành Sinh học, Công nghệ Sinh học', active: true },
        { id: 'ndt15', code: 'NDT15', name: 'Ngoại thương', description: 'Ngành Ngoại thương, Xuất nhập khẩu', active: true },
        { id: 'ndt16', code: 'NDT16', name: 'Nông nghiệp', description: 'Ngành Nông nghiệp', active: true }
      ]
    },
    {
      id: 'dm_truong_dao_tao',
      code: 'DM15',
      name: 'Trường đào tạo',
      group: 'professional',
      icon: '🏫',
      description: 'Danh mục các trường đào tạo',
      items: [
        { id: 'tr01', code: 'TR01', name: 'ĐH Bách Khoa Hà Nội', description: 'Đại học Bách khoa Hà Nội', active: true },
        { id: 'tr02', code: 'TR02', name: 'ĐH Bách Khoa TP.HCM', description: 'Đại học Bách khoa TP.HCM', active: true },
        { id: 'tr03', code: 'TR03', name: 'ĐH Kinh tế Quốc dân', description: 'ĐH Kinh tế Quốc dân Hà Nội', active: true },
        { id: 'tr04', code: 'TR04', name: 'ĐH Kinh tế TP.HCM', description: 'Đại học Kinh tế TP.HCM', active: true },
        { id: 'tr05', code: 'TR05', name: 'ĐH Nông Lâm TP.HCM', description: 'ĐH Nông Lâm TP.HCM', active: true },
        { id: 'tr06', code: 'TR06', name: 'ĐH Nông nghiệp HN', description: 'Học viện Nông nghiệp Việt Nam', active: true },
        { id: 'tr07', code: 'TR07', name: 'ĐH FPT', description: 'Đại học FPT', active: true },
        { id: 'tr08', code: 'TR08', name: 'ĐH KHTN TP.HCM', description: 'ĐH Khoa học Tự nhiên TP.HCM', active: true },
        { id: 'tr09', code: 'TR09', name: 'ĐH Đà Lạt', description: 'Đại học Đà Lạt', active: true },
        { id: 'tr10', code: 'TR10', name: 'ĐH Vinh', description: 'Đại học Vinh', active: true },
        { id: 'tr11', code: 'TR11', name: 'ĐH Cần Thơ', description: 'Đại học Cần Thơ', active: true },
        { id: 'tr12', code: 'TR12', name: 'ĐH Ngoại thương', description: 'Đại học Ngoại thương', active: true },
        { id: 'tr13', code: 'TR13', name: 'ĐH RMIT', description: 'Đại học RMIT Việt Nam', active: true },
        { id: 'tr14', code: 'TR14', name: 'ĐH Luật TP.HCM', description: 'Đại học Luật TP.HCM', active: true },
        { id: 'tr15', code: 'TR15', name: 'ĐH Bách Khoa Đà Nẵng', description: 'ĐH Bách khoa - ĐH Đà Nẵng', active: true }
      ]
    },
    {
      id: 'dm_hinh_thuc_dt',
      code: 'DM16',
      name: 'Hình thức đào tạo',
      group: 'professional',
      icon: '📝',
      description: 'Hình thức đào tạo chuyên môn',
      items: [
        { id: 'ht01', code: 'HT01', name: 'Chính quy', description: 'Đào tạo chính quy tập trung', active: true },
        { id: 'ht02', code: 'HT02', name: 'Tại chức', description: 'Đào tạo tại chức (vừa học vừa làm)', active: true },
        { id: 'ht03', code: 'HT03', name: 'Từ xa', description: 'Đào tạo từ xa', active: true },
        { id: 'ht04', code: 'HT04', name: 'Liên thông', description: 'Đào tạo liên thông', active: true },
        { id: 'ht05', code: 'HT05', name: 'Văn bằng 2', description: 'Đào tạo văn bằng hai', active: true },
        { id: 'ht06', code: 'HT06', name: 'Bồi dưỡng ngắn hạn', description: 'Khóa bồi dưỡng ngắn hạn', active: true }
      ]
    },
    {
      id: 'dm_ly_luan_ct',
      code: 'DM17',
      name: 'Trình độ lý luận chính trị',
      group: 'professional',
      icon: '🏛️',
      description: 'Trình độ lý luận chính trị',
      items: [
        { id: 'll01', code: 'LL01', name: 'Chưa qua đào tạo', description: 'Chưa học lý luận chính trị', active: true },
        { id: 'll02', code: 'LL02', name: 'Sơ cấp', description: 'Sơ cấp lý luận chính trị', active: true },
        { id: 'll03', code: 'LL03', name: 'Trung cấp', description: 'Trung cấp lý luận chính trị', active: true },
        { id: 'll04', code: 'LL04', name: 'Cao cấp', description: 'Cao cấp lý luận chính trị', active: true },
        { id: 'll05', code: 'LL05', name: 'Cử nhân', description: 'Cử nhân chính trị', active: true }
      ]
    },
    {
      id: 'dm_ql_nha_nuoc',
      code: 'DM18',
      name: 'Trình độ quản lý nhà nước',
      group: 'professional',
      icon: '🏦',
      description: 'Trình độ quản lý hành chính nhà nước',
      items: [
        { id: 'ql01', code: 'QL01', name: 'Chưa qua đào tạo', description: 'Chưa qua lớp QLNN', active: true },
        { id: 'ql02', code: 'QL02', name: 'Sơ cấp', description: 'Bồi dưỡng ngạch cán sự', active: true },
        { id: 'ql03', code: 'QL03', name: 'Chuyên viên', description: 'Bồi dưỡng ngạch chuyên viên', active: true },
        { id: 'ql04', code: 'QL04', name: 'Chuyên viên chính', description: 'Bồi dưỡng ngạch chuyên viên chính', active: true },
        { id: 'ql05', code: 'QL05', name: 'Chuyên viên cao cấp', description: 'Bồi dưỡng ngạch chuyên viên cao cấp', active: true }
      ]
    },
    {
      id: 'dm_ql_kinh_te',
      code: 'DM19',
      name: 'Trình độ quản lý kinh tế',
      group: 'professional',
      icon: '📊',
      description: 'Trình độ quản lý kinh tế',
      items: [
        { id: 'kt01', code: 'KT01', name: 'Chưa qua đào tạo', description: 'Chưa qua lớp QLKT', active: true },
        { id: 'kt02', code: 'KT02', name: 'Đã qua đào tạo', description: 'Đã hoàn thành khóa QLKT', active: true },
        { id: 'kt03', code: 'KT03', name: 'Trung cấp', description: 'Trung cấp quản lý kinh tế', active: true },
        { id: 'kt04', code: 'KT04', name: 'Cao cấp', description: 'Cao cấp quản lý kinh tế', active: true }
      ]
    },
    {
      id: 'dm_trinh_do_th',
      code: 'DM20',
      name: 'Trình độ tin học',
      group: 'professional',
      icon: '💻',
      description: 'Trình độ tin học, vi tính',
      items: [
        { id: 'th01', code: 'TH01', name: 'Chưa biết sử dụng', description: 'Chưa sử dụng máy tính', active: true },
        { id: 'th02', code: 'TH02', name: 'Tin học cơ bản', description: 'Sử dụng cơ bản Word, Excel', active: true },
        { id: 'th03', code: 'TH03', name: 'Tin học văn phòng', description: 'Thành thạo tin học văn phòng', active: true },
        { id: 'th04', code: 'TH04', name: 'Tin học nâng cao', description: 'Sử dụng nâng cao, phần mềm chuyên dụng', active: true },
        { id: 'th05', code: 'TH05', name: 'Kỹ sư CNTT', description: 'Kỹ sư Công nghệ thông tin', active: true },
        { id: 'th06', code: 'TH06', name: 'Chứng chỉ quốc tế (IC3/MOS)', description: 'Có chứng chỉ tin học quốc tế', active: true }
      ]
    },
    {
      id: 'dm_ngoai_ngu',
      code: 'DM21',
      name: 'Ngoại ngữ',
      group: 'professional',
      icon: '🌐',
      description: 'Danh mục các ngoại ngữ',
      items: [
        { id: 'nn01a', code: 'NN01', name: 'Tiếng Anh', description: 'English', active: true },
        { id: 'nn02a', code: 'NN02', name: 'Tiếng Pháp', description: 'Français', active: true },
        { id: 'nn03a', code: 'NN03', name: 'Tiếng Trung Quốc', description: '中文', active: true },
        { id: 'nn04a', code: 'NN04', name: 'Tiếng Nhật', description: '日本語', active: true },
        { id: 'nn05a', code: 'NN05', name: 'Tiếng Hàn', description: '한국어', active: true },
        { id: 'nn06a', code: 'NN06', name: 'Tiếng Nga', description: 'Русский', active: true },
        { id: 'nn07a', code: 'NN07', name: 'Tiếng Đức', description: 'Deutsch', active: true },
        { id: 'nn08a', code: 'NN08', name: 'Tiếng Tây Ban Nha', description: 'Español', active: true },
        { id: 'nn09a', code: 'NN09', name: 'Tiếng Thái', description: 'ภาษาไทย', active: true }
      ]
    },
    {
      id: 'dm_trinh_do_nn',
      code: 'DM22',
      name: 'Trình độ ngoại ngữ',
      group: 'professional',
      icon: '🗣️',
      description: 'Trình độ ngoại ngữ theo khung tham chiếu châu Âu',
      items: [
        { id: 'tdnn01', code: 'TDNN01', name: 'A1 - Sơ cấp', description: 'Beginner - Người bắt đầu', active: true },
        { id: 'tdnn02', code: 'TDNN02', name: 'A2 - Sơ trung cấp', description: 'Elementary - Sơ trung cấp', active: true },
        { id: 'tdnn03', code: 'TDNN03', name: 'B1 - Trung cấp', description: 'Intermediate - Trung cấp', active: true },
        { id: 'tdnn04', code: 'TDNN04', name: 'B2 - Trung cao cấp', description: 'Upper-Intermediate', active: true },
        { id: 'tdnn05', code: 'TDNN05', name: 'C1 - Cao cấp', description: 'Advanced - Cao cấp', active: true },
        { id: 'tdnn06', code: 'TDNN06', name: 'C2 - Thành thạo', description: 'Proficiency - Thành thạo', active: true },
        { id: 'tdnn07', code: 'TDNN07', name: 'Chưa qua đào tạo', description: 'Chưa học ngoại ngữ', active: true }
      ]
    },
    {
      id: 'dm_quan_ham',
      code: 'DM23',
      name: 'Quân hàm',
      group: 'professional',
      icon: '🎖️',
      description: 'Danh mục quân hàm trong quân đội',
      items: [
        { id: 'qh01', code: 'QH01', name: 'Binh nhì', description: 'Binh nhì', active: true },
        { id: 'qh02', code: 'QH02', name: 'Binh nhất', description: 'Binh nhất', active: true },
        { id: 'qh03', code: 'QH03', name: 'Hạ sĩ', description: 'Hạ sĩ', active: true },
        { id: 'qh04', code: 'QH04', name: 'Trung sĩ', description: 'Trung sĩ', active: true },
        { id: 'qh05', code: 'QH05', name: 'Thượng sĩ', description: 'Thượng sĩ', active: true },
        { id: 'qh06', code: 'QH06', name: 'Thiếu úy', description: 'Thiếu úy', active: true },
        { id: 'qh07', code: 'QH07', name: 'Trung úy', description: 'Trung úy', active: true },
        { id: 'qh08', code: 'QH08', name: 'Thượng úy', description: 'Thượng úy', active: true },
        { id: 'qh09', code: 'QH09', name: 'Đại úy', description: 'Đại úy', active: true },
        { id: 'qh10', code: 'QH10', name: 'Thiếu tá', description: 'Thiếu tá', active: true },
        { id: 'qh11', code: 'QH11', name: 'Trung tá', description: 'Trung tá', active: true },
        { id: 'qh12', code: 'QH12', name: 'Thượng tá', description: 'Thượng tá', active: true },
        { id: 'qh13', code: 'QH13', name: 'Đại tá', description: 'Đại tá', active: true }
      ]
    },
    {
      id: 'dm_chuc_vu_vt',
      code: 'DM24',
      name: 'Chức vụ trong lực lượng vũ trang',
      group: 'professional',
      icon: '🪖',
      description: 'Chức vụ trong quân đội, công an',
      items: [
        { id: 'vt01', code: 'VT01', name: 'Tiểu đội trưởng', description: 'Chỉ huy tiểu đội', active: true },
        { id: 'vt02', code: 'VT02', name: 'Trung đội trưởng', description: 'Chỉ huy trung đội', active: true },
        { id: 'vt03', code: 'VT03', name: 'Đại đội trưởng', description: 'Chỉ huy đại đội', active: true },
        { id: 'vt04', code: 'VT04', name: 'Tiểu đoàn trưởng', description: 'Chỉ huy tiểu đoàn', active: true },
        { id: 'vt05', code: 'VT05', name: 'Trung đoàn trưởng', description: 'Chỉ huy trung đoàn', active: true },
        { id: 'vt06', code: 'VT06', name: 'Chiến sĩ', description: 'Chiến sĩ, binh sĩ', active: true }
      ]
    },
    {
      id: 'dm_danh_hieu',
      code: 'DM25',
      name: 'Danh hiệu được phong',
      group: 'professional',
      icon: '🏆',
      description: 'Các danh hiệu, học hàm, học vị được phong tặng',
      items: [
        { id: 'dh01', code: 'DH01', name: 'Giáo sư', description: 'Giáo sư (GS)', active: true },
        { id: 'dh02', code: 'DH02', name: 'Phó Giáo sư', description: 'Phó Giáo sư (PGS)', active: true },
        { id: 'dh03', code: 'DH03', name: 'Nghệ sĩ Nhân dân', description: 'NSND', active: true },
        { id: 'dh04', code: 'DH04', name: 'Nghệ sĩ Ưu tú', description: 'NSƯT', active: true },
        { id: 'dh05', code: 'DH05', name: 'Nhà giáo Nhân dân', description: 'NGND', active: true },
        { id: 'dh06', code: 'DH06', name: 'Nhà giáo Ưu tú', description: 'NGƯT', active: true },
        { id: 'dh07', code: 'DH07', name: 'Thầy thuốc Nhân dân', description: 'TTND', active: true },
        { id: 'dh08', code: 'DH08', name: 'Thầy thuốc Ưu tú', description: 'TTƯT', active: true },
        { id: 'dh09', code: 'DH09', name: 'Anh hùng Lao động', description: 'Anh hùng Lao động thời kỳ đổi mới', active: true },
        { id: 'dh10', code: 'DH10', name: 'Chiến sĩ thi đua toàn quốc', description: 'CSTTQG', active: true }
      ]
    },
    {
      id: 'dm_ngach_cc',
      code: 'DM26',
      name: 'Ngạch công chức',
      group: 'professional',
      icon: '📋',
      description: 'Ngạch bậc công chức, viên chức',
      items: [
        { id: 'nc01', code: 'NC01', name: 'Chuyên viên cao cấp', description: 'Ngạch chuyên viên cao cấp', active: true },
        { id: 'nc02', code: 'NC02', name: 'Chuyên viên chính', description: 'Ngạch chuyên viên chính', active: true },
        { id: 'nc03', code: 'NC03', name: 'Chuyên viên', description: 'Ngạch chuyên viên', active: true },
        { id: 'nc04', code: 'NC04', name: 'Cán sự', description: 'Ngạch cán sự', active: true },
        { id: 'nc05', code: 'NC05', name: 'Nhân viên', description: 'Ngạch nhân viên', active: true },
        { id: 'nc06', code: 'NC06', name: 'Kế toán viên cao cấp', description: 'Ngạch kế toán viên cao cấp', active: true },
        { id: 'nc07', code: 'NC07', name: 'Kế toán viên chính', description: 'Ngạch kế toán viên chính', active: true },
        { id: 'nc08', code: 'NC08', name: 'Kế toán viên', description: 'Ngạch kế toán viên', active: true }
      ]
    },
    {
      id: 'dm_khen_thuong_ky_luat',
      code: 'DM27',
      name: 'Hình thức khen thưởng, kỷ luật',
      group: 'professional',
      icon: '🏅',
      description: 'Các hình thức khen thưởng và kỷ luật',
      items: [
        { id: 'ktkl01', code: 'KT01', name: 'Giấy khen', description: 'Giấy khen cấp đơn vị', active: true },
        { id: 'ktkl02', code: 'KT02', name: 'Bằng khen', description: 'Bằng khen cấp Bộ, Tỉnh', active: true },
        { id: 'ktkl03', code: 'KT03', name: 'Huân chương', description: 'Huân chương các loại', active: true },
        { id: 'ktkl04', code: 'KT04', name: 'Huy chương', description: 'Huy chương các loại', active: true },
        { id: 'ktkl05', code: 'KT05', name: 'Chiến sĩ thi đua', description: 'Chiến sĩ thi đua cấp cơ sở', active: true },
        { id: 'ktkl06', code: 'KT06', name: 'Lao động tiên tiến', description: 'Danh hiệu Lao động tiên tiến', active: true },
        { id: 'ktkl07', code: 'KL01', name: 'Khiển trách', description: 'Hình thức kỷ luật khiển trách', active: true },
        { id: 'ktkl08', code: 'KL02', name: 'Cảnh cáo', description: 'Hình thức kỷ luật cảnh cáo', active: true },
        { id: 'ktkl09', code: 'KL03', name: 'Hạ bậc lương', description: 'Hình thức kỷ luật hạ bậc lương', active: true },
        { id: 'ktkl10', code: 'KL04', name: 'Buộc thôi việc', description: 'Hình thức kỷ luật buộc thôi việc', active: true },
        { id: 'ktkl11', code: 'KL05', name: 'Cách chức', description: 'Hình thức kỷ luật cách chức', active: true }
      ]
    },
    {
      id: 'dm_suc_khoe',
      code: 'DM28',
      name: 'Tình trạng sức khỏe',
      group: 'professional',
      icon: '🏥',
      description: 'Phân loại tình trạng sức khỏe theo khám định kỳ',
      items: [
        { id: 'sk01', code: 'SK01', name: 'Loại I - Rất khỏe', description: 'Sức khỏe rất tốt, không bệnh', active: true },
        { id: 'sk02', code: 'SK02', name: 'Loại II - Khỏe', description: 'Sức khỏe tốt', active: true },
        { id: 'sk03', code: 'SK03', name: 'Loại III - Trung bình', description: 'Sức khỏe trung bình', active: true },
        { id: 'sk04', code: 'SK04', name: 'Loại IV - Yếu', description: 'Sức khỏe yếu', active: true },
        { id: 'sk05', code: 'SK05', name: 'Loại V - Kém', description: 'Sức khỏe kém, cần điều trị', active: true }
      ]
    },
    {
      id: 'dm_thuong_binh',
      code: 'DM29',
      name: 'Hạng thương binh',
      group: 'professional',
      icon: '🎗️',
      description: 'Phân loại hạng thương binh',
      items: [
        { id: 'tb01', code: 'TB01', name: 'Hạng 1/4 (81-100%)', description: 'Thương tật từ 81% đến 100%', active: true },
        { id: 'tb02', code: 'TB02', name: 'Hạng 2/4 (61-80%)', description: 'Thương tật từ 61% đến 80%', active: true },
        { id: 'tb03', code: 'TB03', name: 'Hạng 3/4 (41-60%)', description: 'Thương tật từ 41% đến 60%', active: true },
        { id: 'tb04', code: 'TB04', name: 'Hạng 4/4 (21-40%)', description: 'Thương tật từ 21% đến 40%', active: true },
        { id: 'tb05', code: 'TB05', name: 'Không là thương binh', description: 'Không thuộc diện thương binh', active: true }
      ]
    },
    {
      id: 'dm_quoc_gia',
      code: 'DM30',
      name: 'Các nước trên thế giới',
      group: 'professional',
      icon: '🌍',
      description: 'Danh mục quốc gia, vùng lãnh thổ',
      items: [
        { id: 'qg01', code: 'VN', name: 'Việt Nam', description: 'Cộng hòa Xã hội Chủ nghĩa Việt Nam', active: true },
        { id: 'qg02', code: 'US', name: 'Hoa Kỳ', description: 'United States of America', active: true },
        { id: 'qg03', code: 'JP', name: 'Nhật Bản', description: 'Japan', active: true },
        { id: 'qg04', code: 'KR', name: 'Hàn Quốc', description: 'South Korea', active: true },
        { id: 'qg05', code: 'CN', name: 'Trung Quốc', description: 'China', active: true },
        { id: 'qg06', code: 'TH', name: 'Thái Lan', description: 'Thailand', active: true },
        { id: 'qg07', code: 'SG', name: 'Singapore', description: 'Singapore', active: true },
        { id: 'qg08', code: 'AU', name: 'Úc', description: 'Australia', active: true },
        { id: 'qg09', code: 'DE', name: 'Đức', description: 'Germany', active: true },
        { id: 'qg10', code: 'FR', name: 'Pháp', description: 'France', active: true },
        { id: 'qg11', code: 'GB', name: 'Anh', description: 'United Kingdom', active: true },
        { id: 'qg12', code: 'CA', name: 'Canada', description: 'Canada', active: true },
        { id: 'qg13', code: 'NZ', name: 'New Zealand', description: 'New Zealand', active: true },
        { id: 'qg14', code: 'LA', name: 'Lào', description: 'Laos', active: true },
        { id: 'qg15', code: 'KH', name: 'Campuchia', description: 'Cambodia', active: true }
      ]
    },
    {
      id: 'dm_linh_vuc_ct',
      code: 'DM31',
      name: 'Lĩnh vực công tác',
      group: 'professional',
      icon: '🗂️',
      description: 'Phân loại lĩnh vực công tác',
      items: [
        { id: 'lv01', code: 'LV01', name: 'Hành chính - Quản lý', description: 'Công tác hành chính, quản lý', active: true },
        { id: 'lv02', code: 'LV02', name: 'Sản xuất', description: 'Trực tiếp sản xuất', active: true },
        { id: 'lv03', code: 'LV03', name: 'Kinh doanh - Thương mại', description: 'Kinh doanh, bán hàng', active: true },
        { id: 'lv04', code: 'LV04', name: 'Tài chính - Kế toán', description: 'Tài chính, kế toán, kiểm toán', active: true },
        { id: 'lv05', code: 'LV05', name: 'Nghiên cứu - Phát triển', description: 'R&D, nghiên cứu khoa học', active: true },
        { id: 'lv06', code: 'LV06', name: 'Công nghệ thông tin', description: 'CNTT, phần mềm', active: true },
        { id: 'lv07', code: 'LV07', name: 'Nhân sự - Đào tạo', description: 'Quản lý nhân sự, đào tạo', active: true },
        { id: 'lv08', code: 'LV08', name: 'Marketing - Truyền thông', description: 'Marketing, quảng cáo', active: true },
        { id: 'lv09', code: 'LV09', name: 'Pháp luật', description: 'Pháp lý, tuân thủ', active: true },
        { id: 'lv10', code: 'LV10', name: 'Kỹ thuật - Bảo trì', description: 'Kỹ thuật, bảo trì thiết bị', active: true },
        { id: 'lv11', code: 'LV11', name: 'Chất lượng', description: 'Quản lý chất lượng, QA/QC', active: true },
        { id: 'lv12', code: 'LV12', name: 'Logistics - Vận tải', description: 'Logistics, kho bãi, vận chuyển', active: true },
        { id: 'lv13', code: 'LV13', name: 'Chăn nuôi - Nông nghiệp', description: 'Trang trại, chăn nuôi', active: true }
      ]
    }
  ]
};
