@extends('layouts.app')

@section('title', 'Trung tâm Báo cáo Thống kê & HR Analytics — Vinamilk HRIS')

@section('content')
<div class="container-fluid py-4">
    <!-- Header -->
    <div class="d-flex justify-content-between align-items-center mb-4">
        <div>
            <h1 class="h3 fw-bold text-primary mb-1">
                <i class="bi bi-graph-up-arrow"></i> Trung tâm Báo cáo Thống kê & HR Analytics
            </h1>
            <p class="text-muted mb-0">Hệ thống phân tích dữ liệu nhân sự, 10 chỉ số trọng yếu và biểu mẫu hành chính BM01–BM04</p>
        </div>
        <div class="d-flex gap-2">
            <div class="dropdown">
                <button class="btn btn-outline-primary dropdown-toggle" type="button" data-bs-toggle="dropdown">
                    <i class="bi bi-file-earmark-text"></i> Truy cập Biểu mẫu BM01–BM04
                </button>
                <ul class="dropdown-menu dropdown-menu-end shadow">
                    <li><a class="dropdown-item py-2" href="{{ route('reports.bm01') }}"><i class="bi bi-person-lines-fill text-primary me-2"></i> BM01 - Sơ yếu trích ngang</a></li>
                    <li><a class="dropdown-item py-2" href="{{ route('reports.bm02') }}"><i class="bi bi-arrow-left-right text-success me-2"></i> BM02 - Biến động nhân sự</a></li>
                    <li><a class="dropdown-item py-2" href="{{ route('reports.bm03') }}"><i class="bi bi-award text-warning me-2"></i> BM03 - Khen thưởng & Kỷ luật</a></li>
                    <li><a class="dropdown-item py-2" href="{{ route('reports.bm04') }}"><i class="bi bi-clock-history text-danger me-2"></i> BM04 - Nghỉ hưu (NĐ 135/2020)</a></li>
                </ul>
            </div>
        </div>
    </div>

    <!-- Thanh chuyển đổi 3 Cấp độ Dashboard -->
    <ul class="nav nav-pills nav-fill bg-light p-2 rounded mb-4 shadow-sm" id="dashboardTabs" role="tablist">
        <li class="nav-item" role="presentation">
            <button class="nav-link active fw-bold py-2" id="bod-tab" data-bs-toggle="pill" data-bs-target="#bod-dashboard" type="button" role="tab">
                <i class="bi bi-building"></i> 1. Ban Tổng Giám Đốc (Toàn Tập Đoàn)
            </button>
        </li>
        <li class="nav-item" role="presentation">
            <button class="nav-link fw-bold py-2" id="plant-tab" data-bs-toggle="pill" data-bs-target="#plant-dashboard" type="button" role="tab">
                <i class="bi bi-gear-wide-connected"></i> 2. Giám Đốc Nhà Máy (Khối Vận Hành)
            </button>
        </li>
        <li class="nav-item" role="presentation">
            <button class="nav-link fw-bold py-2" id="dept-tab" data-bs-toggle="pill" data-bs-target="#dept-dashboard" type="button" role="tab">
                <i class="bi bi-people"></i> 3. Trưởng Phòng / Line Manager
            </button>
        </li>
    </ul>

    <div class="tab-content" id="dashboardTabsContent">
        <!-- =================================================================== -->
        <!-- DASHBOARD 1: BAN TỔNG GIÁM ĐỐC (EXECUTIVE C-LEVEL HR DASHBOARD)    -->
        <!-- =================================================================== -->
        <div class="tab-pane fade show active" id="bod-dashboard" role="tabpanel">
            <div class="card border-0 shadow-sm mb-4 bg-primary text-white p-3 rounded">
                <div class="d-flex align-items-center">
                    <i class="bi bi-question-circle-fill fs-2 me-3 text-light"></i>
                    <div>
                        <h6 class="fw-bold mb-1">CÂU HỎI CHIẾN LƯỢC BAN TGĐ:</h6>
                        <p class="mb-0 small text-light opacity-90">
                            Quy mô và chi phí quỹ lương toàn tập đoàn có tương xứng với hiệu quả kinh doanh? Khối nào đang có tỷ lệ thôi việc tự nguyện cao gây thất thoát nhân tài? Chi phí làm thêm giờ (OT) có đang vượt ngưỡng an toàn?
                        </p>
                    </div>
                </div>
            </div>

            <!-- 5 Thẻ số trọng yếu Ban TGĐ -->
            <div class="row g-3 mb-4">
                <div class="col-md-2">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-body p-3 text-center">
                            <span class="text-muted small fw-semibold">TỔNG HEADCOUNT</span>
                            <h3 class="fw-bold text-primary mt-2 mb-0">10.850</h3>
                            <span class="badge bg-success-subtle text-success mt-2"><i class="bi bi-arrow-up"></i> +2.1% MoM</span>
                        </div>
                    </div>
                </div>
                <div class="col-md-2">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-body p-3 text-center">
                            <span class="text-muted small fw-semibold">TURNOVER TỰ NGUYỆN</span>
                            <h3 class="fw-bold text-danger mt-2 mb-0">4.1%</h3>
                            <span class="badge bg-light text-muted border mt-2">Toàn kỳ 5.4%</span>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-body p-3 text-center">
                            <span class="text-muted small fw-semibold">CHI PHÍ OT / QUỸ LƯƠNG</span>
                            <h3 class="fw-bold text-warning mt-2 mb-0">7.8%</h3>
                            <span class="badge bg-success-subtle text-success mt-2">Dưới trần kiểm soát 10%</span>
                        </div>
                    </div>
                </div>
                <div class="col-md-2">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-body p-3 text-center">
                            <span class="text-muted small fw-semibold">KPI BÌNH QUÂN</span>
                            <h3 class="fw-bold text-success mt-2 mb-0">103.5</h3>
                            <span class="badge bg-success mt-2">Xếp loại Tốt</span>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-body p-3 text-center">
                            <span class="text-muted small fw-semibold">TIME TO HIRE TRUNG BÌNH</span>
                            <h3 class="fw-bold text-info mt-2 mb-0">24.5 ngày</h3>
                            <span class="badge bg-light text-muted border mt-2">Target &le; 30 ngày</span>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Các biểu đồ Ban TGĐ -->
            <div class="row g-4 mb-4">
                <div class="col-md-7">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-header bg-white py-3 fw-bold">
                            <i class="bi bi-bar-chart-steps text-primary"></i> Cơ cấu Quỹ Lương 3P & Chi phí Nhân công theo Khối
                            <small class="text-muted d-block fw-normal">Dạng biểu đồ: Stacked Bar Chart (Cột chồng) giúp C-Level phân rã tức thì P1, P2, P3 và phụ cấp</small>
                        </div>
                        <div class="card-body p-3">
                            <div class="p-4 bg-light rounded text-center" style="height: 280px; display: flex; align-items: center; justify-content: center; flex-direction: column;">
                                <div class="w-100 mb-3">
                                    <div class="d-flex justify-content-between small text-muted mb-1">
                                        <span>Khối Nhà máy (45%)</span>
                                        <span class="fw-bold">145.2 tỷ VNĐ (P1: 40%, P2: 25%, P3: 20%, OT: 15%)</span>
                                    </div>
                                    <div class="progress" style="height: 20px;">
                                        <div class="progress-bar bg-primary" style="width: 40%">P1: 40%</div>
                                        <div class="progress-bar bg-info" style="width: 25%">P2: 25%</div>
                                        <div class="progress-bar bg-success" style="width: 20%">P3: 20%</div>
                                        <div class="progress-bar bg-warning text-dark" style="width: 15%">OT: 15%</div>
                                    </div>
                                </div>
                                <div class="w-100 mb-3">
                                    <div class="d-flex justify-content-between small text-muted mb-1">
                                        <span>Khối Trang trại (25%)</span>
                                        <span class="fw-bold">80.5 tỷ VNĐ</span>
                                    </div>
                                    <div class="progress" style="height: 20px;">
                                        <div class="progress-bar bg-primary" style="width: 50%">P1: 50%</div>
                                        <div class="progress-bar bg-info" style="width: 25%">P2: 25%</div>
                                        <div class="progress-bar bg-success" style="width: 15%">P3: 15%</div>
                                        <div class="progress-bar bg-warning text-dark" style="width: 10%">OT: 10%</div>
                                    </div>
                                </div>
                                <div class="w-100">
                                    <div class="d-flex justify-content-between small text-muted mb-1">
                                        <span>Khối Kinh doanh & Trụ sở (30%)</span>
                                        <span class="fw-bold">96.8 tỷ VNĐ</span>
                                    </div>
                                    <div class="progress" style="height: 20px;">
                                        <div class="progress-bar bg-primary" style="width: 45%">P1: 45%</div>
                                        <div class="progress-bar bg-info" style="width: 30%">P2: 30%</div>
                                        <div class="progress-bar bg-success" style="width: 22%">P3: 22%</div>
                                        <div class="progress-bar bg-warning text-dark" style="width: 3%">OT: 3%</div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="col-md-5">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-header bg-white py-3 fw-bold">
                            <i class="bi bi-pie-chart text-danger"></i> Tỷ trọng Thôi việc Tự nguyện theo Khối
                            <small class="text-muted d-block fw-normal">Dạng biểu đồ: Donut Chart nhận diện ngay khối bị chảy máu chất xám</small>
                        </div>
                        <div class="card-body p-4 text-center">
                            <div class="py-4">
                                <h4 class="fw-bold text-dark mb-1">445 nhân sự thôi việc</h4>
                                <p class="text-muted small">Trong 12 tháng qua (Tỷ lệ bình quân 4.1%)</p>
                                <div class="mt-4 text-start">
                                    <div class="d-flex justify-content-between mb-2">
                                        <span><i class="bi bi-circle-fill text-danger me-2"></i> Khối Kinh doanh & Bán lẻ</span>
                                        <strong>42% (187 người)</strong>
                                    </div>
                                    <div class="d-flex justify-content-between mb-2">
                                        <span><i class="bi bi-circle-fill text-warning me-2"></i> Khối Nhà máy Sản xuất</span>
                                        <strong>33% (147 người)</strong>
                                    </div>
                                    <div class="d-flex justify-content-between mb-2">
                                        <span><i class="bi bi-circle-fill text-info me-2"></i> Khối Trang trại Bò sữa</span>
                                        <strong>16% (71 người)</strong>
                                    </div>
                                    <div class="d-flex justify-content-between">
                                        <span><i class="bi bi-circle-fill text-primary me-2"></i> Khối Văn phòng Trụ sở</span>
                                        <strong>9% (40 người)</strong>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- =================================================================== -->
        <!-- DASHBOARD 2: GIÁM ĐỐC NHÀ MÁY (FACTORY OPERATIONS DASHBOARD)        -->
        <!-- =================================================================== -->
        <div class="tab-pane fade" id="plant-dashboard" role="tabpanel">
            <div class="card border-0 shadow-sm mb-4 bg-info text-white p-3 rounded">
                <div class="d-flex align-items-center">
                    <i class="bi bi-gear-fill fs-2 me-3 text-light"></i>
                    <div>
                        <h6 class="fw-bold mb-1">CÂU HỎI TÁC NGHIỆP GIÁM ĐỐC NHÀ MÁY:</h6>
                        <p class="mb-0 small text-light opacity-90">
                            Tỷ lệ vắng công các ca có ảnh hưởng dây chuyền đóng gói tiệt trùng? Bao nhiêu công nhân sắp chạm trần giờ OT tháng (40h/tháng)? Có bao nhiêu thợ bậc cao sắp nghỉ hưu cần kế hoạch kèm cặp thế hệ mới?
                        </p>
                    </div>
                </div>
            </div>

            <!-- 5 Thẻ số trọng yếu Nhà máy -->
            <div class="row g-3 mb-4">
                <div class="col-md-2">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-body p-3 text-center">
                            <span class="text-muted small fw-semibold">CÔNG NHÂN CÓ MẶT HÔM NAY</span>
                            <h3 class="fw-bold text-primary mt-2 mb-0">1.240</h3>
                            <span class="badge bg-light text-muted border mt-2">Tổng số: 1.270 người</span>
                        </div>
                    </div>
                </div>
                <div class="col-md-2">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-body p-3 text-center">
                            <span class="text-muted small fw-semibold">TỶ LỆ VẮNG CÔNG</span>
                            <h3 class="fw-bold text-danger mt-2 mb-0">2.4%</h3>
                            <span class="badge bg-danger-subtle text-danger mt-2">Ốm: 1.5%, AWOL: 0.9%</span>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-body p-3 text-center">
                            <span class="text-muted small fw-semibold">TỔNG SỐ GIỜ TĂNG CA (OT)</span>
                            <h3 class="fw-bold text-warning mt-2 mb-0">8.450 giờ</h3>
                            <span class="badge bg-light text-muted border mt-2">Bình quân 14.2h / công nhân</span>
                        </div>
                    </div>
                </div>
                <div class="col-md-2">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-body p-3 text-center">
                            <span class="text-muted small fw-semibold">CẢNH BÁO VƯỢT TRẦN OT</span>
                            <h3 class="fw-bold text-danger mt-2 mb-0">18 người</h3>
                            <span class="badge bg-danger mt-2">Đã vượt 32h / tháng</span>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-body p-3 text-center">
                            <span class="text-muted small fw-semibold">TIỆM CẬN NGHỈ HƯU (12 THÁNG)</span>
                            <h3 class="fw-bold text-primary mt-2 mb-0">12 thợ bậc cao</h3>
                            <span class="badge bg-info text-white mt-2">Theo lộ trình NĐ 135/2020</span>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Heatmap Ca kíp & Gauge Chi phí OT -->
            <div class="row g-4 mb-4">
                <div class="col-md-8">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-header bg-white py-3 fw-bold">
                            <i class="bi bi-grid-3x3-gap text-primary"></i> Ma trận Vắng công (Heatmap) theo Ngày trong tuần & Ca làm việc
                            <small class="text-muted d-block fw-normal">Dạng biểu đồ: Heatmap Grid Matrix phát hiện tức thời ca trực có nguy cơ thiếu hụt nhân công</small>
                        </div>
                        <div class="card-body p-4">
                            <div class="table-responsive">
                                <table class="table table-bordered text-center align-middle mb-0">
                                    <thead class="table-light">
                                        <tr>
                                            <th>Ca làm việc</th>
                                            <th>Thứ 2</th>
                                            <th>Thứ 3</th>
                                            <th>Thứ 4</th>
                                            <th>Thứ 5</th>
                                            <th>Thứ 6</th>
                                            <th>Thứ 7</th>
                                            <th>Chủ Nhật</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <tr>
                                            <td class="fw-bold">Ca 1 (06:00 - 14:00)</td>
                                            <td class="bg-success-subtle text-success">1.1%</td>
                                            <td class="bg-success-subtle text-success">0.9%</td>
                                            <td class="bg-success-subtle text-success">1.2%</td>
                                            <td class="bg-success-subtle text-success">1.0%</td>
                                            <td class="bg-warning-subtle text-warning">2.1%</td>
                                            <td class="bg-warning-subtle text-warning">2.8%</td>
                                            <td class="bg-danger-subtle text-danger fw-bold">4.2%</td>
                                        </tr>
                                        <tr>
                                            <td class="fw-bold">Ca 2 (14:00 - 22:00)</td>
                                            <td class="bg-success-subtle text-success">1.4%</td>
                                            <td class="bg-success-subtle text-success">1.3%</td>
                                            <td class="bg-success-subtle text-success">1.5%</td>
                                            <td class="bg-success-subtle text-success">1.6%</td>
                                            <td class="bg-warning-subtle text-warning">2.4%</td>
                                            <td class="bg-danger-subtle text-danger fw-bold">3.9%</td>
                                            <td class="bg-danger-subtle text-danger fw-bold">4.8%</td>
                                        </tr>
                                        <tr>
                                            <td class="fw-bold">Ca 3 (22:00 - 06:00)</td>
                                            <td class="bg-warning-subtle text-warning">2.2%</td>
                                            <td class="bg-warning-subtle text-warning">2.0%</td>
                                            <td class="bg-warning-subtle text-warning">2.5%</td>
                                            <td class="bg-warning-subtle text-warning">2.7%</td>
                                            <td class="bg-danger-subtle text-danger fw-bold">3.8%</td>
                                            <td class="bg-danger-subtle text-danger fw-bold">5.1%</td>
                                            <td class="bg-danger-subtle text-danger fw-bold">6.5%</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                            <div class="mt-3 small text-muted">
                                <span class="badge bg-success-subtle text-success me-2">&lt; 2%: An toàn</span>
                                <span class="badge bg-warning-subtle text-warning me-2">2% - 3.5%: Lưu ý</span>
                                <span class="badge bg-danger-subtle text-danger">&gt; 3.5%: Nguy cơ thiếu hụt</span>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-header bg-white py-3 fw-bold">
                            <i class="bi bi-speedometer2 text-warning"></i> Đo lường Chi phí OT Nhà máy
                            <small class="text-muted d-block fw-normal">Dạng biểu đồ: Gauge Meter kiểm soát ngân sách</small>
                        </div>
                        <div class="card-body p-4 text-center">
                            <div class="py-4">
                                <div class="display-3 fw-bold text-warning mb-2">8.2%</div>
                                <h6 class="fw-bold text-dark">Tỷ lệ Tiền OT / Quỹ Lương Nhà máy</h6>
                                <p class="text-muted small">Giới hạn cảnh báo đỏ: 12.0%</p>
                                <div class="progress mt-4" style="height: 12px;">
                                    <div class="progress-bar bg-success" style="width: 50%"></div>
                                    <div class="progress-bar bg-warning" style="width: 25%"></div>
                                    <div class="progress-bar bg-danger" style="width: 25%"></div>
                                </div>
                                <div class="d-flex justify-content-between small text-muted mt-2">
                                    <span>0%</span>
                                    <span class="text-success">6%</span>
                                    <span class="text-warning">9%</span>
                                    <span class="text-danger">12%</span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- =================================================================== -->
        <!-- DASHBOARD 3: TRƯỞNG PHÒNG / LINE MANAGER (DEPARTMENTAL DASHBOARD)   -->
        <!-- =================================================================== -->
        <div class="tab-pane fade" id="dept-dashboard" role="tabpanel">
            <div class="card border-0 shadow-sm mb-4 bg-success text-white p-3 rounded">
                <div class="d-flex align-items-center">
                    <i class="bi bi-people-fill fs-2 me-3 text-light"></i>
                    <div>
                        <h6 class="fw-bold mb-1">CÂU HỎI QUẢN TRỊ ĐỘI NGŨ TRƯỞNG PHÒNG:</h6>
                        <p class="mb-0 small text-light opacity-90">
                            Tiến độ các nhiệm vụ KPI tháng có đúng hạn không? Mức lương của nhân sự trong phòng so với khung lương (Compa-ratio) có công bằng để tránh bất mãn? Còn bao nhiêu ngày phép tồn cần sắp xếp nghỉ?
                        </p>
                    </div>
                </div>
            </div>

            <!-- 5 Thẻ số Trưởng phòng -->
            <div class="row g-3 mb-4">
                <div class="col-md-2">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-body p-3 text-center">
                            <span class="text-muted small fw-semibold">QUÂN SỐ PHÒNG</span>
                            <h3 class="fw-bold text-primary mt-2 mb-0">18</h3>
                            <span class="badge bg-light text-muted border mt-2">Đủ định biên 100%</span>
                        </div>
                    </div>
                </div>
                <div class="col-md-2">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-body p-3 text-center">
                            <span class="text-muted small fw-semibold">KPI TRUNG BÌNH</span>
                            <h3 class="fw-bold text-success mt-2 mb-0">104.2</h3>
                            <span class="badge bg-success-subtle text-success mt-2">Hoàn thành tốt</span>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-body p-3 text-center">
                            <span class="text-muted small fw-semibold">TỶ LỆ NHIỆM VỤ TRỄ HẠN</span>
                            <h3 class="fw-bold text-danger mt-2 mb-0">4.5%</h3>
                            <span class="badge bg-light text-muted border mt-2">2 / 44 nhiệm vụ</span>
                        </div>
                    </div>
                </div>
                <div class="col-md-2">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-body p-3 text-center">
                            <span class="text-muted small fw-semibold">COMPA-RATIO PHÒNG</span>
                            <h3 class="fw-bold text-info mt-2 mb-0">96.8%</h3>
                            <span class="badge bg-info-subtle text-info mt-2">Vùng tối ưu (80-120%)</span>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-body p-3 text-center">
                            <span class="text-muted small fw-semibold">PHÉP TỒN BÌNH QUÂN</span>
                            <h3 class="fw-bold text-secondary mt-2 mb-0">6.5 ngày/người</h3>
                            <span class="badge bg-warning-subtle text-dark mt-2">Cần điều phối nghỉ phép</span>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Biểu đồ Compa-ratio & Tiến độ công việc của từng thành viên -->
            <div class="row g-4 mb-4">
                <div class="col-md-7">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-header bg-white py-3 fw-bold">
                            <i class="bi bi-sliders text-primary"></i> Phân bổ Compa-ratio của Thành viên trong Phòng
                            <small class="text-muted d-block fw-normal">Dạng biểu đồ: Box Plot / Distribution Bar so sánh lương cá nhân với Midpoint (100%)</small>
                        </div>
                        <div class="card-body p-4">
                            <div class="mb-3">
                                <div class="d-flex justify-content-between small mb-1">
                                    <span>Trả dưới khung (&lt; 80% Midpoint) - Nguy cơ nghỉ việc</span>
                                    <span class="fw-bold text-danger">1 nhân sự (5.5%)</span>
                                </div>
                                <div class="progress" style="height: 10px;">
                                    <div class="progress-bar bg-danger" style="width: 5.5%"></div>
                                </div>
                            </div>
                            <div class="mb-3">
                                <div class="d-flex justify-content-between small mb-1">
                                    <span>Vùng tối ưu chuẩn cạnh tranh (80% - 120% Midpoint)</span>
                                    <span class="fw-bold text-success">15 nhân sự (83.3%)</span>
                                </div>
                                <div class="progress" style="height: 10px;">
                                    <div class="progress-bar bg-success" style="width: 83.3%"></div>
                                </div>
                            </div>
                            <div class="mb-3">
                                <div class="d-flex justify-content-between small mb-1">
                                    <span>Trả vượt khung (&gt; 120% Midpoint) - Cần xem xét thăng tiến</span>
                                    <span class="fw-bold text-warning">2 nhân sự (11.1%)</span>
                                </div>
                                <div class="progress" style="height: 10px;">
                                    <div class="progress-bar bg-warning" style="width: 11.1%"></div>
                                </div>
                            </div>
                            <div class="alert alert-light border small mt-4 mb-0">
                                <i class="bi bi-lightbulb text-warning me-2"></i>
                                <strong>Gợi ý quản trị:</strong> 1 nhân sự dưới 80% (mới tốt nghiệp tuyển dụng) cần được xem xét nâng lương bậc sau 6 tháng nếu KPI đạt loại A.
                            </div>
                        </div>
                    </div>
                </div>
                <div class="col-md-5">
                    <div class="card shadow-sm border-0 h-100">
                        <div class="card-header bg-white py-3 fw-bold">
                            <i class="bi bi-check2-circle text-success"></i> Cơ cấu Trạng thái Nhiệm vụ KPI Tháng
                            <small class="text-muted d-block fw-normal">Dạng biểu đồ: Donut Chart giám sát SLA tức thời</small>
                        </div>
                        <div class="card-body p-4 text-center">
                            <div class="py-4">
                                <div class="display-4 fw-bold text-success mb-1">88.6%</div>
                                <div class="fw-semibold text-dark">Tỷ lệ Hoàn thành Đúng hạn</div>
                                <div class="mt-4 text-start">
                                    <div class="d-flex justify-content-between mb-2">
                                        <span><i class="bi bi-circle-fill text-success me-2"></i> Đã hoàn thành đúng hạn</span>
                                        <strong>39 nhiệm vụ</strong>
                                    </div>
                                    <div class="d-flex justify-content-between mb-2">
                                        <span><i class="bi bi-circle-fill text-primary me-2"></i> Đang thực hiện trong hạn</span>
                                        <strong>3 nhiệm vụ</strong>
                                    </div>
                                    <div class="d-flex justify-content-between">
                                        <span><i class="bi bi-circle-fill text-danger me-2"></i> Quá hạn / Trễ hạn (Cần nhắc nhở)</span>
                                        <strong class="text-danger">2 nhiệm vụ</strong>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>
@endsection
