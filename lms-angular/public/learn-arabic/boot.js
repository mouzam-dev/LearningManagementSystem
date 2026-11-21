// Starts the embedded Learn Arabic app inside the LMS: requires the site login, loads the course from
// our API into window.CATALOG (the format app.js expects), seeds this user's best scores from the
// server, then runs app.js. Quiz results are saved back to the server by saveScore() in app.js.
(async () => {
  // Dev runs the Angular dev server (4201) and the API (5116) separately; production serves both from one origin.
  const API = location.port === '4201' ? 'http://localhost:5116/api' : '/api';
  const read = (k) => { try { return localStorage.getItem(k); } catch { return null; } };
  const write = (k, v) => { try { localStorage.setItem(k, v); } catch { /* storage blocked: app still runs */ } };
  const toLogin = () => location.replace('/login?returnUrl=' + encodeURIComponent('/arabic'));
  const fail = (msg) => { document.getElementById('app').innerHTML = `<p style="padding:24px">${msg}</p>`; };

  const token = read('lms.accessToken');
  if (!token) return toLogin();

  const get = (path) => fetch(API + path, { headers: { Authorization: 'Bearer ' + token } });
  let catalog, progress;
  try {
    [catalog, progress] = await Promise.all([get('/arabic/catalog'), get('/arabic/progress')]);
  } catch {
    return fail('Could not reach the server. Please check your connection and reload.');
  }
  if (catalog.status === 401 || progress.status === 401) return toLogin();
  if (!catalog.ok) return fail('The course could not be loaded. Please reload the page.');

  window.CATALOG = await catalog.json();
  const rows = progress.ok ? await progress.json() : [];
  // app.js keys scores as "<unitId>-<lesson>", with "test" for the unit test (lesson 0 on the server).
  write('la.scores', JSON.stringify(Object.fromEntries(
    rows.map((r) => [`${r.unitId}-${r.lessonNumber || 'test'}`, r.bestScore]))));
  // First visit: follow the site's light/dark choice; afterwards the app's own theme button decides.
  const siteTheme = read('lms.theme');
  if (read('la.theme') == null && siteTheme) write('la.theme', JSON.stringify(siteTheme));

  window.LMS = { api: API, token, home: '/' };
  const s = document.createElement('script');
  s.src = 'app.js?v=lms1';
  document.body.appendChild(s);
})();
