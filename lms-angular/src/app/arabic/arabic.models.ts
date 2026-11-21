export interface ArabicUnitSummary {
  id: string;
  unitNumber: number;
  titleAr: string;
  titleEn: string;
  titleUr: string;
  lessonCount: number;
}

export interface ArabicBook {
  number: number;
  units: ArabicUnitSummary[];
}

export interface ArabicLine {
  speaker: string;
  ar: string;
  tr: string;
  en: string;
  ur: string;
}

export interface ArabicWord {
  ar: string;
  tr: string;
  en: string;
  ur: string;
}

export interface ArabicQuiz {
  questionAr: string;
  questionEn: string;
  questionUr: string;
  options: string[];
  optionsAr: string[] | null;
  answerIndex: number;
}

export interface ArabicLesson {
  number: number;
  titleAr: string;
  titleEn: string;
  titleUr: string;
  introAr: string;
  introEn: string;
  introUr: string;
  dialogue: ArabicLine[];
  vocab: ArabicWord[];
  quiz: ArabicQuiz[];
}

export interface ArabicUnit {
  id: string;
  bookNumber: number;
  unitNumber: number;
  titleAr: string;
  titleEn: string;
  titleUr: string;
  lessons: ArabicLesson[];
}

export interface ArabicWordCard extends ArabicWord {
  unitId: string;
  unitNumber: number;
  lessonNumber: number;
}

export interface ArabicProgress {
  unitId: string;
  lessonNumber: number;
  bestScore: number;
  attempts: number;
  updatedAt: string;
}

/** Which translation(s) to show next to the Arabic. */
export type MeaningLang = 'both' | 'en' | 'ur';

export const BOOK_AR = ['', 'الكِتابُ الأَوَّل', 'الكِتابُ الثّاني', 'الكِتابُ الثّالِث', 'الكِتابُ الرّابِع'];

/** Unit test results are stored as lesson 0. */
export const UNIT_TEST = 0;

/** 90%+ = 3 stars, 60%+ = 2, any attempt = 1, never tried = 0. */
/** One student's Learn Arabic standing, as the teacher sees it. */
export interface TeacherArabicStudent {
  studentId: string;
  name: string;
  email: string;
  lessonsDone: number;
  unitTestsDone: number;
  averageScore: number | null;
  attempts: number;
  lastActivity: string | null;
  lastBook: number | null;
  lastUnit: number | null;
  lastLesson: number | null;
}

export interface TeacherArabicProgress {
  totalLessons: number;
  totalUnits: number;
  students: { items: TeacherArabicStudent[]; page: number; pageSize: number; totalCount: number; totalPages: number };
}

export const starCount = (pct: number | undefined): number =>
  pct == null ? 0 : pct >= 90 ? 3 : pct >= 60 ? 2 : 1;

export const shuffle = <T>(a: readonly T[]): T[] =>
  a.map((v) => [Math.random(), v] as const).sort((x, y) => x[0] - y[0]).map((p) => p[1]);
