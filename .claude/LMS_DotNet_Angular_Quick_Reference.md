# ⚡ .NET Core + Angular 17 - Quick Reference Guide
## Prompts, Code Snippets, and Checklists

---

## QUICK START COMMANDS

### Backend Setup (.NET Core)

```bash
# Create solution structure
dotnet new sln --name LMS
dotnet new classlib --name LMS.Domain --output src/LMS.Domain
dotnet new classlib --name LMS.Application --output src/LMS.Application
dotnet new classlib --name LMS.Infrastructure --output src/LMS.Infrastructure
dotnet new webapi --name LMS.WebAPI --output src/LMS.WebAPI
dotnet new xunit --name LMS.Tests --output tests/LMS.Tests

# Add projects to solution
dotnet sln add src/*/LMS.*.csproj tests/LMS.Tests/LMS.Tests.csproj

# Add NuGet packages
cd src/LMS.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.EntityFrameworkCore.Design

cd ../LMS.WebAPI
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package BCrypt.Net-Next
dotnet add package FluentValidation
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console

# Restore and build
dotnet restore
dotnet build

# Create database migration
cd src/LMS.Infrastructure
dotnet ef migrations add InitialCreate -p . -s ../LMS.WebAPI
dotnet ef database update -p . -s ../LMS.WebAPI

# Run API
cd ../LMS.WebAPI
dotnet run
```

### Frontend Setup (Angular 17)

```bash
# Create Angular workspace
ng new lms-angular --package-manager=npm --skip-git

# Navigate to project
cd lms-angular

# Add Angular Material (optional)
ng add @angular/material

# Add Tailwind CSS
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init -p

# Generate feature modules
ng generate module auth --routing
ng generate module student --routing
ng generate module teacher --routing
ng generate module admin --routing

# Generate components
ng generate component auth/pages/login
ng generate component student/pages/dashboard
ng generate component student/pages/course-catalog

# Run dev server
ng serve
```

---

## CLAUDE CODE PROMPTS FOR .NET + ANGULAR

### Backend Feature Development

**Generic Prompt Template**
```
Create a complete .NET Core feature for [Feature Name] with:

BACKEND:
- Entity classes in LMS.Domain
- DbContext configuration with Fluent API
- EF Core migration
- DTOs in LMS.Application
- MediatR handler (CQRS pattern)
- API Controller endpoint
- Validation with FluentValidation
- Unit tests with xUnit/Moq

FRONTEND:
- Angular service with HttpClient
- Typed responses (interfaces)
- Error handling

Use:
- .NET 8.0 with Entity Framework Core 8.0
- SQL Server database
- JWT authentication
- Async/await patterns
- Dependency injection
- Repository pattern
```

### Specific Feature Prompts

**User Registration Feature**
```
Create user registration feature for .NET Core + Angular:

BACKEND (.NET Core):
- User entity with Id, Email, FirstName, LastName, PasswordHash, Role, CreatedAt
- RegisterRequest DTO with validation rules
- AuthService with RegisterAsync method
- Hash password with BCrypt (cost 12)
- Check email uniqueness
- AuthController.Register endpoint
- Return JWT tokens
- FluentValidator for RegisterRequest

FRONTEND (Angular):
- RegisterComponent with reactive forms
- First name, last name, email, password, confirm password inputs
- Password strength indicator
- Form validation feedback
- API call to /api/auth/register
- Store tokens in localStorage
- Navigate to dashboard on success
- Display error messages

Ensure strong typing and error handling throughout.
```

**Course Creation Feature**
```
Create course creation feature with:

BACKEND:
- Course entity (Title, Description, Category, TeacherId, IsPublished)
- CreateCourseRequest DTO
- MediatR CreateCourseCommand handler
- CourseService for business logic
- CoursesController.Create endpoint
- Database migration
- Validation rules

FRONTEND:
- CourseFormComponent with ReactiveForm
- Title, description, category, thumbnail inputs
- Form validation with error messages
- API integration
- Loading states
- Success/error notifications

Include TypeScript interfaces for type safety.
```

