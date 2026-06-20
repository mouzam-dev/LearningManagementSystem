export type LiveSessionStatus = 'Scheduled' | 'Live' | 'Ended' | 'Cancelled';

export interface LiveSession {
  id: string;
  courseId: string;
  courseTitle: string;
  branchId?: string | null;
  branchName?: string | null;
  hostTeacherName: string;
  title: string;
  scheduledStart: string; // ISO (UTC)
  durationMinutes: number;
  status: LiveSessionStatus;
  provider: string;
  roomName: string;
  startedAt?: string | null;
  endedAt?: string | null;
  enrolledCount: number;
}

export interface ScheduleLiveSessionBody {
  courseId: string;
  title: string;
  scheduledStart: string; // ISO (UTC)
  durationMinutes: number;
}

export interface LiveJoinInfo {
  liveSessionId: string;
  provider: string;
  roomName: string;
  courseTitle: string;
  title: string;
  displayName: string;
  status: string;
}
