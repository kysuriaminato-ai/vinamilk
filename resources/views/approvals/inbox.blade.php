@extends('layouts.app')

@section('title', 'Hộp Thư Phê Duyệt Tập Trung — Unified Approval Inbox')

@section('content')
<div class="space-y-6">
    <!-- Tiêu đề & Hành động Hàng loạt -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 bg-white p-6 rounded-xl border border-slate-200 shadow-xs">
        <div>
            <div class="flex items-center gap-2">
                <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-100 text-status-pending">
                    <i class="bi bi-clock-history me-1"></i> Có 6 hồ sơ đang chờ bạn phê duyệt
                </span>
            </div>
            <h1 class="text-2xl font-bold text-slate-900 mt-1">Hộp Thư Phê Duyệt Tập Trung (Unified Inbox)</h1>
            <p class="text-sm text-slate-600 mt-0.5">Xử lý tập trung tất cả các loại đơn: Nghỉ phép, Tăng lương, Tuyển dụng, Làm thêm giờ và Thôi việc.</p>
        </div>
        <div class="flex items-center gap-3">
            <button onclick="bulkApprove()" class="inline-flex items-center gap-2 px-4 py-2.5 bg-status-approved hover:bg-emerald-700 text-white rounded-lg text-xs font-semibold shadow-xs transition-colors">
                <i class="bi bi-check-all text-base"></i>
                <span>Duyệt hàng loạt đã chọn</span>
            </button>
            <button onclick="bulkReject()" class="inline-flex items-center gap-2 px-4 py-2.5 bg-white hover:bg-slate-50 text-status-rejected border border-red-200 rounded-lg text-xs font-semibold shadow-xs transition-colors">
                <i class="bi bi-x-circle text-base"></i>
                <span>Từ chối</span>
            </button>
        </div>
    </div>

    <!-- Thanh Lọc theo Loại Hồ Sơ (Filter Tabs) -->
    <div class="flex gap-2 overflow-x-auto pb-1 text-xs font-semibold">
        <a href="?type=all" class="px-4 py-2 rounded-lg bg-brand-700 text-white shadow-xs whitespace-nowrap">
            Tất cả loại đơn (6)
        </a>
        <a href="?type=leave" class="px-4 py-2 rounded-lg bg-white border border-slate-200 text-slate-600 hover:bg-slate-50 whitespace-nowrap">
            Đơn nghỉ phép (2)
        </a>
        <a href="?type=salary" class="px-4 py-2 rounded-lg bg-white border border-slate-200 text-slate-600 hover:bg-slate-50 whitespace-nowrap">
            Đề xuất tăng lương (1)
        </a>
        <a href="?type=recruitment" class="px-4 py-2 rounded-lg bg-white border border-slate-200 text-slate-600 hover:bg-slate-50 whitespace-nowrap">
            Yêu cầu tuyển dụng (2)
        </a>
        <a href="?type=overtime" class="px-4 py-2 rounded-lg bg-white border border-slate-200 text-slate-600 hover:bg-slate-50 whitespace-nowrap">
            Đăng ký tăng ca OT (1)
        </a>
        <a href="?type=offboarding" class="px-4 py-2 rounded-lg bg-white border border-slate-200 text-slate-600 hover:bg-slate-50 whitespace-nowrap">
            Thôi việc & Bàn giao (0)
        </a>
    </div>

    <!-- Bảng Danh Sách Hồ Sơ Chờ Duyệt -->
    <div class="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden">
        <div class="table-responsive">
            <table class="w-full text-xs text-left">
                <thead class="bg-slate-50 text-slate-600 font-bold border-b border-slate-200">
                    <tr>
                        <th class="p-3.5 w-10 text-center">
                            <input type="checkbox" id="selectAllCheckbox" onclick="toggleSelectAll(this)" class="rounded border-slate-300 text-brand-700 focus:ring-brand-500">
                        </th>
                        <th class="p-3.5">Loại yêu cầu</th>
                        <th class="p-3.5">Tiêu đề & Nội dung tóm tắt</th>
                        <th class="p-3.5">Người đề xuất</th>
                        <th class="p-3.5">Đơn vị / Bộ phận</th>
                        <th class="p-3.5">Thời gian gửi</th>
                        <th class="p-3.5">Tiến độ phê duyệt</th>
                        <th class="p-3.5 text-center w-36">Hành động</th>
                    </tr>
                </thead>
                <tbody class="divide-y divide-slate-100">
                    <!-- Mục 1: Đề xuất tăng lương -->
                    <tr class="hover:bg-slate-50/80 transition-colors">
                        <td class="p-3.5 text-center">
                            <input type="checkbox" class="row-checkbox rounded border-slate-300 text-brand-700 focus:ring-brand-500">
                        </td>
                        <td class="p-3.5">
                            <span class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-amber-50 text-status-pending font-bold border border-amber-200">
                                <i class="bi bi-cash-stack"></i> Tăng lương
                            </span>
                        </td>
                        <td class="p-3.5">
                            <div class="font-bold text-slate-900">Tờ trình nâng bậc lương năng lực (P2): Kỹ sư Trần Thị Kỹ Sư</div>
                            <div class="text-[11px] text-slate-500 mt-0.5">Đề xuất: 13.200.000 đ &rarr; 15.000.000 đ (Compa-ratio đạt 102%)</div>
                        </td>
                        <td class="p-3.5">
                            <div class="font-semibold text-slate-800">Trần Thị Kỹ Sư</div>
                            <div class="text-[11px] text-slate-400 font-mono">VNM-KS002</div>
                        </td>
                        <td class="p-3.5 text-slate-600">Ban Nghiên cứu & Phát triển (R&D)</td>
                        <td class="p-3.5 text-slate-500">2 giờ trước</td>
                        <td class="p-3.5">
                            <span class="px-2 py-0.5 rounded text-[11px] font-semibold bg-amber-100 text-amber-800">
                                Bước 2/3: Chờ GĐ Khối duyệt
                            </span>
                        </td>
                        <td class="p-3.5 text-center">
                            <div class="flex items-center justify-center gap-1.5">
                                <button onclick="quickApprove(1)" class="p-1.5 bg-status-approved hover:bg-emerald-700 text-white rounded text-xs" title="Phê duyệt">
                                    <i class="bi bi-check-lg"></i>
                                </button>
                                <button onclick="quickReject(1)" class="p-1.5 bg-status-rejected hover:bg-red-700 text-white rounded text-xs" title="Từ chối">
                                    <i class="bi bi-x-lg"></i>
                                </button>
                                <button onclick="openTimelineModal('Tờ trình tăng lương đặc cách: VNM-KS002', 'SALARY')" class="p-1.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded text-xs" title="Xem dòng thời gian duyệt">
                                    <i class="bi bi-eye"></i>
                                </button>
                            </div>
                        </td>
                    </tr>

                    <!-- Mục 2: Yêu cầu tuyển dụng Requisition -->
                    <tr class="hover:bg-slate-50/80 transition-colors">
                        <td class="p-3.5 text-center">
                            <input type="checkbox" class="row-checkbox rounded border-slate-300 text-brand-700 focus:ring-brand-500">
                        </td>
                        <td class="p-3.5">
                            <span class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-blue-50 text-brand-700 font-bold border border-blue-200">
                                <i class="bi bi-person-plus"></i> Tuyển dụng
                            </span>
                        </td>
                        <td class="p-3.5">
                            <div class="font-bold text-slate-900">Yêu cầu tuyển dụng: 05 Công nhân Vận hành Ca 3 Tiệt trùng</div>
                            <div class="text-[11px] text-slate-500 mt-0.5">Bổ sung định biên mở rộng chuyền UHT Nhà máy Sữa Thống Nhất</div>
                        </td>
                        <td class="p-3.5">
                            <div class="font-semibold text-slate-800">Nguyễn Văn Quản Đốc</div>
                            <div class="text-[11px] text-slate-400 font-mono">VNM-QD001</div>
                        </td>
                        <td class="p-3.5 text-slate-600">Nhà máy Sữa Thống Nhất</td>
                        <td class="p-3.5 text-slate-500">5 giờ trước</td>
                        <td class="p-3.5">
                            <span class="px-2 py-0.5 rounded text-[11px] font-semibold bg-amber-100 text-amber-800">
                                Bước 3/4: Chờ Ban TGĐ
                            </span>
                        </td>
                        <td class="p-3.5 text-center">
                            <div class="flex items-center justify-center gap-1.5">
                                <button onclick="quickApprove(2)" class="p-1.5 bg-status-approved hover:bg-emerald-700 text-white rounded text-xs" title="Phê duyệt">
                                    <i class="bi bi-check-lg"></i>
                                </button>
                                <button onclick="quickReject(2)" class="p-1.5 bg-status-rejected hover:bg-red-700 text-white rounded text-xs" title="Từ chối">
                                    <i class="bi bi-x-lg"></i>
                                </button>
                                <button onclick="openTimelineModal('Yêu cầu tuyển dụng: 05 Công nhân Ca 3', 'REQUISITION')" class="p-1.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded text-xs" title="Xem dòng thời gian duyệt">
                                    <i class="bi bi-eye"></i>
                                </button>
                            </div>
                        </td>
                    </tr>

                    <!-- Mục 3: Đơn xin nghỉ phép năm -->
                    <tr class="hover:bg-slate-50/80 transition-colors">
                        <td class="p-3.5 text-center">
                            <input type="checkbox" class="row-checkbox rounded border-slate-300 text-brand-700 focus:ring-brand-500">
                        </td>
                        <td class="p-3.5">
                            <span class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-emerald-50 text-status-approved font-bold border border-emerald-200">
                                <i class="bi bi-calendar-check"></i> Nghỉ phép
                            </span>
                        </td>
                        <td class="p-3.5">
                            <div class="font-bold text-slate-900">Đơn xin nghỉ phép năm (02 ngày): 15/10 - 16/10/2026</div>
                            <div class="text-[11px] text-slate-500 mt-0.5">Lý do: Giải quyết việc gia đình cá nhân (Phép tồn còn 8 ngày)</div>
                        </td>
                        <td class="p-3.5">
                            <div class="font-semibold text-slate-800">Lê Hoàng Nam</div>
                            <div class="text-[11px] text-slate-400 font-mono">VNM-KD003</div>
                        </td>
                        <td class="p-3.5 text-slate-600">Khối Kinh doanh Nội địa</td>
                        <td class="p-3.5 text-slate-500">1 ngày trước</td>
                        <td class="p-3.5">
                            <span class="px-2 py-0.5 rounded text-[11px] font-semibold bg-amber-100 text-amber-800">
                                Bước 1/2: Chờ Trưởng phòng duyệt
                            </span>
                        </td>
                        <td class="p-3.5 text-center">
                            <div class="flex items-center justify-center gap-1.5">
                                <button onclick="quickApprove(3)" class="p-1.5 bg-status-approved hover:bg-emerald-700 text-white rounded text-xs" title="Phê duyệt">
                                    <i class="bi bi-check-lg"></i>
                                </button>
                                <button onclick="quickReject(3)" class="p-1.5 bg-status-rejected hover:bg-red-700 text-white rounded text-xs" title="Từ chối">
                                    <i class="bi bi-x-lg"></i>
                                </button>
                                <button onclick="openTimelineModal('Đơn nghỉ phép năm: Lê Hoàng Nam', 'LEAVE')" class="p-1.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded text-xs" title="Xem dòng thời gian duyệt">
                                    <i class="bi bi-eye"></i>
                                </button>
                            </div>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>
