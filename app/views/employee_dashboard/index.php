<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Cổng thông tin Cá nhân - Vinamilk HRIS</title>
    <!-- CSS Inline cho sự đơn giản của MVC View, thực tế nên dùng external css -->
    <style>
        :root {
            --primary: #1B5E20; /* Xanh lá Vinamilk */
            --primary-light: #4CAF50;
            --bg-color: #F5F7FA;
            --text-main: #333;
            --card-bg: #FFFFFF;
        }
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: var(--bg-color);
            color: var(--text-main);
            margin: 0;
            padding: 20px;
        }
        .container { max-width: 1200px; margin: 0 auto; }
        .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 30px; }
        .header h1 { margin: 0; color: var(--primary); border-left: 5px solid #F6B800; padding-left: 15px; }
        .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 20px; }
        .card { background: var(--card-bg); border-radius: 8px; padding: 20px; box-shadow: 0 2px 10px rgba(0,0,0,0.05); }
        .card h3 { margin-top: 0; color: var(--primary); border-bottom: 1px solid #eee; padding-bottom: 10px; }
        
        .list-item { padding: 10px 0; border-bottom: 1px dashed #eee; display: flex; justify-content: space-between;}
        .list-item:last-child { border-bottom: none; }
        
        .badge { padding: 4px 8px; border-radius: 12px; font-size: 12px; font-weight: bold; color: white; }
        .badge-success { background: var(--primary); }
        .badge-warning { background: #FF9800; }
        
        .payslip-box { text-align: center; padding: 20px; background: #E8F5E9; border-radius: 8px; border: 1px solid #C8E6C9;}
        .btn-view { display: inline-block; margin-top: 15px; padding: 10px 20px; background: var(--primary); color: white; text-decoration: none; border-radius: 4px; }
    </style>
</head>
<body>

<div class="container">
    <div class="header">
        <h1>Bảng điều khiển Cá nhân (Self-Service)</h1>
        <div>Xin chào, <b id="user-name">Nhân viên</b></div>
    </div>

    <div class="grid">
        <!-- Chấm công gần đây -->
        <div class="card">
            <h3>Lịch sử Chấm công (5 ngày)</h3>
            <div id="attendance-list">Đang tải...</div>
        </div>

        <!-- Phép upcoming -->
        <div class="card">
            <h3>Nghỉ phép sắp tới</h3>
            <div id="leaves-list">Đang tải...</div>
        </div>

        <!-- Phiếu lương -->
        <div class="card">
            <h3>Phiếu lương kỳ gần nhất</h3>
            <div class="payslip-box" id="payslip-box">
                Đang tải...
            </div>
        </div>
    </div>
</div>

<!-- Tích hợp API Helper -->
<script src="../../js/api.js"></script>
<script>
    document.addEventListener('DOMContentLoaded', async () => {
        // Fetch API (Lưu ý đường dẫn API có thể khác tùy thuộc vào base path của bạn)
        // Chúng ta giả định Api.BASE_URL đã được setup đúng ở js/api.js
        
        try {
            // Giả lập route api-ess/dashboard-data
            const response = await fetch('/vinamilk/public/index.php?url=api-ess/dashboard-data');
            const result = await response.json();

            if (result.status === 'success') {
                const data = result.data;

                // 1. Render Attendance
                const attList = document.getElementById('attendance-list');
                if (data.attendance.length > 0) {
                    attList.innerHTML = data.attendance.map(a => `
                        <div class="list-item">
                            <span>${new Date(a.check_in_time).toLocaleDateString('vi-VN')}</span>
                            <span class="badge ${a.status === 'Đúng giờ' ? 'badge-success' : 'badge-warning'}">${a.status}</span>
                        </div>
                    `).join('');
                } else {
                    attList.innerHTML = '<p>Không có dữ liệu</p>';
                }

                // 2. Render Leaves
                const leavesList = document.getElementById('leaves-list');
                if (data.leaves.length > 0) {
                    leavesList.innerHTML = data.leaves.map(l => `
                        <div class="list-item">
                            <span>Từ: ${l.start_date} <br> Đến: ${l.end_date}</span>
                            <span class="badge badge-success">Đã duyệt</span>
                        </div>
                    `).join('');
                } else {
                    leavesList.innerHTML = '<p style="color:#757575">Không có lịch nghỉ phép sắp tới.</p>';
                }

                // 3. Render Payslip
                if (data.payslip) {
                    document.getElementById('payslip-box').innerHTML = `
                        <div style="font-size: 14px; color: #555;">Kỳ lương: ${data.payslip.month}</div>
                        <div style="font-size: 24px; font-weight: bold; color: var(--primary); margin: 10px 0;">
                            ${data.payslip.net_salary}
                        </div>
                        <a href="${data.payslip.url}" class="btn-view">Xem chi tiết</a>
                    `;
                }

            }
        } catch (e) {
            console.error('Lỗi khi tải Dashboard:', e);
            document.getElementById('attendance-list').innerHTML = '<span style="color:red">Lỗi tải dữ liệu</span>';
        }
    });
</script>

</body>
</html>
