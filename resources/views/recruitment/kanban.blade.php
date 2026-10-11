@extends('layouts.app')

@section('title', 'Bảng Kanban Tuyển Dụng ATS — Vinamilk HRIS')

@section('content')
<div class="container-fluid py-4">
    <div class="d-flex justify-content-between align-items-center mb-4">
        <div>
            <h1 class="h3 fw-bold text-primary mb-1">
                <i class="bi bi-kanban"></i> Quản lý Quy trình Tuyển dụng (ATS Pipeline)
            </h1>
            <p class="text-muted mb-0">Theo dõi vòng đời ứng viên từ nộp hồ sơ đến ký hợp đồng nhận việc</p>
        </div>
        <div class="d-flex gap-2">
            <!-- Lọc theo Đợt tuyển dụng -->
            <form method="GET" action="{{ route('recruitment.kanban') }}" class="d-flex gap-2">
                <select name="requisition_id" class="form-select" onchange="this.form.submit()">
                    <option value="">-- Tất cả vị trí đang tuyển --</option>
                    @foreach($requisitions as $req)
                        <option value="{{ $req->id }}" {{ $requisitionId == $req->id ? 'selected' : '' }}>
                            [{{ $req->requisition_code }}] {{ $req->title }}
                        </option>
                    @endforeach
                </select>
            </form>
            <a href="#" class="btn btn-primary"><i class="bi bi-plus-circle"></i> Thêm ứng viên giấy</a>
        </div>
    </div>

    @if(session('success'))
        <div class="alert alert-success alert-dismissible fade show" role="alert">
            <i class="bi bi-check-circle-fill me-2"></i> {{ session('success') }}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    @endif

    @if(session('error'))
        <div class="alert alert-danger alert-dismissible fade show" role="alert">
            <i class="bi bi-exclamation-triangle-fill me-2"></i> {{ session('error') }}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    @endif

    <!-- 7 Cột Kanban ATS -->
    <div class="d-flex gap-3 overflow-auto pb-4" style="min-height: 700px;">
        @php
            $colHeaders = [
                'NEW'          => ['title' => '1. Nhận hồ sơ', 'color' => 'secondary'],
                'SCREENING'    => ['title' => '2. Sàng lọc CV', 'color' => 'info'],
                'INTERVIEWING' => ['title' => '3. Phỏng vấn', 'color' => 'warning'],
                'HEALTH_CHECK' => ['title' => '4. Khám sức khỏe', 'color' => 'primary'],
                'OFFERED'      => ['title' => '5. Thỏa thuận Offer', 'color' => 'indigo'],
                'HIRED'        => ['title' => '6. Nhận việc (Onboard)', 'color' => 'success'],
                'REJECTED'     => ['title' => '7. Loại hồ sơ', 'color' => 'danger'],
            ];
        @endphp

        @foreach($colHeaders as $statusKey => $header)
            <div class="kanban-column bg-light rounded p-2 border" style="min-width: 280px; width: 280px;">
                <div class="d-flex justify-content-between align-items-center mb-3 px-2">
                    <span class="fw-bold text-dark">{{ $header['title'] }}</span>
                    <span class="badge bg-{{ $header['color'] }} rounded-pill">
                        {{ $columns[$statusKey]->count() }}
                    </span>
                </div>

                <div class="kanban-cards d-flex flex-column gap-2">
                    @forelse($columns[$statusKey] as $app)
                        <div class="card shadow-sm border-0">
                            <div class="card-body p-3">
                                <div class="d-flex justify-content-between align-items-start mb-1">
                                    <h6 class="fw-bold mb-0 text-dark">{{ $app->candidate->full_name }}</h6>
                                    <small class="text-muted">{{ $app->applied_date->format('d/m') }}</small>
                                </div>
                                <div class="small text-muted mb-2">
                                    <i class="bi bi-briefcase"></i> {{ $app->requisition->title }}
                                </div>

                                <!-- Thẻ trạng thái khám sức khỏe an toàn thực phẩm -->
                                @if($app->healthCheck)
                                    <div class="mb-2">
                                        @if($app->healthCheck->is_passed)
                                            <span class="badge bg-success-subtle text-success border border-success">
                                                <i class="bi bi-heart-pulse"></i> Thẻ Xanh: ĐẠT
                                            </span>
                                        @else
                                            <span class="badge bg-danger-subtle text-danger border border-danger">
                                                <i class="bi bi-x-circle"></i> Sức khỏe: KHÔNG ĐẠT
                                            </span>
                                        @endif
                                    </div>
                                @elseif($statusKey === 'HEALTH_CHECK' || $statusKey === 'OFFERED')
                                    <div class="mb-2">
                                        <span class="badge bg-warning-subtle text-warning border border-warning">
                                            <i class="bi bi-clock"></i> Chưa có KSK
                                        </span>
                                    </div>
                                @endif

                                <!-- Chi tiết Offer nếu có -->
                                @if($app->offer)
                                    <div class="p-2 bg-light rounded small mb-2">
                                        <div><strong>P1:</strong> {{ number_format($app->offer->offered_p1) }} đ</div>
                                        <div><strong>Thử việc:</strong> {{ $app->offer->probation_rate }}% (2 tháng)</div>
                                    </div>
                                @endif

                                <!-- Nút hành động tương ứng từng cột -->
                                <div class="d-flex justify-content-between align-items-center pt-2 border-top mt-2">
                                    @if($app->candidate->cv_file_url)
                                        <a href="{{ Storage::url($app->candidate->cv_file_url) }}" target="_blank" class="btn btn-sm btn-outline-secondary py-0" title="Xem CV">
                                            <i class="bi bi-file-earmark-pdf"></i> CV
                                        </a>
                                    @else
                                        <span></span>
                                    @endif

                                    <!-- Nút Tạo Offer khi đang ở cột Khám sức khỏe -->
                                    @if($statusKey === 'HEALTH_CHECK')
                                        <button class="btn btn-sm btn-primary py-0" 
                                                onclick="openOfferModal({{ $app->id }}, {{ $app->requisition->position_id }}, '{{ $app->candidate->full_name }}')">
                                            Tạo Offer
                                        </button>
                                    @endif

                                    <!-- Nút Tiếp nhận (Onboard) khi ứng viên đang ở Offer -->
                                    @if($statusKey === 'OFFERED' && $app->offer && $app->offer->status !== 'ACCEPTED')
                                        <form method="POST" action="{{ route('recruitment.offers.accept', $app->offer->id) }}">
                                            @csrf
                                            <button type="submit" class="btn btn-sm btn-success py-0" onclick="return confirm('Xác nhận ứng viên đã đồng ý Offer và kích hoạt tiếp nhận nhân viên mới?')">
                                                <i class="bi bi-person-check"></i> Onboard
                                            </button>
                                        </form>
                                    @endif

                                    @if($statusKey === 'HIRED')
                                        <span class="badge bg-success">Đã cấp mã NV</span>
                                    @endif
                                </div>
                            </div>
                        </div>
                    @empty
                        <div class="text-center text-muted py-4 small">
                            Không có hồ sơ
                        </div>
                    @endforelse
                </div>
            </div>
        @endforeach
    </div>
