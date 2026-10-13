@extends('layouts.app')

@section('title', 'Biểu mẫu 03: Thống kê Khen thưởng & Kỷ luật — Vinamilk HRIS')

@section('content')
<div class="container-fluid py-4">
    <!-- Header -->
    <div class="d-flex justify-content-between align-items-center mb-4">
        <div>
            <h1 class="h3 fw-bold text-primary mb-1">
                <i class="bi bi-award"></i> Biểu mẫu 03: Thống kê Khen thưởng & Kỷ luật
            </h1>
            <p class="text-muted mb-0">Theo dõi thi đua, danh hiệu xuất sắc và các quyết định kỷ luật lao động</p>
        </div>
        <div class="d-flex gap-2">
            <a href="{{ route('reports.export-excel', ['type' => 'bm03', 'year' => $year, 'org_path' => $orgPath]) }}" class="btn btn-success">
                <i class="bi bi-file-earmark-excel"></i> Xuất Excel (CSV)
            </a>
            <a href="{{ route('reports.print-report', ['type' => 'bm03', 'year' => $year, 'org_path' => $orgPath]) }}" target="_blank" class="btn btn-outline-primary">
                <i class="bi bi-printer"></i> In Khổ A4
            </a>
            <a href="{{ route('reports.index') }}" class="btn btn-outline-secondary">
                <i class="bi bi-arrow-left"></i> Quay lại
            </a>
        </div>
    </div>

    <!-- Bộ lọc -->
    <div class="card shadow-sm border-0 mb-4">
        <div class="card-body p-3">
            <form method="GET" action="{{ route('reports.bm03') }}" class="row g-3 align-items-end">
                <div class="col-md-3">
                    <label class="form-label small fw-semibold text-muted">Năm báo cáo</label>
                    <select name="year" class="form-select">
                        @for($y = date('Y') + 1; $y >= 2020; $y--)
                            <option value="{{ $y }}" {{ $year == $y ? 'selected' : '' }}>Năm {{ $y }}</option>
                        @endfor
                    </select>
                </div>
                <div class="col-md-4">
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
                <div class="col-md-2">
                    <button type="submit" class="btn btn-primary w-100">
                        <i class="bi bi-filter"></i> Áp dụng
                    </button>
                </div>
            </form>
        </div>
    </div>

    <!-- Bảng danh sách khen thưởng & kỷ luật -->
    <div class="card shadow-sm border-0">
        <div class="card-body p-0">
            <div class="table-responsive">
                <table class="table table-hover align-middle mb-0 text-nowrap">
                    <thead class="table-light">
                        <tr>
                            <th class="text-center" style="width: 50px;">STT</th>
                            <th>Số Quyết định</th>
                            <th>Ngày QĐ</th>
                            <th>Mã NV</th>
                            <th>Họ và Tên</th>
                            <th>Đơn vị</th>
                            <th>Chức danh</th>
                            <th>Hình thức</th>
                            <th>Lý do</th>
                            <th class="text-end">Số tiền thưởng (VND)</th>
                        </tr>
                    </thead>
                    <tbody>
                        @forelse($list as $idx => $r)
                        <tr>
                            <td class="text-center text-muted">{{ $idx + 1 }}</td>
                            <td class="font-monospace fw-semibold">{{ $r->so_quyet_dinh }}</td>
                            <td>{{ date('d/m/Y', strtotime($r->ngay_quyet_dinh)) }}</td>
                            <td><span class="badge bg-light text-primary border">{{ $r->employee_code }}</span></td>
                            <td class="fw-semibold text-dark">{{ $r->full_name }}</td>
                            <td>{{ $r->unit_name }}</td>
                            <td>{{ $r->position_name }}</td>
                            <td><span class="badge bg-success-subtle text-success border border-success">{{ $r->hinh_thuc }}</span></td>
                            <td>{{ $r->ly_do }}</td>
                            <td class="text-end font-monospace fw-bold text-success">{{ number_format($r->so_tien_thuong, 0, ',', '.') }}</td>
                        </tr>
                        @empty
                        <tr>
                            <td colspan="10" class="text-center py-5 text-muted">
                                <i class="bi bi-inbox fs-1 d-block mb-2"></i>
                                Không có bản ghi khen thưởng kỷ luật nào trong năm {{ $year }}.
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
