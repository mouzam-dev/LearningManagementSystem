namespace LMS.Domain.Enums;

/// <summary>
/// Lifecycle of a live online class. Scheduled (created, not yet started) → Live
/// (teacher has started it; enrolled students can join the room) → Ended. Cancelled
/// marks a class that won't run. Stored as a string for readable rows.
/// </summary>
public enum LiveSessionStatus
{
    Scheduled = 0,
    Live = 1,
    Ended = 2,
    Cancelled = 3,
}
