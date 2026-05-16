using FluentValidation;
using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using LMS.Domain.Entities;
using MediatR;

namespace LMS.Application.Teacher.Commands;

public record CreateCourseCommand(
    string Title,
    string Description,
    string Category,
    string? ThumbnailUrl,
    int? MaxStudents
) : IRequest<CreatedCourseDto>;

public class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title can be at most 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(4000).WithMessage("Description can be at most 4000 characters.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(80).WithMessage("Category can be at most 80 characters.");

        RuleFor(x => x.ThumbnailUrl)
            .MaximumLength(500)
            .Must(BeAbsoluteUrlOrEmpty)
                .WithMessage("Thumbnail URL must be a valid http(s) URL when provided.");

        RuleFor(x => x.MaxStudents)
            .Must(v => v is null || v > 0)
                .WithMessage("Max students must be at least 1 (or leave blank for unlimited).");
    }

    private static bool BeAbsoluteUrlOrEmpty(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var u)
            && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
    }
}

public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, CreatedCourseDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateCourseCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CreatedCourseDto> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();
        var now = DateTime.UtcNow;

        // Stamp tenancy from the JWT so the row is immediately scoped to the
        // teacher's org+branch. OrgAdmin moderation queries filter on these
        // columns; without the stamp the course would be invisible to the
        // teacher's own OrgAdmin until a transfer happened.
        var organizationId = _currentUser.GetOrganizationId();
        var branchId = _currentUser.GetBranchId();

        var course = new Course
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            OrganizationId = organizationId,
            BranchId = branchId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            ThumbnailUrl = string.IsNullOrWhiteSpace(request.ThumbnailUrl) ? null : request.ThumbnailUrl.Trim(),
            MaxStudents = request.MaxStudents,
            IsPublished = false, // courses always start as drafts; teacher publishes once content is ready
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.Courses.Add(course);
        await _db.SaveChangesAsync(cancellationToken);

        return new CreatedCourseDto
        {
            CourseId = course.Id,
            Title = course.Title,
            Description = course.Description,
            Category = course.Category,
            ThumbnailUrl = course.ThumbnailUrl,
            MaxStudents = course.MaxStudents,
            IsPublished = course.IsPublished,
            CreatedAt = course.CreatedAt,
        };
    }
}
