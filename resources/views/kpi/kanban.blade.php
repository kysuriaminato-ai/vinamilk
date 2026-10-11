<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Bảng Quản Lý KPI (Kanban) - Vinamilk HRIS</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
    <style>
        :root {
            --vnm-blue: #003399;
            --vnm-blue-light: #e6f0fa;
            --vnm-gold: #c69214;
            --bg-gray: #f4f6f9;
            --text-main: #1f2937;
            --text-muted: #6b7280;
            --border-color: #e5e7eb;
            --card-bg: #ffffff;
        }
        * { box-sizing: border-box; margin: 0; padding: 0; font-family: 'Inter', sans-serif; }
        body { background-color: var(--bg-gray); color: var(--text-main); min-height: 100vh; }
        
        .header-bar {
            background: linear-gradient(135deg, #002b80 0%, #0044cc 100%);
            color: white; padding: 16px 24px; display: flex; justify-content: space-between; align-items: center;
            box-shadow: 0 2px 8px rgba(0,0,0,0.15);
        }
        .header-title { font-size: 20px; font-weight: 700; display: flex; align-items: center; gap: 10px; }
        .header-actions { display: flex; gap: 12px; align-items: center; }
        
        .btn {
            padding: 8px 16px; border-radius: 6px; font-size: 14px; font-weight: 600;
            cursor: pointer; border: none; transition: all 0.2s; display: inline-flex; align-items: center; gap: 6px;
        }
        .btn-primary { background-color: var(--vnm-gold); color: white; }
        .btn-primary:hover { background-color: #a8790b; }
        .btn-outline { background: rgba(255,255,255,0.15); color: white; border: 1px solid rgba(255,255,255,0.3); }
        .btn-outline:hover { background: rgba(255,255,255,0.25); }

        .filter-container {
            padding: 16px 24px; background: white; border-bottom: 1px solid var(--border-color);
            display: flex; gap: 16px; align-items: center; flex-wrap: wrap;
        }
        select, input {
            padding: 8px 12px; border: 1px solid var(--border-color); border-radius: 6px;
            font-size: 14px; outline: none; background: white;
        }
        
        .kanban-board {
            display: flex; gap: 16px; padding: 24px; overflow-x: auto; min-height: calc(100vh - 140px);
            align-items: flex-start;
        }
        .kanban-column {
            flex: 0 0 310px; background: #eef2f6; border-radius: 10px; display: flex; flex-direction: column;
            max-height: calc(100vh - 170px); box-shadow: 0 1px 3px rgba(0,0,0,0.05); border: 1px solid #e2e8f0;
        }
        .column-header {
            padding: 14px 16px; font-weight: 700; font-size: 15px; display: flex; justify-content: space-between;
            align-items: center; border-bottom: 2px solid transparent;
        }
        .column-assigned { border-color: #64748b; }
        .column-inprogress { border-color: #0284c7; }
        .column-pending { border-color: #d97706; background: #fffbeb; }
        .column-approved { border-color: #16a34a; background: #f0fdf4; }
        .column-rejected { border-color: #dc2626; }
        .column-notcompleted { border-color: #475569; }

        .badge-count {
            background: rgba(0,0,0,0.1); padding: 2px 8px; border-radius: 12px; font-size: 12px;
        }

        .column-content {
            padding: 12px; overflow-y: auto; display: flex; flex-direction: column; gap: 12px; flex: 1;
        }

        .task-card {
            background: white; border-radius: 8px; padding: 14px; box-shadow: 0 2px 4px rgba(0,0,0,0.04);
            border-left: 4px solid #003399; display: flex; flex-direction: column; gap: 8px; cursor: pointer;
            transition: transform 0.15s, box-shadow 0.15s;
        }
        .task-card:hover { transform: translateY(-2px); box-shadow: 0 4px 8px rgba(0,0,0,0.1); }
        .task-title { font-weight: 600; font-size: 14px; color: var(--text-main); }
        .task-metric { font-size: 12px; color: var(--text-muted); background: #f8fafc; padding: 4px 8px; border-radius: 4px; }
        .task-meta { display: flex; justify-content: space-between; font-size: 12px; color: var(--text-muted); }
        .task-weight { font-weight: 700; color: #003399; }
        .task-sla { font-size: 12px; font-weight: 600; border-radius: 4px; padding: 2px 6px; }
        .sla-bonus { background: #dcfce7; color: #15803d; }
        .sla-penalty { background: #fee2e2; color: #b91c1c; }

        /* Modal Styles */
        .modal-overlay {
            display: none; position: fixed; top: 0; left: 0; width: 100%; height: 100%;
            background: rgba(0,0,0,0.5); z-index: 1000; justify-content: center; align-items: center;
        }
        .modal-content {
            background: white; border-radius: 10px; padding: 24px; width: 500px; max-width: 90%;
            box-shadow: 0 10px 25px rgba(0,0,0,0.2); display: flex; flex-direction: column; gap: 16px;
        }
    </style>
</head>
<body>
    <header class="header-bar">
        <div class="header-title">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor"><path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-5 14H7v-2h7v2zm3-4H7v-2h10v2zm0-4H7V7h10v2z"/></svg>
            Bảng Quản Lý Nhiệm Vụ KPI (Kanban) - Vinamilk
        </div>
        <div class="header-actions">
            <a href="/kpi/my-tasks" class="btn btn-outline">Xem "Việc của tôi"</a>
            <button class="btn btn-primary" onclick="openExportModal()">Xuất sang Lương (P3)</button>
        </div>
    </header>

    <div class="filter-container">
        <div>
            <label style="font-size: 12px; font-weight: 600; color: var(--text-muted); display: block;">Kỳ đánh giá:</label>
            <select id="periodSelect" onchange="loadKanbanData()">
                @foreach($periods as $p)
                    <option value="{{ $p->id }}" {{ $currentPeriod?->id == $p->id ? 'selected' : '' }}>
                        {{ $p->period_name }} ({{ $p->period_code }})
                    </option>
                @endforeach
            </select>
        </div>
        <div>
            <label style="font-size: 12px; font-weight: 600; color: var(--text-muted); display: block;">Tìm kiếm nhân viên:</label>
            <input type="text" id="employeeFilter" placeholder="Nhập tên nhân viên..." oninput="filterCards()">
        </div>
    </div>

    <main class="kanban-board">
        <!-- 1. Assigned -->
        <div class="kanban-column">
            <div class="column-header column-assigned">
                <span>Đã Giao (Assigned)</span>
                <span class="badge-count" id="count-Assigned">0</span>
            </div>
            <div class="column-content" id="col-Assigned"></div>
        </div>

        <!-- 2. InProgress -->
        <div class="kanban-column">
            <div class="column-header column-inprogress">
                <span>Đang Làm (In Progress)</span>
                <span class="badge-count" id="count-InProgress">0</span>
            </div>
            <div class="column-content" id="col-InProgress"></div>
        </div>

        <!-- 3. PendingApproval -->
        <div class="kanban-column">
            <div class="column-header column-pending">
                <span>Chờ Duyệt (Pending)</span>
                <span class="badge-count" id="count-PendingApproval">0</span>
            </div>
            <div class="column-content" id="col-PendingApproval"></div>
        </div>

        <!-- 4. Approved -->
        <div class="kanban-column">
            <div class="column-header column-approved">
                <span>Đã Duyệt (Approved)</span>
                <span class="badge-count" id="count-Approved">0</span>
            </div>
            <div class="column-content" id="col-Approved"></div>
        </div>

        <!-- 5. Rejected -->
        <div class="kanban-column">
            <div class="column-header column-rejected">
                <span>Trả Lại (Rejected)</span>
                <span class="badge-count" id="count-Rejected">0</span>
            </div>
            <div class="column-content" id="col-Rejected"></div>
        </div>

        <!-- 6. NotCompleted -->
        <div class="kanban-column">
            <div class="column-header column-notcompleted">
                <span>Không Hoàn Thành</span>
                <span class="badge-count" id="count-NotCompleted">0</span>
            </div>
            <div class="column-content" id="col-NotCompleted"></div>
        </div>
    </main>

    <!-- Modal Phê Duyệt / Trả lại Nhiệm vụ -->
    <div class="modal-overlay" id="approvalModal">
        <div class="modal-content">
            <h3 style="color: var(--vnm-blue);" id="modalTaskTitle">Thẩm định Nhiệm vụ KPI</h3>
            <p id="modalTaskMeta" style="font-size: 13px; color: var(--text-muted);"></p>
            <div style="background: #f8fafc; padding: 10px; border-radius: 6px; font-size: 13px;">
                <strong>Kết quả thực hiện:</strong>
                <div id="modalTaskResult" style="margin-top: 4px;"></div>
            </div>

            <div>
                <label style="font-size: 13px; font-weight: 600;">Điểm điều chỉnh của Lãnh đạo (+/- điểm):</label>
                <input type="number" id="adjustmentScore" value="0" min="-100" max="100" style="width: 100%; margin-top: 4px;">
                <span style="font-size: 11px; color: var(--text-muted);">Điều chỉnh vượt ±20 điểm bắt buộc phải nhập lý do cụ thể.</span>
            </div>

            <div id="reasonGroup" style="display: none;">
                <label style="font-size: 13px; font-weight: 600; color: #b91c1c;">Lý do điều chỉnh vượt ±20 điểm (*):</label>
                <textarea id="adjustmentReason" rows="2" style="width: 100%; border: 1px solid var(--border-color); border-radius: 6px; padding: 8px; margin-top: 4px;"></textarea>
            </div>

            <div>
                <label style="font-size: 13px; font-weight: 600;">Nhận xét / Phản hồi:</label>
                <textarea id="feedbackText" rows="2" style="width: 100%; border: 1px solid var(--border-color); border-radius: 6px; padding: 8px; margin-top: 4px;"></textarea>
            </div>

            <div style="display: flex; justify-content: flex-end; gap: 10px; margin-top: 10px;">
                <button class="btn" style="background: #ef4444; color: white;" onclick="submitReject()">Trả về làm lại</button>
                <button class="btn btn-primary" onclick="submitApprove()">Phê duyệt điểm</button>
                <button class="btn" style="background: #e2e8f0;" onclick="closeModal()">Đóng</button>
            </div>
        </div>
    </div>

    <script>
        let currentTaskId = null;
        let tasksData = {};

        document.getElementById('adjustmentScore').addEventListener('input', function() {
            const val = Math.abs(parseFloat(this.value) || 0);
            document.getElementById('reasonGroup').style.display = val > 20 ? 'block' : 'none';
        });

        async function loadKanbanData() {
            const periodId = document.getElementById('periodSelect').value;
            const res = await fetch(`/api/v1/kpi/kanban?kpi_period_id=${periodId}`);
            const data = await res.json();
            if (data.success) {
                tasksData = data.data;
                renderKanban();
            }
        }

        function renderKanban() {
            ['Assigned', 'InProgress', 'PendingApproval', 'Approved', 'Rejected', 'NotCompleted'].forEach(status => {
                const col = document.getElementById(`col-${status}`);
                col.innerHTML = '';
                const items = tasksData[status] || [];
                document.getElementById(`count-${status}`).innerText = items.length;

                items.forEach(task => {
                    const card = document.createElement('div');
                    card.className = 'task-card';
                    card.onclick = () => openApprovalModal(task);
                    card.innerHTML = `
                        <div class="task-title">${task.title}</div>
                        <div class="task-metric">Chỉ tiêu: ${task.target_metric}</div>
                        <div class="task-meta">
                            <span>NV: <strong>${task.employee ? task.employee.full_name : ''}</strong></span>
                            <span class="task-weight">${task.weight_percentage}%</span>
                        </div>
                        <div class="task-meta">
                            <span>Hạn: ${task.due_date}</span>
                            ${task.time_score !== 0 ? `<span class="task-sla ${task.time_score > 0 ? 'sla-bonus' : 'sla-penalty'}">SLA: ${task.time_score > 0 ? '+' : ''}${task.time_score}đ</span>` : ''}
                        </div>
                        ${task.final_score !== null ? `<div style="font-weight:700; color:#16a34a; font-size:13px;">Điểm chốt: ${task.final_score}/100</div>` : ''}
                    `;
                    col.appendChild(card);
                });
            });
        }

        function openApprovalModal(task) {
            currentTaskId = task.id;
            document.getElementById('modalTaskTitle').innerText = task.title;
            document.getElementById('modalTaskMeta').innerText = `Nhân viên: ${task.employee ? task.employee.full_name : ''} | Trọng số: ${task.weight_percentage}% | Hạn: ${task.due_date}`;
            document.getElementById('modalTaskResult').innerText = task.actual_result || '(Chưa nộp kết quả)';
            document.getElementById('approvalModal').style.display = 'flex';
        }

        function closeModal() {
            document.getElementById('approvalModal').style.display = 'none';
        }

        async function submitApprove() {
            const adj = parseFloat(document.getElementById('adjustmentScore').value) || 0;
            const reason = document.getElementById('adjustmentReason').value;
            const fb = document.getElementById('feedbackText').value;

            if (Math.abs(adj) > 20 && !reason.trim()) {
                alert("Điều chỉnh vượt ±20 điểm bắt buộc phải nhập lý do thẩm định.");
                return;
            }

            const res = await fetch(`/api/v1/kpi/tasks/${currentTaskId}/approve`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': '{{ csrf_token() }}' },
                body: JSON.stringify({ manager_adjustment: adj, adjustment_reason: reason, feedback: fb })
            });
            const data = await res.json();
            if (data.success) {
                alert("Phê duyệt nhiệm vụ thành công!");
                closeModal();
                loadKanbanData();
            } else {
                alert(data.message);
            }
        }

        async function submitReject() {
            const reason = prompt("Vui lòng nhập lý do trả lại yêu cầu bổ sung:");
            if (!reason) return;

            const res = await fetch(`/api/v1/kpi/tasks/${currentTaskId}/reject`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': '{{ csrf_token() }}' },
                body: JSON.stringify({ rejection_reason: reason })
            });
            const data = await res.json();
            if (data.success) {
                alert("Đã trả lại nhiệm vụ!");
                closeModal();
                loadKanbanData();
            } else {
                alert(data.message);
            }
        }

        async function openExportModal() {
            const periodId = document.getElementById('periodSelect').value;
            if (confirm("Bạn có chắc chắn muốn xuất toàn bộ điểm KPI đã duyệt của kỳ này sang phân hệ Tính lương P3 không?")) {
                const res = await fetch(`/api/v1/kpi/periods/${periodId}/export-payroll`, {
                    method: 'POST',
                    headers: { 'X-CSRF-TOKEN': '{{ csrf_token() }}' }
                });
                const data = await res.json();
                alert(data.message);
            }
        }

        loadKanbanData();
    </script>
</body>
</html>