</div>

<!-- Modal Chi Tiết Tờ Trình & Dòng Thời Gian Duyệt (Approval Timeline Stepper) -->
<div id="timelineModal" class="fixed inset-0 bg-slate-900/50 z-50 flex items-center justify-center p-4 hidden">
    <div class="bg-white rounded-xl shadow-xl max-w-2xl w-full p-6 space-y-6">
        <div class="flex items-center justify-between border-b pb-4">
            <div>
                <h3 class="text-base font-bold text-slate-900" id="modalTitle">Chi tiết Tờ trình & Dòng thời gian</h3>
                <span class="text-xs text-slate-500">Mã theo dõi quy trình: WF-2026-09881</span>
            </div>
            <button onclick="closeTimelineModal()" class="text-slate-400 hover:text-slate-600 text-lg">
                <i class="bi bi-x-lg"></i>
            </button>
        </div>

        <!-- Dòng Thời Gian Phê Duyệt Đa Cấp (Stepper Timeline) -->
        <div class="relative border-l-2 border-brand-200 ml-4 space-y-6 text-xs">
            <!-- Bước 1: Người đề xuất (Đã duyệt) -->
            <div class="relative pl-6">
                <span class="absolute -left-2.5 top-0 w-5 h-5 rounded-full bg-status-approved text-white flex items-center justify-center ring-4 ring-white text-[10px]">
                    <i class="bi bi-check"></i>
                </span>
                <div class="flex justify-between items-start">
                    <div>
                        <div class="font-bold text-slate-900">Bước 1: Khởi tạo Tờ trình & Gửi phê duyệt</div>
                        <div class="text-slate-500">Người thực hiện: Trưởng phòng HC Đơn vị • 09:30 11/10/2026</div>
                        <div class="mt-1 p-2 bg-slate-50 rounded border text-slate-700">"Kính gửi Ban Lãnh đạo xem xét phê duyệt theo nhu cầu vận hành thực tế."</div>
                    </div>
                    <span class="px-2 py-0.5 rounded text-[10px] font-bold bg-emerald-100 text-status-approved">Đã nộp</span>
                </div>
            </div>

            <!-- Bước 2: Ban C&B / Thẩm định (Đã duyệt) -->
            <div class="relative pl-6">
                <span class="absolute -left-2.5 top-0 w-5 h-5 rounded-full bg-status-approved text-white flex items-center justify-center ring-4 ring-white text-[10px]">
                    <i class="bi bi-check"></i>
                </span>
                <div class="flex justify-between items-start">
                    <div>
                        <div class="font-bold text-slate-900">Bước 2: Ban Nhân sự & C&B Tập đoàn Thẩm định</div>
                        <div class="text-slate-500">Người thực hiện: Giám đốc Nhân sự • 14:15 11/10/2026</div>
                        <div class="mt-1 p-2 bg-slate-50 rounded border text-slate-700">"Hồ sơ đầy đủ minh chứng, nằm trong quỹ ngân sách tăng lương còn lại của đơn vị."</div>
                    </div>
                    <span class="px-2 py-0.5 rounded text-[10px] font-bold bg-emerald-100 text-status-approved">Đã thẩm định</span>
                </div>
            </div>

            <!-- Bước 3: Giám đốc Khối / TGĐ (Đang chờ - Active Amber) -->
            <div class="relative pl-6">
                <span class="absolute -left-2.5 top-0 w-5 h-5 rounded-full bg-status-pending text-white flex items-center justify-center ring-4 ring-white text-[10px]">
                    <i class="bi bi-clock"></i>
                </span>
                <div class="flex justify-between items-start">
                    <div>
                        <div class="font-bold text-amber-700">Bước 3: Tổng Giám Đốc Phê duyệt Cuối cùng</div>
                        <div class="text-slate-500">Đang chờ bạn thực hiện thao tác ký duyệt điện tử...</div>
                    </div>
                    <span class="px-2 py-0.5 rounded text-[10px] font-bold bg-amber-100 text-status-pending">Chờ bạn duyệt</span>
                </div>
            </div>
        </div>

        <!-- Form nhập ý kiến & Nút thao tác -->
        <div class="border-t pt-4 space-y-3">
            <label class="block text-xs font-semibold text-slate-700">Ý kiến chỉ đạo / Lý do (Bắt buộc nếu Từ chối):</label>
            <textarea class="w-full text-xs p-2.5 border rounded-lg focus:ring-1 focus:ring-brand-500" rows="2" placeholder="Nhập ghi chú phê duyệt hoặc lý do điều chỉnh..."></textarea>
            <div class="flex justify-end gap-3 pt-2">
                <button onclick="closeTimelineModal()" class="px-4 py-2 border rounded-lg text-xs font-semibold text-slate-600 hover:bg-slate-50">
                    Đóng
                </button>
                <button onclick="alert('Đã từ chối hồ sơ thành công!'); closeTimelineModal();" class="px-4 py-2 bg-status-rejected hover:bg-red-700 text-white rounded-lg text-xs font-semibold">
                    Từ chối hồ sơ
                </button>
                <button onclick="alert('Đã phê duyệt hồ sơ thành công!'); closeTimelineModal();" class="px-4 py-2 bg-status-approved hover:bg-emerald-700 text-white rounded-lg text-xs font-semibold">
                    Đồng ý Phê duyệt
                </button>
            </div>
        </div>
    </div>
