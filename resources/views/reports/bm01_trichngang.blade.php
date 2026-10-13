@extends('layouts.app')

@section('title', 'Biểu mẫu 01: Sơ yếu Lý lịch Trích ngang — Vinamilk HRIS')

@section('content')
<div class="container-fluid py-4">
    <!-- Header -->
    <div class="d-flex justify-content-between align-items-center mb-4">
        <div>
            <h1 class="h3 fw-bold text-primary mb-1">
                <i class="bi bi-file-earmark-person"></i> Biểu mẫu 01: Sơ yếu Lý lịch Trích ngang CBNV
            </h1>
            <p class="text-muted mb-0">Hồ sơ trích ngang cốt lõi cán bộ công nhân viên toàn hệ thống Vinamilk</p>
        </div>
        <div class="d-flex gap-2">
            <a href="{{ route('reports.export-excel', ['type' => 'bm01', 'org_path' => $filters['org_path'] ?? '/1/']) }}" class="btn btn-success">
                <i class="bi bi-file-earmark-excel"></i> Xuất Excel (CSV)
            </a>
            <a href="{{ route('reports.print-report', ['type' => 'bm01', 'org_path' => $filters['org_path'] ?? '/1/']) }}" target="_blank" class="btn btn-outline-primary">
                <i class="bi bi-printer"></i> In Khổ A4
            </a>
            <a href="{{ route('reports.index') }}" class="btn btn-outline-secondary">
                <i class="bi bi-arrow-left"></i> Quay lại
            </a>
        </div>
    </div>

    <!-- Bộ lọc tìm kiếm -->
    <div class="card shadow-sm border-0 mb-4">
        <div class="card-body p-3">
            <form method="GET" action="{{ route('reports.bm01') }}" class="row g-3 align-items-end">
                <div class="col-md-3">
                    <label class="form-label small fw-semibold text-muted">Đơn vị / Chi nhánh</label>
                    <select name="org_path" class="form-select">
                        <option value="/1/">-- Toàn Tập đoàn Vinamilk --</option>
                        @foreach($orgUnits as $ou)
                            <option value="{{ $ou->path }}" {{ ($filters['org_path'] ?? '') == $ou->path ? 'selected' : '' }}>
                                {{ str_repeat('— ', $ou->level - 1) }} {{ $ou->unit_name }}
                            </option>
                        @endforeach
                    </select>
                </div>
                <div class="col-md-3">
                    <label class="form-label small fw-semibold text-muted">Chức danh</label>
                    <select name="position_id" class="form-select">
                        <option value="">-- Tất cả chức danh --</option>
                        @foreach($positions as $pos)
                            <option value="{{ $pos->id }}" {{ ($filters['position_id'] ?? '') == $pos->id ? 'selected' : '' }}>
                                {{ $pos->position_name }}
                            </option>
                        @endforeach
                    </select>
                </div>
                <div class="col-md-2">
                    <label class="form-label small fw-semibold text-muted">Trạng thái</label>
                    <select name="employment_status" class="form-select">
                        <option value="">-- Tất cả --</option>
                        <option value="ACTIVE" {{ ($filters['employment_status'] ?? '') == 'ACTIVE' ? 'selected' : '' }}>Chính thức</option>
                        <option value="PROBATION" {{ ($filters['employment_status'] ?? '') == 'PROBATION' ? 'selected' : '' }}>Thử việc</option>
                        <option value="TERMINATED" {{ ($filters['employment_status'] ?? '') == 'TERMINATED' ? 'selected' : '' }}>Đã thôi việc</option>
                    </select>
                </div>
                <div class="col-md-3">
                    <label class="form-label small fw-semibold text-muted">Tìm kiếm từ khóa</label>
                    <input type="text" name="search" class="form-control" placeholder="Mã NV, Họ tên, CCCD..." value="{{ $filters['search'] ?? '' }}">
                </div>
                <div class="col-md-1">
                    <button type="submit" class="btn btn-primary w-100">
                        <i class="bi bi-search"></i> Lọc
                    </button>
                </div>
            </form>
        </div>
    </div>

    <!-- Bảng dữ liệu trích ngang -->
    <div class="card shadow-sm border-0">
        <div class="card-header bg-white py-3 d-flex justify-content-between align-items-center">
            <span class="fw-bold text-dark">
                Danh sách nhân sự (Tổng cộng: <span class="text-primary">{{ count($list) }}</span> người)
            </span>
        </div>
        <div class="card-body p-0">
            <div class="table-responsive" style="max-height: 600px;">
                <table class="table table-hover align-middle mb-0 text-nowrap">
                    <thead class="table-light sticky-top">
                        <tr>
                            <th class="text-center" style="width: 50px;">STT</th>
                            <th>Mã NV</th>
                            <th>Họ và Tên</th>
                            <th>Giới tính</th>
                            <th>Ngày sinh</th>
                            <th>Số CCCD</th>
                            <th>Đơn vị / Phòng ban</th>
                            <th>Chức danh</th>
                            <th>Khung lương</th>
                            <th class="text-end">Lương P1 (VND)</th>
                            <th class="text-end">Lương P2 (VND)</th>
                            <th class="text-end">Lương đóng BH</th>
                            <th class="text-center">Trạng thái</th>
                        </tr>
                    </thead>
                    <tbody>
                        @forelse($list as $idx => $r)
                        <tr>
                            <td class="text-center text-muted">{{ $idx + 1 }}</td>
                            <td><span class="badge bg-light text-primary border">{{ $r->employee_code }}</span></td>
                            <td class="fw-semibold text-dark">{{ $r->full_name }}</td>
                            <td>{{ $r->gender === 'MALE' ? 'Nam' : 'Nữ' }}</td>
                            <td>{{ date('d/m/Y', strtotime($r->date_of_birth)) }}</td>
                            <td>{{ $r->national_id }}</td>
                            <td>{{ $r->unit_name }}</td>
                            <td>{{ $r->position_name }}</td>
                            <td><span class="badge bg-secondary">{{ $r->band_code ?? 'BAND-STD' }}</span></td>
                            <td class="text-end font-monospace">{{ number_format($r->p1_position_salary, 0, ',', '.') }}</td>
                            <td class="text-end font-monospace">{{ number_format($r->p2_competency_salary, 0, ',', '.') }}</td>
                            <td class="text-end font-monospace text-success fw-semibold">{{ number_format($r->insurance_salary, 0, ',', '.') }}</td>
                            <td class="text-center">
                                @if($r->employment_status === 'ACTIVE')
                                    <span class="badge bg-success">Chính thức</span>
                                @elseif($r->employment_status === 'PROBATION')
                                    <span class="badge bg-warning text-dark">Thử việc</span>
                                @else
                                    <span class="badge bg-danger">{{ $r->employment_status }}</span>
                                @endif
                            </td>
                        </tr>
                        @empty
                        <tr>
                            <td colspan="13" class="text-center py-5 text-muted">
                                <i class="bi bi-inbox fs-1 d-block mb-2"></i>
                                Không tìm thấy bản ghi nhân sự nào phù hợp với bộ lọc.
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
