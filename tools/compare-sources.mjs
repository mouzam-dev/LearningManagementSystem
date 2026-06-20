// Rigorously compare live Sunnah.com vs fawazahmed0 for the canonical collections:
// per-book counts + per-hadith Arabic/English text match rate (sampled books).
import { readFileSync } from 'node:fs';

const KEY = readFileSync('.claude/apikey.txt', 'utf8').trim();
const LIVE = 'https://api.sunnah.com/v1';
const CDN = 'https://cdn.jsdelivr.net/gh/fawazahmed0/hadith-api@1';
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function live(p, tries = 5) {
  for (let i = 0; i < tries; i++) {
    const r = await fetch(`${LIVE}${p}`, { headers: { 'X-API-Key': KEY } });
    if (r.ok) return r.json();
    await sleep(1200 * (i + 1));
  }
  return null;
}
const cdn = (p) => fetch(`${CDN}${p}`).then((r) => (r.ok ? r.json() : null)).catch(() => null);

// --- normalizers ---
const stripTags = (s) => (s || '').replace(/<[^>]+>/g, ' ').replace(/\[[^\]]*\]/g, ' ');
const normEn = (s) => stripTags(s).replace(/\s+/g, ' ').trim();
const looseEn = (s) => normEn(s).toLowerCase().replace(/[^a-z0-9]/g, '');
const TASHKEEL = /[ؐ-ًؚ-ٰٟۖ-ۜ۟-۪ۨ-ۭـ]/g;
const normAr = (s) => stripTags(s).replace(/\s+/g, ' ').trim();
const looseAr = (s) => normAr(s).replace(TASHKEEL, '').replace(/[^ء-يٱ]/g, '');

const COLLECTIONS = ['bukhari', 'muslim', 'nasai', 'abudawud', 'tirmidhi', 'ibnmajah', 'malik'];

for (const c of COLLECTIONS) {
  console.log(`\n========== ${c} ==========`);

  // fawaz full editions -> map by hadithnumber
  const fe = await cdn(`/editions/eng-${c}.json`);
  const fa = await cdn(`/editions/ara-${c}.json`);
  if (!fe || !fa) { console.log('  fawaz edition missing'); continue; }
  const fEn = new Map(fe.hadiths.map((h) => [String(h.hadithnumber), h.text]));
  const fAr = new Map(fa.hadiths.map((h) => [String(h.hadithnumber), h.text]));
  console.log(`  fawaz total: ${fe.hadiths.length}`);

  // sunnah books
  const books = (await live(`/collections/${c}/books?limit=300`))?.data?.filter((b) => b.numberOfHadith > 0) ?? [];
  if (books.length === 0) { console.log('  sunnah.com exposes 0 books -> CANNOT serve this collection via API'); continue; }
  const sunnahTotal = books.reduce((a, b) => a + b.numberOfHadith, 0);
  console.log(`  sunnah books: ${books.length}, sunnah total (sum of book counts): ${sunnahTotal}`);

  // sample ~5 books spread across
  const idxs = [...new Set([0, Math.floor(books.length / 4), Math.floor(books.length / 2), Math.floor((3 * books.length) / 4), books.length - 1])];
  let matched = 0, enExact = 0, enLoose = 0, arExact = 0, arLoose = 0, noCounterpart = 0;
  const examples = [];

  for (const bi of idxs) {
    const bk = books[bi];
    const page = (await live(`/collections/${c}/books/${bk.bookNumber}/hadiths?limit=100`))?.data ?? [];
    for (const h of page) {
      const num = String(h.hadithNumber);
      const sEn = normEn(h.hadith.find((x) => x.lang === 'en')?.body);
      const sAr = normAr(h.hadith.find((x) => x.lang === 'ar')?.body);
      if (!fEn.has(num) && !fAr.has(num)) { noCounterpart++; continue; }
      matched++;
      const feEn = normEn(fEn.get(num)); const faAr = normAr(fAr.get(num));
      const eE = sEn === feEn, eL = looseEn(sEn) === looseEn(feEn);
      const aE = sAr === faAr, aL = looseAr(sAr) === looseAr(faAr);
      if (eE) enExact++; if (eL) enLoose++; if (aE) arExact++; if (aL) arLoose++;
      if ((!eL || !aL) && examples.length < 2) {
        examples.push({ num, en_loose_match: eL, ar_loose_match: aL,
          sEn: sEn.slice(0, 90), fEn: feEn.slice(0, 90) });
      }
    }
    await sleep(500);
  }

  const pct = (n) => matched ? ((100 * n) / matched).toFixed(1) + '%' : 'n/a';
  console.log(`  sampled matched-by-number: ${matched} (no counterpart: ${noCounterpart})`);
  console.log(`  EN  exact: ${pct(enExact)}   loose(letters only): ${pct(enLoose)}`);
  console.log(`  AR  exact: ${pct(arExact)}   loose(no tashkeel):  ${pct(arLoose)}`);
  for (const e of examples) {
    console.log(`   ~ #${e.num} enLoose=${e.en_loose_match} arLoose=${e.ar_loose_match}`);
    console.log(`     sunnah: ${e.sEn}`);
    console.log(`     fawaz : ${e.fEn}`);
  }
}
