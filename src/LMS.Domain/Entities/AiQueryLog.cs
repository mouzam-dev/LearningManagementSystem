namespace LMS.Domain.Entities;

/// <summary>One AI answer/notes request: enforces the per-user daily limit and records token use.</summary>
public class AiQueryLog
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Mode { get; set; } = string.Empty;      // "answer" | "notes"
    public string Question { get; set; } = string.Empty;
    public int PassageCount { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public bool Succeeded { get; set; }
    public DateTime CreatedAt { get; set; }
}
