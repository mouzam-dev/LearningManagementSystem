export interface CertificateSummary {
  certificateId: string;
  courseId: string;
  courseTitle: string;
  courseCategory?: string | null;
  verifyCode: string;
  issuedAt: string;
  completedAt?: string | null;
}

export interface Certificate {
  certificateId: string;
  verifyCode: string;
  issuedAt: string;
  courseId: string;
  courseTitle: string;
  courseDescription?: string | null;
  courseCategory?: string | null;
  studentId: string;
  studentName: string;
  instructorName: string;
  enrolledAt: string;
  completedAt?: string | null;
  totalLessons: number;
}

export interface VerifyCertificate {
  isValid: boolean;
  verifyCode: string;
  courseTitle?: string | null;
  studentName?: string | null;
  instructorName?: string | null;
  issuedAt?: string | null;
  completedAt?: string | null;
}
