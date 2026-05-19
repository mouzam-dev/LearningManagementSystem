// Renders the user-manual HTML to PDF via Playwright's print-to-PDF,
// AND emits a web-servable copy of the same manual under
// lms-angular/public/docs/user-manual/ (with screenshots copied alongside)
// so the Angular app's Support page can iframe it.

const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');
const { renderHtml } = require('./manual-template');

const ROOT = path.resolve(__dirname, '../..');
const SCREENSHOTS = path.join(ROOT, 'docs/user-manual/screenshots');
const OUT_HTML = path.join(ROOT, 'docs/user-manual/LMS-User-Manual.html');
const OUT_PDF  = path.join(ROOT, 'docs/user-manual/LMS-User-Manual.pdf');

// Web-servable copy lives in Angular's public/ so it's both available to the
// dev server (http://localhost:4201/docs/user-manual/) and copied into the
// production build's wwwroot.
const WEB_DIR    = path.join(ROOT, 'lms-angular/public/docs/user-manual');
const WEB_HTML   = path.join(WEB_DIR, 'index.html');
const WEB_SHOTS  = path.join(WEB_DIR, 'screenshots');
const WEB_PDF    = path.join(WEB_DIR, 'LMS-User-Manual.pdf');

function copyDirSync(src, dst) {
  fs.mkdirSync(dst, { recursive: true });
  for (const entry of fs.readdirSync(src, { withFileTypes: true })) {
    const s = path.join(src, entry.name);
    const d = path.join(dst, entry.name);
    if (entry.isDirectory()) copyDirSync(s, d);
    else fs.copyFileSync(s, d);
  }
}

(async () => {
  // 1. PDF source: img src uses file:// absolute paths so Chromium can read
  //    the screenshots from disk when we render to PDF.
  const pdfHtml = renderHtml(SCREENSHOTS);
  fs.writeFileSync(OUT_HTML, pdfHtml, 'utf8');

  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext();
  const page = await ctx.newPage();
  await page.goto('file://' + OUT_HTML.replace(/\\/g, '/'), { waitUntil: 'networkidle' });
  await page.waitForTimeout(1200);
  await page.pdf({
    path: OUT_PDF,
    format: 'A4',
    printBackground: true,
    preferCSSPageSize: true,
  });
  await browser.close();
  const pdfSize = (fs.statSync(OUT_PDF).size / 1024 / 1024).toFixed(2);
  console.log(`PDF:           ${OUT_PDF}  (${pdfSize} MB)`);

  // 2. Web copy: img src uses RELATIVE paths so the same HTML works when
  //    served from /docs/user-manual/index.html alongside screenshots/.
  if (fs.existsSync(WEB_DIR)) fs.rmSync(WEB_DIR, { recursive: true, force: true });
  fs.mkdirSync(WEB_DIR, { recursive: true });
  fs.writeFileSync(WEB_HTML, renderHtml('screenshots'), 'utf8');
  copyDirSync(SCREENSHOTS, WEB_SHOTS);
  fs.copyFileSync(OUT_PDF, WEB_PDF);
  console.log(`Web copy:      ${WEB_HTML}`);
  console.log(`Web shots:     ${WEB_SHOTS}`);
  console.log(`Web PDF mirror:${WEB_PDF}`);
})();
