<!DOCTYPE html>
<html lang="vi" class="h-full bg-slate-50">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta name="csrf-token" content="{{ csrf_token() }}">
    <title>@yield('title', 'Hệ Thống Quản Trị Nhân Lực Doanh Nghiệp — Enterprise HRM')</title>

    <!-- Google Fonts: Inter -->
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">

    <!-- Bootstrap Icons -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css">

    <!-- Tailwind CSS (Play CDN với Corporate Enterprise Theme) -->
    <script src="https://cdn.tailwindcss.com"></script>
    <script>
        tailwind.config = {
            theme: {
                extend: {
                    fontFamily: {
                        sans: ['Inter', 'system-ui', '-apple-system', 'sans-serif'],
                    },
                    colors: {
                        brand: {
                            50: '#eef4ff',
                            100: '#d9e6ff',
                            200: '#bcd3ff',
                            500: '#1e60f0',
                            600: '#0047cc',
                            700: '#003399', // Màu chủ đạo Doanh nghiệp
                            800: '#002573',
                            900: '#0a1a44',
                        },
                        status: {
                            approved: '#059669', // Xanh lá = Đã duyệt
                            pending:  '#d97706', // Vàng = Chờ duyệt
                            rejected: '#dc2626', // Đỏ = Từ chối / Trễ hạn
                        }
                    }
                }
            }
        }
    </script>
    <style>
        /* Đảm bảo cỡ chữ tối thiểu 14px và độ tương phản WCAG AA */
        body {
            font-size: 14px;
            color: #1e293b;
            background-color: #f8fafc;
        }
        /* Custom scrollbar */
        ::-webkit-scrollbar {
            width: 6px;
            height: 6px;
        }
        ::-webkit-scrollbar-track {
            background: #f1f5f9;
        }
        ::-webkit-scrollbar-thumb {
            background: #cbd5e1;
            border-radius: 4px;
        }
        ::-webkit-scrollbar-thumb:hover {
            background: #94a3b8;
        }
    </style>
