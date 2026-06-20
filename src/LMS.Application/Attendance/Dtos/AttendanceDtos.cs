namespace LMS.Application.Attendance.Dtos;

// ---------------------------------------------------------------------------
// Session + roster (teacher marking grid)
// ---------------------------------------------------------------------------

public class AttendanceSessionDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public DateOnly SessionDate { get; set; }
    public int Slot { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? Topic { get; set; }
    public string Status { get; set; } = string.Empty; // AttendanceSessionStatus name

    public int StudentCount { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public int ExcusedCount { get; set; }
    public decimal AttendancePercent { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class AttendanceRecordDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? StudentAvatarUrl { get; set; }
    public string Status { get; set; } = string.Empty; // AttendanceStatus name
    public TimeOnly? CheckInTime { get; set; }
    public int? MinutesLate { get; set; }
    public string? Remark { get; set; }
}

public class SessionRosterDto
{
    public AttendanceSessionDto Session { get; set; } = new();
    public List<AttendanceRecordDto> Records { get; set; } = new();
}

// One student's status update inside a bulk save.
public class MarkInputDto
{
    public Guid StudentId { get; set; }
    public string Status { get; set; } = "Present"; // AttendanceStatus name
    public int? MinutesLate { get; set; }
    public string? Remark { get; set; }
}

// ---------------------------------------------------------------------------
// Course-level summary (teacher analytics)
// ---------------------------------------------------------------------------

public class CourseAttendanceSummaryDto
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public decimal OverallPercent { get; set; }
    public List<StudentAttendanceRowDto> Students { get; set; } = new();
}

public class StudentAttendanceRowDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? StudentAvatarUrl { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Late { get; set; }
    public int Excused { get; set; }
    public int Remote { get; set; }
    public int LeftEarly { get; set; }
    public int TotalCounted { get; set; }
    public decimal Percent { get; set; }
}

// ---------------------------------------------------------------------------
// Org-admin branch-wise reporting
// ---------------------------------------------------------------------------

public class OrgAttendanceOverviewDto
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public decimal OverallPercent { get; set; }
    public int TotalSessions { get; set; }
    public int TotalRecords { get; set; }
    public List<BranchAttendanceRowDto> Branches { get; set; } = new();
}

public class BranchAttendanceRowDto
{
    public Guid? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int CourseCount { get; set; }
    public int SessionCount { get; set; }
    public int StudentCount { get; set; }
    public int RecordCount { get; set; }
    public decimal AttendancePercent { get; set; }
}

public class BranchAttendanceDetailDto
{
    public Guid? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public decimal OverallPercent { get; set; }
    public List<CourseAttendanceRowDto> Courses { get; set; } = new();
}

public class CourseAttendanceRowDto
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public int StudentCount { get; set; }
    public decimal AttendancePercent { get; set; }
}

// ---------------------------------------------------------------------------
// Student self-view
// ---------------------------------------------------------------------------

public class MyAttendanceDto
{
    public decimal OverallPercent { get; set; }
    public int TotalSessions { get; set; }
    public List<MyCourseAttendanceDto> Courses { get; set; } = new();
    public List<MyAttendanceMarkDto> Recent { get; set; } = new();
}

public class MyCourseAttendanceDto
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Late { get; set; }
    public int Excused { get; set; }
    public int TotalCounted { get; set; }
    public decimal Percent { get; set; }
}

public class MyAttendanceMarkDto
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public DateOnly SessionDate { get; set; }
    public int Slot { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remark { get; set; }
}
