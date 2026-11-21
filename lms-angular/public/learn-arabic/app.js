// Learn Arabic – single-page app, embedded in the LMS. Data comes from window.CATALOG, which boot.js
// loads from /api/arabic/catalog (the course stored in our database); scores are saved to the server too.
// Two interface modes: translations on (English UI, meanings in English / Roman Urdu / both)
// and Arabic only (whole UI in Arabic, right-to-left, no English or Urdu anywhere).
const BOOKS = window.CATALOG;
const BOOK_AR = ['', 'الكِتابُ الأَوَّل', 'الكِتابُ الثّاني', 'الكِتابُ الثّالِث', 'الكِتابُ الرّابِع'];
const AR_DIGIT = (n) => String(n).replace(/\d/g, (d) => '٠١٢٣٤٥٦٧٨٩'[d]);
const $ = (s, el = document) => el.querySelector(s);
const $$ = (s, el = document) => [...el.querySelectorAll(s)];
const esc = (s) => String(s ?? '').replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
const allUnits = () => BOOKS.flatMap((b) => b.units);
const unit = (id) => allUnits().find((u) => u.id === id);
const bookOf = (id) => +id.match(/^b(\d+)/)[1];
const shuffle = (a) => a.map((v) => [Math.random(), v]).sort((x, y) => x[0] - y[0]).map((p) => p[1]);
const hasArabic = (s) => /[؀-ۿ]/.test(s);
const hasLatin = (s) => /[A-Za-z]/.test(s);
// Escape text and wrap Arabic phrases inside English/Urdu sentences so they keep right-to-left order.
const mix = (s) => esc(s).replace(/[؀-ۿ][؀-ۿ\s،؟.!]*[؀-ۿ؟]/g, (m) => `<bdi lang="ar" class="arx">${m}</bdi>`);

// ---- icons (inline SVG, Lucide-style) ----
const ICON = {
  home: '<path d="M3 10.5 12 3l9 7.5V20a1 1 0 0 1-1 1h-5v-6h-6v6H4a1 1 0 0 1-1-1z"/>',
  book: '<path d="M2 4h6a4 4 0 0 1 4 4v13a3 3 0 0 0-3-3H2z"/><path d="M22 4h-6a4 4 0 0 0-4 4v13a3 3 0 0 1 3-3h7z"/>',
  cards: '<rect x="3" y="6" width="13" height="15" rx="2"/><path d="M8 3h11a2 2 0 0 1 2 2v12"/>',
  search: '<circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/>',
  globe: '<circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3a14 14 0 0 1 0 18M12 3a14 14 0 0 0 0 18"/>',
  moon: '<path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z"/>',
  sun: '<circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/>',
  auto: '<circle cx="12" cy="12" r="9"/><path d="M12 3v18" /><path d="M12 3a9 9 0 0 1 0 18z" fill="currentColor"/>',
  back: '<path d="m15 18-6-6 6-6"/>',
  next: '<path d="m9 18 6-6-6-6"/>',
  vol: '<path d="M11 5 6 9H2v6h4l5 4z"/><path d="M15.5 8.5a5 5 0 0 1 0 7M19 5a10 10 0 0 1 0 14"/>',
  trophy: '<path d="M8 21h8M12 17v4M7 4h10v5a5 5 0 0 1-10 0z"/><path d="M17 5h3v2a3 3 0 0 1-3 3M7 5H4v2a3 3 0 0 0 3 3"/>',
  check: '<path d="M20 6 9 17l-5-5"/>',
  x: '<path d="M18 6 6 18M6 6l12 12"/>',
  flame: '<path d="M12 22a7 7 0 0 0 7-7c0-4-3-6-4-10-2 2-3 4-3 6-1-1-2-2-2-4-2 2-5 5-5 8a7 7 0 0 0 7 7z"/>',
  star: '<path d="m12 3 2.8 5.7 6.2.9-4.5 4.4 1 6.2L12 17.3 6.5 20.2l1-6.2L3 9.6l6.2-.9z"/>',
  play: '<path d="M7 4v16l13-8z"/>',
  lock: '<rect x="4" y="11" width="16" height="10" rx="2"/><path d="M8 11V7a4 4 0 0 1 8 0v4"/>',
  chat: '<path d="M21 12a8 8 0 0 1-11.6 7.1L3 21l1.9-6.4A8 8 0 1 1 21 12z"/>',
  bulb: '<path d="M9 18h6M10 22h4M12 2a7 7 0 0 0-4 12.7V17h8v-2.3A7 7 0 0 0 12 2z"/>',
  image: '<rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="9" cy="9" r="2"/><path d="m21 15-5-5L5 21"/>',
  quiz: '<circle cx="12" cy="12" r="9"/><path d="M9.5 9a2.5 2.5 0 1 1 3.5 2.3c-.6.3-1 .9-1 1.6V14M12 17h.01"/>',
  again: '<path d="M3 12a9 9 0 0 1 15.5-6.2L21 8M21 3v5h-5M21 12a9 9 0 0 1-15.5 6.2L3 16M3 21v-5h5"/>',
  shuffle: '<path d="M16 3h5v5M4 20 21 3M21 16v5h-5M15 15l6 6M4 4l5 5"/>',
  expand: '<path d="M15 3h6v6M9 21H3v-6M21 3l-7 7M3 21l7-7"/>',
  sparkle: '<path d="M12 3l1.8 5.2L19 10l-5.2 1.8L12 17l-1.8-5.2L5 10l5.2-1.8z"/><path d="M19 15l.8 2.2L22 18l-2.2.8L19 21l-.8-2.2L16 18l2.2-.8z"/>',
};
// back/next arrows get "dir" so they flip in right-to-left mode
const ic = (n, cls = '') => `<svg class="ic ${cls} ${n === 'back' || n === 'next' ? 'dir' : ''}" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">${ICON[n]}</svg>`;

// ---- saved state (browser only; the app still works if storage is blocked) ----
const store = {
  // "la." prefix keeps these apart from the main site's own keys on the same origin.
  get(k, d) { try { return JSON.parse(localStorage.getItem('la.' + k)) ?? d; } catch { return d; } },
  set(k, v) { try { localStorage.setItem('la.' + k, JSON.stringify(v)); } catch {} },
};

// ---- language mode ----
const LANGS = { both: 'English + Roman Urdu', en: 'English only', ur: 'Roman Urdu only' };
let lang = store.get('lang', 'both');
let trans = store.get('trans', true), showTr = store.get('showTr', true);
const AR = () => !trans;                       // Arabic-only interface
const t = (en, ar) => (AR() ? ar : en);        // pick interface text
const N = (n) => (AR() ? AR_DIGIT(n) : String(n));
const bookName = (n) => t(`Book ${n}`, BOOK_AR[n]);
const unitName = (n) => t(`Unit ${n}`, `الوَحْدَةُ ${AR_DIGIT(n)}`);
const lessonName = (n) => t(`Lesson ${n}`, `الدَّرْسُ ${AR_DIGIT(n)}`);
// Meaning of an Arabic word/line in the chosen language(s); nothing at all in Arabic-only mode.
const T = (o) => !o || AR() ? '' :
  lang === 'both' ? `<span class="en-line">${mix(o.en)}</span>${o.ur ? `<span class="tl">${mix(o.ur)}</span>` : ''}`
  : `<span class="en-line">${mix(o[lang] || o.en)}</span>`;
