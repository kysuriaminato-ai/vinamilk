<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Việc Của Tôi (KPI) - Vinamilk ESS</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
    <style>
        :root {
            --vnm-blue: #003399;
            --vnm-gold: #c69214;
            --bg-gray: #f8fafc;
            --text-main: #1f2937;
            --border: #e2e8f0;
        }
        * { box-sizing: border-box; margin: 0; padding: 0; font-family: 'Inter', sans-serif; }
        body { background: var(--bg-gray); color: var(--text-main); }
        .header { background: #003399; color: white; padding: 18px 24px; display: flex; justify-content: space-between; align-items: center; }
        .container { max-width: 1100px; margin: 24px auto; padding: 0 16px; }
        
        .summary-card {
            background: white; border-radius: 10px; padding: 20px; box-shadow: 0 2px 4px rgba(0,0,0,0.05);
            margin-bottom: 24px; display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px;
            border-top: 4px solid var(--vnm-gold);
        }
        .stat-box { display: flex; flex-direction: column; gap: 4px; }
        .stat-label { font-size: 13px; color: #64748b; }
        .stat-value { font-size: 24px; font-weight: 700; color: #003399; }
        .grade-badge { font-size: 20px; font-weight: 700; color: #16a34a; }

        .task-list { display: flex; flex-direction: column; gap: 16px; }
        .task-item {
            background: white; border-radius: 8px; padding: 20px; border: 1px solid var(--border);
            display: flex; justify-content: space-between; align-items: center; gap: 16px;
        }
        .task-left { display: flex; flex-direction: column; gap: 8px; flex: 1; }
        .task-title { font-size: 16px; font-weight: 600; }
        .task-meta { display: flex; gap: 16px; font-size: 13px; color: #64748b; }
        
        .badge { padding: 4px 10px; border-radius: 12px; font-size: 12px; font-weight: 600; }
        .badge-assigned { background: #e2e8f0; color: #475569; }
        .badge-inprogress { background: #e0f2fe; color: #0284c7; }
        .badge-pending { background: #fef3c7; color: #d97706; }
        .badge-approved { background: #dcfce7; color: #16a34a; }
        .badge-rejected { background: #fee2e2; color: #dc2626; }

        .btn { padding: 8px 16px; border-radius: 6px; font-weight: 600; border: none; cursor: pointer; }
        .btn-start { background: #0284c7; color: white; }
        .btn-submit { background: #003399; color: white; }
    </style>
</head>
<body>
    <div class="header">
        <h2>Mục Tiêu & Nhiệm Vụ KPI Của Tôi (ESS)</h2>
        <a href="/kpi/kanban" style="color: white; text-decoration: underline;">Xem Bảng Quản Lý</a>
    </div>

    <div class="container">
        <!-- Tổng kết điểm KPI kỳ -->
        <div class="summary-card">
            <div class="stat-box">
                <span class="stat-label">Kỳ Đánh Giá</span>
                <span class="stat-value" style="font-size: 18px;">{{ $currentPeriod?->period_name ?? 'N/A' }}</span>
            </div>
            <div class="stat-box">
                <span class="stat-label">Tổng Trọng Số Đã Giao</span>
                <span class="stat-value">{{ $summary?->total_weight_percentage ?? 0 }}%</span>
            </div>
            <div class="stat-box">
                <span class="stat-label">Điểm KPI Tạm Tính</span>
                <span class="stat-value">{{ $summary?->kpi_score ?? 0 }} / 100</span>
            </div>
            <div class="stat-box">
                <span class="stat-label">Xếp Loại & Hệ Số P3</span>
                <span class="grade-badge">Loại {{ $summary?->kpi_grade ?? 'B' }} (×{{ $summary?->p3_multiplier ?? 1.0 }})</span>
            </div>
        </div>

        <!-- Danh sách nhiệm vụ -->
        <h3 style="margin-bottom: 16px;">Danh Sách Nhiệm Vụ Trong Kỳ</h3>
        <div class="task-list">
            @forelse($tasks as $t)
                <div class="task-item">
                    <div class="task-left">
                        <div style="display: flex; align-items: center; gap: 10px;">
                            <span class="badge badge-{{ strtolower($t->status) }}">{{ $t->status }}</span>
                            <span class="task-title">{{ $t->title }}</span>
                        </div>
                        <p style="font-size: 13px; color: #475569;">{{ $t->description }}</p>
                        <div class="task-meta">
                            <span>Chỉ tiêu: <strong>{{ $t->target_metric }}</strong></span>
                            <span>Trọng số: <strong>{{ $t->weight_percentage }}%</strong></span>
                            <span>Hạn chót: <strong>{{ $t->due_date->format('d/m/Y') }}</strong></span>
                            @if($t->final_score !== null)
                                <span style="color: #16a34a; font-weight: 700;">Điểm chốt: {{ $t->final_score }}/100</span>
                            @endif
                        </div>
                    </div>
                    <div>
                        @if($t->status === 'Assigned')
                            <button class="btn btn-start" onclick="startTask({{ $t->id }})">Bắt đầu làm</button>
                        @elseif($t->status === 'InProgress' || $t->status === 'Rejected')
                            <button class="btn btn-submit" onclick="openSubmitModal({{ $t->id }}, '{{ $t->title }}')">Nộp kết quả</button>
                        @elseif($t->status === 'PendingApproval')
                            <span style="font-size: 13px; color: #d97706; font-weight: 600;">Đang chờ Lãnh đạo thẩm định...</span>
                        @endif
                    </div>
                </div>
            @empty
                <p style="text-align: center; color: #64748b; padding: 40px;">Bạn chưa có nhiệm vụ KPI nào được giao trong kỳ này.</p>
            @endforelse
        </div>
    </div>

    <script>
        async function startTask(id) {
            const res = await fetch(`/api/v1/kpi/tasks/${id}/start`, {
                method: 'POST',
                headers: { 'X-CSRF-TOKEN': '{{ csrf_token() }}' }
            });
            const data = await res.json();
            if (data.success) {
                location.reload();
            }
        }

        async function openSubmitModal(id, title) {
            const result = prompt(`Báo cáo kết quả thực hiện cho nhiệm vụ [${title}]:`);
            if (!result) return;

            const res = await fetch(`/api/v1/kpi/tasks/${id}/submit`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': '{{ csrf_token() }}' },
                body: JSON.stringify({ actual_result: result })
            });
            const data = await res.json();
            if (data.success) {
                alert(data.message);
                location.reload();
            } else {
                alert(data.message);
            }
        }
    </script>
</body>
</html>
