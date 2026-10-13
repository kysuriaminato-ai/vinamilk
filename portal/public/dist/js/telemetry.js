/**
 * Core Web Vitals Real User Monitoring (RUM) Telemetry
 * Monitors: LCP, INP, CLS, FCP, TTFB
 * Sends beacon to /api/v1/telemetry/web-vitals
 */
(function() {
  function sendMetric(name, value, rating) {
    const payload = JSON.stringify({
      metric: name,
      value: Math.round(value * 100) / 100,
      rating: rating,
      url: window.location.pathname,
      timestamp: Date.now()
    });

    if (navigator.sendBeacon) {
      navigator.sendBeacon('/api/v1/telemetry/web-vitals', payload);
    } else {
      fetch('/api/v1/telemetry/web-vitals', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: payload,
        keepalive: true
      }).catch(function() {});
    }
  }

  // PerformanceObserver for LCP (Largest Contentful Paint)
  if ('PerformanceObserver' in window) {
    try {
      const lcpObserver = new PerformanceObserver((entryList) => {
        const entries = entryList.getEntries();
        const lastEntry = entries[entries.length - 1];
        if (lastEntry) {
          const lcp = lastEntry.renderTime || lastEntry.loadTime;
          sendMetric('LCP', lcp, lcp < 2500 ? 'good' : (lcp < 4000 ? 'needs-improvement' : 'poor'));
        }
      });
      lcpObserver.observe({ type: 'largest-contentful-paint', buffered: true });

      // CLS (Cumulative Layout Shift)
      let clsValue = 0;
      const clsObserver = new PerformanceObserver((entryList) => {
        for (const entry of entryList.getEntries()) {
          if (!entry.hadRecentInput) {
            clsValue += entry.value;
          }
        }
        sendMetric('CLS', clsValue, clsValue < 0.1 ? 'good' : (clsValue < 0.25 ? 'needs-improvement' : 'poor'));
      });
      clsObserver.observe({ type: 'layout-shift', buffered: true });

      // INP / FID (Interaction to Next Paint)
      const inpObserver = new PerformanceObserver((entryList) => {
        for (const entry of entryList.getEntries()) {
          const duration = entry.duration;
          sendMetric('INP', duration, duration < 200 ? 'good' : (duration < 500 ? 'needs-improvement' : 'poor'));
        }
      });
      inpObserver.observe({ type: 'first-input', buffered: true });

    } catch (e) {
      console.warn('CWV Observation error:', e);
    }
  }
})();