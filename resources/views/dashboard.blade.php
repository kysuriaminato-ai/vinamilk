@extends('layouts.app')

@section('title', 'Bảng Điều Hành Doanh Nghiệp — Enterprise Dashboard')

@section('content')
<div class="space-y-6">
    <!-- Tiêu đề & Lời chào điều hành -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 bg-white p-6 rounded-xl border border-slate-200 shadow-xs">
        <div>
            <div class="flex items-center gap-2">
                <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-brand-100 text-brand-800">
                    Phạm vi: Toàn Tập đoàn
                </span>
                <span class="text-xs text-slate-500">Hôm nay: {{ date('d/m/Y') }}</span>
            </div>
            <h1 class="text-2xl font-bold text-slate-900 mt-1">Xin chào, {{ auth()->user()->name ?? 'Ban Lãnh Đạo Tập Đoàn' }}</h1>
            <p class="text-sm text-slate-600 mt-0.5">Tổng quan tình hình nhân sự, công ca, quỹ lương và các công việc cần xử lý hôm nay.</p>
        </div>
        <div class="flex items-center gap-3">
            <a href="{{ route('approvals.inbox') ?? url('/approvals/inbox') }}" class="inline-flex items-center gap-2 px-4 py-2.5 bg-amber-500 hover:bg-amber-600 text-white rounded-lg text-sm font-semibold shadow-xs transition-colors">
                <i class="bi bi-inbox-fill"></i>
                <span>Hộp thư chờ duyệt</span>
                <span class="px-2 py-0.5 text-xs bg-white/25 rounded-full font-bold">6</span>
            </a>
            <a href="{{ url('/employees') }}" class="inline-flex items-center gap-2 px-4 py-2.5 bg-brand-700 hover:bg-brand-800 text-white rounded-lg text-sm font-semibold shadow-xs transition-colors">
                <i class="bi bi-person-plus-fill"></i>
                <span>Thêm nhân sự mới</span>
            </a>
        </div>
    </div>

    <!-- 4 Thẻ số Trọng yếu (Status Accent Colors) -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 sm:gap-6">
        <!-- 1. Tổng quân số -->
        <div class="bg-white p-5 rounded-xl border border-slate-200 shadow-xs flex items-center justify-between">
            <div>
                <span class="text-xs font-semibold uppercase tracking-wider text-slate-500">Tổng Headcount</span>
                <div class="text-2xl sm:text-3xl font-bold text-slate-900 mt-1">10.850</div>
                <div class="flex items-center gap-1 mt-1 text-xs text-status-approved font-medium">
                    <i class="bi bi-arrow-up-right"></i>
                    <span>+2.1% so với tháng trước</span>
                </div>
            </div>
            <div class="w-12 h-12 rounded-xl bg-brand-50 text-brand-700 flex items-center justify-center text-xl">
                <i class="bi bi-people-fill"></i>
            </div>
        </div>

        <!-- 2. Đi làm thực tế hôm nay (Xanh lá = Đã duyệt/Có mặt) -->
        <div class="bg-white p-5 rounded-xl border border-slate-200 shadow-xs flex items-center justify-between">
            <div>
                <span class="text-xs font-semibold uppercase tracking-wider text-slate-500">Có mặt hôm nay</span>
                <div class="text-2xl sm:text-3xl font-bold text-status-approved mt-1">98.2%</div>
                <div class="mt-1 text-xs text-slate-500">
                    <span>10.655 / 10.850 nhân sự đi làm</span>
                </div>
            </div>
            <div class="w-12 h-12 rounded-xl bg-emerald-50 text-status-approved flex items-center justify-center text-xl">
                <i class="bi bi-check-circle-fill"></i>
            </div>
        </div>

        <!-- 3. Đơn chờ duyệt (Vàng = Chờ duyệt) -->
        <div class="bg-white p-5 rounded-xl border border-slate-200 shadow-xs flex items-center justify-between">
            <div>
                <span class="text-xs font-semibold uppercase tracking-wider text-slate-500">Đơn chờ phê duyệt</span>
                <div class="text-2xl sm:text-3xl font-bold text-status-pending mt-1">14 hồ sơ</div>
                <div class="mt-1 text-xs text-amber-600 font-medium">
                    <span>6 đơn cần xử lý trong 24h</span>
                </div>
            </div>
            <div class="w-12 h-12 rounded-xl bg-amber-50 text-status-pending flex items-center justify-center text-xl">
                <i class="bi bi-clock-history"></i>
            </div>
        </div>

        <!-- 4. Tỷ lệ Chi phí OT (Đỏ = Cần kiểm soát) -->
        <div class="bg-white p-5 rounded-xl border border-slate-200 shadow-xs flex items-center justify-between">
            <div>
                <span class="text-xs font-semibold uppercase tracking-wider text-slate-500">Tỷ lệ OT / Quỹ lương</span>
                <div class="text-2xl sm:text-3xl font-bold text-slate-900 mt-1">7.8%</div>
                <div class="flex items-center gap-1 mt-1 text-xs text-slate-500">
                    <span class="inline-block w-2 h-2 rounded-full bg-emerald-500"></span>
                    <span>Dưới trần an toàn (10%)</span>
                </div>
            </div>
            <div class="w-12 h-12 rounded-xl bg-blue-50 text-brand-700 flex items-center justify-center text-xl">
                <i class="bi bi-cash-stack"></i>
            </div>
        </div>
    </div>

    <!-- Khu vực Bảng điều khiển tác vụ & Biểu đồ -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Cột trái (2/3): Tác vụ phê duyệt khẩn & Biến động nhân sự -->
        <div class="lg:col-span-2 space-y-6">
            <!-- Thẻ tác vụ cần duyệt gấp -->
            <div class="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden">
                <div class="p-5 border-b border-slate-200 flex items-center justify-between">
                    <div>
                        <h2 class="text-base font-bold text-slate-900">Hồ Sơ Cần Bạn Duyệt Gấp</h2>
                        <p class="text-xs text-slate-500 mt-0.5">Các yêu cầu nghỉ phép, tăng lương, tuyển dụng đang chờ bạn ký duyệt</p>
                    </div>
                    <a href="{{ route('approvals.inbox') ?? url('/approvals/inbox') }}" class="text-xs font-semibold text-brand-700 hover:text-brand-800 flex items-center gap-1">
                        Xem tất cả <i class="bi bi-arrow-right"></i>
                    </a>
                </div>
                <div class="divide-y divide-slate-100">
                    <!-- Mục 1: Đề xuất tăng lương -->
                    <div class="p-4 flex items-center justify-between gap-4 hover:bg-slate-50/80 transition-colors">
                        <div class="flex items-center gap-3 min-w-0">
                            <span class="w-10 h-10 rounded-lg bg-amber-50 text-status-pending flex items-center justify-center shrink-0">
                                <i class="bi bi-cash-coin text-lg"></i>
                            </span>
                            <div class="min-w-0">
                                <div class="text-sm font-semibold text-slate-900 truncate">Tờ trình tăng lương đặc cách: Kỹ sư Nguyễn Văn A</div>
                                <div class="text-xs text-slate-500 flex items-center gap-2 mt-0.5">
                                    <span class="font-medium text-slate-700">Nhà máy Mega Bình Dương</span>
                                    <span>•</span>
                                    <span class="text-status-pending font-semibold">Chờ GĐ Khối duyệt</span>
                                    <span>•</span>
                                    <span>2 giờ trước</span>
                                </div>
                            </div>
                        </div>
                        <div class="flex items-center gap-2 shrink-0">
                            <button class="px-3 py-1.5 bg-status-approved hover:bg-emerald-700 text-white rounded-md text-xs font-semibold shadow-xs">
                                Duyệt
                            </button>
                            <button class="px-3 py-1.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-md text-xs font-semibold">
                                Xem
                            </button>
                        </div>
                    </div>

                    <!-- Mục 2: Yêu cầu tuyển dụng -->
                    <div class="p-4 flex items-center justify-between gap-4 hover:bg-slate-50/80 transition-colors">
                        <div class="flex items-center gap-3 min-w-0">
                            <span class="w-10 h-10 rounded-lg bg-blue-50 text-brand-700 flex items-center justify-center shrink-0">
                                <i class="bi bi-person-lines-fill text-lg"></i>
                            </span>
                            <div class="min-w-0">
                                <div class="text-sm font-semibold text-slate-900 truncate">Yêu cầu tuyển dụng: 05 Công nhân Vận hành Ca 3</div>
                                <div class="text-xs text-slate-500 flex items-center gap-2 mt-0.5">
                                    <span class="font-medium text-slate-700">Nhà máy Sữa Thống Nhất</span>
                                    <span>•</span>
                                    <span class="text-status-pending font-semibold">Chờ Ban Nhân sự</span>
                                    <span>•</span>
                                    <span>5 giờ trước</span>
                                </div>
                            </div>
                        </div>
                        <div class="flex items-center gap-2 shrink-0">
                            <button class="px-3 py-1.5 bg-status-approved hover:bg-emerald-700 text-white rounded-md text-xs font-semibold shadow-xs">
                                Duyệt
                            </button>
                            <button class="px-3 py-1.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-md text-xs font-semibold">
                                Xem
                            </button>
                        </div>
                    </div>

                    <!-- Mục 3: Đơn nghỉ thai sản -->
                    <div class="p-4 flex items-center justify-between gap-4 hover:bg-slate-50/80 transition-colors">
                        <div class="flex items-center gap-3 min-w-0">
                            <span class="w-10 h-10 rounded-lg bg-purple-50 text-purple-600 flex items-center justify-center shrink-0">
                                <i class="bi bi-calendar-heart text-lg"></i>
                            </span>
                            <div class="min-w-0">
                                <div class="text-sm font-semibold text-slate-900 truncate">Đơn nghỉ chế độ thai sản (6 tháng): Lê Thị B</div>
                                <div class="text-xs text-slate-500 flex items-center gap-2 mt-0.5">
                                    <span class="font-medium text-slate-700">Khối Trụ sở Tập đoàn</span>
                                    <span>•</span>
                                    <span class="text-status-pending font-semibold">Chờ Trưởng phòng</span>
                                    <span>•</span>
                                    <span>1 ngày trước</span>
                                </div>
                            </div>
                        </div>
                        <div class="flex items-center gap-2 shrink-0">
                            <button class="px-3 py-1.5 bg-status-approved hover:bg-emerald-700 text-white rounded-md text-xs font-semibold shadow-xs">
                                Duyệt
                            </button>
                            <button class="px-3 py-1.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-md text-xs font-semibold">
                                Xem
                            </button>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Tình hình Phân bổ Quỹ Lương 3P theo Đơn vị -->
            <div class="bg-white p-5 rounded-xl border border-slate-200 shadow-xs">
                <div class="flex items-center justify-between mb-4">
                    <h2 class="text-base font-bold text-slate-900">Cơ Cấu Quỹ Lương 3P Theo Khối Hoạt Động</h2>
                    <span class="text-xs text-slate-500">Kỳ lương gần nhất: Tháng 10/2026</span>
                </div>
                <div class="space-y-4">
                    <div>
                        <div class="flex justify-between text-xs font-medium mb-1.5">
                            <span class="text-slate-700">Khối Nhà máy Sản xuất (145.2 tỷ VNĐ)</span>
                            <span class="text-slate-500">P1: 40% | P2: 25% | P3: 20% | OT: 15%</span>
                        </div>
                        <div class="h-3 rounded-full bg-slate-100 flex overflow-hidden">
                            <div class="bg-brand-700" style="width: 40%"></div>
                            <div class="bg-blue-400" style="width: 25%"></div>
                            <div class="bg-emerald-500" style="width: 20%"></div>
                            <div class="bg-amber-400" style="width: 15%"></div>
                        </div>
                    </div>
                    <div>
                        <div class="flex justify-between text-xs font-medium mb-1.5">
                            <span class="text-slate-700">Khối Trang trại Bò sữa (80.5 tỷ VNĐ)</span>
                            <span class="text-slate-500">P1: 50% | P2: 25% | P3: 15% | OT: 10%</span>
                        </div>
                        <div class="h-3 rounded-full bg-slate-100 flex overflow-hidden">
                            <div class="bg-brand-700" style="width: 50%"></div>
                            <div class="bg-blue-400" style="width: 25%"></div>
                            <div class="bg-emerald-500" style="width: 15%"></div>
                            <div class="bg-amber-400" style="width: 10%"></div>
                        </div>
                    </div>
                    <div>
                        <div class="flex justify-between text-xs font-medium mb-1.5">
                            <span class="text-slate-700">Khối Kinh doanh & Trụ sở (96.8 tỷ VNĐ)</span>
                            <span class="text-slate-500">P1: 45% | P2: 30% | P3: 22% | OT: 3%</span>
                        </div>
                        <div class="h-3 rounded-full bg-slate-100 flex overflow-hidden">
                            <div class="bg-brand-700" style="width: 45%"></div>
                            <div class="bg-blue-400" style="width: 30%"></div>
                            <div class="bg-emerald-500" style="width: 22%"></div>
                            <div class="bg-amber-400" style="width: 3%"></div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Cột phải (1/3): Lối tắt nghiệp vụ & Cảnh báo nghỉ hưu NĐ 135 -->
        <div class="space-y-6">
            <!-- Cảnh báo nhân sự tiệm cận nghỉ hưu -->
            <div class="bg-white p-5 rounded-xl border border-slate-200 shadow-xs">
                <div class="flex items-center justify-between mb-3">
                    <h3 class="text-sm font-bold text-slate-900 flex items-center gap-2">
                        <i class="bi bi-clock-history text-status-rejected"></i>
                        <span>Sắp Nghỉ Hưu (NĐ 135/2020)</span>
                    </h3>
                    <span class="px-2 py-0.5 rounded text-[11px] font-bold bg-red-100 text-status-rejected">12 tháng tới</span>
                </div>
                <p class="text-xs text-slate-500 mb-4">Có <strong>12 cán bộ, kỹ thuật viên</strong> sẽ đến tuổi nghỉ hưu theo lộ trình luật định.</p>
                <div class="space-y-3">
                    <div class="p-3 bg-slate-50 rounded-lg border border-slate-200 flex items-center justify-between">
                        <div>
                            <div class="text-xs font-bold text-slate-800">Nguyễn Văn Nam (61 tuổi)</div>
                            <div class="text-[11px] text-slate-500">Trưởng ca Vận hành • NM Bình Dương</div>
                        </div>
                        <span class="text-xs font-bold text-red-600">Còn 2 tháng</span>
                    </div>
                    <div class="p-3 bg-slate-50 rounded-lg border border-slate-200 flex items-center justify-between">
                        <div>
                            <div class="text-xs font-bold text-slate-800">Trần Thị Nữ (57 tuổi)</div>
                            <div class="text-[11px] text-slate-500">Chuyên viên QLCL • NM Thống Nhất</div>
                        </div>
                        <span class="text-xs font-bold text-amber-600">Còn 5 tháng</span>
                    </div>
                </div>
                <a href="{{ route('reports.bm04') }}" class="block text-center text-xs font-semibold text-brand-700 hover:text-brand-800 mt-4">
                    Xem danh sách chi tiết BM04 →
                </a>
            </div>

            <!-- Truy cập nhanh các báo cáo quản trị -->
            <div class="bg-white p-5 rounded-xl border border-slate-200 shadow-xs">
                <h3 class="text-sm font-bold text-slate-900 mb-3">Phím Tắt Nghiệp Vụ</h3>
                <div class="grid grid-cols-2 gap-2 text-xs font-medium">
                    <a href="{{ route('reports.bm01') }}" class="p-3 bg-slate-50 hover:bg-brand-50 hover:text-brand-700 rounded-lg border border-slate-200 text-center transition-colors">
                        <i class="bi bi-person-lines-fill text-base block mb-1"></i>
                        BM01 Trích ngang
                    </a>
                    <a href="{{ route('reports.bm02') }}" class="p-3 bg-slate-50 hover:bg-brand-50 hover:text-brand-700 rounded-lg border border-slate-200 text-center transition-colors">
                        <i class="bi bi-arrow-left-right text-base block mb-1"></i>
                        BM02 Biến động
                    </a>
                    <a href="{{ route('reports.bm03') }}" class="p-3 bg-slate-50 hover:bg-brand-50 hover:text-brand-700 rounded-lg border border-slate-200 text-center transition-colors">
                        <i class="bi bi-award text-base block mb-1"></i>
                        BM03 Khen thưởng
                    </a>
                    <a href="{{ url('/payroll') }}" class="p-3 bg-slate-50 hover:bg-brand-50 hover:text-brand-700 rounded-lg border border-slate-200 text-center transition-colors">
                        <i class="bi bi-calculator text-base block mb-1"></i>
                        Tính lương 3P
                    </a>
                </div>
            </div>
        </div>
    </div>
</div>
@endsection
