using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Queries;

/// <summary>
/// Paged list of the teacher's own bank questions. Optional <paramref name="Search"/>
/// matches against question text (substring, case-insensitive); optional
/// <paramref name="Tag"/> is an exact match.
/// </summary>
public record GetBankQuestionsQuery(
    string? Search,
    string? Tag,
    int Page,
    int PageSize
) : IRequest<PagedResult<BankQuestionListItemDto>>;

public class GetBankQuestionsQueryHandler
    : IRequestHandler<GetBankQuestionsQuery, PagedResult<BankQuestionListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetBankQuestionsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<BankQuestionListItemDto>> Handle(
        GetBankQuestionsQuery request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _db.BankQuestions.AsNoTracking()
            .Where(q => q.TeacherId == teacherId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(q => EF.Functions.Like(q.QuestionText, $"%{s}%"));
        }

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            var t = request.Tag.Trim();
            query = query.Where(q => q.Tag == t);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(q => q.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new BankQuestionListItemDto
            {
                Id = q.Id,
                QuestionText = q.QuestionText,
                Type = q.Type,
                Points = q.Points,
                Tag = q.Tag,
                // Cheap option count without deserializing the JSON: count commas + 1
                // for any non-null Options string. Good enough for the list view; the
                // editor query deserializes the full array.
                OptionCount = q.Options == null
                    ? 0
                    : q.Options.Length - q.Options.Replace(",", "").Length + 1,
                CreatedAt = q.CreatedAt,
                UpdatedAt = q.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<BankQuestionListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }
}
