@extends('layouts.app')

@section('title', 'Hồ Sơ Nhân Sự Chi Tiết — ' . ($employee->full_name ?? 'Nguyễn Văn Công'))

@section('content')
<div class="space-y-6">
    <!-- Breadcrumb & Phím chức năng -->
    <div class="flex items-center justify-between text-xs text-slate-500">
        <div class="flex items-center gap-2">
            <a href="{{ url('/employees') }}" class="hover:text-brand-700">Nhân sự</a>
            <span>/</span>
            <span class="text-slate-800 font-semibold">{{ $employee->employee_code ?? 'VNM-CN001' }}</span>
        </div>
        <div class="flex gap-2">
            <a href="{{ route('reports.bm01') }}" class="text-brand-700 hover:underline flex items-center gap-1">
                <i class="bi bi-printer"></i> In trích ngang BM01
            </a>
        </div>
    </div>

    <!-- Header Hồ sơ Nhân sự -->
    <div class="bg-white p-6 rounded-xl border border-slate-200 shadow-xs">
        <div class="flex flex-col md:flex-row md:items-center justify-between gap-6">
            <div class="flex items-center gap-5">
                <!-- Avatar Placeholder -->
                <div class="relative">
                    <div class="w-20 h-20 rounded-full bg-brand-700 text-white flex items-center justify-center font-bold text-2xl shadow-md border-2 border-white">
                        {{ substr($employee->full_name ?? 'Nguyễn Văn Công', 0, 2) }}
                    </div>
                    <span class="absolute bottom-0 right-0 w-5 h-5 bg-status-approved rounded-full ring-2 ring-white" title="Trạng thái: Đang làm việc"></span>
                </div>
                <div>
                    <div class="flex items-center gap-3">
                        <h1 class="text-xl sm:text-2xl font-bold text-slate-900">{{ $employee->full_name ?? 'Nguyễn Văn Công' }}</h1>
                        <span class="px-2.5 py-0.5 rounded-full text-xs font-bold bg-emerald-100 text-status-approved">
                            {{ $employee->employment_status ?? 'Chính thức' }}
                        </span>
                    </div>
                    <div class="text-sm font-semibold text-brand-700 mt-1 flex items-center gap-2">
                        <span>{{ $employee->current_position?->position_name ?? 'Công nhân Vận hành Ca 3' }}</span>
                        <span class="text-slate-300">•</span>
                        <span class="text-slate-600 font-normal">{{ $employee->current_org_unit?->unit_name ?? 'Nhà máy Sữa Thống Nhất' }}</span>
                    </div>
                    <div class="text-xs text-slate-500 mt-1 flex flex-wrap gap-4">
                        <span>Mã NV: <strong class="text-slate-700">{{ $employee->employee_code ?? 'VNM-CN001' }}</strong></span>
                        <span>Ngày vào làm: <strong>{{ date('d/m/Y', strtotime($employee->hire_date ?? '2023-01-01')) }}</strong></span>
                        <span>Sổ BHXH: <strong>{{ $employee->social_insurance_number ?? '7912345678' }}</strong></span>
                    </div>
                </div>
            </div>

            <!-- Các nút hành động chính -->
            <div class="flex flex-wrap gap-2.5">
                <a href="{{ url('/salary-proposals/create?employee_id=' . ($employee->id ?? 1)) }}" class="px-3.5 py-2 bg-brand-50 hover:bg-brand-100 text-brand-700 rounded-lg text-xs font-semibold border border-brand-200 transition-colors flex items-center gap-1.5">
                    <i class="bi bi-graph-up-arrow"></i> Đề xuất tăng lương
                </a>
                <a href="{{ url('/assignments/transfer?employee_id=' . ($employee->id ?? 1)) }}" class="px-3.5 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg text-xs font-semibold border border-slate-300 transition-colors flex items-center gap-1.5">
                    <i class="bi bi-arrow-left-right"></i> Điều chuyển công tác
                </a>
                <button class="px-3.5 py-2 bg-brand-700 hover:bg-brand-800 text-white rounded-lg text-xs font-semibold shadow-xs transition-colors flex items-center gap-1.5">
                    <i class="bi bi-pencil-square"></i> Cập nhật hồ sơ
                </button>
            </div>
        </div>
    </div>

    <!-- 6 Tabs Chi Tiết Hồ Sơ -->
    <div class="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden">
        <!-- Tab Navigation Bar -->
        <div class="border-b border-slate-200 bg-slate-50/50 px-4 flex overflow-x-auto gap-2">
            <button onclick="switchProfileTab('tab-personal')" id="btn-tab-personal" class="profile-tab-btn py-3 px-4 text-xs font-bold text-brand-700 border-b-2 border-brand-700 whitespace-nowrap">
                <i class="bi bi-person me-1.5"></i> 1. Cá nhân
            </button>
            <button onclick="switchProfileTab('tab-assignment')" id="btn-tab-assignment" class="profile-tab-btn py-3 px-4 text-xs font-semibold text-slate-500 hover:text-slate-800 border-b-2 border-transparent whitespace-nowrap">
                <i class="bi bi-diagram-3 me-1.5"></i> 2. Công tác (SCD2)
            </button>
            <button onclick="switchProfileTab('tab-salary')" id="btn-tab-salary" class="profile-tab-btn py-3 px-4 text-xs font-semibold text-slate-500 hover:text-slate-800 border-b-2 border-transparent whitespace-nowrap">
                <i class="bi bi-cash-stack me-1.5"></i> 3. Lương 3P & Đãi ngộ
            </button>
            <button onclick="switchProfileTab('tab-contract')" id="btn-tab-contract" class="profile-tab-btn py-3 px-4 text-xs font-semibold text-slate-500 hover:text-slate-800 border-b-2 border-transparent whitespace-nowrap">
                <i class="bi bi-file-earmark-text me-1.5"></i> 4. Hợp đồng lao động
            </button>
            <button onclick="switchProfileTab('tab-document')" id="btn-tab-document" class="profile-tab-btn py-3 px-4 text-xs font-semibold text-slate-500 hover:text-slate-800 border-b-2 border-transparent whitespace-nowrap">
                <i class="bi bi-folder2-open me-1.5"></i> 5. Tài liệu đính kèm
            </button>
            <button onclick="switchProfileTab('tab-history')" id="btn-tab-history" class="profile-tab-btn py-3 px-4 text-xs font-semibold text-slate-500 hover:text-slate-800 border-b-2 border-transparent whitespace-nowrap">
                <i class="bi bi-clock-history me-1.5"></i> 6. Dòng thời gian lịch sử
            </button>
        </div>

        <div class="p-6">
            <!-- TAB 1: THÔNG TIN CÁ NHÂN -->
            <div id="tab-personal" class="profile-tab-content space-y-6">
                <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                    <div>
                        <h4 class="text-xs font-bold uppercase tracking-wider text-slate-400 mb-3">Lý Lịch Nhân Thân</h4>
                        <dl class="space-y-2 text-xs">
                            <div class="flex justify-between py-1 border-b border-slate-100"><dt class="text-slate-500">Giới tính:</dt><dd class="font-semibold text-slate-800">Nam</dd></div>
                            <div class="flex justify-between py-1 border-b border-slate-100"><dt class="text-slate-500">Ngày sinh:</dt><dd class="font-semibold text-slate-800">15/06/1990</dd></div>
                            <div class="flex justify-between py-1 border-b border-slate-100"><dt class="text-slate-500">Dân tộc / Tôn giáo:</dt><dd class="font-semibold text-slate-800">Kinh / Không</dd></div>
                            <div class="flex justify-between py-1 border-b border-slate-100"><dt class="text-slate-500">Tình trạng hôn nhân:</dt><dd class="font-semibold text-slate-800">Đã kết hôn</dd></div>
                        </dl>
                    </div>

                    <div>
                        <h4 class="text-xs font-bold uppercase tracking-wider text-slate-400 mb-3">Giấy Tờ Tùy Thân & Liên Hệ</h4>
                        <dl class="space-y-2 text-xs">
                            <div class="flex justify-between py-1 border-b border-slate-100"><dt class="text-slate-500">Số CCCD:</dt><dd class="font-semibold text-slate-800">079090012345</dd></div>
                            <div class="flex justify-between py-1 border-b border-slate-100"><dt class="text-slate-500">Ngày cấp / Nơi cấp:</dt><dd class="font-semibold text-slate-800">10/05/2021 (Cục CSQLHC)</dd></div>
                            <div class="flex justify-between py-1 border-b border-slate-100"><dt class="text-slate-500">Số điện thoại:</dt><dd class="font-semibold text-slate-800">0903 123 456</dd></div>
                            <div class="flex justify-between py-1 border-b border-slate-100"><dt class="text-slate-500">Email công ty:</dt><dd class="font-semibold text-slate-800">cong.nv@vinamilk.com.vn</dd></div>
                        </dl>
                    </div>

                    <div>
                        <h4 class="text-xs font-bold uppercase tracking-wider text-slate-400 mb-3">Thuế & Ngân Hàng</h4>
                        <dl class="space-y-2 text-xs">
                            <div class="flex justify-between py-1 border-b border-slate-100"><dt class="text-slate-500">Mã số thuế cá nhân:</dt><dd class="font-semibold text-slate-800">8012345678</dd></div>
                            <div class="flex justify-between py-1 border-b border-slate-100"><dt class="text-slate-500">Số tài khoản:</dt><dd class="font-semibold text-slate-800">0071001234567</dd></div>
                            <div class="flex justify-between py-1 border-b border-slate-100"><dt class="text-slate-500">Ngân hàng chi nhánh:</dt><dd class="font-semibold text-slate-800">Vietcombank - CN Bình Dương</dd></div>
                            <div class="flex justify-between py-1 border-b border-slate-100"><dt class="text-slate-500">Người phụ thuộc:</dt><dd class="font-semibold text-slate-800">0 người (Độc thân)</dd></div>
                        </dl>
                    </div>
                </div>
            </div>

            <!-- TAB 2: CÔNG TÁC & BỔ NHIỆM (SCD TYPE 2) -->
            <div id="tab-assignment" class="profile-tab-content space-y-6 hidden">
                <h4 class="text-xs font-bold uppercase tracking-wider text-slate-400">Lịch Sử Điều Chuyển & Bổ Nhiệm (SCD Type 2)</h4>
                <div class="border rounded-lg overflow-hidden">
                    <table class="w-full text-xs text-left">
                        <thead class="bg-slate-50 text-slate-600 font-bold border-b">
                            <tr>
                                <th class="p-3">Hiệu lực từ</th>
                                <th class="p-3">Đến ngày</th>
                                <th class="p-3">Đơn vị công tác</th>
                                <th class="p-3">Vị trí chức danh</th>
                                <th class="p-3">Số Quyết định</th>
                                <th class="p-3">Trạng thái</th>
                            </tr>
                        </thead>
                        <tbody class="divide-y divide-slate-100">
                            <tr class="bg-emerald-50/40">
                                <td class="p-3 font-semibold text-slate-800">01/01/2024</td>
                                <td class="p-3 text-emerald-700 font-bold">Hiện hành</td>
                                <td class="p-3">Nhà máy Sữa Thống Nhất</td>
                                <td class="p-3 font-semibold">Công nhân Vận hành Ca 3 (Bậc 3)</td>
                                <td class="p-3 font-mono">QĐ-BN/2024/VNM-08</td>
                                <td class="p-3"><span class="badge px-2 py-0.5 rounded text-[11px] font-bold bg-emerald-100 text-status-approved">Đang hiệu lực</span></td>
                            </tr>
                            <tr>
                                <td class="p-3 font-semibold text-slate-800">01/01/2023</td>
                                <td class="p-3 text-slate-500">31/12/2023</td>
                                <td class="p-3">Nhà máy Sữa Thống Nhất</td>
                                <td class="p-3">Công nhân Vận hành Tập sự</td>
                                <td class="p-3 font-mono">QĐ-TD/2023/VNM-12</td>
                                <td class="p-3"><span class="badge px-2 py-0.5 rounded text-[11px] font-bold bg-slate-100 text-slate-600">Đã hết hiệu lực</span></td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>

            <!-- TAB 3: LƯƠNG 3P & ĐÃI NGỘ -->
            <div id="tab-salary" class="profile-tab-content space-y-6 hidden">
                <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div class="p-4 bg-slate-50 rounded-lg border">
                        <div class="text-xs text-slate-500 font-semibold">LƯƠNG VỊ TRÍ (P1)</div>
                        <div class="text-xl font-bold text-slate-900 mt-1">8.800.000 VNĐ</div>
                        <div class="text-[11px] text-slate-500 mt-1">Theo ngạch công nhân vận hành</div>
                    </div>
                    <div class="p-4 bg-slate-50 rounded-lg border">
                        <div class="text-xs text-slate-500 font-semibold">LƯƠNG NĂNG LỰC (P2)</div>
                        <div class="text-xl font-bold text-slate-900 mt-1">2.200.000 VNĐ</div>
                        <div class="text-[11px] text-slate-500 mt-1">Bậc thợ 3/7 kỹ thuật</div>
                    </div>
                    <div class="p-4 bg-slate-50 rounded-lg border">
                        <div class="text-xs text-slate-500 font-semibold">LƯƠNG ĐÓNG BHXH</div>
                        <div class="text-xl font-bold text-brand-700 mt-1">11.000.000 VNĐ</div>
                        <div class="text-[11px] text-slate-500 mt-1">NLĐ trích 10.5% = 1.155.000 đ</div>
                    </div>
                </div>

                <div class="border rounded-lg p-4">
                    <h4 class="text-xs font-bold uppercase tracking-wider text-slate-400 mb-3">Chế Độ Phụ Cấp Hàng Tháng</h4>
                    <div class="grid grid-cols-2 md:grid-cols-4 gap-4 text-xs">
                        <div><span class="text-slate-500">Phụ cấp độc hại:</span> <strong class="text-slate-800 d-block">1.000.000 VNĐ</strong></div>
                        <div><span class="text-slate-500">Phụ cấp ca đêm (+30%):</span> <strong class="text-slate-800 d-block">Theo số giờ thực tế</strong></div>
                        <div><span class="text-slate-500">Phụ cấp ăn giữa ca:</span> <strong class="text-slate-800 d-block">730.000 VNĐ (Miễn thuế)</strong></div>
                        <div><span class="text-slate-500">Chuyên cần / Trách nhiệm:</span> <strong class="text-slate-800 d-block">500.000 VNĐ</strong></div>
                    </div>
                </div>
            </div>

            <!-- TAB 4: HỢP ĐỒNG LAO ĐỘNG -->
            <div id="tab-contract" class="profile-tab-content space-y-6 hidden">
                <h4 class="text-xs font-bold uppercase tracking-wider text-slate-400">Danh Sách Hợp Đồng Lao Động</h4>
                <div class="border rounded-lg overflow-hidden text-xs">
                    <table class="w-full text-left">
                        <thead class="bg-slate-50 font-bold border-b">
                            <tr>
                                <th class="p-3">Số Hợp đồng</th>
                                <th class="p-3">Loại hợp đồng</th>
                                <th class="p-3">Ngày bắt đầu</th>
                                <th class="p-3">Ngày hết hạn</th>
                                <th class="p-3">Tệp đính kèm</th>
                                <th class="p-3">Trạng thái</th>
                            </tr>
                        </thead>
                        <tbody class="divide-y divide-slate-100">
                            <tr class="bg-emerald-50/40">
                                <td class="p-3 font-mono font-bold text-brand-700">HĐLĐ-2023/VNM-0142</td>
                                <td class="p-3 font-semibold">Xác định thời hạn (36 tháng)</td>
                                <td class="p-3">01/03/2023</td>
                                <td class="p-3">28/02/2026</td>
                                <td class="p-3"><a href="#" class="text-brand-700 underline"><i class="bi bi-file-pdf"></i> HDLD_ChinhThuc.pdf</a></td>
                                <td class="p-3"><span class="badge px-2 py-0.5 rounded text-[11px] font-bold bg-emerald-100 text-status-approved">Đang hiệu lực</span></td>
                            </tr>
                            <tr>
                                <td class="p-3 font-mono text-slate-600">HĐTV-2023/VNM-0055</td>
                                <td class="p-3">Hợp đồng thử việc (2 tháng)</td>
                                <td class="p-3">01/01/2023</td>
                                <td class="p-3">28/02/2023</td>
                                <td class="p-3"><a href="#" class="text-slate-500 underline"><i class="bi bi-file-pdf"></i> HD_ThuViec.pdf</a></td>
                                <td class="p-3"><span class="badge px-2 py-0.5 rounded text-[11px] font-bold bg-slate-100 text-slate-600">Đã hoàn thành</span></td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>

            <!-- TAB 5: TÀI LIỆU ĐÍNH KÈM -->
            <div id="tab-document" class="profile-tab-content space-y-6 hidden">
                <div class="flex justify-between items-center">
                    <h4 class="text-xs font-bold uppercase tracking-wider text-slate-400">Hồ Sơ Điện Tử & Bằng Cấp (PDF &le; 5MB)</h4>
                    <button class="px-3 py-1.5 bg-brand-700 text-white rounded text-xs font-semibold"><i class="bi bi-upload"></i> Tải lên tài liệu mới</button>
                </div>
                <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-4 text-xs">
                    <div class="p-4 border rounded-lg flex items-center justify-between bg-slate-50">
                        <div class="flex items-center gap-3">
                            <i class="bi bi-file-earmark-pdf text-red-500 text-2xl"></i>
                            <div>
                                <div class="font-bold text-slate-800">CV_UngTuyen.pdf</div>
                                <div class="text-[11px] text-slate-500">1.2 MB • Tải lên 01/01/2023</div>
                            </div>
                        </div>
                        <a href="#" class="text-brand-700 hover:text-brand-900"><i class="bi bi-download"></i></a>
                    </div>
                    <div class="p-4 border rounded-lg flex items-center justify-between bg-slate-50">
                        <div class="flex items-center gap-3">
                            <i class="bi bi-file-earmark-pdf text-red-500 text-2xl"></i>
                            <div>
                                <div class="font-bold text-slate-800">KhamSucKhoe_DinhKy.pdf</div>
                                <div class="text-[11px] text-slate-500">2.4 MB • Loại 1 (Đạt yêu cầu)</div>
                            </div>
                        </div>
                        <a href="#" class="text-brand-700 hover:text-brand-900"><i class="bi bi-download"></i></a>
                    </div>
                    <div class="p-4 border rounded-lg flex items-center justify-between bg-slate-50">
                        <div class="flex items-center gap-3">
                            <i class="bi bi-file-earmark-image text-blue-500 text-2xl"></i>
                            <div>
                                <div class="font-bold text-slate-800">Bang_TrungCap_CoKhi.pdf</div>
                                <div class="text-[11px] text-slate-500">850 KB • Đã đối soát gốc</div>
                            </div>
                        </div>
                        <a href="#" class="text-brand-700 hover:text-brand-900"><i class="bi bi-download"></i></a>
                    </div>
                </div>
            </div>

            <!-- TAB 6: LỊCH SỬ BIẾN ĐỘNG (TIMELINE) -->
            <div id="tab-history" class="profile-tab-content space-y-6 hidden">
                <h4 class="text-xs font-bold uppercase tracking-wider text-slate-400">Dòng Thời Gian Quá Trình Công Tác & Diễn Biến Nhân Sự</h4>
                <div class="relative border-l-2 border-brand-200 ml-4 space-y-6 text-xs">
                    <div class="relative pl-6">
                        <span class="absolute -left-2 top-0 w-4 h-4 rounded-full bg-status-approved ring-4 ring-white"></span>
                        <div class="font-bold text-slate-800">01/01/2024 — Nâng bậc lương năng lực (P2) lên Bậc 3</div>
                        <div class="text-slate-500 mt-0.5">Quyết định tăng lương định kỳ toàn nhà máy số QĐ-TL/2024/09. P2 tăng từ 1.8M lên 2.2M.</div>
                    </div>
                    <div class="relative pl-6">
                        <span class="absolute -left-2 top-0 w-4 h-4 rounded-full bg-brand-700 ring-4 ring-white"></span>
                        <div class="font-bold text-slate-800">01/03/2023 — Ký hợp đồng chính thức xác định thời hạn 3 năm</div>
                        <div class="text-slate-500 mt-0.5">Hoàn thành thử việc xuất sắc, chuyển sang trạng thái Chính thức (ACTIVE).</div>
                    </div>
                    <div class="relative pl-6">
                        <span class="absolute -left-2 top-0 w-4 h-4 rounded-full bg-slate-400 ring-4 ring-white"></span>
                        <div class="font-bold text-slate-800">01/01/2023 — Tiếp nhận vào làm việc (Hire Date)</div>
                        <div class="text-slate-500 mt-0.5">Vị trí Công nhân Vận hành Tập sự tại Phân xưởng Tiệt trùng UHT, NM Thống Nhất.</div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<script>
    function switchProfileTab(tabId) {
        // Ẩn tất cả tab nội dung
        document.querySelectorAll('.profile-tab-content').forEach(el => el.classList.add('hidden'));
        // Xóa class active khỏi các nút
        document.querySelectorAll('.profile-tab-btn').forEach(btn => {
            btn.classList.remove('text-brand-700', 'border-brand-700', 'font-bold');
            btn.classList.add('text-slate-500', 'border-transparent', 'font-semibold');
        });

        // Kích hoạt tab được chọn
        const targetTab = document.getElementById(tabId);
        const targetBtn = document.getElementById('btn-' + tabId);
        if (targetTab && targetBtn) {
            targetTab.classList.remove('hidden');
            targetBtn.classList.add('text-brand-700', 'border-brand-700', 'font-bold');
            targetBtn.classList.remove('text-slate-500', 'border-transparent', 'font-semibold');
        }
    }
</script>
@endsection