</div>

<script>
    function toggleSelectAll(master) {
        document.querySelectorAll('.row-checkbox').forEach(cb => cb.checked = master.checked);
    }

    function quickApprove(id) {
        if (confirm('Bạn có chắc chắn muốn phê duyệt hồ sơ này?')) {
            alert('Đã phê duyệt thành công hồ sơ #' + id);
        }
    }

    function quickReject(id) {
        const reason = prompt('Vui lòng nhập lý do từ chối:');
        if (reason) {
            alert('Đã từ chối hồ sơ #' + id + '. Lý do: ' + reason);
        }
    }

    function bulkApprove() {
        const checked = document.querySelectorAll('.row-checkbox:checked').length;
        if (checked === 0) {
            alert('Vui lòng chọn ít nhất một hồ sơ để duyệt hàng loạt.');
            return;
        }
        if (confirm('Phê duyệt đồng thời ' + checked + ' hồ sơ đã chọn?')) {
            alert('Đã duyệt thành công ' + checked + ' hồ sơ.');
        }
    }

    function bulkReject() {
        const checked = document.querySelectorAll('.row-checkbox:checked').length;
        if (checked === 0) {
            alert('Vui lòng chọn ít nhất một hồ sơ để từ chối.');
            return;
        }
        prompt('Nhập lý do từ chối cho ' + checked + ' hồ sơ đã chọn:');
    }

    function openTimelineModal(title, type) {
        document.getElementById('modalTitle').textContent = title;
        document.getElementById('timelineModal').classList.remove('hidden');
    }

    function closeTimelineModal() {
        document.getElementById('timelineModal').classList.add('hidden');
    }
</script>
@endsection
