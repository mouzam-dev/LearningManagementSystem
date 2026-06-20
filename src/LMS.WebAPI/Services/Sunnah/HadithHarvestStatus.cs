namespace LMS.WebAPI.Services.Sunnah;

public enum HarvestState { Idle, Running, Completed, Failed }

/// <summary>
/// Thread-safe, in-memory status for the hadith harvest. Singleton — the admin
/// page polls <see cref="Snapshot"/> while a background harvest updates it.
/// </summary>
public class HadithHarvestStatus
{
    private readonly object _lock = new();

    public HarvestState State { get; private set; } = HarvestState.Idle;
    public DateTime? StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    public string? CurrentCollection { get; private set; }
    public int CollectionsDone { get; private set; }
    public int TotalCollections { get; private set; }
    public int HadithsWritten { get; private set; }
    public string? Error { get; private set; }

    /// <summary>Atomically transitions Idle/Completed/Failed → Running. Returns false if already running.</summary>
    public bool TryBegin(int total)
    {
        lock (_lock)
        {
            if (State == HarvestState.Running) return false;
            State = HarvestState.Running;
            StartedAt = DateTime.UtcNow;
            FinishedAt = null;
            CurrentCollection = null;
            CollectionsDone = 0;
            TotalCollections = total;
            HadithsWritten = 0;
            Error = null;
            return true;
        }
    }

    public void SetCurrent(string slug) { lock (_lock) { CurrentCollection = slug; } }
    public void AddWritten(int n) { lock (_lock) { HadithsWritten += n; } }
    public void CompleteCollection() { lock (_lock) { CollectionsDone++; } }

    public void Finish(string? error)
    {
        lock (_lock)
        {
            State = error is null ? HarvestState.Completed : HarvestState.Failed;
            Error = error;
            FinishedAt = DateTime.UtcNow;
            CurrentCollection = null;
        }
    }

    public object Snapshot()
    {
        lock (_lock)
        {
            return new
            {
                state = State.ToString(),
                startedAt = StartedAt,
                finishedAt = FinishedAt,
                currentCollection = CurrentCollection,
                collectionsDone = CollectionsDone,
                totalCollections = TotalCollections,
                hadithsWritten = HadithsWritten,
                error = Error,
            };
        }
    }
}
