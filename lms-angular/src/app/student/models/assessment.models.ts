export interface AssessmentListItem {
  assessmentId: string;
  courseId: string;
  title: string;
  /** "Quiz" or "Assignment". */
  type: string;
  dueDate?: string | null;
  timeLimit?: number | null;
  passingScore: number;
  maxAttempts?: number | null;
  questionCount: number;
  attemptCount: number;
  bestScore?: number | null;
  isPassed: boolean;
}

export interface Question {
  questionId: string;
  questionText: string;
  /** "MCQ" | "TrueFalse" | "ShortAnswer" | "Essay". */
  type: string;
  options?: string[] | null;
  points: number;
  order: number;
}

export interface QuestionResult {
  questionId: string;
  questionText: string;
  studentAnswer?: string | null;
  correctAnswer?: string | null;
  isCorrect: boolean;
  pointsAwarded: number;
  pointsPossible: number;
}

export interface SubmissionResult {
  submissionId: string;
  assessmentId: string;
  assessmentType: string;
  submittedAt: string;
  gradedAt?: string | null;
  score?: number | null;
  totalPoints: number;
  scorePercentage?: number | null;
  isPassed: boolean;
  feedback?: string | null;
  questionResults?: QuestionResult[] | null;
}

export interface AssessmentDetail {
  assessmentId: string;
  courseId: string;
  courseTitle: string;
  title: string;
  type: string;
  timeLimit?: number | null;
  passingScore: number;
  totalPoints: number;
  dueDate?: string | null;
  maxAttempts?: number | null;
  attemptCount: number;
  attemptsRemaining?: number | null;
  questions: Question[];
  lastSubmission?: SubmissionResult | null;
}

export interface SubmitAssessmentBody {
  /** Map of QuestionId → student's answer. */
  answers: Record<string, string>;
}
