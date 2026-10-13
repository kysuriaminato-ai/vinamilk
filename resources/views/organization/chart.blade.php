@extends('layouts.app')

@section('title', 'Sơ đồ Cây Tổ chức Doanh nghiệp — Vinamilk HRIS')

@section('content')
<div class="container-fluid py-4">
    <div class="d-flex justify-content-between align-items-center mb-4">
        <div>
            <h1 class="h3 fw-bold text-primary mb-1">
                <i class="bi bi-diagram-3"></i> Sơ đồ Cây Tổ chức Vinamilk
            </h1>
            <p class="text-muted mb-0">Cơ cấu điều hành phân cấp đa tầng toàn tập đoàn (Hơn 10.000 CBCNV)</p>
        </div>
        <div class="d-flex gap-2">
            <button class="btn btn-outline-secondary" onclick="OrgTreeApp.expandAll()">
                <i class="bi bi-arrows-expand"></i> Mở rộng tất cả
            </button>
            <button class="btn btn-outline-secondary" onclick="OrgTreeApp.collapseAll()">
                <i class="bi bi-arrows-collapse"></i> Thu gọn
            </button>
            <button class="btn btn-primary" onclick="OrgTreeApp.openCreateModal()">
                <i class="bi bi-plus-circle"></i> Thêm đơn vị mới
            </button>
        </div>
    </div>

    <!-- Khung hiển thị sơ đồ cây tương tác -->
    <div class="card shadow-sm border-0">
        <div class="card-body p-4 bg-light overflow-auto" style="min-height: 650px;">
            <div id="org-tree-canvas" class="d-flex justify-content-center">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Đang tải cấu trúc cây...</span>
                </div>
            </div>
        </div>
    </div>
</div>

<!-- Modal Chi tiết Số liệu Đơn vị khi Click vào Nút -->
<div class="modal fade" id="unitDetailModal" tabindex="-1" aria-labelledby="unitDetailModalLabel" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered modal-lg">
        <div class="modal-content border-0 shadow">
            <div class="modal-header bg-primary text-white">
                <div>
                    <h5 class="modal-title fw-bold" id="modal-unit-name">Chi tiết đơn vị</h5>
                    <span class="badge bg-light text-primary" id="modal-unit-code">MÃ ĐƠN VỊ</span>
                    <span class="badge bg-info text-white" id="modal-unit-type">LOẠI HÌNH</span>
                </div>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body p-4">
                <!-- 3 Thẻ thống kê chỉ số trọng yếu -->
                <div class="row g-3 mb-4">
                    <div class="col-md-6">
                        <div class="p-3 bg-light rounded border text-center">
                            <div class="text-muted small fw-semibold">QUÂN SỐ TOÀN NHÁNH CÂY CON</div>
                            <div class="h2 fw-bold text-primary mb-0" id="metric-headcount">0</div>
                            <div class="text-muted small mt-1">Trực tiếp tại đơn vị: <span id="metric-direct-headcount" class="fw-bold">0</span> người</div>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="p-3 bg-light rounded border text-center">
                            <div class="text-muted small fw-semibold">QUỸ LƯƠNG ĐỊNH KỲ THÁNG (P1 + P2)</div>
                            <div class="h2 fw-bold text-success mb-0" id="metric-payroll">0 đ</div>
                            <div class="text-muted small mt-1">Tổng hợp theo Materialized Path</div>
                        </div>
                    </div>
                </div>

                <!-- Thông tin Trưởng đơn vị -->
                <h6 class="fw-bold text-secondary mb-3"><i class="bi bi-person-badge"></i> Lãnh đạo Đơn vị (Manager)</h6>
                <div class="p-3 border rounded bg-white" id="manager-info-box">
                    <div class="d-flex align-items-center gap-3">
                        <div class="avatar bg-primary text-white rounded-circle d-flex align-items-center justify-content-center" style="width: 50px; height: 50px; font-size: 20px;">
                            <span id="manager-avatar-text">V</span>
                        </div>
                        <div>
                            <h5 class="mb-0 fw-bold" id="manager-name">Chưa bổ nhiệm</h5>
                            <div class="text-muted small" id="manager-position">Trưởng đơn vị</div>
                            <div class="text-muted small mt-1">
                                <span class="me-3"><i class="bi bi-envelope"></i> <span id="manager-email">---</span></span>
                                <span><i class="bi bi-telephone"></i> <span id="manager-phone">---</span></span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Đóng</button>
                <a href="#" id="btn-view-employees" class="btn btn-primary">
                    <i class="bi bi-people"></i> Xem danh sách nhân sự
                </a>
            </div>
        </div>
    </div>
</div>