**Quiz Taking Feature**
```
Create quiz attempt feature:

BACKEND:
- Quiz/Assessment entity
- Question entity with options
- QuizAttemptResponse for tracking answers
- CreateQuizAttemptHandler
- SubmitQuizHandler with auto-grading for MCQ
- Auto calculate score
- QuizzesController endpoints

FRONTEND:
- QuizAttemptComponent
- Timer component (if time-limited)
- Question display with options
- Radio buttons for MCQ
- Next/Previous navigation
- Submit button
- Results display after submission

Include proper validation and state management.
```

---

## C# CODE PATTERNS

### Entity Configuration with Fluent API

```csharp
modelBuilder.Entity<Course>(entity =>
{
    entity.HasKey(e => e.Id);
    
    entity.Property(e => e.Title)
        .IsRequired()
        .HasMaxLength(200);
    
    entity.Property(e => e.Description)
        .IsRequired();
    
    entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("GETUTCDATE()");
    
    // Relationships
    entity.HasOne(e => e.Teacher)
        .WithMany(u => u.CreatedCourses)
        .HasForeignKey(e => e.TeacherId)
        .OnDelete(DeleteBehavior.NoAction);
    
    entity.HasMany(e => e.Modules)
        .WithOne(m => m.Course)
        .HasForeignKey(m => m.CourseId)
        .OnDelete(DeleteBehavior.Cascade);
    
    // Indexes
    entity.HasIndex(e => e.Title);
    entity.HasIndex(e => new { e.TeacherId, e.CreatedAt });
});
```

### MediatR Query Handler (CQRS)

```csharp
public class GetCourseQuery : IRequest<CourseDto>
{
    public Guid CourseId { get; set; }
}

public class GetCourseQueryHandler : IRequestHandler<GetCourseQuery, CourseDto>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    
    public GetCourseQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
    
    public async Task<CourseDto> Handle(GetCourseQuery request, CancellationToken cancellationToken)
    {
        var course = await _context.Courses
            .Include(c => c.Modules)
            .Include(c => c.Teacher)
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken)
            ?? throw new NotFoundException($"Course {request.CourseId} not found");
        
        return _mapper.Map<CourseDto>(course);
    }
}
```

### JWT Token Generation

```csharp
public (string AccessToken, string RefreshToken) GenerateTokens(User user)
{
    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    
    var accessToken = new JwtSecurityToken(
        issuer: _config["Jwt:Issuer"],
        audience: _config["Jwt:Audience"],
        claims: new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        },
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: credentials
    );
    
    var handler = new JwtSecurityTokenHandler();
    return (handler.WriteToken(accessToken), "refresh-token-logic");
}
```

### FluentValidation Rule

```csharp
public class CreateCourseValidator : AbstractValidator<CreateCourseRequest>
{
    public CreateCourseValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MinimumLength(3).WithMessage("Title must be at least 3 characters")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");
        
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MinimumLength(10).WithMessage("Description must be at least 10 characters");
        
        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .Must(c => ValidCategories.Contains(c)).WithMessage("Invalid category");
    }
    
    private static readonly string[] ValidCategories = 
        { "Technology", "Business", "Design", "Science" };
}
```

### AutoMapper Configuration

```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.FullName, 
                opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
        
        CreateMap<CreateUserRequest, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());
        
        // Course mappings
        CreateMap<Course, CourseDto>()
            .ForMember(dest => dest.TeacherName,
                opt => opt.MapFrom(src => $"{src.Teacher.FirstName} {src.Teacher.LastName}"))
            .ForMember(dest => dest.ModuleCount,
                opt => opt.MapFrom(src => src.Modules.Count));
        
        CreateMap<CreateCourseRequest, Course>();
        
        ReverseMap();
    }
}
```

### Dependency Injection Configuration

```csharp
// In Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("LMS.Infrastructure")));

// Add repositories and services
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICourseService, CourseService>();

// Add MediatR
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Add Serilog
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.WriteTo.Console());

// Add Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"]
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowAngular");
app.MapControllers();
app.Run();
```

---

## ANGULAR CODE PATTERNS

### Typed HTTP Service with RxJS

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Course {
  id: string;
  title: string;
  description: string;
  category: string;
  teacherId: string;
  isPublished: boolean;
  createdAt: Date;
}

export interface CreateCourseRequest {
  title: string;
  description: string;
  category: string;
  isPublished: boolean;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

@Injectable({
  providedIn: 'root'
})
export class CourseService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5000/api/courses';
  
