@extends('layouts.app')

@section('title', 'Biểu mẫu 02: Báo cáo Biến động Nhân sự — Vinamilk HRIS')

@section('content')
<div class="container-fluid py-4">
    <!-- Header -->
    <div class="d-flex justify-content-between align-items-center mb-4">
        <div>
            <h1 class="h3 fw-bold text-primary mb-1">
                <i class="bi bi-arrow-left-right"></i> Biểu mẫu 02: Báo cáo Biến động Nhân sự
            </h1>
            <p class="text-muted mb-0">Theo dõi chi tiết tuyển dụng mới, điều chuyển, bổ nhiệm và thôi việc theo năm</p>
        </div>
        <div class="d-flex gap-2">
            <a href="{{ route('reports.export-excel', ['type' => 'bm02', 'year' => $year, 'org_path' => $orgPath]) }}" class="btn btn-success">
                <i class="bi bi-file-earmark-excel"></i> Xuất Excel (CSV)
            </a>
            <a href="{{ route('reports.print-report', ['type' => 'bm02', 'year' => $year, 'org_path' => $orgPath]) }}" target="_blank" class="btn btn-outline-primary">
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
            <form method="GET" action="{{ route('reports.bm02') }}" class="row g-3 align-items-end">
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

    <!-- Bảng danh sách biến động -->
    <div class="card shadow-sm border-0">
        <div class="card-body p-0">
            <div class="table-responsive">
                <table class="table table-hover align-middle mb-0 text-nowrap">
                    <thead class="table-light">
                        <tr>
                            <th class="text-center" style="width: 50px;">STT</th>
                            <th>Mã NV</th>
                            <th>Họ và Tên</th>
                            <th class="text-center">Loại biến động</th>
                            <th>Diễn giải</th>
                            <th>Ngày hiệu lực</th>
                            <th>Đơn vị cũ</th>
                            <th>Đơn vị mới</th>
                            <th>Chức danh mới</th>
                            <th>Số Quyết định</th>
                        </tr>
                    </thead>
                    <tbody>
                        @forelse($list as $idx => $r)
                        <tr>
                            <td class="text-center text-muted">{{ $idx + 1 }}</td>
                            <td><span class="badge bg-light text-primary border">{{ $r->employee_code }}</span></td>
                            <td class="fw-semibold text-dark">{{ $r->full_name }}</td>
                            <td class="text-center">
                                @if($r->loai_bien_dong === 'TUYEN_MOI')
                                    <span class="badge bg-success"><i class="bi bi-person-plus"></i> Tuyển mới</span>
                                @elseif($r->loai_bien_dong === 'DIEU_CHUYEN')
                                    <span class="badge bg-info text-white"><i class="bi bi-arrow-repeat"></i> Điều chuyển</span>
                                @else
                                    <span class="badge bg-danger"><i class="bi bi-person-x"></i> Thôi việc</span>
                                @endif
                            </td>
                            <td>{{ $r->dien_giai }}</td>
                            <td>{{ date('d/m/Y', strtotime($r->ngay_hieu_luc)) }}</td>
                            <td class="text-muted">{{ $r->don_vi_cu }}</td>
                            <td class="fw-semibold text-primary">{{ $r->don_vi_moi }}</td>
                            <td>{{ $r->chuc_vu_moi }}</td>
                            <td class="text-muted font-monospace">{{ $r->so_quyet_dinh }}</td>
                        </tr>
                        @empty
                        <tr>
                            <td colspan="10" class="text-center py-5 text-muted">
                                <i class="bi bi-inbox fs-1 d-block mb-2"></i>
                                Không có bản ghi biến động nhân sự nào trong năm {{ $year }}.
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