</div>

<!-- Modal Nhập Thông Tin Thư Mời Nhận Việc (Offer) -->
<div class="modal fade" id="createOfferModal" tabindex="-1">
    <div class="modal-dialog modal-dialog-centered">
        <form method="POST" id="form-create-offer" class="modal-content">
            @csrf
            <div class="modal-header bg-primary text-white">
                <h5 class="modal-title fw-bold">Khởi tạo Thư mời nhận việc (Offer)</h5>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body p-4">
                <input type="hidden" name="position_id" id="offer-position-id">
                
                <div class="mb-3">
                    <label class="form-label fw-bold">Ứng viên:</label>
                    <div id="offer-candidate-name" class="form-control-plaintext fw-bold text-primary"></div>
                </div>

                <div class="mb-3">
                    <label class="form-label fw-bold">Mức lương vị trí P1 (VND):</label>
                    <input type="number" name="offered_p1" class="form-control" required min="5000000" step="500000" value="15000000">
                </div>

                <div class="mb-3">
                    <label class="form-label fw-bold">Mức lương năng lực P2 (VND):</label>
                    <input type="number" name="offered_p2" class="form-control" required min="0" step="500000" value="3000000">
                </div>

                <div class="row g-2 mb-3">
                    <div class="col-md-6">
                        <label class="form-label fw-bold">Tỷ lệ lương thử việc (%):</label>
                        <input type="number" name="probation_rate" class="form-control" required min="85" max="100" value="85">
                        <small class="text-muted">Tối thiểu 85% theo luật</small>
                    </div>
                    <div class="col-md-6">
                        <label class="form-label fw-bold">Ngày bắt đầu làm việc:</label>
                        <input type="date" name="expected_start_date" class="form-control" required min="{{ date('Y-m-d') }}">
                    </div>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Hủy</button>
                <button type="submit" class="btn btn-primary"><i class="bi bi-send"></i> Phát hành Offer</button>
            </div>
        </form>
    </div>
</div>

<script>
function openOfferModal(appId, positionId, candidateName) {
    document.getElementById('form-create-offer').action = `/recruitment/applications/${appId}/offers`;
    document.getElementById('offer-position-id').value = positionId;
    document.getElementById('offer-candidate-name').textContent = candidateName;
    new bootstrap.Modal(document.getElementById('createOfferModal')).show();
}
</script>
@endsection