  getCourses(page = 1, pageSize = 10): Observable<PaginatedResponse<Course>> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    
    return this.http.get<PaginatedResponse<Course>>(this.apiUrl, { params });
  }
  
  getCourseById(id: string): Observable<Course> {
    return this.http.get<Course>(`${this.apiUrl}/${id}`);
  }
  
  createCourse(request: CreateCourseRequest): Observable<Course> {
    return this.http.post<Course>(this.apiUrl, request);
  }
  
  updateCourse(id: string, request: CreateCourseRequest): Observable<Course> {
    return this.http.put<Course>(`${this.apiUrl}/${id}`, request);
  }
  
  deleteCourse(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
```

### Standalone Component with Signals

```typescript
import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { CourseService, Course } from '../../services/course.service';

@Component({
  selector: 'app-course-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="container">
      <h1>Courses</h1>
      
      <div *ngIf="isLoading()" class="loading">Loading...</div>
      
      <div *ngIf="error()" class="error">{{ error() }}</div>
      
      <div class="course-grid">
        <div *ngFor="let course of courses()" class="course-card">
          <h3>{{ course.title }}</h3>
          <p>{{ course.description }}</p>
          <button (click)="onSelectCourse(course.id)">View</button>
        </div>
      </div>
    </div>
  `
})
export class CourseListComponent implements OnInit {
  private courseService = inject(CourseService);
  
  courses = signal<Course[]>([]);
  isLoading = signal(false);
  error = signal<string | null>(null);
  
  ngOnInit(): void {
    this.loadCourses();
  }
  
  loadCourses(): void {
    this.isLoading.set(true);
    this.error.set(null);
    
    this.courseService.getCourses().subscribe({
      next: (response) => {
        this.courses.set(response.items);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load courses');
        this.isLoading.set(false);
      }
    });
  }
  
  onSelectCourse(id: string): void {
    // Navigate to course detail
  }
}
```

### Reactive Form Component

```typescript
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CourseService } from '../../services/course.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-create-course',
  templateUrl: './create-course.component.html',
  styleUrls: ['./create-course.component.css']
})
export class CreateCourseComponent implements OnInit {
  private fb = inject(FormBuilder);
  private courseService = inject(CourseService);
  private router = inject(Router);
  
  courseForm!: FormGroup;
  submitted = false;
  loading = false;
  error: string | null = null;
  
  ngOnInit(): void {
    this.initializeForm();
  }
  
  initializeForm(): void {
    this.courseForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(3)]],
      description: ['', [Validators.required, Validators.minLength(10)]],
      category: ['', Validators.required],
      isPublished: [false]
    });
  }
  
  get title() { return this.courseForm.get('title'); }
  get description() { return this.courseForm.get('description'); }
  get category() { return this.courseForm.get('category'); }
  
  onSubmit(): void {
    this.submitted = true;
    
    if (this.courseForm.invalid) return;
    
    this.loading = true;
    this.error = null;
    
    this.courseService.createCourse(this.courseForm.value).subscribe({
      next: (course) => {
        this.router.navigate(['/teacher/courses', course.id]);
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to create course';
        this.loading = false;
      }
    });
  }
}
```

---

## DATABASE SCHEMA CHECKLIST

- [ ] Users (Id, Email, PasswordHash, FirstName, LastName, Role, CreatedAt)
- [ ] Courses (Id, Title, Description, Category, TeacherId, IsPublished)
- [ ] Modules (Id, CourseId, Title, Order)
- [ ] Lessons (Id, ModuleId, Title, Type, Content, Duration, Order)
- [ ] Enrollments (Id, UserId, CourseId, EnrolledAt, ProgressPercentage)
- [ ] LessonProgress (Id, UserId, LessonId, CompletedAt, WatchTimeSeconds)
- [ ] Assessments (Id, CourseId, Title, Type, TimeLimit, PassingScore)
- [ ] Questions (Id, AssessmentId, QuestionText, Type, Options, CorrectAnswer)
- [ ] Submissions (Id, UserId, AssessmentId, Answers, Score, SubmittedAt)
- [ ] Certificates (Id, UserId, CourseId, VerifyCode, IssuedAt)
- [ ] Notifications (Id, UserId, Type, Title, Message, CreatedAt)
- [ ] Audit Logs (Id, UserId, Action, Entity, Changes, CreatedAt)

---

## TESTING CHECKLIST

### Unit Tests (.NET with xUnit)

- [ ] Service logic tests with mocked DbContext
- [ ] Validator tests for DTOs
- [ ] Handler tests for MediatR queries/commands
- [ ] ≥80% code coverage

### Component Tests (Angular with Jasmine)

- [ ] Component initialization
- [ ] Form validation
- [ ] Service calls
- [ ] Event handlers

### E2E Tests (Cypress)

- [ ] Registration flow
- [ ] Login flow
- [ ] Course creation
- [ ] Quiz taking
- [ ] Grade viewing

---

## DEPLOYMENT CHECKLIST

### Backend (.NET Core)

- [ ] Build in Release mode: `dotnet build -c Release`
- [ ] Run migrations: `dotnet ef database update`
- [ ] Set environment variables
- [ ] Test API endpoints with Postman
- [ ] Run security scan
- [ ] Check appsettings.json (no secrets)
- [ ] Enable HTTPS
- [ ] Setup logging with Serilog
- [ ] Test database backup/restore

### Frontend (Angular)

- [ ] Build production bundle: `ng build --configuration production`
- [ ] Test production build locally: `ng serve --configuration production`
- [ ] Optimize images
- [ ] Setup PWA (if needed)
- [ ] Test on multiple browsers
- [ ] Check Lighthouse score ≥80
- [ ] Minify CSS and JavaScript
- [ ] Setup CI/CD pipeline

### Infrastructure

- [ ] SQL Server configured with backup
- [ ] Redis cache setup
- [ ] Docker containers ready
- [ ] Environment variables configured
- [ ] SSL certificate installed
- [ ] CORS properly configured
- [ ] Rate limiting enabled
- [ ] Monitoring alerts setup

---

## PERFORMANCE OPTIMIZATION TIPS

### .NET Core

```csharp
// Use AsNoTracking for read-only queries
var courses = await _context.Courses
    .AsNoTracking()
    .ToListAsync();

// Use Select to fetch only needed columns
var courseNames = await _context.Courses
    .Select(c => c.Title)
    .ToListAsync();

// Use pagination
var courses = await _context.Courses
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// Add indexes
modelBuilder.Entity<Course>()
    .HasIndex(c => c.TeacherId);
```

### Angular

```typescript
// Use trackBy in *ngFor
<div *ngFor="let course of courses; trackBy: trackByCourseId">
  {{ course.title }}
</div>

trackByCourseId(index: number, course: Course): string {
  return course.id;
}

// Use OnPush change detection
@Component({
  selector: 'app-course-card',
  changeDetection: ChangeDetectionStrategy.OnPush
})

// Unsubscribe with takeUntilDestroyed
constructor() {
  effect(() => {
    this.courseService.courses
      .pipe(takeUntilDestroyed())
      .subscribe(courses => this.courses.set(courses));
  });
}
```

---

## COMMON ERRORS & FIXES

### .NET Core

| Error | Fix |
|-------|-----|
| "Invalid token" | Check JWT key in appsettings.json |
| "DbContext not registered" | Add `AddDbContext<>` in Program.cs |
| "Migration pending" | Run `dotnet ef database update` |
| "CORS error" | Configure CORS policy in Program.cs |
| "Connection timeout" | Check SQL Server connection string |

### Angular

| Error | Fix |
|-------|-----|
| "Cannot match any routes" | Check route definitions |
| "Module not found" | Check import statements |
| "Signals not working" | Use latest Angular version |
| "CORS error" | Backend must have CORS enabled |
| "Token undefined" | Check localStorage.getItem() |

---

## USEFUL .NET + ANGULAR RESOURCES

- **Entity Framework Docs**: https://docs.microsoft.com/en-us/ef/core/
- **ASP.NET Core Docs**: https://docs.microsoft.com/en-us/aspnet/core/
- **Angular Docs**: https://angular.io/docs
- **TypeScript**: https://www.typescriptlang.org/
- **RxJS**: https://rxjs.dev/
- **MediatR**: https://github.com/jbogard/MediatR
- **FluentValidation**: https://docs.fluentvalidation.net/
- **AutoMapper**: https://docs.automapper.org/

---

**Version**: 2.0 (.NET Edition)
**Last Updated**: May 2026
**Status**: Ready for Development

