// ============================================
// VINAMILK HRIS - Chart Utilities
// Canvas API chart rendering
// ============================================

const Charts = {
  colors: {
    primary: '#0052CC',
    primaryLight: '#4C9AFF',
    cyan: '#00B8D9',
    teal: '#00C7B7',
    green: '#36B37E',
    yellow: '#FFAB00',
    orange: '#FF8B00',
    red: '#FF5630',
    purple: '#6554C0',
    pink: '#E91E8C',
    gray: '#8993A4'
  },

  palette: ['#0052CC', '#00B8D9', '#36B37E', '#FFAB00', '#FF8B00', '#FF5630', '#6554C0', '#E91E8C', '#008DA6', '#006644'],

  // Draw a donut chart
  donut(canvasId, data, opts = {}) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    const dpr = window.devicePixelRatio || 1;
    const size = opts.size || Math.min(canvas.parentElement.offsetWidth, 220);
    canvas.width = size * dpr;
    canvas.height = size * dpr;
    canvas.style.width = size + 'px';
    canvas.style.height = size + 'px';
    ctx.scale(dpr, dpr);

    const cx = size / 2, cy = size / 2;
    const radius = size / 2 - 10;
    const innerRadius = radius * (opts.innerRadius || 0.65);
    const total = data.reduce((s, d) => s + d.value, 0);

    let startAngle = -Math.PI / 2;
    data.forEach((d, i) => {
      const sliceAngle = (d.value / total) * 2 * Math.PI;
      const endAngle = startAngle + sliceAngle;

      ctx.beginPath();
      ctx.arc(cx, cy, radius, startAngle, endAngle);
      ctx.arc(cx, cy, innerRadius, endAngle, startAngle, true);
      ctx.closePath();
      ctx.fillStyle = d.color || this.palette[i % this.palette.length];
      ctx.fill();

      startAngle = endAngle;
    });

    // Center text
    if (opts.centerText) {
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      ctx.fillStyle = '#172B4D';
      ctx.font = `800 ${size * 0.13}px 'Be Vietnam Pro', sans-serif`;
      ctx.fillText(opts.centerText, cx, cy - 6);
      if (opts.centerSubtext) {
        ctx.font = `500 ${size * 0.06}px 'Be Vietnam Pro', sans-serif`;
        ctx.fillStyle = '#8993A4';
        ctx.fillText(opts.centerSubtext, cx, cy + 14);
      }
    }
  },

  // Draw a bar chart
  bar(canvasId, labels, datasets, opts = {}) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    const dpr = window.devicePixelRatio || 1;
    const w = canvas.parentElement.offsetWidth;
    const h = opts.height || 280;
    canvas.width = w * dpr;
    canvas.height = h * dpr;
    canvas.style.width = w + 'px';
    canvas.style.height = h + 'px';
    ctx.scale(dpr, dpr);

    const pad = { top: 20, right: 20, bottom: 40, left: 60 };
    const chartW = w - pad.left - pad.right;
    const chartH = h - pad.top - pad.bottom;

    // Find max value
    let maxVal = 0;
    datasets.forEach(ds => {
      ds.data.forEach(v => { if (v > maxVal) maxVal = v; });
    });
    maxVal = Math.ceil(maxVal / 10) * 10 || 10;

    // Draw grid
    const gridLines = 5;
    ctx.strokeStyle = '#E1E4E8';
    ctx.lineWidth = 0.5;
    ctx.font = `500 11px 'Be Vietnam Pro', sans-serif`;
    ctx.fillStyle = '#8993A4';
    ctx.textAlign = 'right';

    for (let i = 0; i <= gridLines; i++) {
      const y = pad.top + (chartH / gridLines) * i;
      const val = Math.round(maxVal - (maxVal / gridLines) * i);
      ctx.beginPath();
      ctx.moveTo(pad.left, y);
      ctx.lineTo(w - pad.right, y);
      ctx.stroke();
      ctx.fillText(Helpers.formatNumber(val), pad.left - 8, y + 4);
    }

    // Draw bars
    const groupWidth = chartW / labels.length;
    const barWidth = Math.min(groupWidth / (datasets.length + 1), 32);
    const gap = (groupWidth - barWidth * datasets.length) / 2;

    datasets.forEach((ds, di) => {
      ds.data.forEach((val, i) => {
        const x = pad.left + i * groupWidth + gap + di * barWidth;
        const barH = (val / maxVal) * chartH;
        const y = pad.top + chartH - barH;

        // Bar with rounded top
        const r = Math.min(barWidth / 2, 4);
        ctx.beginPath();
        ctx.moveTo(x, y + r);
        ctx.arcTo(x, y, x + r, y, r);
        ctx.arcTo(x + barWidth, y, x + barWidth, y + r, r);
        ctx.lineTo(x + barWidth, pad.top + chartH);
        ctx.lineTo(x, pad.top + chartH);
        ctx.closePath();

        ctx.fillStyle = ds.color || this.palette[di];
        ctx.globalAlpha = 0.85;
        ctx.fill();
        ctx.globalAlpha = 1;
      });
    });

    // Draw x labels
    ctx.textAlign = 'center';
    ctx.fillStyle = '#8993A4';
    ctx.font = `500 11px 'Be Vietnam Pro', sans-serif`;
    labels.forEach((label, i) => {
      const x = pad.left + i * groupWidth + groupWidth / 2;
      ctx.fillText(label, x, h - pad.bottom + 20);
    });
  },

  // Draw a line chart
  line(canvasId, labels, datasets, opts = {}) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    const dpr = window.devicePixelRatio || 1;
    const w = canvas.parentElement.offsetWidth;
    const h = opts.height || 280;
    canvas.width = w * dpr;
    canvas.height = h * dpr;
    canvas.style.width = w + 'px';
    canvas.style.height = h + 'px';
    ctx.scale(dpr, dpr);

    const pad = { top: 20, right: 20, bottom: 40, left: 60 };
    const chartW = w - pad.left - pad.right;
    const chartH = h - pad.top - pad.bottom;

    let maxVal = 0;
    datasets.forEach(ds => {
      ds.data.forEach(v => { if (v > maxVal) maxVal = v; });
    });
    maxVal = Math.ceil(maxVal / 10) * 10 || 10;

    // Grid
    const gridLines = 5;
    ctx.strokeStyle = '#E1E4E8';
    ctx.lineWidth = 0.5;
    ctx.font = `500 11px 'Be Vietnam Pro', sans-serif`;
    ctx.fillStyle = '#8993A4';
    ctx.textAlign = 'right';

    for (let i = 0; i <= gridLines; i++) {
      const y = pad.top + (chartH / gridLines) * i;
      const val = Math.round(maxVal - (maxVal / gridLines) * i);
      ctx.beginPath();
      ctx.moveTo(pad.left, y);
      ctx.lineTo(w - pad.right, y);
      ctx.stroke();
      ctx.fillText(opts.formatY ? opts.formatY(val) : Helpers.formatNumber(val), pad.left - 8, y + 4);
    }

    // Lines
    datasets.forEach((ds, di) => {
      const color = ds.color || this.palette[di];
      const points = ds.data.map((val, i) => ({
        x: pad.left + (i / (labels.length - 1)) * chartW,
        y: pad.top + chartH - (val / maxVal) * chartH
      }));

      // Area fill
      if (ds.fill) {
        ctx.beginPath();
        ctx.moveTo(points[0].x, pad.top + chartH);
        points.forEach(p => ctx.lineTo(p.x, p.y));
        ctx.lineTo(points[points.length - 1].x, pad.top + chartH);
        ctx.closePath();
        const grad = ctx.createLinearGradient(0, pad.top, 0, pad.top + chartH);
        grad.addColorStop(0, color + '30');
        grad.addColorStop(1, color + '05');
        ctx.fillStyle = grad;
        ctx.fill();
      }

      // Line
      ctx.beginPath();
      ctx.strokeStyle = color;
      ctx.lineWidth = 2.5;
      ctx.lineJoin = 'round';
      ctx.lineCap = 'round';
      points.forEach((p, i) => {
        if (i === 0) ctx.moveTo(p.x, p.y);
        else ctx.lineTo(p.x, p.y);
      });
      ctx.stroke();

      // Dots
      points.forEach(p => {
        ctx.beginPath();
        ctx.arc(p.x, p.y, 4, 0, Math.PI * 2);
        ctx.fillStyle = 'white';
        ctx.fill();
        ctx.strokeStyle = color;
        ctx.lineWidth = 2.5;
        ctx.stroke();
      });
    });

    // X labels
    ctx.textAlign = 'center';
    ctx.fillStyle = '#8993A4';
    ctx.font = `500 11px 'Be Vietnam Pro', sans-serif`;
    labels.forEach((label, i) => {
      const x = pad.left + (i / (labels.length - 1)) * chartW;
      ctx.fillText(label, x, h - pad.bottom + 20);
    });
  },

  // Draw sparkline
  sparkline(canvasId, data, color = '#0052CC') {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    const dpr = window.devicePixelRatio || 1;
    const w = canvas.parentElement.offsetWidth;
    const h = 32;
    canvas.width = w * dpr;
    canvas.height = h * dpr;
    canvas.style.width = w + 'px';
    canvas.style.height = h + 'px';
    ctx.scale(dpr, dpr);

    const max = Math.max(...data);
    const min = Math.min(...data);
    const range = max - min || 1;
    const pad = 2;

    const points = data.map((v, i) => ({
      x: pad + (i / (data.length - 1)) * (w - pad * 2),
      y: pad + (1 - (v - min) / range) * (h - pad * 2)
    }));

    // Area
    ctx.beginPath();
    ctx.moveTo(points[0].x, h);
    points.forEach(p => ctx.lineTo(p.x, p.y));
    ctx.lineTo(points[points.length - 1].x, h);
    ctx.closePath();
    const grad = ctx.createLinearGradient(0, 0, 0, h);
    grad.addColorStop(0, color + '25');
    grad.addColorStop(1, color + '05');
    ctx.fillStyle = grad;
    ctx.fill();

    // Line
    ctx.beginPath();
    ctx.strokeStyle = color;
    ctx.lineWidth = 1.5;
    ctx.lineJoin = 'round';
    points.forEach((p, i) => {
      if (i === 0) ctx.moveTo(p.x, p.y);
      else ctx.lineTo(p.x, p.y);
    });
    ctx.stroke();
  },

  // Horizontal bar chart (for funnel)
  horizontalBar(canvasId, data, opts = {}) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    const dpr = window.devicePixelRatio || 1;
    const w = canvas.parentElement.offsetWidth;
    const h = opts.height || data.length * 44 + 20;
    canvas.width = w * dpr;
    canvas.height = h * dpr;
    canvas.style.width = w + 'px';
    canvas.style.height = h + 'px';
    ctx.scale(dpr, dpr);

    const maxVal = Math.max(...data.map(d => d.value));
    const pad = { left: 120, right: 60, top: 10 };
    const barH = 26;
    const gap = 18;

    data.forEach((d, i) => {
      const y = pad.top + i * (barH + gap);
      const barW = ((d.value / maxVal) * (w - pad.left - pad.right));

      // Label
      ctx.textAlign = 'right';
      ctx.fillStyle = '#5E6C84';
      ctx.font = `500 12px 'Be Vietnam Pro', sans-serif`;
      ctx.fillText(d.label, pad.left - 12, y + barH / 2 + 4);

      // Bar
      const r = barH / 2;
      ctx.beginPath();
      ctx.moveTo(pad.left, y);
      ctx.lineTo(pad.left + barW - r, y);
      ctx.arcTo(pad.left + barW, y, pad.left + barW, y + r, r);
      ctx.arcTo(pad.left + barW, y + barH, pad.left + barW - r, y + barH, r);
      ctx.lineTo(pad.left, y + barH);
      ctx.closePath();
      ctx.fillStyle = d.color || this.palette[i];
      ctx.fill();

      // Value
      ctx.textAlign = 'left';
      ctx.fillStyle = '#172B4D';
      ctx.font = `700 12px 'Be Vietnam Pro', sans-serif`;
      ctx.fillText(Helpers.formatNumber(d.value), pad.left + barW + 8, y + barH / 2 + 4);
    });
  }
};