<script>
const OrgTreeApp = {
    treeData: null,
    expandedNodes: new Set(),

    async init() {
        try {
            const res = await fetch('/api/org-units/tree');
            this.treeData = await res.json();
            if (this.treeData.length > 0) {
                this.expandedNodes.add(this.treeData[0].id); // Mở sẵn root node
            }
            this.render();
        } catch (e) {
            console.error('Lỗi tải cây tổ chức:', e);
        }
    },

    render() {
        const canvas = document.getElementById('org-tree-canvas');
        if (!this.treeData || this.treeData.length === 0) {
            canvas.innerHTML = '<div class="alert alert-warning">Không có dữ liệu trong phạm vi quản lý.</div>';
            return;
        }

        canvas.innerHTML = this.renderNodes(this.treeData);
    },

    renderNodes(nodes) {
        return `
            <div class="d-flex gap-4 justify-content-center">
                ${nodes.map(node => this.renderSingleNode(node)).join('')}
            </div>
        `;
    },

    renderSingleNode(node) {
        const hasChildren = node.children && node.children.length > 0;
        const isExpanded = this.expandedNodes.has(node.id);

        return `
            <div class="d-flex flex-column align-items-center">
                <div class="card p-3 shadow-sm border-2 text-center" 
                     style="width: 220px; cursor: pointer; border-color: ${node.level === 1 ? '#0d6efd' : '#ced4da'};"
                     onclick="OrgTreeApp.onNodeClick(${node.id})">
                    <span class="badge bg-secondary mb-1" style="font-size: 10px;">${node.type}</span>
                    <h6 class="fw-bold mb-1 text-truncate" title="${node.name}">${node.name}</h6>
                    <div class="small text-muted mb-2">${node.code}</div>
                    <div class="small text-primary"><i class="bi bi-person"></i> ${node.manager_name}</div>
                    ${hasChildren ? `
                        <button class="btn btn-sm btn-light border mt-2 py-0" 
                                onclick="event.stopPropagation(); OrgTreeApp.toggleExpand(${node.id})">
                            ${isExpanded ? '− Thu gọn' : '+ Mở rộng (' + node.children.length + ')'}
                        </button>
                    ` : ''}
                </div>
                ${hasChildren && isExpanded ? `
                    <div class="vr my-2" style="height: 20px;"></div>
                    <div class="pt-2 border-top">
                        ${this.renderNodes(node.children)}
                    </div>
                ` : ''}
            </div>
        `;
    },

    toggleExpand(nodeId) {
        if (this.expandedNodes.has(nodeId)) {
            this.expandedNodes.delete(nodeId);
        } else {
            this.expandedNodes.add(nodeId);
        }
        this.render();
    },

    expandAll() {
        const collect = (nodes) => {
            nodes.forEach(n => {
                this.expandedNodes.add(n.id);
                if (n.children) collect(n.children);
            });
        };
        collect(this.treeData);
        this.render();
    },

    collapseAll() {
        this.expandedNodes.clear();
        if (this.treeData.length > 0) {
            this.expandedNodes.add(this.treeData[0].id);
        }
        this.render();
    },

    async onNodeClick(unitId) {
        try {
            const res = await fetch(`/api/org-units/${unitId}/metrics`);
            const metrics = await res.json();

            document.getElementById('modal-unit-name').textContent = metrics.unit_name;
            document.getElementById('modal-unit-code').textContent = metrics.unit_code;
            document.getElementById('modal-unit-type').textContent = metrics.unit_type;
            document.getElementById('metric-headcount').textContent = new Intl.NumberFormat('vi-VN').format(metrics.total_headcount);
            document.getElementById('metric-direct-headcount').textContent = new Intl.NumberFormat('vi-VN').format(metrics.direct_headcount);
            document.getElementById('metric-payroll').textContent = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(metrics.total_monthly_payroll);

            if (metrics.manager) {
                document.getElementById('manager-name').textContent = metrics.manager.name;
                document.getElementById('manager-position').textContent = metrics.manager.position;
                document.getElementById('manager-email').textContent = metrics.manager.email || 'Chưa cập nhật';
                document.getElementById('manager-phone').textContent = metrics.manager.phone || 'Chưa cập nhật';
                document.getElementById('manager-avatar-text').textContent = metrics.manager.name.charAt(0);
            } else {
                document.getElementById('manager-name').textContent = 'Chưa bổ nhiệm lãnh đạo';
                document.getElementById('manager-position').textContent = 'Đang khuyết vị trí';
                document.getElementById('manager-email').textContent = '---';
                document.getElementById('manager-phone').textContent = '---';
                document.getElementById('manager-avatar-text').textContent = '?';
            }

            document.getElementById('btn-view-employees').href = `/employees?org_unit_id=${unitId}`;

            const modal = new bootstrap.Modal(document.getElementById('unitDetailModal'));
            modal.show();
        } catch (e) {
            console.error('Lỗi nạp số liệu đơn vị:', e);
        }
    }
};

document.addEventListener('DOMContentLoaded', () => OrgTreeApp.init());
</script>
@endsection
