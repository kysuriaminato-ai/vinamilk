import { test, expect } from '@playwright/test';

const BASE_URL = process.env.VNM_BASE_URL || 'http://localhost:5000';

test.describe('Vinamilk HRM Public SEO & Core Endpoints', () => {
  test('Public About page has Organization JSON-LD & SEO tags', async ({ page }) => {
    await page.goto(`${BASE_URL}/gioi-thieu`);
    await expect(page).toHaveTitle(/Vinamilk/);
    const jsonLd = await page.locator('script[type="application/ld+json"]').textContent();
    expect(jsonLd).toContain('Organization');
  });

  test('Public Careers page renders job list & breadcrumb', async ({ page }) => {
    await page.goto(`${BASE_URL}/tuyen-dung`);
    await expect(page.locator('h1')).toContainText('Cơ hội Nghề nghiệp');
  });

  test('Sitemap XML & Robots.txt & Health Check respond 200', async ({ request }) => {
    const sitemap = await request.get(`${BASE_URL}/sitemap.xml`);
    expect(sitemap.status()).toBe(200);
    expect(await sitemap.text()).toContain('<urlset');

    const robots = await request.get(`${BASE_URL}/robots.txt`);
    expect(robots.status()).toBe(200);
    expect(await robots.text()).toContain('Sitemap:');

    const health = await request.get(`${BASE_URL}/health/live`);
    expect(health.status()).toBe(200);
  });
});