// Title in the interface language (Arabic title in Arabic-only mode)
const T1 = (o) => AR() ? esc(o.ar || '') : esc(lang === 'ur' ? o.ur || o.en || o.ar : o.en || o.ar);
const trLine = (tr) => (AR() || !showTr || !tr ? '' : `<span class="tr">${esc(tr)}</span>`);

const scores = () => store.get('scores', {});
function saveScore(key, pct) {
  const s = scores();
  s[key] = Math.max(s[key] || 0, pct);
  store.set('scores', s);
  // Save to the learner's account (best score + attempts); "test" is lesson 0 on the server.
  if (window.LMS) {
    const i = key.lastIndexOf('-'), n = key.slice(i + 1);
    fetch(`${LMS.api}/arabic/progress`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${LMS.token}` },
      body: JSON.stringify({ unitId: key.slice(0, i), lessonNumber: n === 'test' ? 0 : +n, score: pct }),
    }).catch(() => {});
  }
  const today = new Date().toDateString(), st = store.get('streak', { day: '', n: 0 });
  if (st.day !== today) {
    st.n = st.day === new Date(Date.now() - 864e5).toDateString() ? st.n + 1 : 1;
    st.day = today;
    store.set('streak', st);
  }
}
const starCount = (pct) => (pct == null ? 0 : pct >= 90 ? 3 : pct >= 60 ? 2 : 1);
const stars = (pct) => pct == null ? '' :
  `<span class="stars" aria-label="${t(`${starCount(pct)} of 3 stars`, `${AR_DIGIT(starCount(pct))} من ٣ نجوم`)}">${[1, 2, 3].map((i) => ic('star', 'fill' + (i > starCount(pct) ? ' off' : ''))).join('')}</span>`;
const ring = (pct, size = 56, sw = 6) => {
  const r = (size - sw) / 2, c = 2 * Math.PI * r, h = size / 2;
  return `<svg class="ring" width="${size}" height="${size}" viewBox="0 0 ${size} ${size}" role="img" aria-label="${t(`${pct}% complete`, `${AR_DIGIT(pct)}٪ مكتمل`)}">
    <circle cx="${h}" cy="${h}" r="${r}" class="ring-bg" stroke-width="${sw}" fill="none"/>
    <circle cx="${h}" cy="${h}" r="${r}" class="ring-fg" stroke-width="${sw}" fill="none" stroke-linecap="round"
      stroke-dasharray="${c}" stroke-dashoffset="${c * (1 - pct / 100)}" transform="rotate(-90 ${h} ${h})"/>
    <text x="50%" y="50%" dominant-baseline="central" text-anchor="middle">${t(`${pct}%`, `${AR_DIGIT(pct)}٪`)}</text></svg>`;
};
const unitProgress = (u) => {
  if (!u.lessons) return 0;
  const s = scores();
  return Math.round((u.lessons.filter((l) => s[`${u.id}-${l.n}`] != null).length / u.lessons.length) * 100);
};
const bookProgress = (b) => {
  const ready = b.units.filter((u) => u.lessons);
  return ready.length ? Math.round(ready.reduce((a, u) => a + unitProgress(u), 0) / b.units.length) : 0;
};
const nextLesson = (u) => u.lessons.find((l) => scores()[`${u.id}-${l.n}`] == null) || u.lessons[0];

// Quiz questions usable in the current mode: Arabic-only needs Arabic question text and Arabic options.
const quizItems = (list) => list.map((q) => {
  if (!AR()) return { ...q, text: lang === 'ur' ? q.q.ur : q.q.en, text2: lang === 'both' ? q.q.ur : '', opts: q.options };
  const opts = q.options_ar || q.options;
  return q.q.ar && !opts.some(hasLatin) ? { ...q, text: q.q.ar, text2: '', opts } : null;
}).filter(Boolean);

// ---- theme: auto / light / dark ----
let theme = store.get('theme', 'auto');
function applyTheme() {
  if (theme === 'auto') delete document.documentElement.dataset.theme;
  else document.documentElement.dataset.theme = theme;
  const b = $('#themeBtn');
  b.innerHTML = ic(theme === 'dark' ? 'moon' : theme === 'light' ? 'sun' : 'auto');
  b.title = b.ariaLabel = t(`Theme: ${theme} (tap to change)`, { auto: 'المظهر: تلقائي', light: 'المظهر: فاتح', dark: 'المظهر: داكن' }[theme]);
}

// ---- language dropdown (globe button) ----
// One list: Arabic only, English, Roman Urdu, or both. Arabic only switches the whole app to Arabic, right-to-left.
const MODES = [
  ['ar', 'Arabic only', 'Whole app in Arabic', 'العربية فقط', 'التطبيق كلُّه بالعربية'],
  ['en', 'English', 'Meanings in English', 'الإنجليزية', 'المعاني بالإنجليزية'],
  ['ur', 'Roman Urdu', 'Meanings in Roman Urdu', 'الأردية', 'المعاني بالأردية'],
  ['both', 'English + Urdu', 'Meanings in both', 'الإنجليزية والأردية', 'المعاني باللغتين'],
];
const mode = () => (AR() ? 'ar' : lang);
const SHORT = { ar: 'العربية', en: 'English', ur: 'Urdu', both: 'EN + UR' };
function applyLang() {
  const root = document.documentElement;
  root.lang = AR() ? 'ar' : 'en';
  root.dir = AR() ? 'rtl' : 'ltr';
  const b = $('#langBtn'), open = !!$('#prefs') && !$('#prefs').hidden;
  // only redraw the button when its label changes, so a tap on it is never lost
  const html = `${ic('globe')}<span class="lb-text">${SHORT[mode()]}</span><svg class="ic chev" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"><path d="m6 9 6 6 6-6"/></svg>`;
  if (b.dataset.mode !== mode()) { b.innerHTML = html; b.dataset.mode = mode(); }
  b.title = b.ariaLabel = t(`Language: ${MODES.find((m) => m[0] === mode())[1]}`, 'اللغة: العربية فقط');
  b.setAttribute('aria-haspopup', 'true');
  b.setAttribute('aria-expanded', String(open));
  $('#q').placeholder = t('Search words in Arabic, English or Roman Urdu', 'ابحث عن كلمة أو درس…');
  $('.skip').textContent = t('Skip to content', 'انتقل إلى المحتوى');
  $('#backBtn').ariaLabel = t('Back', 'رجوع');
  $('.topsearch .sr').textContent = t('Search', 'بحث');
  $('.brand.mobile-only').ariaLabel = t('Home', 'الرئيسية');
  $('#side').ariaLabel = $('#tabbar').ariaLabel = t('Main navigation', 'القائمة الرئيسية');
}
function setMode(m) {
  if (m === 'ar') { trans = false; } else { trans = true; lang = m; store.set('lang', lang); }
  store.set('trans', trans);
  INDEX = null;
  togglePrefs(false);
  applyLang(); route(true);
  toast(t(`${MODES.find((x) => x[0] === m)[1]}`, 'العربية فقط'));
  $('#langBtn').focus();
}
function fillPrefs() {
  const el = $('#prefs');
  el.setAttribute('aria-label', t('Choose language', 'اختر اللغة'));
  el.innerHTML = `
    <div class="menu-head">${t('Language', 'اللغة')}</div>
    ${MODES.map(([k, en, enSub, ar, arSub]) => `
      <button class="mitem ${k === mode() ? 'on' : ''}" role="menuitemradio" aria-checked="${k === mode()}" data-mode="${k}">
        <span class="mtext"><b>${t(en, ar)}</b><small>${t(enSub, arSub)}</small></span>
        <span class="mcheck">${k === mode() ? ic('check') : ''}</span>
      </button>`).join('')}
    ${AR() ? '' : `<div class="msep"></div>
    <label class="sw"><input type="checkbox" role="switch" id="pTr" ${showTr ? 'checked' : ''}><span class="track" aria-hidden="true"></span><span>Show pronunciation <span class="muted small">(as-salāmu…)</span></span></label>`}`;
  $$('.mitem', el).forEach((it) => (it.onclick = () => setMode(it.dataset.mode)));
  if ($('#pTr')) $('#pTr').onchange = (e) => { showTr = e.target.checked; store.set('showTr', showTr); route(true); };
}
function togglePrefs(open) {
  let el = $('#prefs');
  if (!el) { el = document.createElement('div'); el.id = 'prefs'; el.className = 'prefs'; el.hidden = true; el.setAttribute('role', 'menu'); document.body.append(el); }
  el.hidden = open === undefined ? !el.hidden : !open;
  if (!el.hidden) { fillPrefs(); ($('.mitem.on', el) || $('.mitem', el)).focus(); }
  applyLang();
}
// close when tapping outside; composedPath still sees the button even if its contents were redrawn
document.addEventListener('click', (e) => {
  const el = $('#prefs'), path = e.composedPath();
  if (el && !el.hidden && !path.includes(el) && !path.includes($('#langBtn'))) togglePrefs(false);
});
document.addEventListener('keydown', (e) => {
  const el = $('#prefs');
  if (!el || el.hidden) return;
  if (e.key === 'Escape') { togglePrefs(false); $('#langBtn').focus(); }
  if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
    const items = $$('.mitem', el), i = items.indexOf(document.activeElement);
    items[(i + (e.key === 'ArrowDown' ? 1 : -1) + items.length) % items.length].focus();
    e.preventDefault();
  }
});
$('#themeBtn').onclick = () => { theme = { auto: 'light', light: 'dark', dark: 'auto' }[theme]; store.set('theme', theme); applyTheme(); };
$('#langBtn').onclick = () => togglePrefs();
function toast(msg) {
  let el = $('#toast');
  if (!el) { el = document.createElement('div'); el.id = 'toast'; el.setAttribute('role', 'status'); document.body.append(el); }
  el.textContent = msg; el.className = 'show';
  clearTimeout(el._h); el._h = setTimeout(() => (el.className = ''), 1600);
}

// ---- Arabic speech for words (uses the device's Arabic voice when it has one) ----
function speak(text) {
  if (!('speechSynthesis' in window)) return toast(t('Speech not available on this device', 'النطق غير متوفر على هذا الجهاز'));
  speechSynthesis.cancel();
  const u = new SpeechSynthesisUtterance(text);
  u.lang = 'ar-SA'; u.rate = 0.8;
  speechSynthesis.speak(u);
}
document.addEventListener('click', (e) => {
  const b = e.target.closest('[data-say]');
  if (b) { e.preventDefault(); e.stopPropagation(); play(b); return; }
  const tl = e.target.closest('.hide .tile');
  if (tl) tl.classList.toggle('open');
}, true);
// 🔊 plays our recorded clip (audio/<file>.mp3) when there is one, otherwise the device's Arabic voice
const sayBtn = (ar, au) => `<button class="say" data-say="${esc(ar)}" ${au ? `data-au="${au}"` : ''} aria-label="${t('Listen', 'استمع')}">${ic('vol')}</button>`;
let player;
function play(btn) {
  stopAll();
  if (player) { player.pause(); player = null; }
  if (!btn.dataset.au) return speak(btn.dataset.say);
  player = new Audio(`audio/${btn.dataset.au}`);
  btn.classList.add('playing');
  const done = () => btn.classList.remove('playing');
  player.onended = player.onpause = done;
  player.play().catch(() => { done(); speak(btn.dataset.say); });
}

// ▶ Play the whole conversation: every line in order, highlighted and scrolled into view; tap again to stop.
let seq = null;
const playAllLabel = (on) => (on ? `${ic('x')} ${t('Stop', 'إيقاف')}` : `${ic('play', 'fill')} ${t('Play whole conversation', 'استمع إلى الحوار كاملًا')}`);
function stopAll() {
  if (!seq) return;
  seq.stop = true; seq = null;
  if (player) { player.pause(); player = null; }
  if ('speechSynthesis' in window) speechSynthesis.cancel();
  $$('.dl.speaking').forEach((li) => li.classList.remove('speaking'));
  const b = $('#playAll');
  if (b) { b.innerHTML = playAllLabel(false); b.setAttribute('aria-pressed', 'false'); }
}
function speakThen(text, done) {
  if (!('speechSynthesis' in window)) return done();
  speechSynthesis.cancel();
  const u = new SpeechSynthesisUtterance(text);
  u.lang = 'ar-SA'; u.rate = 0.8; u.onend = done; u.onerror = done;
  speechSynthesis.speak(u);
}
function playAll() {
  if (seq) return stopAll();
  if (player) { player.pause(); player = null; }
  const lines = $$('.dlg .dl'), me = (seq = { stop: false });
  const b = $('#playAll');
  b.innerHTML = playAllLabel(true); b.setAttribute('aria-pressed', 'true');
  let i = 0;
  const next = () => {
    if (me.stop) return;
    $$('.dl.speaking').forEach((li) => li.classList.remove('speaking'));
    if (i >= lines.length) return stopAll();
    const li = lines[i++], btn = $('.say', li);
    li.classList.add('speaking');
    li.scrollIntoView({ behavior: 'smooth', block: 'center' });
    const after = () => { if (!me.stop) setTimeout(next, 450); };  // short pause between speakers
    if (btn.dataset.au) {
      player = new Audio(`audio/${btn.dataset.au}`);
      player.onended = after;
      player.play().catch(() => speakThen(btn.dataset.say, after));
    } else speakThen(btn.dataset.say, after);
  };
  next();
}

// ---- page chrome: top bar, sidebar, bottom tabs ----
let ctx = {};  // what the current page is: { tab, title, back, book, unitId, lessonN }
function chrome() {
  const back = $('#backBtn');
  back.hidden = !ctx.back; back.href = ctx.back || '#/'; back.innerHTML = ic('back');
  $('#topTitle').innerHTML = ctx.title || '';
  const tabs = [
    ['home', t('Home', 'الرئيسية'), '#/'],
    ['learn', t('Learn', 'تعلَّم'), `#/b/${ctx.book || store.get('book', 1)}`],
    ['words', t('Words', 'الكلمات'), '#/words'],
    ['search', t('Search', 'بحث'), '#/search/'],
  ];
  const icn = { home: 'home', learn: 'book', words: 'cards', search: 'search' };
  const link = (cls) => ([k, label, href]) => `<a class="${cls}" href="${href}" ${ctx.tab === k ? 'aria-current="page"' : ''}>${ic(icn[k])}<span>${label}</span></a>`;
  $('#tabbar').innerHTML = tabs.map(link('tab')).join('');

  const bn = ctx.book || store.get('book', 1), b = BOOKS.find((x) => x.n === bn), s = scores();
  $('#side').innerHTML = `
    <a href="#/" class="brand"><span class="mark" lang="ar">ع</span> ${t('Learn Arabic', 'تعلَّمِ العربية')}</a>
    ${window.LMS ? `<a class="navlink" href="${LMS.home}">${ic('back')}<span>${t('Back to Daura-e-Ilmiyah', 'العودة إلى الموقع')}</span></a>` : ''}
    ${tabs.map(link('navlink')).join('')}
    <h3>${t(`Book ${bn} · <span lang="ar">${BOOK_AR[bn]}</span>`, BOOK_AR[bn])}</h3>
    <div class="tree">${b.units.map((u) => `
      <a href="${u.lessons ? `#/u/${u.id}` : `#/b/${bn}`}" class="${u.id === ctx.unitId ? 'on' : ''} ${u.lessons ? '' : 'dim'}">
        <span class="un">${N(u.unit)}</span><span class="t">${T1(u.title)}</span>${u.lessons ? '' : ic('lock')}
      </a>
      ${u.id === ctx.unitId ? `<div class="sub">${u.lessons.map((l) => `
        <a href="#/l/${u.id}/${l.n}" class="${+ctx.lessonN === l.n ? 'on' : ''}"><span class="t">${N(l.n)}. ${T1(l.title)}</span>${s[`${u.id}-${l.n}`] != null ? ic('check') : ''}</a>`).join('')}
        <a href="#/q/${u.id}/test" class="${ctx.lessonN === 'test' ? 'on' : ''}"><span class="t">${t('Unit test', 'اختبار الوحدة')}</span>${ic('trophy')}</a></div>` : ''}`).join('')}
    </div>`;
}

// ---- pages ----
function allVocab() {
  return allUnits().flatMap((u) => (u.lessons || []).flatMap((l) => (l.vocab || []).map((v) => ({ ...v, unit: u, lesson: l }))));
}
// "Continue" card text, rebuilt in the current language
const lastTitle = (last) => {
  const m = (last?.href || '').match(/^#\/l\/b(\d+)u(\d+)\/(\d+)/);
  return m ? `${bookName(+m[1])} · ${unitName(+m[2])} · ${lessonName(+m[3])}` : '';
};

function home() {
  ctx = { tab: 'home', title: '' };
  const s = scores(), st = store.get('streak', { n: 0 }), last = store.get('last', null);
  const vocab = allVocab();
  const learned = vocab.filter((v) => s[`${v.unit.id}-${v.lesson.n}`] != null).length;
  const w = vocab[Math.floor(Date.now() / 864e5) % vocab.length];
  const first = allUnits().find((u) => u.lessons);
  const hour = new Date().getHours();
  const starsWon = Object.values(s).reduce((a, p) => a + starCount(p), 0);
  return `
    <section class="hero">
      <div class="salam">السَّلامُ عَلَيْكُم</div>
      <h1>${t(`${hour < 12 ? 'Good morning' : hour < 18 ? 'Good afternoon' : 'Good evening'}, little learner!`, `${hour < 12 ? 'صَباحُ الخَيْرِ' : 'مَساءُ الخَيْرِ'} يا بَطَل!`)}</h1>
      <p>${t('Learn a little every day. Read the lesson, listen to the book, then win stars in the quiz.', 'تَعَلَّمْ قَليلًا كُلَّ يَوْمٍ: اقْرَأِ الدَّرْسَ، وَاسْتَمِعْ إِلى الكِتابِ، ثُمَّ اربَحِ النُّجومَ في الاخْتِبار.')}</p>
      <div class="row">
        <div class="stat">${ic('flame', 'fill')}<div><b>${N(st.n)}</b><span>${t('day streak', 'أيّام متتالية')}</span></div></div>
        <div class="stat">${ic('star', 'fill')}<div><b>${N(starsWon)}</b><span>${t('stars won', 'نُجوم')}</span></div></div>
        <div class="stat">${ic('cards')}<div><b>${N(learned)}</b><span>${t('words learned', 'كلمة تعلَّمتَها')}</span></div></div>
      </div>
    </section>
    <div class="home-grid">
      <a class="card continue" href="${last ? last.href : `#/l/${first.id}/1`}">
        <span class="play">${ic('play', 'fill')}</span>
        <span><span class="kicker">${last ? t('Continue learning', 'تابِعِ التَّعَلُّم') : t('Start here', 'ابْدَأْ مِن هُنا')}</span><br>
        <b style="font-size:1.1rem">${esc(last ? lastTitle(last) : `${bookName(1)} · ${unitName(1)} · ${lessonName(1)}`)}</b><br>
        <span class="muted small">${last ? t('Pick up where you left off', 'أَكْمِلْ مِن حَيْثُ تَوَقَّفْت') : T1(first.title)}</span></span>
      </a>
      ${w ? `<div class="card wotd">
        <span class="kicker">${ic('sparkle')} ${t('Word of the day', 'كَلِمَةُ اليَوْم')}</span>
        <span class="ar">${esc(w.ar)}</span>
        ${trLine(w.tr)}<div class="trans">${T(w)}</div>
        <div style="margin-top:8px">${sayBtn(w.ar, w.au)}</div>
      </div>` : ''}
    </div>
    <div class="section">
      <h2>${ic('book')} ${t('Your books', 'كُتُبُك')}</h2>
      <div class="grid">${BOOKS.map((b) => {
        const ready = b.units.filter((u) => u.lessons).length, p = bookProgress(b);
        return `<a class="card bookcard b${b.n}" href="#/b/${b.n}">
          <span class="num" aria-hidden="true">${AR_DIGIT(b.n)}</span>
          <span class="kicker">${t(`Level ${b.n}`, `المُسْتَوى ${AR_DIGIT(b.n)}`)}</span>
          <h2>${bookName(b.n)}</h2>
          ${AR() ? '' : `<span class="ar-title">${BOOK_AR[b.n]}</span>`}
          <span class="grow"></span>
          <span>${ready ? `<span class="chip c">${t(`${ready} of ${b.units.length} units ready`, `${AR_DIGIT(b.units.length)} وحدة`)}</span>` : `<span class="chip">${ic('lock')} ${t('Coming soon', 'قريبًا')}</span>`}</span>
          <div class="bar"><i style="width:${p}%"></i></div>
        </a>`;
      }).join('')}</div>
    </div>`;
}

function book(n) {
  const b = BOOKS.find((x) => x.n === +n);
  if (!b) return notFound();
  store.set('book', b.n);
  ctx = { tab: 'learn', title: bookName(b.n), back: '#/', book: b.n };
  return `<div class="b${b.n}">
    <div class="pagehead"><div><span class="kicker">${t(`Level ${b.n}`, `المُسْتَوى ${AR_DIGIT(b.n)}`)}</span>
      <h1>${bookName(b.n)} ${AR() ? '' : `<span lang="ar" class="muted" style="font-size:.7em">${BOOK_AR[b.n]}</span>`}</h1></div></div>
    <nav class="seg" aria-label="${t('Choose a book', 'اختر كتابًا')}">${BOOKS.map((x) => `<a href="#/b/${x.n}" class="b${x.n}" ${x.n === b.n ? 'aria-current="page"' : ''}>${bookName(x.n)}</a>`).join('')}</nav>
    <div class="grid">${b.units.map((u) => u.lessons ? `
      <a class="card unitcard" href="#/u/${u.id}">
        <span class="badge">${N(u.unit)}</span>
        <span class="grow"><span class="ar">${esc(u.title.ar)}</span>${AR() ? '' : `<span class="en">${T1(u.title)}</span>`}</span>
        ${ring(unitProgress(u), 50, 5)}
      </a>` : `
      <div class="card unitcard locked" aria-label="${unitName(u.unit)}">
        <span class="badge">${N(u.unit)}</span>
        <span class="grow"><span class="ar">${esc(u.title.ar)}</span><span class="chip">${ic('lock')} ${t('Coming soon', 'قريبًا')}</span></span>
      </div>`).join('')}
    </div></div>`;
}

function unitPage(id) {
  const u = unit(id);
  if (!u?.lessons) return notFound();
  const s = scores(), b = bookOf(id), nx = nextLesson(u), p = unitProgress(u);
  const done = u.lessons.filter((l) => s[`${id}-${l.n}`] != null).length;
  ctx = { tab: 'learn', title: unitName(u.unit), back: `#/b/${b}`, book: b, unitId: id };
  return `<div class="b${b}">
    <section class="unit-hero">
      <div class="cover art" aria-hidden="true"><span class="art-num">${AR_DIGIT(u.unit)}</span><span class="art-title">${esc(u.title.ar)}</span></div>
      <div>
        <span class="kicker">${bookName(b)} · ${unitName(u.unit)}</span>
        ${AR() ? `<h1 class="ar-big">${esc(u.title.ar)}</h1>` : `<div class="ar-big">${esc(u.title.ar)}</div><h1>${T1(u.title)}</h1>
        ${lang === 'both' ? `<p class="muted" style="margin:4px 0 0">${esc(u.title.ur)}</p>` : ''}`}
        <div class="meta">${ring(p)}<span class="muted small">${t(`${done} of ${u.lessons.length} lessons done`, `أنهيتَ ${AR_DIGIT(done)} من ${AR_DIGIT(u.lessons.length)} دروس`)}</span></div>
        <div class="actions">
          <a class="btn" href="#/l/${id}/${nx.n}">${ic('play', 'fill')} ${p ? t('Continue', 'تابِعْ') : t('Start', 'ابْدَأْ')}: ${lessonName(nx.n)}</a>
          <a class="btn soft" href="#/words/${id}">${ic('cards')} ${t('Word cards', 'بطاقات الكلمات')}</a>
        </div>
      </div>
    </section>
    <div class="section">
      <h2>${ic('sparkle')} ${t('Learning path', 'مَسارُ التَّعَلُّم')}</h2>
      <div class="path">${u.lessons.map((l) => {
        const sc = s[`${id}-${l.n}`];
        return `<a class="card step ${sc != null ? 'done' : l === nx ? 'next' : ''}" href="#/l/${id}/${l.n}">
          <span class="node">${sc != null ? ic('check') : N(l.n)}</span>
          <span class="grow"><span class="t">${T1(l.title)}</span>${AR() ? '' : `<span class="ar muted">${esc(l.title.ar)}</span>`}</span>
          ${stars(sc)}${ic('next')}
        </a>`;
      }).join('')}
        <a class="card step test" href="#/q/${id}/test">
          <span class="node">${ic('trophy')}</span>
          <span class="grow"><span class="t">${t('Unit test', 'اختبار الوحدة')}</span><span class="muted small">${t('10 questions from all lessons', '١٠ أسئلة من كلِّ الدروس')}</span></span>
          ${stars(s[`${id}-test`])}${ic('next')}
        </a>
      </div>
    </div></div>`;
}

function lesson(id, n) {
  const u = unit(id), l = u?.lessons?.find((x) => x.n === +n);
  if (!l) return notFound();
  const b = bookOf(id), s = scores();
  ctx = { tab: 'learn', title: `${lessonName(l.n)} · ${unitName(u.unit)}`, back: `#/u/${id}`, book: b, unitId: id, lessonN: l.n };
  store.set('last', { href: `#/l/${id}/${l.n}` });
  const prev = u.lessons.find((x) => x.n === l.n - 1), next = u.lessons.find((x) => x.n === l.n + 1);
  const intro = AR() ? l.intro.ar : true;
  const parts = [intro && ['explain', 'bulb', t('Learn', 'نتعلَّم')], l.dialogue && ['dialogue', 'chat', t('Dialogue', 'الحوار')],
    l.vocab && ['words', 'cards', t('Words', 'الكلمات')], ['quizcta', 'quiz', t('Quiz', 'الاختبار')]].filter(Boolean);
  const speakers = [...new Set((l.dialogue || []).map((d) => d.who))];
  const nq = quizItems(l.quiz).length;
  setTimeout(() => { tabSpy(); const b = $('#playAll'); if (b) b.onclick = playAll; }, 0);
  return `<div class="lesson b${b}">
    <nav class="strip" aria-label="${t('Lessons in this unit', 'دروس هذه الوحدة')}">${u.lessons.map((x) => `
      <a href="#/l/${id}/${x.n}" class="${s[`${id}-${x.n}`] != null ? 'done' : ''}" ${x.n === l.n ? 'aria-current="page"' : ''} aria-label="${lessonName(x.n)}">${N(x.n)}</a>`).join('')}
      <a href="#/q/${id}/test" class="test" aria-label="${t('Unit test', 'اختبار الوحدة')}">${ic('trophy')}</a>
    </nav>

    <header class="lhero">
      <div class="lhero-text">
        <span class="kicker">${unitName(u.unit)} · ${t(`Lesson ${l.n} of ${u.lessons.length}`, `الدَّرْسُ ${AR_DIGIT(l.n)} مِن ${AR_DIGIT(u.lessons.length)}`)}</span>
        <h1>${T1(l.title)}</h1>
        ${AR() ? '' : `<div class="lhero-ar" lang="ar">${esc(l.title.ar)}</div>`}
        <div class="lhero-chips">
          ${l.dialogue ? `<span class="chip">${ic('chat')} ${t(`${l.dialogue.length} lines`, `${AR_DIGIT(l.dialogue.length)} جُمَل`)}</span>` : ''}
          ${l.vocab ? `<span class="chip">${ic('cards')} ${t(`${l.vocab.length} words`, `${AR_DIGIT(l.vocab.length)} كلمة`)}</span>` : ''}
          ${stars(s[`${id}-${l.n}`])}
        </div>
      </div>
      ${nq ? `<a class="btn gold lhero-cta" href="#/q/${id}/${l.n}">${ic('quiz')} ${t('Take the quiz', 'ابدأ الاختبار')}</a>` : ''}
    </header>

    <nav class="tabs" aria-label="${t('Lesson sections', 'أقسام الدرس')}">${parts.map(([k, i, x]) => `<button data-jump="${k}">${ic(i)}${x}</button>`).join('')}</nav>

    ${intro ? `<section class="section" id="explain">
      <h2>${ic('bulb')} ${t('What we learn', 'ماذا نتعلَّم؟')}</h2>
      ${AR() ? `<div class="explain"><div class="note ar-note"><p>${esc(l.intro.ar)}</p></div></div>` : `
      <div class="explain ${lang === 'both' ? 'two' : ''}">
        ${lang !== 'ur' ? `<div class="note en"><span class="lab"><span class="flag">EN</span> English</span><p>${mix(l.intro.en)}</p></div>` : ''}
        ${lang !== 'en' ? `<div class="note ur"><span class="lab"><span class="flag">UR</span> Roman Urdu</span><p>${mix(l.intro.ur)}</p></div>` : ''}
      </div>`}
    </section>` : ''}

    ${l.dialogue ? `<section class="section" id="dialogue">
      <h2>${ic('chat')} ${t('Dialogue', 'الحوار')}
        <button class="btn playall" id="playAll" aria-pressed="false">${playAllLabel(false)}</button></h2>
      <div class="cast">${speakers.map((w, k) => `<span class="who s${k % 2}" lang="ar">${esc(w)}</span>`).join('<span class="muted small">·</span>')}</div>
      <ol class="dlg">${l.dialogue.map((d) => `
        <li class="dl s${speakers.indexOf(d.who) % 2}">
          <span class="who s${speakers.indexOf(d.who) % 2}" lang="ar">${esc(d.who)}</span>
          <div class="dl-body">
            <p class="ar">${esc(d.ar)}</p>
            <div class="dl-foot">
              ${sayBtn(d.ar, d.au)}
              <div class="dl-text">${trLine(d.tr)}<div class="trans">${T(d)}</div></div>
            </div>
          </div>
        </li>`).join('')}
      </ol>
    </section>` : ''}

    ${l.vocab ? `<section class="section" id="words">
      <h2>${ic('cards')} ${t('New words', 'كلمات جديدة')} <span class="chip">${N(l.vocab.length)}</span></h2>
      <div class="tiles">${l.vocab.map(tile).join('')}</div>
    </section>` : ''}

    <section class="section" id="quizcta">
      <div class="card quizcta">
        <span class="medal-sm">${ic('trophy')}</span>
        <div class="grow"><h2>${t('Ready for the quiz?', 'هل أنت مستعدٌّ للاختبار؟')}</h2><p class="muted" style="margin:4px 0 0">${
          nq ? t(`${nq} questions · win up to 3 stars`, `${AR_DIGIT(nq)} أسئلة · اربح حتى ٣ نجوم`) : t('No questions yet', 'لا توجد أسئلة بعد')} ${stars(s[`${id}-${l.n}`]) || ''}</p></div>
        ${nq ? `<a class="btn gold" href="#/q/${id}/${l.n}">${t('Start quiz', 'ابدأ الاختبار')} ${ic('next')}</a>` : ''}
      </div>
    </section>

    <nav class="pager" aria-label="${t('Previous and next lesson', 'الدرس السابق والتالي')}">
      ${prev ? `<a class="card" href="#/l/${id}/${prev.n}"><span class="lab">${ic('back')} ${t('Previous', 'السابق')}</span><span class="t">${T1(prev.title)}</span></a>` : '<span></span>'}
      ${next ? `<a class="card nx" href="#/l/${id}/${next.n}"><span class="lab">${t('Next', 'التالي')} ${ic('next')}</span><span class="t">${T1(next.title)}</span></a>`
        : `<a class="card nx" href="#/q/${id}/test"><span class="lab">${t('Finish', 'النهاية')} ${ic('next')}</span><span class="t">${t('Unit test', 'اختبار الوحدة')}</span></a>`}
    </nav>
  </div>`;
}

// Word tile: Arabic on top, meaning below (none in Arabic-only mode), speaker in the bottom corner.
const tile = (v) => `
  <div class="tile" tabindex="0">
    <p class="ar">${esc(v.ar)}</p>
    ${AR() || !showTr ? '' : `<p class="tr">${esc(v.tr)}</p>`}
    ${AR() ? '' : `<div class="mean trans">${T(v)}</div>`}
    <div class="tile-foot">${sayBtn(v.ar, v.au)}</div>
  </div>`;

// Sticky section tabs: jump on tap, highlight the section in view.
function tabSpy() {
  const btns = $$('.tabs [data-jump]');
  btns.forEach((b) => (b.onclick = () => $('#' + b.dataset.jump)?.scrollIntoView({ behavior: 'smooth', block: 'start' })));
  const io = new IntersectionObserver((es) => es.forEach((e) => {
    if (e.isIntersecting) btns.forEach((b) => b.classList.toggle('on', b.dataset.jump === e.target.id));
  }), { rootMargin: '-40% 0px -55% 0px' });
  btns.forEach((b) => { const el = $('#' + b.dataset.jump); if (el) io.observe(el); });
  btns[0]?.classList.add('on');
}

function quiz(id, n) {
  const u = unit(id);
  if (!u?.lessons) return notFound();
  const isTest = n === 'test', l = u.lessons.find((x) => x.n === +n);
  if (!isTest && !l) return notFound();
  const b = bookOf(id);
  ctx = { tab: 'learn', title: isTest ? t(`Unit ${u.unit} test`, `اختبار ${unitName(u.unit)}`) : t(`Lesson ${n} quiz`, `اختبار ${lessonName(n)}`),
    back: isTest ? `#/u/${id}` : `#/l/${id}/${n}`, book: b, unitId: id, lessonN: isTest ? 'test' : +n };
  const qs = isTest ? shuffle(quizItems(u.lessons.flatMap((x) => x.quiz))).slice(0, 10) : shuffle(quizItems(l.quiz));
  if (!qs.length) return `<div class="empty">${ic('quiz')}<h1>${t('No questions yet', 'لا توجد أسئلة بعد')}</h1><p><a class="btn" href="${ctx.back}">${t('Back', 'رجوع')}</a></p></div>`;
  let i = 0, right = 0, locked = false;
  const key = `${id}-${n}`, next = !isTest && u.lessons.find((x) => x.n === +n + 1);

  const draw = () => {
    const box = $('#quiz');
    if (!box) return;
    if (i === qs.length) {
      const pct = Math.round((right / qs.length) * 100);
      saveScore(key, pct);
      const colors = ['#0e7c6b', '#f0b84a', '#e0573f', '#2f6fdb', '#7c4ddb'];
      box.innerHTML = `<div class="card result">
        ${pct >= 60 ? `<div class="confetti" aria-hidden="true">${Array.from({ length: 36 }, (_, k) => `<i style="left:${Math.random() * 100}%;background:${colors[k % 5]};animation-delay:${Math.random() * .6}s"></i>`).join('')}</div>` : ''}
        <div class="medal">${ic(pct >= 60 ? 'trophy' : 'again')}</div>
        <h1>${pct >= 90 ? t('Mashallah! Perfect!', 'ما شاءَ اللهُ! مُمْتاز!') : pct >= 60 ? t('Shabash! Well done!', 'أَحْسَنْتَ! عَمَلٌ رائِع!') : t('Good try! Koshish jaari rakhein', 'مُحاوَلَةٌ جَيِّدَة! واصِلِ المُحاوَلَة')}</h1>
        <p class="muted">${t(`${right} of ${qs.length} correct`, `${AR_DIGIT(right)} من ${AR_DIGIT(qs.length)} إجابات صحيحة`)}</p>
        ${stars(pct)}
        <div class="actions">
          <button class="btn soft" id="again">${ic('again')} ${t('Try again', 'حاوِلْ مَرَّةً أُخْرى')}</button>
          ${isTest ? `<a class="btn" href="#/u/${id}">${t('Back to unit', 'العودة إلى الوحدة')}</a>`
            : next ? `<a class="btn" href="#/l/${id}/${next.n}">${t('Next lesson', 'الدرس التالي')} ${ic('next')}</a>` : `<a class="btn gold" href="#/q/${id}/test">${ic('trophy')} ${t('Unit test', 'اختبار الوحدة')}</a>`}
        </div></div>`;
      $('#again').onclick = () => route(true);
      return;
    }
    const q = qs[i], order = shuffle(q.opts.map((_, k) => k));
    locked = false;
    box.innerHTML = `
      <div class="top-row">
        <a class="iconbtn" href="${ctx.back}" aria-label="${t('Leave quiz', 'الخروج من الاختبار')}">${ic('x')}</a>
        <div class="bar" role="progressbar" aria-valuenow="${i}" aria-valuemax="${qs.length}"><i style="width:${(i / qs.length) * 100}%"></i></div>
        <span class="chip c">${N(i + 1)}/${N(qs.length)}</span>
      </div>
      <div class="card qcard">
        <span class="kicker">${t(`Question ${i + 1}`, `السُّؤالُ ${AR_DIGIT(i + 1)}`)}</span>
        <p class="q">${AR() ? esc(q.text) : mix(q.text)}</p>
        ${q.text2 ? `<p class="q2">${mix(q.text2)}</p>` : ''}
        <div class="opts">${order.map((k, j) => `<button class="opt" data-k="${k}"><span class="key">${N(j + 1)}</span><span class="${hasArabic(q.opts[k]) ? 'ar' : ''}">${esc(q.opts[k])}</span></button>`).join('')}</div>
        <div class="feedback" aria-live="polite"></div>
      </div>`;
    $$('.opt', box).forEach((btn) => (btn.onclick = () => pick(btn, q)));
  };
  const pick = (btn, q) => {
    if (locked) return;
    locked = true;
    const ok = +btn.dataset.k === q.answer, box = $('#quiz');
    if (ok) right++;
    $$('.opt', box).forEach((o) => { o.disabled = true; if (+o.dataset.k === q.answer) o.classList.add('ok'); });
    if (!ok) btn.classList.add('bad');
    const fb = $('.feedback', box);
    fb.className = 'feedback ' + (ok ? 'ok' : 'bad');
    fb.innerHTML = ok ? `${ic('check')} ${t('Correct! Bilkul sahi!', 'إجابةٌ صحيحة! أحسنت!')}` : `${ic('x')} ${t('Not quite. The green answer is right.', 'ليست صحيحة. الإجابة الخضراء هي الصحيحة.')}`;
    setTimeout(() => { i++; draw(); }, ok ? 1000 : 2000);
  };
  quiz.key = (e) => { const o = $$('#quiz .opt')[+e.key - 1]; if (o && !o.disabled) o.click(); };
  setTimeout(draw, 0);
  return `<div class="quiz b${b}" id="quiz"></div>`;
}

// #/words → current book; #/words/b2 → one book; #/words/b2u05 → one unit. Tiles render in pages of 120 so phones stay fast.
const PAGE = 120;
function words(id) {
  const u = id && /u\d+$/.test(id) ? unit(id) : null;
  const bn = u ? bookOf(u.id) : +(id || '').replace('b', '') || store.get('book', 1);
  const b = BOOKS.find((x) => x.n === bn) || BOOKS[0];
  const key = u ? u.id : `b${b.n}`;
  if (words.key !== key) { words.key = key; words.limit = PAGE; words.order = null; }
  ctx = { tab: 'words', title: t('Word cards', 'بطاقات الكلمات'), back: u ? `#/u/${u.id}` : '', book: b.n, unitId: u?.id };
  let list = allVocab().filter((v) => (u ? v.unit.id === u.id : bookOf(v.unit.id) === b.n));
  if (words.shuffled) { words.order ||= shuffle(list.map((_, i) => i)); list = words.order.map((i) => list[i]); }
  const total = list.length;
  list = list.slice(0, words.limit);
  const hide = words.hide && !AR();
  setTimeout(() => { const m = $('#more'); if (m) m.onclick = () => { words.limit += PAGE; route(true); }; }, 0);
  return `<div class="pagehead"><div><span class="kicker">${t('Practice', 'تَدْريب')}</span><h1>${t('Word cards', 'بطاقات الكلمات')}</h1>
      <p class="muted" style="margin:4px 0 0">${t(`${total} words in ${u ? `Unit ${u.unit}` : `Book ${b.n}`}.`, `${AR_DIGIT(total)} كلمة في ${u ? unitName(u.unit) : BOOK_AR[b.n]}.`)} ${
        hide ? 'Say the meaning, then tap a tile to check.' : t('Tap the speaker to hear each word.', 'اضغط على السمّاعة لتسمع كلَّ كلمة.')}</p></div>
      <div class="actions-row">
        ${AR() ? '' : `<button class="btn soft" id="hideBtn" aria-pressed="${!!words.hide}">${ic(words.hide ? 'bulb' : 'quiz')} ${words.hide ? 'Show meanings' : 'Hide meanings'}</button>`}
        <button class="btn soft" id="shuf">${ic('shuffle')} ${words.shuffled ? t('In order', 'بالترتيب') : t('Shuffle', 'ترتيب عشوائي')}</button>
      </div></div>
    <nav class="seg" aria-label="${t('Choose a book', 'اختر كتابًا')}">${BOOKS.map((x) => `<a href="#/words/b${x.n}" ${x.n === b.n ? 'aria-current="page"' : ''}>${bookName(x.n)}</a>`).join('')}</nav>
    <nav class="filters" aria-label="${t('Choose a unit', 'اختر وحدة')}">
      <a href="#/words/b${b.n}" ${!u ? 'aria-current="page"' : ''}>${t('All units', 'كلُّ الوحدات')}</a>
      ${b.units.filter((x) => x.lessons).map((x) => `<a href="#/words/${x.id}" ${u?.id === x.id ? 'aria-current="page"' : ''}>${unitName(x.unit)}</a>`).join('')}
    </nav>
    <div class="tiles ${hide ? 'hide' : ''}">${list.map(tile).join('')}</div>
    ${total > list.length ? `<div style="text-align:center;margin-top:20px"><button class="btn soft" id="more">${t(`Show more (${total - list.length} left)`, `عرض المزيد (${AR_DIGIT(total - list.length)} متبقية)`)}</button></div>` : ''}`;
}

// ---- search: ignores Arabic vowel marks and alef/ya/ta-marbuta spelling differences ----
const norm = (s) => String(s || '').toLowerCase()
  .replace(/[ً-ٰٟـ]/g, '')
  .replace(/[أإآٱ]/g, 'ا').replace(/ى/g, 'ي').replace(/ة/g, 'ه')
  .replace(/[ʿ'’ḥṣṭḍẓāīū]/g, (c) => ({ 'ʿ': '', "'": '', '’': '', ḥ: 'h', ṣ: 's', ṭ: 't', ḍ: 'd', ẓ: 'z', ā: 'a', ī: 'i', ū: 'u' }[c]));
let INDEX;
function buildIndex() {
  INDEX = [];
  const where = (b, u, l) => `${bookName(b)} · ${unitName(u)}${l ? ` · ${lessonName(l)}` : ''}`;
  const KIND = { unit: t('Unit', 'وحدة'), lesson: t('Lesson', 'درس'), word: t('Word', 'كلمة'), line: t('Sentence', 'جملة') };
  for (const b of BOOKS) for (const u of b.units) {
    INDEX.push({ text: [u.title.ar, u.title.en, u.title.ur], href: u.lessons ? `#/u/${u.id}` : `#/b/${b.n}`, ar: u.title.ar, label: AR() ? '' : u.title.en || '', where: where(b.n, u.unit), kind: KIND.unit });
    for (const l of u.lessons || []) {
      const w = where(b.n, u.unit, l.n), href = `#/l/${u.id}/${l.n}`;
      INDEX.push({ text: [l.title.ar, l.title.en, l.title.ur], href, ar: l.title.ar, label: AR() ? '' : l.title.en, where: w, kind: KIND.lesson });
      for (const v of l.vocab || []) INDEX.push({ text: [v.ar, v.tr, v.en, v.ur], href, ar: v.ar, label: AR() ? '' : `${v.en} · ${v.ur}`, where: w, kind: KIND.word });
      for (const v of l.dialogue || []) INDEX.push({ text: [v.ar, v.tr, v.en, v.ur], href, ar: v.ar, label: AR() ? '' : v.en, where: w, kind: KIND.line });
    }
  }
  INDEX.forEach((x) => (x.n = norm(x.text.join(' | '))));
}
function results(q) {
  if (!INDEX) buildIndex();
  const nq = norm(q.trim());
  if (!nq) return `<div class="empty">${ic('search')}<p>${t('Try <b>teacher</b>, <b>bhai</b> or <span lang="ar">اسم</span>', 'جرِّبْ: <b>اسم</b>، <b>بيت</b>، <b>مدرسة</b>')}</p></div>`;
  const seen = new Set();
  const hits = INDEX.filter((x) => x.n.includes(nq) && !seen.has(x.ar + x.href) && seen.add(x.ar + x.href)).slice(0, 80);
  return `<p class="muted small">${t(`${hits.length} result${hits.length === 1 ? '' : 's'}`, `${AR_DIGIT(hits.length)} نتيجة`)}</p>` + (hits.map((h) => `
    <a class="card hit" href="${h.href}">
      <span class="ar">${esc(h.ar)}</span>
      <span class="grow">${h.label ? `<b>${esc(h.label)}</b><br>` : ''}<span class="muted small">${h.kind} · ${h.where}</span></span>${ic('next')}
    </a>`).join('') || `<div class="empty">${ic('search')}<p>${t(`No matches for “${esc(q)}”.`, `لا توجد نتائج لـِ «${esc(q)}».`)}</p></div>`);
}
function search(q) {
  ctx = { tab: 'search', title: t('Search', 'بحث'), back: '' };
  $('#q').value = q;
  setTimeout(() => {
    const inp = $('#sq');
    inp.oninput = () => { $('#q').value = inp.value; runSearch(inp.value); };
    if (matchMedia('(max-width: 1023px)').matches && !q) inp.focus();
  }, 0);
  return `<h1>${t('Search', 'بحث')}</h1>
    <input id="sq" class="page-search-input" type="search" value="${esc(q)}" placeholder="${t('Arabic, English or Roman Urdu…', 'ابحث عن كلمة أو درس…')}" aria-label="${t('Search', 'بحث')}" autocomplete="off">
    <div class="hits" id="hits">${results(q)}</div>`;
}
function runSearch(q) {
  history.replaceState(null, '', '#/search/' + encodeURIComponent(q));
  const h = $('#hits'); if (h) h.innerHTML = results(q);
}
$('#q').oninput = (e) => {
  if (!location.hash.startsWith('#/search')) location.hash = '#/search/' + encodeURIComponent(e.target.value);
  else { const s = $('#sq'); if (s) s.value = e.target.value; runSearch(e.target.value); }
};

function notFound() {
  ctx = { tab: 'home', title: t('Not found', 'غير موجود'), back: '#/' };
  return `<div class="empty">${ic('search')}<h1>${t('Page not found', 'الصفحة غير موجودة')}</h1><p><a class="btn" href="#/">${t('Go home', 'العودة إلى الرئيسية')}</a></p></div>`;
}

// ---- router ----
function route(keepScroll) {
  const p = location.hash.slice(2).split('/');
  if ('speechSynthesis' in window) speechSynthesis.cancel();
  stopAll(); if (player) { player.pause(); player = null; }  // leaving the page stops any audio
  quiz.key = null;
  const y = scrollY;
  $('#app').innerHTML =
    p[0] === 'b' ? book(p[1]) :
    p[0] === 'u' ? unitPage(p[1]) :
    p[0] === 'l' ? lesson(p[1], p[2]) :
    p[0] === 'q' ? quiz(p[1], p[2]) :
    p[0] === 'words' ? words(p[1]) :
    p[0] === 'search' ? search(decodeURIComponent(p.slice(1).join('/'))) :
    home();
  chrome();
  $('#shuf') && ($('#shuf').onclick = () => { words.shuffled = !words.shuffled; words.order = null; route(true); });
  $('#hideBtn') && ($('#hideBtn').onclick = () => { words.hide = !words.hide; route(true); });
  document.title = (ctx.title ? ctx.title + ' · ' : '') + t('Learn Arabic', 'تعلَّمِ العربية');
  window.scrollTo(0, keepScroll === true ? y : 0);
}
document.addEventListener('keydown', (e) => {
  if (quiz.key && /^[1-4]$/.test(e.key) && !e.target.matches('input')) quiz.key(e);
});
window.onhashchange = () => route();
applyTheme(); applyLang();
route();
