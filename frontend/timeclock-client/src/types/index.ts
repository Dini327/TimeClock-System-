// ── Enums (mirror backend C# enums) ──────────────────────────────────────────

export type UserRole      = 'Employee' | 'Admin';
export type EventType     = 'ClockIn' | 'ClockOut' | 'AutoClose';
export type AlertSeverity = 'Info' | 'Warning' | 'Critical';

// ── Auth ─────────────────────────────────────────────────────────────────────

export interface LoginRequest {
  email:    string;
  password: string;
}

/** Mirrors LoginResponseDto */
export interface LoginResponse {
  token:    string;
  userId:   string;
  fullName: string;
  role:     UserRole;
}

/** Stored in localStorage after a successful login */
export interface StoredUser {
  userId:   string;
  fullName: string;
  role:     UserRole;
}

// ── Attendance ────────────────────────────────────────────────────────────────

/** Mirrors AttendanceLogDto */
export interface AttendanceLog {
  id:                string;
  userId:            string;
  fullName:          string;
  email:             string;
  eventType:         EventType;
  officialTimestamp: string;   // ISO 8601 with offset, e.g. "2025-06-01T08:30:00+02:00"
  timeSource:        string;
  isAutoClosed:      boolean;
}

/** Mirrors UserStatusDto */
export interface UserStatus {
  status:    'ClockedIn' | 'ClockedOut';
  lastEvent: AttendanceLog | null;
}

// ── Admin ─────────────────────────────────────────────────────────────────────

/** Mirrors SystemAlertDto */
export interface SystemAlert {
  id:           string;
  message:      string;
  severity:     AlertSeverity;
  createdAtUtc: string;
}

/** Mirrors ManualShiftCloseDto */
export interface ManualShiftClose {
  userId: string;
}
