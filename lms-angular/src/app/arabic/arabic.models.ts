// Learn Arabic itself is the embedded app at /learn-arabic/ (lms-angular/public/learn-arabic).
// These are the types for the teacher's "Students' Arabic progress" page.

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