</head>
<body class="h-full flex flex-col font-sans antialiased text-slate-800">
    <div class="min-h-full flex flex-col lg:flex-row">

        <!-- =================================================================== -->
        <!-- 1. SIDEBAR THEO PHÂN HỆ (DESKTOP + MOBILE DRAWER)                  -->
        <!-- =================================================================== -->
        <aside id="sidebar" class="fixed inset-y-0 left-0 z-50 w-72 bg-brand-900 text-slate-200 transform -translate-x-full lg:translate-x-0 transition-transform duration-200 ease-in-out flex flex-col shadow-xl">
            <!-- Header Sidebar & Logo Placeholder -->
            <div class="h-16 flex items-center justify-between px-6 bg-brand-800 border-b border-brand-700/50">
                <div class="flex items-center gap-3">
                    <!-- KHUNG LOGO DOANH NGHIỆP TỐI GIẢN (CHỖ TRỐNG CHO LOGO THEO YÊU CẦU) -->
                    <div class="w-10 h-10 rounded-lg border-2 border-dashed border-white/50 bg-white/10 flex items-center justify-center text-xs font-bold text-white tracking-widest" title="Chỗ trống cho Logo Doanh nghiệp">
                        LOGO
                    </div>
                    <div>
                        <div class="text-sm font-bold text-white tracking-wide">ENTERPRISE HRM</div>
                        <div class="text-[11px] text-brand-200 tracking-wider uppercase font-semibold">Tập đoàn Sữa</div>
                    </div>
                </div>
                <!-- Nút đóng trên Mobile -->
                <button onclick="toggleMobileSidebar()" class="lg:hidden text-white/80 hover:text-white p-1">
                    <i class="bi bi-x-lg text-lg"></i>
                </button>
            </div>

            <!-- Menu Phân hệ (Sidebar Navigation) -->
            <nav class="flex-1 overflow-y-auto px-4 py-4 space-y-1">
                <!-- 1. Tổng quan -->
                <a href="{{ route('dashboard') ?? url('/dashboard') }}" class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors {{ request()->is('dashboard') ? 'bg-brand-700 text-white shadow-sm' : 'text-slate-300 hover:bg-white/10 hover:text-white' }}">
                    <i class="bi bi-grid-1x2 text-base w-5 text-center"></i>
                    <span>Bảng Điều Hành (Dashboard)</span>
                </a>

                <!-- 2. Hộp thư phê duyệt tập trung -->
                <a href="{{ route('approvals.inbox') ?? url('/approvals/inbox') }}" class="flex items-center justify-between px-3 py-2.5 rounded-lg text-sm font-medium transition-colors {{ request()->is('approvals*') ? 'bg-brand-700 text-white shadow-sm' : 'text-slate-300 hover:bg-white/10 hover:text-white' }}">
                    <div class="flex items-center gap-3">
                        <i class="bi bi-inbox text-base w-5 text-center"></i>
                        <span>Hộp Thư Phê Duyệt</span>
                    </div>
                    <span class="inline-flex items-center justify-center px-2 py-0.5 text-xs font-bold bg-amber-500 text-white rounded-full">
                        {{ $pendingCount ?? 6 }}
                    </span>
                </a>

                <div class="pt-3 pb-1 px-3 text-[11px] font-bold uppercase tracking-wider text-slate-400">Quản Trị Nhân Sự</div>

                <!-- 3. Cây Tổ chức -->
                <a href="{{ url('/organization/chart') }}" class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors {{ request()->is('organization*') ? 'bg-brand-700 text-white' : 'text-slate-300 hover:bg-white/10 hover:text-white' }}">
                    <i class="bi bi-diagram-3 text-base w-5 text-center"></i>
                    <span>Cơ Cấu Tổ Chức</span>
                </a>

                <!-- 4. Danh sách Nhân sự -->
                <a href="{{ url('/employees') }}" class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors {{ request()->is('employees*') ? 'bg-brand-700 text-white' : 'text-slate-300 hover:bg-white/10 hover:text-white' }}">
                    <i class="bi bi-people text-base w-5 text-center"></i>
                    <span>Hồ Sơ Nhân Sự (HRM)</span>
                </a>

                <!-- 5. Tuyển dụng ATS -->
                <a href="{{ url('/recruitment/kanban') }}" class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors {{ request()->is('recruitment*') ? 'bg-brand-700 text-white' : 'text-slate-300 hover:bg-white/10 hover:text-white' }}">
                    <i class="bi bi-kanban text-base w-5 text-center"></i>
                    <span>Tuyển Dụng (ATS Pipeline)</span>
                </a>

                <div class="pt-3 pb-1 px-3 text-[11px] font-bold uppercase tracking-wider text-slate-400">Vận Hành & Chấm Công</div>

                <!-- 6. Ca kíp & Chấm công -->
                <a href="{{ url('/attendance/roster') }}" class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors {{ request()->is('attendance*') ? 'bg-brand-700 text-white' : 'text-slate-300 hover:bg-white/10 hover:text-white' }}">
                    <i class="bi bi-calendar-week text-base w-5 text-center"></i>
                    <span>Lịch Xếp Ca & Chấm Công</span>
                </a>

                <!-- 7. Nghỉ phép & Thôi việc -->
                <a href="{{ url('/leave') }}" class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors {{ request()->is('leave*') ? 'bg-brand-700 text-white' : 'text-slate-300 hover:bg-white/10 hover:text-white' }}">
                    <i class="bi bi-calendar2-check text-base w-5 text-center"></i>
                    <span>Nghỉ Phép & Thôi Việc</span>
                </a>

                <!-- 8. Đánh giá KPI -->
                <a href="{{ url('/kpi/kanban') }}" class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors {{ request()->is('kpi*') ? 'bg-brand-700 text-white' : 'text-slate-300 hover:bg-white/10 hover:text-white' }}">
                    <i class="bi bi-speedometer2 text-base w-5 text-center"></i>
                    <span>Đánh Giá Hiệu Suất (KPI)</span>
                </a>

                <div class="pt-3 pb-1 px-3 text-[11px] font-bold uppercase tracking-wider text-slate-400">Lương & Báo Cáo</div>

                <!-- 9. Tính Lương 3P -->
                <a href="{{ url('/payroll') }}" class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors {{ request()->is('payroll*') ? 'bg-brand-700 text-white' : 'text-slate-300 hover:bg-white/10 hover:text-white' }}">
                    <i class="bi bi-cash-stack text-base w-5 text-center"></i>
                    <span>Tính Lương 3P & Thuế</span>
                </a>

                <!-- 10. Báo cáo & Thống kê BM01-BM04 -->
                <a href="{{ route('reports.index') ?? url('/reports') }}" class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors {{ request()->is('reports*') ? 'bg-brand-700 text-white' : 'text-slate-300 hover:bg-white/10 hover:text-white' }}">
                    <i class="bi bi-file-earmark-bar-graph text-base w-5 text-center"></i>
                    <span>Trung Tâm Báo Cáo & BM01–04</span>
                </a>
            </nav>

            <!-- Chân Sidebar: Phiếu lương ESS & Trợ giúp -->
            <div class="p-4 bg-brand-800/80 border-t border-brand-700/50 text-xs">
                <a href="{{ url('/ess/payslip') }}" class="flex items-center gap-2 p-2 rounded-lg bg-white/10 hover:bg-white/20 text-white font-medium transition-colors">
                    <i class="bi bi-wallet2 text-amber-300 text-sm"></i>
                    <span>Tra Cứu Phiếu Lương ESS</span>
                </a>
            </div>
        </aside>

        <!-- Overlay backdrop cho mobile khi bật sidebar -->
        <div id="mobileBackdrop" onclick="toggleMobileSidebar()" class="fixed inset-0 bg-slate-900/50 z-40 lg:hidden hidden"></div>

        <!-- =================================================================== -->
        <!-- 2. NỘI DUNG CHÍNH & THANH ĐIỀU HƯỚNG TRÊN (TOPBAR)                  -->
        <!-- =================================================================== -->
        <div class="flex-1 flex flex-col lg:pl-72 min-w-0">

            <!-- Topbar Header -->
            <header class="h-16 bg-white border-b border-slate-200 flex items-center justify-between px-4 sm:px-6 sticky top-0 z-30 shadow-xs">
                <div class="flex items-center gap-4 flex-1">
                    <!-- Nút mở menu trên Mobile -->
                    <button onclick="toggleMobileSidebar()" class="lg:hidden p-2 text-slate-600 hover:text-brand-700 focus:outline-none">
                        <i class="bi bi-list text-2xl"></i>
                    </button>

                    <!-- BỘ CHỌN ĐƠN VỊ THEO PHẠM VI (ORG UNIT SCOPE SELECTOR) -->
                    <div class="relative w-72 sm:w-80">
                        <label for="topScopeSelect" class="sr-only">Phạm vi dữ liệu tổ chức</label>
                        <div class="flex items-center gap-2 bg-slate-100 hover:bg-slate-200/80 border border-slate-300 rounded-lg px-3 py-1.5 transition-colors cursor-pointer text-xs font-semibold text-slate-700">
                            <i class="bi bi-geo-alt-fill text-brand-700 text-sm"></i>
                            <select id="topScopeSelect" class="bg-transparent border-0 text-slate-700 text-xs font-semibold focus:ring-0 w-full cursor-pointer p-0" onchange="window.location.search = '?org_path=' + this.value">
                                <option value="/1/">Toàn Tập đoàn Vinamilk</option>
                                <option value="/1/2/">Khối Sản xuất — Nhà máy Mega Bình Dương</option>
                                <option value="/1/3/">Khối Vận hành — Nhà máy Sữa Thống Nhất</option>
                                <option value="/1/4/">Khối Vùng Nguyên liệu — Trang trại Bò sữa</option>
                                <option value="/1/5/">Khối Bán lẻ & Chuỗi Giấc Mơ Sữa</option>
                            </select>
                        </div>
                    </div>
                </div>

                <!-- Khu vực bên phải: Thông báo, Vai trò & Hồ sơ cá nhân -->
                <div class="flex items-center gap-3 sm:gap-4">
                    <!-- Chuông Thông báo -->
                    <div class="relative">
                        <button class="relative p-2 text-slate-600 hover:text-brand-700 hover:bg-slate-100 rounded-full transition-colors" title="Thông báo hệ thống">
                            <i class="bi bi-bell text-lg"></i>
                            <span class="absolute top-1.5 right-1.5 w-2.5 h-2.5 bg-red-500 rounded-full ring-2 ring-white"></span>
                        </button>
                    </div>

                    <!-- Vai trò người dùng (Role Badge) -->
                    <div class="hidden md:flex flex-col items-end">
                        <span class="text-xs font-bold text-slate-800">{{ auth()->user()->name ?? 'Nguyễn Quản Trị' }}</span>
                        <span class="text-[11px] text-brand-700 font-semibold bg-brand-50 px-2 py-0.5 rounded border border-brand-200">
                            {{ auth()->user()->role ?? 'HR Local / Giám đốc Nhà máy' }}
                        </span>
                    </div>

                    <!-- Ảnh đại diện & Menu cá nhân -->
                    <div class="relative">
                        <button onclick="toggleUserDropdown()" class="flex items-center gap-2 focus:outline-none">
                            <div class="w-9 h-9 rounded-full bg-brand-700 text-white flex items-center justify-center font-bold text-sm shadow-xs border border-white">
                                {{ substr(auth()->user()->name ?? 'HR', 0, 2) }}
                            </div>
                            <i class="bi bi-chevron-down text-slate-400 text-xs hidden sm:inline"></i>
                        </button>

                        <!-- Dropdown Menu -->
                        <div id="userDropdown" class="hidden absolute right-0 mt-2 w-48 bg-white rounded-lg shadow-lg border border-slate-200 py-1 z-50 text-xs">
                            <div class="px-4 py-2 border-b border-slate-100 font-semibold text-slate-700">Tài khoản Doanh nghiệp</div>
                            <a href="{{ url('/profile') }}" class="block px-4 py-2 text-slate-600 hover:bg-slate-50 hover:text-brand-700"><i class="bi bi-person me-2"></i> Thông tin cá nhân</a>
                            <a href="{{ url('/profile/security') }}" class="block px-4 py-2 text-slate-600 hover:bg-slate-50 hover:text-brand-700"><i class="bi bi-shield-lock me-2"></i> Đổi mật khẩu / 2FA</a>
                            <div class="border-t border-slate-100 my-1"></div>
                            <form method="POST" action="{{ route('logout') ?? url('/logout') }}">
                                @csrf
                                <button type="submit" class="w-full text-left px-4 py-2 text-red-600 hover:bg-red-50 font-semibold">
                                    <i class="bi bi-box-arrow-right me-2"></i> Đăng xuất
                                </button>
                            </form>
                        </div>
                    </div>
                </div>
            </header>

            <!-- Vùng hiển thị Nội dung Màn hình chính -->
            <main class="flex-1 p-4 sm:p-6 lg:p-8 max-w-7xl w-full mx-auto">
                @yield('content')
            </main>

            <!-- Footer Bản quyền Doanh nghiệp -->
            <footer class="bg-white border-t border-slate-200 py-4 px-6 text-center text-xs text-slate-500">
                <span>© {{ date('Y') }} Hệ Thống Quản Trị Nhân Lực Doanh Nghiệp (Enterprise HRM). Đạt chuẩn Bảo mật Dữ liệu ISO 27001 & WCAG AA.</span>
            </footer>
        </div>
    </div>

    <!-- Scripts điều khiển UI Layout -->
    <script>
        function toggleMobileSidebar() {
            const sidebar = document.getElementById('sidebar');
            const backdrop = document.getElementById('mobileBackdrop');
            sidebar.classList.toggle('-translate-x-full');
            backdrop.classList.toggle('hidden');
        }

        function toggleUserDropdown() {
            const dropdown = document.getElementById('userDropdown');
            dropdown.classList.toggle('hidden');
        }

        // Đóng dropdown khi click ngoài
        document.addEventListener('click', function(event) {
            const dropdown = document.getElementById('userDropdown');
            const button = event.target.closest('button');
            if (dropdown && !dropdown.contains(event.target) && (!button || !button.contains(event.target))) {
                dropdown.classList.add('hidden');
            }
        });
    </script>
</body>
</html>
