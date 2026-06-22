export interface QuranChapter {
  id: number;
  nameSimple: string;
  nameArabic: string;
  nameComplex: string;
  versesCount: number;
  revelationPlace: string; // 'makkah' | 'madinah'
  bismillahPre: boolean;
  translatedName: string;
}

export interface QuranVerse {
  verseNumber: number;
  verseKey: string; // e.g. "2:255"
  textUthmani: string;
  textIndopak: string;
  translation: string;
}

/** A selectable translation / tafsir resource. */
export interface ResourceOption {
  id: number;
  label: string;
  rtl?: boolean; // true for Urdu (right-to-left) resources
}

export interface ReciterOption {
  id: number;
  label: string;
}

export interface QuranSearchResult {
  verseKey: string;
  chapterId: number;
  verseNumber: number;
  textArabic: string;
}

export interface ArabicFont {
  label: string;
  family: string;
}

export const ARABIC_FONTS: ArabicFont[] = [
  { label: 'Amiri', family: "'Amiri', serif" },
  { label: 'Scheherazade', family: "'Scheherazade New', serif" },
  { label: 'Noto Naskh', family: "'Noto Naskh Arabic', serif" },
];

export interface ScriptOption {
  label: string;
  key: 'uthmani' | 'indopak';
}

export const QURAN_SCRIPTS: ScriptOption[] = [
  { label: 'Uthmani', key: 'uthmani' },
  { label: 'IndoPak', key: 'indopak' },
];

// Translations the LMS offers: Saheeh International + Al-Hilali & Khan (English) + Junagarhi (Urdu).
export const QURAN_TRANSLATIONS: ResourceOption[] = [
  { id: 20, label: 'Saheeh International (English)' },
  { id: 203, label: 'Al-Hilali & Khan (English)' },
  { id: 54, label: 'Muhammad Junagarhi (Urdu)', rtl: true },
];

// Tafsir restricted to Ibn Kathir (English + Urdu).
export const QURAN_TAFSIRS: ResourceOption[] = [
  { id: 169, label: 'Ibn Kathir (English)' },
  { id: 160, label: 'Ibn Kathir (Urdu)', rtl: true },
];

// Sensible defaults (valid Quran.com resource ids).
export const DEFAULT_TRANSLATION = 20; // Saheeh International
export const DEFAULT_RECITER = 7; // Mishary Rashid Alafasy
export const DEFAULT_TAFSIR = 169; // Ibn Kathir (Abridged), English
