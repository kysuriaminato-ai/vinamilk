#!/usr/bin/env python3
"""
=============================================================================
VINAMILK HRM - SYNTHETIC DATA GENERATOR FOR EMPLOYEE TURNOVER (gen_synthetic.py)
=============================================================================
Mô tả:
Sinh dữ liệu giả lập chuẩn xác suất cho 1.000 nhân viên trong 24 tháng (24.000 bản ghi)
có quy luật thực tế:
- Tỷ lệ nghỉ việc cao hơn rõ rệt ở người thâm niên < 1 năm (nhóm hội nhập)
- Tỷ lệ nghỉ việc tăng khi giờ OT cao (> 25h - 40h/tháng, kiệt sức)
- Tỷ lệ nghỉ việc tăng khi lâu chưa được tăng lương (> 18 - 24 tháng)
- Tỷ lệ nghỉ việc tăng khi điểm KPI thấp (< 85 điểm)
=============================================================================
"""

import os
import csv
import math
import random

def generate_synthetic_turnover(output_path='data/synthetic_turnover_24m.csv', num_employees=1000, num_months=24, seed=42):
    random.seed(seed)
    
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    
    # 4 Khối chức năng Vinamilk
    khoi_list = ['SAN_XUAT', 'KINH_DOANH', 'VAN_PHONG', 'VUNG_NGUYEN_LIEU']
    khoi_weights = [0.55, 0.20, 0.15, 0.10]
    
    # Khởi tạo thuộc tính cơ sở cho 1.000 nhân viên
    employees = []
    for i in range(1, num_employees + 1):
        emp_id = f"VNM-{i:04d}"
        
        # Chọn khối
        r_khoi = random.random()
        cum = 0
        assigned_khoi = khoi_list[0]
        for k, w in zip(khoi_list, khoi_weights):
            cum += w
            if r_khoi <= cum:
                assigned_khoi = k
                break
                
        # Tuổi ban đầu (20 - 58 tuổi)
        base_age = round(random.gauss(34, 7), 1)
        base_age = max(20.0, min(58.0, base_age))
        
        # Thâm niên ban đầu (0.1 - 15.0 năm)
        # 30% nhân sự mới (< 1 năm)
        if random.random() < 0.30:
            base_tenure = round(random.uniform(0.1, 0.95), 2)
        else:
            base_tenure = round(random.uniform(1.0, min(15.0, base_age - 20)), 2)
            
        # Số lần thuyên chuyển (0 - 3 lần)
        transfers = random.choices([0, 1, 2, 3], weights=[0.60, 0.25, 0.10, 0.05])[0]
        
        # Số tháng kể từ lần tăng lương gần nhất (1 - 36 tháng)
        months_since_raise = random.randint(1, 24)
        if base_tenure < 1.0:
            months_since_raise = int(base_tenure * 12)
            
        employees.append({
            'id': emp_id,
            'khoi': assigned_khoi,
            'base_age': base_age,
            'base_tenure': base_tenure,
            'transfers': transfers,
            'months_since_raise': months_since_raise
        })
        
    records = []
    turnover_count = 0
    
    # Các mốc tháng từ 2024-11 (tháng 1) đến 2026-10 (tháng 24)
    start_year = 2024
    start_month = 11
    
    for emp in employees:
        has_left = False
        curr_tenure = emp['base_tenure']
        curr_age = emp['base_age']
        curr_raise_months = emp['months_since_raise']
        
        for m_idx in range(1, num_months + 1):
            if has_left:
                break
                
            # Tính kỳ YYYY-MM
            total_m = (start_year * 12 + start_month - 1) + (m_idx - 1)
            p_year = total_m // 12
            p_month = (total_m % 12) + 1
            period_str = f"{p_year}-{p_month:02d}"
            
            # Cập nhật thâm niên và tuổi theo từng tháng
            curr_tenure = round(curr_tenure + (1.0 / 12.0), 3)
            curr_age = round(curr_age + (1.0 / 12.0), 3)
            curr_raise_months += 1
            
            # Giờ làm thêm OT trung bình 3 tháng (Sản xuất OT cao hơn Văn phòng)
            if emp['khoi'] == 'SAN_XUAT':
                ot_3m = max(0.0, round(random.gauss(24.0, 10.0), 1))
            elif emp['khoi'] == 'KINH_DOANH':
                ot_3m = max(0.0, round(random.gauss(14.0, 6.0), 1))
            else:
                ot_3m = max(0.0, round(random.gauss(6.0, 4.0), 1))
                
            # Số ngày nghỉ phép / vắng mặt trong tháng (0 - 6 ngày)
            absence_days = max(0, min(8, int(random.expovariate(0.8))))
            
            # Điểm đánh giá KPI (50 - 130 điểm, trung bình 95)
            kpi_score = round(random.gauss(95.0, 15.0), 1)
            kpi_score = max(50.0, min(130.0, kpi_score))
            
            # =================================================================
            # QUY LUẬT XÁC SUẤT NGHỈ VIỆC (LOGIT FORMULA)
            # =================================================================
            # 1. Base intercept âm (xác suất nền thấp ~ 3-4%)
            z = -3.85
            
            # 2. Thâm niên < 1 năm: Tỷ lệ nhảy việc rất cao
            if curr_tenure < 1.0:
                z += 2.20 * (1.0 - curr_tenure)
            elif curr_tenure > 5.0:
                z -= 0.60 # Thâm niên lâu thì trung thành hơn
                
            # 3. Giờ OT cao (> 25h/tháng kiệt sức)
            if ot_3m > 25.0:
                z += 0.075 * (ot_3m - 25.0)
                
            # 4. Lâu chưa tăng lương (> 18 tháng)
            if curr_raise_months > 18:
                z += 0.065 * (curr_raise_months - 18)
                
            # 5. Điểm KPI thấp (< 85 điểm dễ chán nản / bị cảnh cáo)
            if kpi_score < 85.0:
                z += 0.055 * (85.0 - kpi_score)
            elif kpi_score >= 115.0:
                z -= 0.40 # KPI xuất sắc thì gắn bó
                
            # 6. Tuổi trẻ (< 28 tuổi) dễ nhảy việc
            if curr_age < 28.0:
                z += 0.35
            elif curr_age > 45.0:
                z -= 0.45
                
            # 7. Số lần thuyên chuyển
            if emp['transfers'] >= 2:
                z += 0.25 * emp['transfers']
                
            # Tính xác suất theo Sigmoid
            prob = 1.0 / (1.0 + math.exp(-z))
            
            # Quyết định nghỉ việc
            is_turnover = 1 if (random.random() < prob) else 0
            
            if is_turnover == 1:
                has_left = True
                turnover_count += 1
                
            records.append({
                'employee_id': emp['id'],
                'month_idx': m_idx,
                'period': period_str,
                'tham_nien': curr_tenure,
                'tuoi': curr_age,
                'khoi': emp['khoi'],
                'ot_3_thang': ot_3m,
                'so_ngay_nghi': absence_days,
                'thang_tu_tang_luong': curr_raise_months,
                'diem_kpi': kpi_score,
                'so_lan_thuyen_chuyen': emp['transfers'],
                'turnover': is_turnover
            })
            
    # Ghi ra file CSV
    fieldnames = [
        'employee_id', 'month_idx', 'period', 'tham_nien', 'tuoi', 'khoi',
        'ot_3_thang', 'so_ngay_nghi', 'thang_tu_tang_luong', 'diem_kpi',
        'so_lan_thuyen_chuyen', 'turnover'
    ]
    
    with open(output_path, 'w', newline='', encoding='utf-8') as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(records)
        
    print(f"[GEN_SYNTHETIC SUCCESS]")
    print(f"  -> File lưu tại: {output_path}")
    print(f"  -> Tổng số bản ghi (employee-month): {len(records):,}")
    print(f"  -> Tổng số lượt thôi việc: {turnover_count:,} ({turnover_count / len(records) * 100:.2f}%)")
    return output_path

if __name__ == '__main__':
    generate_synthetic_turnover()
