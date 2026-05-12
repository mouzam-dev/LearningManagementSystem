using FluentValidation;
using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Commands;

public record UpdateCourseCommand(
    Guid CourseId,
    string Title,
    string Description,
    string Category,
    string? ThumbnailUrl,
    int? MaxStudents
) : IRequest<TeacherCourseDetailDto>;

public class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
{
    public UpdateCourseCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(80);
        RuleFor(x => x.ThumbnailUrl).MaximumLength(500)
            .Must(BeAbsoluteUrlOrEmpty)
            .WithMessage("Thumbnail URL must be a valid http(s) URL when provided.");
        RuleFor(x => x.MaxStudents).Must(v => v is null || v > 0)
            .WithMessage("Max students must be at least 1.");
    }

    private static bool BeAbsoluteUrlOrEmpty(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var u)
            && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
    }
}

public class UpdateCourseCommandHandler : IRequestHandler<UpdateCourseCommand, TeacherCourseDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public UpdateCourseCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<TeacherCourseDetailDto> Handle(UpdateCourseCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var course = await _db.Courses
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

        if (course.TeacherId != teacherId)
        {
            throw new KeyNotFoundException($"Course {request.CourseId} was not found.");
        }

        course.Title = request.Title.Trim();
        course.Description = request.Description.Trim();
        course.Category = request.Category.Trim();
        course.ThumbnailUrl = string.IsNullOrWhiteSpace(request.ThumbnailUrl) ? null : request.ThumbnailUrl.Trim();
        course.MaxStudents = request.MaxStudents;
        course.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        // Refetch via the detail query so the response shape matches the GET endpoint.
        return await _mediator.Send(new Queries.GetTeacherCourseDetailQuery(course.Id), cancellationToken);
    }
}
