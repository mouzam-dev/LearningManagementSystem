using System.Text.Json;
using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Queries;

public record GetBankQuestionQuery(Guid Id) : IRequest<BankQuestionDto>;

public class GetBankQuestionQueryHandler : IRequestHandler<GetBankQuestionQuery, BankQuestionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetBankQuestionQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BankQuestionDto> Handle(GetBankQuestionQuery request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var q = await _db.BankQuestions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TeacherId == teacherId, cancellationToken)
            ?? throw new KeyNotFoundException($"Bank question {request.Id} was not found.");

        return new BankQuestionDto
        {
            Id = q.Id,
            QuestionText = q.QuestionText,
            Type = q.Type,
            Options = ParseOptions(q.Options),
            CorrectAnswer = q.CorrectAnswer,
            Points = q.Points,
            Tag = q.Tag,
            CreatedAt = q.CreatedAt,
            UpdatedAt = q.UpdatedAt,
        };
    }

    internal static IReadOnlyList<string>? ParseOptions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<List<string>>(json); }
        catch (JsonException) { return null; }
    }
}
