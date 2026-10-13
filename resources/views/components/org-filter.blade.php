@props([
    'selectedRegion' => request('region'),
    'selectedUnitId' => request('unit_id'),
    'selectedSubId'  => request('sub_id'),
    'formAction'     => null,
])

<div class="org-filter-card card mb-4">
    <div class="card-body">
        <form method="GET" action="{{ $formAction ?? url()->current() }}" id="cascading-org-filter-form" class="row g-3 align-items-end">
            <!-- 1. Bộ lọc Vùng -->
            <div class="col-md-3">
                <label for="filter-region" class="form-label fw-bold">1. Phân vùng Địa lý</label>
                <select name="region" id="filter-region" class="form-select" onchange="CascadingFilter.onRegionChange(this.value)">
                    <option value="">-- Tất cả các vùng --</option>
                    <option value="NORTH" {{ $selectedRegion === 'NORTH' ? 'selected' : '' }}>Miền Bắc (Nhà máy Tiên Sơn, Nghệ An...)</option>
                    <option value="CENTRAL" {{ $selectedRegion === 'CENTRAL' ? 'selected' : '' }}>Miền Trung (Nhà máy Đà Nẵng, Bình Định...)</option>
                    <option value="SOUTH" {{ $selectedRegion === 'SOUTH' ? 'selected' : '' }}>Miền Nam (Mega Bình Dương, Trụ sở, Tây Ninh...)</option>
                </select>
            </div>

            <!-- 2. Bộ lọc Đơn vị cơ sở (Nhà máy, Trang trại, Khối trụ sở, Chi nhánh) -->
            <div class="col-md-4">
                <label for="filter-unit" class="form-label fw-bold">2. Đơn vị Cơ sở</label>
                <select name="unit_id" id="filter-unit" class="form-select" onchange="CascadingFilter.onUnitChange(this.value)">
                    <option value="">-- Chọn đơn vị cơ sở --</option>
                </select>
            </div>

            <!-- 3. Bộ lọc Phòng ban / Phân xưởng / Tổ đội -->
            <div class="col-md-3">
                <label for="filter-sub" class="form-label fw-bold">3. Phòng ban / Phân xưởng</label>
                <select name="sub_id" id="filter-sub" class="form-select">
                    <option value="">-- Tất cả phòng / xưởng --</option>
                </select>
            </div>

            <!-- Nút hành động -->
            <div class="col-md-2 d-flex gap-2">
                <button type="submit" class="btn btn-primary w-100">
                    <i class="bi bi-funnel"></i> Lọc dữ liệu
                </button>
                <a href="{{ $formAction ?? url()->current() }}" class="btn btn-outline-secondary" title="Xóa bộ lọc">
                    <i class="bi bi-arrow-counterclockwise"></i>
                </a>
            </div>
        </form>
    </div>
</div>

<script>
const CascadingFilter = {
    selectedRegion: '{{ $selectedRegion }}',
    selectedUnitId: '{{ $selectedUnitId }}',
    selectedSubId:  '{{ $selectedSubId }}',
    unitsData: [],

    async init() {
        try {
            // Tải danh mục đơn vị người dùng được phép truy cập từ backend (đã qua OrgUnitScope)
            const response = await fetch('/api/org-units/scoped-list');
            this.unitsData = await response.json();
            
            this.populateUnits(this.selectedRegion);
            if (this.selectedUnitId) {
                document.getElementById('filter-unit').value = this.selectedUnitId;
                this.populateSubUnits(this.selectedUnitId);
                if (this.selectedSubId) {
                    document.getElementById('filter-sub').value = this.selectedSubId;
                }
            }
        } catch (e) {
            console.error('Lỗi nạp danh mục đơn vị tổ chức:', e);
        }
    },

    onRegionChange(region) {
        this.populateUnits(region);
        document.getElementById('filter-sub').innerHTML = '<option value="">-- Tất cả phòng / xưởng --</option>';
    },

    populateUnits(region) {
        const unitSelect = document.getElementById('filter-unit');
        unitSelect.innerHTML = '<option value="">-- Chọn đơn vị cơ sở --</option>';

        const filtered = this.unitsData.filter(u => (!region || u.region === region) && u.level <= 3);
        filtered.forEach(u => {
            const opt = document.createElement('option');
            opt.value = u.id;
            opt.textContent = `[${u.unit_type}] ${u.unit_name}`;
            unitSelect.appendChild(opt);
        });
    },

    onUnitChange(unitId) {
        this.populateSubUnits(unitId);
    },

    populateSubUnits(unitId) {
        const subSelect = document.getElementById('filter-sub');
        subSelect.innerHTML = '<option value="">-- Tất cả phòng / xưởng --</option>';

        if (!unitId) return;

        const parentUnit = this.unitsData.find(u => u.id == unitId);
        if (!parentUnit) return;

        // Tìm các phòng ban/xưởng là con của đơn vị được chọn
        const subs = this.unitsData.filter(u => u.path.startsWith(parentUnit.path) && u.id != unitId);
        subs.forEach(s => {
            const opt = document.createElement('option');
            opt.value = s.id;
            opt.textContent = `— ${s.unit_name}`;
            subSelect.appendChild(opt);
        });
    }
};

document.addEventListener('DOMContentLoaded', () => CascadingFilter.init());
</script>
