// ============================================
// VINAMILK HRIS - Recruitment Module
// ATS with Kanban Board
// ============================================

const RecruitmentModule = {
  render(container) {
    const stats = RecruitmentData.getStats();

    container.innerHTML = `
      <div class="page-header">
        <div class="page-header-left">
          <h1 class="page-title">Tuyển dụng (ATS)</h1>
          <p class="page-subtitle">Applicant Tracking System — Quy trình tuyển dụng Kanban</p>
        </div>
        <div class="page-header-actions">
          <button class="btn btn-primary" onclick="RecruitmentModule.showNewJobModal()">➕ Đề xuất tuyển dụng</button>
        </div>
      </div>

      <!-- Stats -->
      <div class="content-grid grid-cols-4" style="margin-bottom:var(--space-5)">
        <div class="stat-card stat-blue animate-fade-in-up stagger-1">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.totalJobs}</div>
          <div class="stat-card-label">Vị trí đang tuyển</div>
        </div>
        <div class="stat-card stat-cyan animate-fade-in-up stagger-2">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.totalCandidates}</div>
          <div class="stat-card-label">Tổng ứng viên</div>
        </div>
        <div class="stat-card stat-green animate-fade-in-up stagger-3">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.avgMatchScore}%</div>
          <div class="stat-card-label">TB Match Score</div>
        </div>
        <div class="stat-card stat-orange animate-fade-in-up stagger-4">
          <div class="stat-card-value" style="font-size:var(--font-size-2xl)">${stats.avgTimeToHire}d</div>
          <div class="stat-card-label">TB thời gian tuyển</div>
        </div>
      </div>

      <!-- Job Openings -->
      <div class="card animate-fade-in-up" style="margin-bottom:var(--space-5)">
        <div class="card-header">
          <div class="card-header-title">📋 Vị trí tuyển dụng</div>
        </div>
        <div class="card-body" style="padding:0">
          <table class="data-table">
            <thead>
              <tr>
                <th>Mã</th>
                <th>Vị trí</th>
                <th>Đơn vị</th>
                <th>SL</th>
                <th>Mức lương</th>
                <th>Nguồn</th>
                <th>Định biên</th>
                <th>Trạng thái</th>
                <th>Hạn</th>
              </tr>
            </thead>
            <tbody>
              ${RecruitmentData.jobs.map(job => {
                const dept = DepartmentsData.getDepartment(job.department);
                const candidates = RecruitmentData.getByJob(job.id);
                return `
                <tr>
                  <td><code style="color:var(--primary)">${job.id}</code></td>
                  <td><strong>${job.title}</strong><br><span style="font-size:var(--font-size-xs);color:var(--text-tertiary)">${candidates.length} ứng viên</span></td>
                  <td style="font-size:var(--font-size-sm)">${dept ? dept.name : job.department}</td>
                  <td style="font-weight:700">${job.quantity}</td>
                  <td style="font-size:var(--font-size-sm)">${job.salaryRange}</td>
                  <td><span class="badge badge-info">${job.source}</span></td>
                  <td>${job.inBudget ? '<span class="badge badge-active"><span class="badge-dot"></span>Trong ĐB</span>' : '<span class="badge badge-danger">⚠️ Vượt ĐB</span>'}</td>
                  <td>${Helpers.statusBadge(job.status)}</td>
                  <td>${Helpers.formatDate(job.deadline)}</td>
                </tr>`;
              }).join('')}
            </tbody>
          </table>
        </div>
      </div>

      <!-- Kanban Board -->
      <div class="card animate-fade-in-up">
        <div class="card-header">
          <div class="card-header-title">📊 Kanban Pipeline</div>
          <div class="card-header-subtitle">Kéo thả ứng viên qua các giai đoạn</div>
        </div>
        <div class="card-body">
          <div class="kanban-board" id="kanban-board">
            ${RecruitmentData.stages.map(stage => {
              const candidates = RecruitmentData.getByStage(stage);
              const stageColor = RecruitmentData.stageColors[stage];
              return `
              <div class="kanban-column">
                <div class="kanban-column-header">
                  <div class="kanban-column-title">
                    <span style="width:8px;height:8px;border-radius:50%;background:${stageColor};display:inline-block"></span>
                    ${stage}
                  </div>
                  <span class="kanban-column-count">${candidates.length}</span>
                </div>
                <div class="kanban-column-body">
                  ${candidates.map(c => {
                    const job = RecruitmentData.jobs.find(j => j.id === c.jobId);
                    const scoreClass = c.matchScore >= 85 ? 'high' : c.matchScore >= 70 ? 'medium' : 'low';
                    const healthBlock = stage === 'Kiểm tra sức khỏe' && c.healthCheck !== 'Đạt';
                    return `
                    <div class="kanban-card" onclick="RecruitmentModule.showCandidateDetail('${c.id}')">
                      <div class="kanban-card-header">
                        ${Helpers.avatarHTML(c.name, 28)}
                        <div class="kanban-card-title">${c.name}</div>
                      </div>
                      <div class="kanban-card-position">${job ? job.title : c.jobId}</div>
                      ${healthBlock ? '<div style="font-size:var(--font-size-xs);color:#BF2600;font-weight:600">🔒 Hard Block: Chờ kết quả y tế (TT14)</div>' : ''}
                      <div class="kanban-card-footer">
                        <span class="kanban-card-score ${scoreClass}">${c.matchScore}% match</span>
                        <span style="font-size:var(--font-size-xs);color:var(--text-tertiary)">${Helpers.timeAgo(c.appliedDate)}</span>
                      </div>
                    </div>`;
                  }).join('')}
                  ${candidates.length === 0 ? '<div style="text-align:center;color:var(--text-tertiary);font-size:var(--font-size-sm);padding:var(--space-4)">Chưa có ứng viên</div>' : ''}
                </div>
              </div>`;
            }).join('')}
          </div>
        </div>
      </div>
    `;
  },

  showCandidateDetail(candidateId) {
    const c = RecruitmentData.candidates.find(x => x.id === candidateId);
    if (!c) return;
    const job = RecruitmentData.jobs.find(j => j.id === c.jobId);

    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
    overlay.innerHTML = `
      <div class="modal">
        <div class="modal-header">
          <div>
            <div class="modal-title">${c.name}</div>
            <div style="font-size:var(--font-size-sm);color:var(--text-secondary);margin-top:4px">Ứng viên ${c.id} — ${job ? job.title : c.jobId}</div>
          </div>
          <button class="modal-close" onclick="this.closest('.modal-overlay').remove()">✕</button>
        </div>
        <div class="modal-body">
          <div style="display:flex;gap:var(--space-4);margin-bottom:var(--space-5)">
            ${Helpers.avatarHTML(c.name, 56)}
            <div>
              <div style="font-size:var(--font-size-lg);font-weight:700;margin-bottom:4px">${c.name}</div>
              <div style="font-size:var(--font-size-sm);color:var(--text-secondary)">${c.education}</div>
              <div style="font-size:var(--font-size-sm);color:var(--text-secondary)">📧 ${c.email} · 📞 ${c.phone}</div>
            </div>
          </div>
          
          <div class="form-row" style="margin-bottom:var(--space-4)">
            <div class="form-group" style="margin:0"><div class="form-label">Match Score</div><div><span class="kanban-card-score ${c.matchScore >= 85 ? 'high' : 'medium'}" style="font-size:var(--font-size-md)">${c.matchScore}%</span></div></div>
            <div class="form-group" style="margin:0"><div class="form-label">Giai đoạn</div><div><span class="badge badge-primary">${c.stage}</span></div></div>
          </div>
          <div class="form-row" style="margin-bottom:var(--space-4)">
            <div class="form-group" style="margin:0"><div class="form-label">Nguồn</div><div>${c.source}</div></div>
            <div class="form-group" style="margin:0"><div class="form-label">Ngày ứng tuyển</div><div>${Helpers.formatDate(c.appliedDate)}</div></div>
          </div>
          ${c.interviewScore !== null ? `
          <div class="form-group">
            <div class="form-label">Điểm phỏng vấn (Rubric)</div>
            <div class="progress-bar" style="height:12px">
              <div class="progress-bar-fill" style="width:${c.interviewScore}%;background:linear-gradient(90deg,#0052CC,#36B37E)"></div>
            </div>
            <div style="text-align:right;font-weight:700;color:var(--primary);margin-top:4px">${c.interviewScore}/100</div>
          </div>` : ''}
          ${c.healthCheck ? `
          <div class="form-group">
            <div class="form-label">Kiểm tra sức khỏe (Thông tư 14)</div>
            <div>${c.healthCheck === 'Đạt' ? '<span class="badge badge-active"><span class="badge-dot"></span>Đạt yêu cầu</span>' : '<span class="badge badge-trial"><span class="badge-dot"></span>' + c.healthCheck + '</span>'}</div>
          </div>` : ''}
          <div class="form-group" style="margin:0">
            <div class="form-label">Ghi chú</div>
            <div style="background:var(--bg-hover);padding:var(--space-3);border-radius:var(--radius-lg);font-size:var(--font-size-sm)">${c.notes}</div>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" onclick="this.closest('.modal-overlay').remove()">Đóng</button>
          <button class="btn btn-danger btn-sm">❌ Từ chối</button>
          <button class="btn btn-success">✅ Chuyển giai đoạn tiếp</button>
        </div>
      </div>
    `;
    document.body.appendChild(overlay);
  },

  showNewJobModal() {
    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
    overlay.innerHTML = `
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title">📝 Đề xuất Tuyển dụng mới</div>
          <button class="modal-close" onclick="this.closest('.modal-overlay').remove()">✕</button>
        </div>
        <div class="modal-body">
          <div class="alert-banner alert-banner-warning" style="margin-bottom:var(--space-4)">
            <span class="alert-banner-icon">⚠️</span>
            <div class="alert-banner-content">
              <div class="alert-banner-title">Kiểm tra định biên</div>
              <div class="alert-banner-text">Hệ thống sẽ tự động kiểm tra định biên. Nếu vượt định biên, yêu cầu cần CHRO phê duyệt.</div>
            </div>
          </div>
          <div class="form-group"><label class="form-label">Vị trí tuyển <span class="required">*</span></label><input class="form-input" placeholder="VD: Kỹ sư QA/QC"></div>
          <div class="form-row">
            <div class="form-group"><label class="form-label">Đơn vị</label><select class="form-select">${DepartmentsData.getDepartmentList().map(d => `<option>${d.name}</option>`).join('')}</select></div>
            <div class="form-group"><label class="form-label">Số lượng</label><input class="form-input" type="number" value="1" min="1"></div>
          </div>
          <div class="form-group"><label class="form-label">Mức lương dự kiến</label><input class="form-input" placeholder="VD: 16-22 triệu"></div>
          <div class="form-group"><label class="form-label">Lý do tuyển</label><textarea class="form-textarea" placeholder="Mô tả lý do cần tuyển vị trí này..."></textarea></div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" onclick="this.closest('.modal-overlay').remove()">Hủy</button>
          <button class="btn btn-primary" onclick="alert('Demo: Đề xuất đã được gửi đến CHRO!'); this.closest('.modal-overlay').remove()">Gửi đề xuất</button>
        </div>
      </div>
    `;
    document.body.appendChild(overlay);
  }
};
