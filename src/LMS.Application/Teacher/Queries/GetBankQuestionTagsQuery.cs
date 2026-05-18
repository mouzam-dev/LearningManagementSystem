using LMS.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Queries;

/// <summary>
/// Distinct tags currently used by the teacher, alphabetised — feeds the
/// tag-filter dropdown on the bank list page.
/// </summary>
public record GetBankQuestionTagsQuery() : IRequest<IReadOnlyList<string>>;

public class GetBankQuestionTagsQueryHandler
    : IRequestHandler<GetBankQuestionTagsQuery, IReadOnlyList<string>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetBankQuestionTagsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<string>> Handle(
        GetBankQuestionTagsQuery request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        return await _db.BankQuestions.AsNoTracking()
            .Where(q => q.TeacherId == teacherId && q.Tag != null && q.Tag != "")
            .Select(q => q.Tag!)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(cancellationToken);
    }
}
