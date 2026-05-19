// LMS user-manual screenshot capture (one-off).
//
// Walks each role through a fixed list of pages on the running dev server
// (localhost:4201 with API on localhost:5117) and writes PNGs to
// docs/user-manual/screenshots/<role>/<NN-slug>.png.
//
// Run from tools/screenshot-capture/:
//   node capture.js

const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

const BASE_URL = 'http://localhost:4201';
const API_URL = 'http://localhost:5117/api';
const OUT_ROOT = path.resolve(__dirname, '../../docs/user-manual/screenshots');
const VIEWPORT = { width: 1440, height: 900 };

const PASSWORD = 'Password1!';
const ROLES = {
  student:    { email: 'demo.student@lms.dev',  out: 'student'    },
  teacher:    { email: 'demo.teacher@lms.dev',  out: 'teacher'    },
  orgadmin:   { email: 'demo.orgadmin@lms.dev', out: 'orgadmin'   },
  superadmin: { email: 'demo.admin@lms.dev',    out: 'superadmin' },
};

// Public pages — captured without logging in.
const PUBLIC_SHOTS = [
  { slug: '01-landing-hero',         path: '/home',     scrollY: 0 },
  { slug: '02-landing-courses-grid', path: '/home',     scrollY: 900 },
  { slug: '03-landing-value-props',  path: '/home',     scrollY: 1900 },
  { slug: '04-login',                path: '/login' },
  { slug: '05-register',             path: '/register' },
];

// Per-role page lists. Each entry can pin a dynamic id via `discover` —
// the helper passes the role's auth token + a `fetch` helper, and the
// returned object is merged into the entry as overrides (e.g. {path}).
const ROLE_SHOTS = {
  student: [
    { slug: '01-dashboard',     path: '/student/dashboard' },
    { slug: '02-catalog',       path: '/student/catalog' },
    { slug: '03-course-detail', discover: async (api) => {
        const r = await api(`/student/courses?page=1&pageSize=1`);
        const id = r?.items?.[0]?.courseId;
        return id ? { path: `/student/courses/${id}` } : null;
      } },
    { slug: '04-certificates',  path: '/student/certificates' },
    { slug: '05-profile',       path: '/profile' },
    { slug: '06-notifications', path: '/notifications' },
  ],
  teacher: [
    { slug: '01-dashboard',     path: '/teacher/dashboard' },
    { slug: '02-courses-list',  path: '/teacher/courses' },
    { slug: '03-course-builder', discover: async (api) => {
        const r = await api(`/teacher/courses`);
        // /api/teacher/courses returns a bare array, /api/student/courses returns {items: []}.
        const list = Array.isArray(r) ? r : r?.items;
        const id = list?.[0]?.courseId;
        return id ? { path: `/teacher/courses/${id}` } : null;
      } },
    { slug: '04-create-course', path: '/teacher/courses/new' },
    { slug: '05-grading-inbox', path: '/teacher/grading' },
    { slug: '06-question-bank', path: '/teacher/question-bank' },
    { slug: '07-rubrics',       path: '/teacher/rubrics' },
    { slug: '08-profile',       path: '/profile' },
  ],
  orgadmin: [
    { slug: '01-dashboard', path: '/orgadmin/dashboard' },
    { slug: '02-branches',  path: '/orgadmin/branches' },
    { slug: '03-teachers',  path: '/orgadmin/teachers' },
    { slug: '04-courses',   path: '/orgadmin/courses' },
    { slug: '05-profile',   path: '/profile' },
  ],
  superadmin: [
    { slug: '01-dashboard',         path: '/admin/dashboard' },
    { slug: '02-organizations',     path: '/admin/organizations' },
    { slug: '03-users',             path: '/admin/users' },
    { slug: '04-role-permissions',  path: '/admin/role-permissions' },
    { slug: '05-courses',           path: '/admin/courses' },
    { slug: '06-reports',           path: '/admin/reports' },
    { slug: '07-audit',             path: '/admin/audit' },
    { slug: '08-profile',           path: '/profile' },
  ],
};

function ensureDir(d) { fs.mkdirSync(d, { recursive: true }); }

async function loginViaApi(email) {
  const res = await fetch(`${API_URL}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password: PASSWORD }),
  });
  if (!res.ok) throw new Error(`Login for ${email} failed: ${res.status}`);
  const body = await res.json();
  return body;
}

async function shoot(page, fullPath, fullUrl, { scrollY = 0 } = {}) {
  await page.goto(fullUrl, { waitUntil: 'networkidle', timeout: 30000 });
  await page.waitForTimeout(700);
  if (scrollY) await page.evaluate((y) => window.scrollTo(0, y), scrollY);
  await page.waitForTimeout(300);
  await page.screenshot({ path: fullPath, fullPage: scrollY === undefined });
}

async function capturePublic(browser) {
  const ctx = await browser.newContext({ viewport: VIEWPORT });
  const page = await ctx.newPage();
  const outDir = path.join(OUT_ROOT, 'public');
  ensureDir(outDir);
  for (const s of PUBLIC_SHOTS) {
    const out = path.join(outDir, `${s.slug}.png`);
    try {
      await shoot(page, out, BASE_URL + s.path, { scrollY: s.scrollY });
      console.log(`[public] ${s.slug}.png`);
    } catch (err) {
      console.error(`[public] ${s.slug} FAILED: ${err.message}`);
    }
  }
  await ctx.close();
}

async function captureRole(browser, roleKey) {
  const { email, out } = ROLES[roleKey];
  const auth = await loginViaApi(email);
  const ctx = await browser.newContext({ viewport: VIEWPORT });
  const page = await ctx.newPage();

  // Inject the JWT + user into localStorage before app boot so the auth
  // service finds it on first paint.
  await ctx.addInitScript(({ token, refresh, user }) => {
    localStorage.setItem('lms.accessToken', token);
    localStorage.setItem('lms.refreshToken', refresh);
    localStorage.setItem('lms.user', JSON.stringify(user));
  }, { token: auth.accessToken, refresh: auth.refreshToken, user: auth.user });

  const api = (suffix) =>
    fetch(`${API_URL}${suffix}`, { headers: { Authorization: `Bearer ${auth.accessToken}` } })
      .then((r) => (r.ok ? r.json() : null))
      .catch(() => null);

  const outDir = path.join(OUT_ROOT, out);
  ensureDir(outDir);

  for (const entry of ROLE_SHOTS[roleKey]) {
    let { slug, path: pathOverride, scrollY } = entry;
    try {
      if (entry.discover) {
        const overrides = await entry.discover(api);
        if (!overrides) {
          console.warn(`[${roleKey}] ${slug} SKIPPED (discover returned null)`);
          continue;
        }
        pathOverride = overrides.path ?? pathOverride;
      }
      const fullPath = path.join(outDir, `${slug}.png`);
      await shoot(page, fullPath, BASE_URL + pathOverride, { scrollY });
      console.log(`[${roleKey}] ${slug}.png`);
    } catch (err) {
      console.error(`[${roleKey}] ${slug} FAILED: ${err.message}`);
    }
  }
  await ctx.close();
}

(async () => {
  ensureDir(OUT_ROOT);
  const browser = await chromium.launch({ headless: true });
  try {
    console.log('--- public ---');
    await capturePublic(browser);
    for (const role of Object.keys(ROLES)) {
      console.log(`--- ${role} ---`);
      await captureRole(browser, role);
    }
  } finally {
    await browser.close();
  }
})();
