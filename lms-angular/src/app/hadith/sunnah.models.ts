export interface SunnahCollection {
  name: string; // "bukhari"
  titleEn: string;
  titleAr: string;
  shortIntroEn?: string | null;
  hasBooks: boolean;
  totalHadith: number;
}

export interface SunnahBook {
  bookNumber: string;
  nameEn: string;
  nameAr: string;
  numberOfHadith: number;
  hadithStartNumber: number;
  hadithEndNumber: number;
}

export interface SunnahHadith {
  collection: string;
  bookNumber: string;
  bookNameEn?: string | null;
  hadithNumber: string;
  chapterEn?: string | null;
  chapterAr?: string | null;
  bodyEn: string; // HTML
  bodyAr: string; // HTML
  grade?: string | null;
  gradeCategory?: string | null; // Sahih/Hasan/Daif/Maudu/Other
}

export interface SunnahPage<T> {
  data: T[];
  total: number;
  next?: number | null;
  previous?: number | null;
}
