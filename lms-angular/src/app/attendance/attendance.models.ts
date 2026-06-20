// Attendance module — shared TypeScript models mirroring the backend DTOs.

export type AttendanceStatus = 'Present' | 'Absent' | 'Late' | 'Excused' | 'Remote' | 'LeftEarly';
export type SessionStatus = 'Open' | 'Finalized' | 'Cancelled';

export const ATTENDANCE_STATUSES: AttendanceStatus[] = [
  'Present', 'Absent', 'Late', 'Excused', 'Remote', 'LeftEarly',
];

export interface AttendanceSession {
  id: string;
  courseId: string;
  courseTitle: string;
  branchId?: string | null;
  branchName?: string | null;
  sessionDate: string; // yyyy-MM-dd
  slot: number;
  startTime?: string | null;
  endTime?: string | null;
  topic?: string | null;
  status: SessionStatus;
  studentCount: number;
  presentCount: number;
  absentCount: number;
  lateCount: number;
  excusedCount: number;
  attendancePercent: number;
  createdAt: string;
}

export interface AttendanceRecord {
  id: string;
  studentId: string;
  studentName: string;
  studentAvatarUrl?: string | null;
  status: AttendanceStatus;
  checkInTime?: string | null;
  minutesLate?: number | null;
  remark?: string | null;
}

export interface SessionRoster {
  session: AttendanceSession;
  records: AttendanceRecord[];
}

export interface CreateSessionBody {
  courseId: string;
  sessionDate: string;
  slot: number;
  startTime?: string | null;
  endTime?: string | null;
  topic?: string | null;
}

export interface MarkInput {
  studentId: string;
  status: AttendanceStatus;
  minutesLate?: number | null;
  remark?: string | null;
}

export interface StudentAttendanceRow {
  studentId: string;
  studentName: string;
  studentAvatarUrl?: string | null;
  present: number;
  absent: number;
  late: number;
  excused: number;
  remote: number;
  leftEarly: number;
  totalCounted: number;
  percent: number;
}

export interface CourseAttendanceSummary {
  courseId: string;
  courseTitle: string;
  sessionCount: number;
  overallPercent: number;
  students: StudentAttendanceRow[];
}

export interface BranchAttendanceRow {
  branchId?: string | null;
  branchName: string;
  courseCount: number;
  sessionCount: number;
  studentCount: number;
  recordCount: number;
  attendancePercent: number;
}

export interface OrgAttendanceOverview {
  fromDate?: string | null;
  toDate?: string | null;
  overallPercent: number;
  totalSessions: number;
  totalRecords: number;
  branches: BranchAttendanceRow[];
}

export interface CourseAttendanceRow {
  courseId: string;
  courseTitle: string;
  teacherName: string;
  sessionCount: number;
  studentCount: number;
  attendancePercent: number;
}

export interface BranchAttendanceDetail {
  branchId?: string | null;
  branchName: string;
  overallPercent: number;
  courses: CourseAttendanceRow[];
}

export interface MyCourseAttendance {
  courseId: string;
  courseTitle: string;
  present: number;
  absent: number;
  late: number;
  excused: number;
  totalCounted: number;
  percent: number;
}

export interface MyAttendanceMark {
  courseId: string;
  courseTitle: string;
  sessionDate: string;
  slot: number;
  status: AttendanceStatus;
  remark?: string | null;
}

export interface MyAttendance {
  overallPercent: number;
  totalSessions: number;
  courses: MyCourseAttendance[];
  recent: MyAttendanceMark[];
}
