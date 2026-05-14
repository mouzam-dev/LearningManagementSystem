export interface TeacherDashboardSummary {
  totalCourses: number;
  publishedCourses: number;
  draftCourses: number;
  totalStudents: number;
  totalActiveStudents: number;
  completedEnrollments: number;
  pendingGradingCount: number;
  certificatesIssued: number;
}

export interface TeacherCourseSummary {
  courseId: string;
  title: string;
  category: string;
  isPublished: boolean;
  studentCount: number;
  lessonCount: number;
  updatedAt: string;
}

export interface RecentEnrollment {
  enrollmentId: string;
  courseId: string;
  courseTitle: string;
  studentName: string;
  enrolledAt: string;
}

export interface PendingGrading {
  submissionId: string;
  assessmentId: string;
  assessmentTitle: string;
  courseTitle: string;
  studentName: string;
  submittedAt: string;
}

export interface TeacherDashboard {
  summary: TeacherDashboardSummary;
  topCourses: TeacherCourseSummary[];
  recentEnrollments: RecentEnrollment[];
  pendingGrading: PendingGrading[];
}

export interface TeacherCourseListItem {
  courseId: string;
  title: string;
  description: string;
  category: string;
  thumbnailUrl?: string | null;
  maxStudents?: number | null;
  isPublished: boolean;
  moduleCount: number;
  lessonCount: number;
  assessmentCount: number;
  studentCount: number;
  averageProgress: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCourseBody {
  title: string;
  description: string;
  category: string;
  thumbnailUrl?: string | null;
  maxStudents?: number | null;
}

export interface CreatedCourse {
  courseId: string;
  title: string;
  description: string;
  category: string;
  thumbnailUrl?: string | null;
  maxStudents?: number | null;
  isPublished: boolean;
  createdAt: string;
}

// ---------------- Course detail / builder --------------------------------

export interface TeacherLesson {
  lessonId: string;
  title: string;
  /** "Video" | "Document" | "Text" | "Quiz" | "Assignment". */
  type: string;
  /** Raw JSON content payload. For "Video" lessons it's `{"videoUrl":"…"}`. */
  content?: string | null;
  duration?: number | null;
  order: number;
  isPublished: boolean;
}

export interface TeacherModule {
  moduleId: string;
  title: string;
  description?: string | null;
  order: number;
  lessons: TeacherLesson[];
}

export interface TeacherCourseDetail {
  courseId: string;
  title: string;
  description: string;
  category: string;
  thumbnailUrl?: string | null;
  maxStudents?: number | null;
  isPublished: boolean;
  createdAt: string;
  updatedAt: string;
  studentCount: number;
  assessmentCount: number;
  averageProgress: number;
  modules: TeacherModule[];
  assessments: TeacherAssessmentListItem[];
}

export interface UpdateCourseBody {
  title: string;
  description: string;
  category: string;
  thumbnailUrl?: string | null;
  maxStudents?: number | null;
}

export interface CreateModuleBody {
  title: string;
  description?: string | null;
}

export interface UpdateModuleBody {
  title: string;
  description?: string | null;
  order?: number | null;
}

export interface CreateLessonBody {
  title: string;
  type: string;
  content?: string | null;
  duration?: number | null;
  isPublished: boolean;
}

export interface UpdateLessonBody {
  title: string;
  type: string;
  content?: string | null;
  duration?: number | null;
  order?: number | null;
  isPublished: boolean;
}

// ---------------- Assessments + questions --------------------------------

export interface TeacherAssessmentListItem {
  assessmentId: string;
  courseId: string;
  title: string;
  /** "Quiz" or "Assignment". */
  type: string;
  timeLimit?: number | null;
  passingScore: number;
  dueDate?: string | null;
  maxAttempts?: number | null;
  questionCount: number;
  totalPoints: number;
  submissionCount: number;
  ungradedCount: number;
}

export interface TeacherQuestion {
  questionId: string;
  questionText: string;
  /** "MCQ" | "TrueFalse" | "ShortAnswer" | "Essay". */
  type: string;
  options?: string[] | null;
  /** Teacher view exposes the correct answer. Student-side DTO hides it. */
  correctAnswer?: string | null;
  points: number;
  order: number;
}

export interface TeacherAssessment {
  assessmentId: string;
  courseId: string;
  courseTitle: string;
  title: string;
  type: string;
  timeLimit?: number | null;
  passingScore: number;
  dueDate?: string | null;
  maxAttempts?: number | null;
  totalPoints: number;
  submissionCount: number;
  questions: TeacherQuestion[];
}

export interface CreateAssessmentBody {
  title: string;
  type: string;
  timeLimit?: number | null;
  passingScore: number;
  dueDate?: string | null;
  maxAttempts?: number | null;
}

export interface UpdateAssessmentBody {
  title: string;
  timeLimit?: number | null;
  passingScore: number;
  dueDate?: string | null;
  maxAttempts?: number | null;
}

export interface CreateQuestionBody {
  questionText: string;
  type: string;
  options?: string[] | null;
  correctAnswer?: string | null;
  points: number;
}

export interface UpdateQuestionBody {
  questionText: string;
  type: string;
  options?: string[] | null;
  correctAnswer?: string | null;
  points: number;
  order?: number | null;
}

// ---------------- Grading ------------------------------------------------

export interface GradingQueueItem {
  submissionId: string;
  assessmentId: string;
  assessmentTitle: string;
  assessmentType: string;
  courseId: string;
  courseTitle: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  submittedAt: string;
  score?: number | null;
  gradedAt?: string | null;
  totalPoints: number;
  passingScore: number;
}

export interface SubmissionAnswer {
  questionId: string;
  questionText: string;
  type: string;
  studentAnswer?: string | null;
  correctAnswer?: string | null;
  isCorrect: boolean;
  points: number;
  order: number;
}

export interface TeacherSubmissionDetail {
  submissionId: string;
  submittedAt: string;
  gradedAt?: string | null;
  score?: number | null;
  feedback?: string | null;
  assessmentId: string;
  assessmentTitle: string;
  assessmentType: string;
  passingScore: number;
  totalPoints: number;
  courseId: string;
  courseTitle: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  answers: SubmissionAnswer[];
  assignmentResponse?: string | null;
}

export interface GradeSubmissionBody {
  score: number;
  feedback?: string | null;
}

// ---------------- Per-course students + analytics -----------------------

export interface CourseStudentListItem {
  studentId: string;
  studentName: string;
  studentEmail: string;
  enrolledAt: string;
  progressPercentage: number;
  isCompleted: boolean;
  completedAt?: string | null;
  lessonsCompleted: number;
  totalLessons: number;
  submissionsCount: number;
  averageScore?: number | null;
  lastActivityAt?: string | null;
  hasCertificate: boolean;
}

export interface StudentLessonProgress {
  lessonId: string;
  lessonTitle: string;
  moduleTitle: string;
  moduleOrder: number;
  lessonOrder: number;
  isCompleted: boolean;
  watchTimeSeconds: number;
  completedAt?: string | null;
}

export interface StudentSubmission {
  submissionId: string;
  assessmentId: string;
  assessmentTitle: string;
  assessmentType: string;
  passingScore: number;
  submittedAt: string;
  gradedAt?: string | null;
  score?: number | null;
  isPassed: boolean;
}

export interface CourseStudentDetail {
  studentId: string;
  studentName: string;
  studentEmail: string;
  enrolledAt: string;
  progressPercentage: number;
  isCompleted: boolean;
  completedAt?: string | null;
  lastActivityAt?: string | null;
  courseId: string;
  courseTitle: string;
  certificateId?: string | null;
  certificateVerifyCode?: string | null;
  lessons: StudentLessonProgress[];
  submissions: StudentSubmission[];
}

export interface LessonAnalytics {
  lessonId: string;
  lessonTitle: string;
  moduleTitle: string;
  moduleOrder: number;
  lessonOrder: number;
  completedCount: number;
  completionRate: number;
}

export interface AssessmentAnalytics {
  assessmentId: string;
  assessmentTitle: string;
  assessmentType: string;
  passingScore: number;
  submissionCount: number;
  averageScore?: number | null;
  passRate: number;
  ungradedCount: number;
}

export interface StudentMini {
  studentId: string;
  studentName: string;
  studentEmail: string;
  progressPercentage: number;
  lastActivityAt?: string | null;
}

export interface CourseAnalytics {
  courseId: string;
  courseTitle: string;
  totalStudents: number;
  activeStudents: number;
  completedStudents: number;
  averageProgress: number;
  completionRate: number;
  certificatesIssued: number;
  totalLessons: number;
  totalAssessments: number;
  totalSubmissions: number;
  ungradedSubmissions: number;
  lessonStats: LessonAnalytics[];
  assessmentStats: AssessmentAnalytics[];
  topStudents: StudentMini[];
  atRiskStudents: StudentMini[];
}
