@extends('layouts.app')

@section('title', 'Biểu mẫu 04: Nhân sự Tiệm cận Nghỉ hưu (NĐ 135/2020) — Vinamilk HRIS')

@section('content')
<div class="container-fluid py-4">
    <!-- Header -->
    <div class="d-flex justify-content-between align-items-center mb-4">
        <div>
            <h1 class="h3 fw-bold text-primary mb-1">
                <i class="bi bi-clock-history"></i> Biểu mẫu 04: Thống kê Nhân sự Tiệm cận Nghỉ hưu
            </h1>
            <p class="text-muted mb-0">Tính toán chính xác theo lộ trình tuổi nghỉ hưu của <strong>Nghị định 135/2020/NĐ-CP</strong> phục vụ quy hoạch kế cận</p>
        </div>
        <div class="d-flex gap-2">
            <a href="{{ route('reports.export-excel', ['type' => 'bm04', 'org_path' => $orgPath]) }}" class="btn btn-success">
                <i class="bi bi-file-earmark-excel"></i> Xuất Excel (CSV)
            </a>
            <a href="{{ route('reports.print-report', ['type' => 'bm04', 'org_path' => $orgPath]) }}" target="_blank" class="btn btn-outline-primary">
                <i class="bi bi-printer"></i> In Khổ A4
            </a>
            <a href="{{ route('reports.index') }}" class="btn btn-outline-secondary">
                <i class="bi bi-arrow-left"></i> Quay lại
            </a>
        </div>
    </div>

    <!-- Alert thông tin pháp lý NĐ 135/2020 -->
    <div class="alert alert-info border-0 shadow-sm d-flex align-items-center mb-4">
        <i class="bi bi-info-circle-fill fs-3 me-3 text-info"></i>
        <div class="small">
            <strong>Căn cứ Pháp lý: Nghị định 135/2020/NĐ-CP (Bộ luật Lao động 2019):</strong>
            <span class="d-block">
                • <strong>Lao động Nam:</strong> Năm 2021 là 60 tuổi 03 tháng; sau đó mỗi năm tăng thêm 03 tháng đến khi đạt 62 tuổi vào năm 2028.
            </span>
            <span class="d-block">
                • <strong>Lao động Nữ:</strong> Năm 2021 là 55 tuổi 04 tháng; sau đó mỗi năm tăng thêm 04 tháng đến khi đạt 60 tuổi vào năm 2035.
            </span>
        </div>
    </div>

    <!-- Bộ lọc -->
    <div class="card shadow-sm border-0 mb-4">
        <div class="card-body p-3">
            <form method="GET" action="{{ route('reports.bm04') }}" class="row g-3 align-items-end">
                <div class="col-md-5">
                    <label class="form-label small fw-semibold text-muted">Đơn vị / Chi nhánh</label>
                    <select name="org_path" class="form-select">
                        <option value="/1/">-- Toàn Tập đoàn Vinamilk --</option>
                        @foreach($orgUnits as $ou)
                            <option value="{{ $ou->path }}" {{ $orgPath == $ou->path ? 'selected' : '' }}>
                                {{ str_repeat('— ', $ou->level - 1) }} {{ $ou->unit_name }}
                            </option>
                        @endforeach
                    </select>
                </div>
                <div class="col-md-3">
                    <label class="form-label small fw-semibold text-muted">Thời gian rà soát</label>
                    <select name="within_months" class="form-select">
                        <option value="6" {{ $withinMonths == 6 ? 'selected' : '' }}>Trong vòng 6 tháng tới</option>
                        <option value="12" {{ $withinMonths == 12 ? 'selected' : '' }}>Trong vòng 12 tháng tới (1 năm)</option>
                        <option value="24" {{ $withinMonths == 24 ? 'selected' : '' }}>Trong vòng 24 tháng tới (2 năm)</option>
                        <option value="36" {{ $withinMonths == 36 ? 'selected' : '' }}>Trong vòng 36 tháng tới (3 năm)</option>
                    </select>
                </div>
                <div class="col-md-2">
                    <button type="submit" class="btn btn-primary w-100">
                        <i class="bi bi-filter"></i> Rà soát
                    </button>
                </div>
            </form>
        </div>
    </div>

    <!-- Bảng danh sách tiệm cận nghỉ hưu -->
    <div class="card shadow-sm border-0">
        <div class="card-header bg-white py-3 d-flex justify-content-between align-items-center">
            <span class="fw-bold text-dark">
                Danh sách nhân sự tiệm cận nghỉ hưu (Tìm thấy: <span class="text-danger fw-bold">{{ count($list) }}</span> người)
            </span>
        </div>
        <div class="card-body p-0">
            <div class="table-responsive">
                <table class="table table-hover align-middle mb-0 text-nowrap">
                    <thead class="table-light">
                        <tr>
                            <th class="text-center" style="width: 50px;">STT</th>
                            <th>Mã NV</th>
                            <th>Họ và Tên</th>
                            <th class="text-center">Giới tính</th>
                            <th>Ngày sinh</th>
                            <th class="text-center">Tuổi hiện tại</th>
                            <th>Đơn vị / Nhà máy</th>
                            <th>Chức danh</th>
                            <th class="text-center">Ngày nghỉ hưu chuẩn</th>
                            <th class="text-center">Thời gian còn lại</th>
                            <th class="text-center">Tình trạng kế cận</th>
                        </tr>
                    </thead>
                    <tbody>
                        @forelse($list as $idx => $r)
                        <tr>
                            <td class="text-center text-muted">{{ $idx + 1 }}</td>
                            <td><span class="badge bg-light text-primary border">{{ $r->employee_code }}</span></td>
                            <td class="fw-semibold text-dark">{{ $r->full_name }}</td>
                            <td class="text-center">
                                @if($r->gender === 'MALE')
                                    <span class="badge bg-primary-subtle text-primary">Nam</span>
                                @else
                                    <span class="badge bg-danger-subtle text-danger">Nữ</span>
                                @endif
                            </td>
                            <td>{{ date('d/m/Y', strtotime($r->date_of_birth)) }}</td>
                            <td class="text-center fw-bold">{{ $r->current_age }} tuổi</td>
                            <td>{{ $r->unit_name }}</td>
                            <td>{{ $r->position_name }}</td>
                            <td class="text-center fw-bold text-danger">{{ date('d/m/Y', strtotime($r->retirement_date)) }}</td>
                            <td class="text-center">
                                @if($r->months_left <= 3)
                                    <span class="badge bg-danger">Còn {{ $r->months_left }} tháng ({{ $r->days_left }} ngày)</span>
                                @elseif($r->months_left <= 6)
                                    <span class="badge bg-warning text-dark">Còn {{ $r->months_left }} tháng</span>
                                @else
                                    <span class="badge bg-secondary">Còn {{ $r->months_left }} tháng</span>
                                @endif
                            </td>
                            <td class="text-center">
                                <span class="badge bg-light text-dark border">
                                    <i class="bi bi-people"></i> Đang quy hoạch thay thế
                                </span>
                            </td>
                        </tr>
                        @empty
                        <tr>
                            <td colspan="11" class="text-center py-5 text-muted">
                                <i class="bi bi-check-circle fs-1 text-success d-block mb-2"></i>
                                Không có nhân sự nào đến hạn nghỉ hưu trong vòng {{ $withinMonths }} tháng tới.
                            </td>
                        </tr>
                        @endforelse
                    </tbody>
                </table>
            </div>
        </div>
    </div>
</div>
@endsection
