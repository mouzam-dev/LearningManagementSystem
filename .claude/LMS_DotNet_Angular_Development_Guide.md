# 🚀 LMS Development Guide: .NET Core 8 + Entity Framework + Angular 17+
## Complete Step-by-Step Implementation

---

## TABLE OF CONTENTS
1. [Prerequisites & Setup](#prerequisites)
2. [Week 1: Foundation](#week-1)
3. [Week 2-3: Student Module](#week-2-3)
4. [Week 4-5: Teacher Module](#week-4-5)
5. [Week 6: Admin Module](#week-6)
6. [Week 7-8: Testing & Deployment](#week-7-8)
7. [Code Patterns & Examples](#patterns)
8. [Best Practices](#best-practices)

---

## PREREQUISITES & SETUP {#prerequisites}

### System Requirements
- Windows 10+, macOS 10.15+, or Linux
- .NET 8 SDK (LTS)
- SQL Server 2022 (Express, Developer, or full)
- Node.js 18+
- Visual Studio 2022 or VS Code with C# extension

### Installation

```bash
# 1. Install .NET 8 SDK
# Download from https://dotnet.microsoft.com/download

# 2. Verify .NET installation
dotnet --version
# Should show: 8.0.x

# 3. Install Node.js
# Download from https://nodejs.org (LTS)

# 4. Create project directory
mkdir lms-platform
cd lms-platform
git init
```

### Create Backend (.NET Core)

```bash
# Create solution
dotnet new globaljson --sdk-version 8.0.0 --roll-forward latestMinor
dotnet new sln --name LMS

# Create class libraries (layered architecture)
dotnet new classlib --name LMS.Domain --output src/LMS.Domain
dotnet new classlib --name LMS.Application --output src/LMS.Application
dotnet new classlib --name LMS.Infrastructure --output src/LMS.Infrastructure

# Create Web API project
dotnet new webapi --name LMS.WebAPI --output src/LMS.WebAPI --framework net8.0

# Create test project
dotnet new xunit --name LMS.Tests --output tests/LMS.Tests

# Add projects to solution
dotnet sln add src/LMS.Domain/LMS.Domain.csproj
dotnet sln add src/LMS.Application/LMS.Application.csproj
dotnet sln add src/LMS.Infrastructure/LMS.Infrastructure.csproj
dotnet sln add src/LMS.WebAPI/LMS.WebAPI.csproj
dotnet sln add tests/LMS.Tests/LMS.Tests.csproj

# Add project references
cd src/LMS.WebAPI
dotnet add reference ../LMS.Application/LMS.Application.csproj
cd ../LMS.Application
dotnet add reference ../LMS.Domain/LMS.Domain.csproj
cd ../LMS.Infrastructure
dotnet add reference ../LMS.Domain/LMS.Domain.csproj
cd ../../tests/LMS.Tests
dotnet add reference ../../src/LMS.WebAPI/LMS.WebAPI.csproj
```

### Create Frontend (Angular 17+)

```bash
# Install Angular CLI
npm install -g @angular/cli

# Create Angular workspace
ng new lms-angular --package-manager=npm --skip-git

# Navigate to project
cd lms-angular

# Add Angular Material (optional)
ng add @angular/material

# Add Tailwind CSS
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init -p
```

### Project Structure

```
lms-platform/
├── src/
│   ├── LMS.Domain/                    # Domain entities, interfaces
│   │   └── Entities/
│   ├── LMS.Application/               # DTOs, business logic
│   │   ├── DTOs/
│   │   ├── Features/
│   │   ├── Validators/
│   │   └── Mappings/
│   ├── LMS.Infrastructure/            # EF Core, repositories
│   │   ├── Persistence/
│   │   ├── Repositories/
│   │   └── Services/
│   └── LMS.WebAPI/                    # Controllers, middleware
│       ├── Controllers/
│       ├── Middleware/
│       ├── appsettings.json
│       └── Startup.cs
├── tests/
│   └── LMS.Tests/                     # xUnit tests
├── lms-angular/                       # Angular frontend
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/                  # Services, guards, interceptors
│   │   │   ├── shared/                # Shared components, pipes
│   │   │   ├── student/               # Student feature module
│   │   │   ├── teacher/               # Teacher feature module
│   │   │   ├── admin/                 # Admin feature module
│   │   │   └── auth/                  # Auth module
│   │   ├── assets/
│   │   ├── styles/
│   │   └── main.ts
│   └── package.json
├── docker-compose.yml
└── .gitignore
```

---

## WEEK 1: FOUNDATION & INFRASTRUCTURE {#week-1}

### Day 1: Database Setup & Entity Framework

**Goal**: Create complete database schema with EF Core migrations

**Step 1: Add NuGet Packages to LMS.Infrastructure**

```bash
cd src/LMS.Infrastructure

# Core EF packages
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0

# Additional tools
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 8.0.0
```

**Step 2: Create Entity Classes (LMS.Domain/Entities/)**

```csharp
// User.cs
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Role { get; set; } // "Student", "Teacher", "Admin"
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}

// Course.cs
public class Course
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public string? ThumbnailUrl { get; set; }
    public Guid TeacherId { get; set; }
    public int? MaxStudents { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public User Teacher { get; set; }
    public ICollection<Module> Modules { get; set; } = new List<Module>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}

// Lesson.cs
public class Lesson
{
    public Guid Id { get; set; }
    public Guid ModuleId { get; set; }
    public string Title { get; set; }
    public string Type { get; set; } // "Video", "Document", "Text", "Quiz", "Assignment"
    public string? Content { get; set; } // JSON content
    public int? Duration { get; set; } // In minutes
    public int Order { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Module Module { get; set; }
    public ICollection<LessonProgress> Progress { get; set; } = new List<LessonProgress>();
}

// Module.cs
public class Module
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public Course Course { get; set; }
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}

// Create similar entities for: Enrollment, Assessment, Question, Submission, Certificate, Notification, etc.
```

**Step 3: Create DbContext**

```csharp
// ApplicationDbContext.cs in LMS.Infrastructure/Persistence/
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Module> Modules { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<Assessment> Assessments { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Submission> Submissions { get; set; }
    public DbSet<Certificate> Certificates { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure entities with Fluent API
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).IsRequired();
            entity.HasMany(e => e.Enrollments).WithOne(e => e.Student).HasForeignKey(e => e.StudentId);
            entity.HasMany(e => e.Submissions).WithOne(e => e.Student).HasForeignKey(e => e.StudentId);
        });
        
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired();
            entity.HasOne(e => e.Teacher).WithMany().HasForeignKey(e => e.TeacherId);
            entity.HasMany(e => e.Modules).WithOne(e => e.Course).HasForeignKey(e => e.CourseId);
            entity.HasMany(e => e.Enrollments).WithOne(e => e.Course).HasForeignKey(e => e.CourseId);
        });
        
        // Configure other entities similarly...
    }
}
```

**Step 4: Create Initial Migration**

```bash
cd src/LMS.Infrastructure

# Create migration
dotnet ef migrations add InitialCreate -p . -s ../LMS.WebAPI

# Update database
dotnet ef database update -p . -s ../LMS.WebAPI
```

---

### Day 2: Authentication System

**Goal**: Implement JWT authentication with login/register endpoints

**Step 1: Add NuGet Packages to LMS.WebAPI**

```bash
cd src/LMS.WebAPI

dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Microsoft.IdentityModel.Tokens
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package BCrypt.Net-Next
```

**Step 2: Create Auth DTOs (LMS.Application/DTOs/)**

```csharp
public class RegisterRequest
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string Role { get; set; } // "Student" or "Teacher"
}

public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public UserDto? User { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Role { get; set; }
}
```

**Step 3: Create Auth Service**

```csharp
// IAuthService.cs
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
}

// AuthService.cs
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly IMapper _mapper;
    
    public AuthService(ApplicationDbContext context, IConfiguration config, IMapper mapper)
    {
        _context = context;
        _config = config;
        _mapper = mapper;
    }
    
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if user exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            return new AuthResponse { Success = false, Message = "Email already registered" };
        
        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        // Generate tokens
        var tokens = GenerateTokens(user);
        
        return new AuthResponse
        {
            Success = true,
            Message = "Registration successful",
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            User = _mapper.Map<UserDto>(user)
        };
    }
    
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return new AuthResponse { Success = false, Message = "Invalid credentials" };
        
        if (!user.IsActive)
            return new AuthResponse { Success = false, Message = "Account is inactive" };
        
        var tokens = GenerateTokens(user);
        
        return new AuthResponse
        {
            Success = true,
            Message = "Login successful",
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            User = _mapper.Map<UserDto>(user)
        };
    }
    
    private (string AccessToken, string RefreshToken) GenerateTokens(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
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
        
        var refreshToken = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) },
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );
        
        var handler = new JwtSecurityTokenHandler();
        return (handler.WriteToken(accessToken), handler.WriteToken(refreshToken));
    }
}
```

**Step 4: Configure Authentication in Startup**

```csharp
// Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddAutoMapper(typeof(Program));

// Add Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

**appsettings.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LmsDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "your-super-secret-key-must-be-at-least-32-characters-long",
    "Issuer": "LmsApp",
    "Audience": "LmsUsers",
    "ExpirationMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

---

### Day 3: Create Auth Endpoints

**Goal**: Build REST API endpoints for registration and login

```csharp
// Controllers/AuthController.cs
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return result.Success ? Ok(result) : Unauthorized(result);
    }
    
    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // Return user profile
        return Ok();
    }
}
```

---

### Day 4-5: Angular Setup & Auth Module

**Goal**: Create Angular authentication module and guards

**Step 1: Create Auth Service**

```typescript
// src/app/auth/services/auth.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { signal } from '@angular/core';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

export interface AuthResponse {
  success: boolean;
  message: string;
  accessToken?: string;
  refreshToken?: string;
  user?: any;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = 'http://localhost:5000/api/auth';
  isAuthenticated = signal(false);
  user = signal<any>(null);
  
  constructor(private http: HttpClient) {}
  
  register(data: any): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, data)
      .pipe(
        tap(response => {
          if (response.success) {
            this.setTokens(response.accessToken!, response.refreshToken!);
            this.user.set(response.user);
            this.isAuthenticated.set(true);
          }
        })
      );
  }
  
  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, { email, password })
      .pipe(
        tap(response => {
          if (response.success) {
            this.setTokens(response.accessToken!, response.refreshToken!);
            this.user.set(response.user);
            this.isAuthenticated.set(true);
          }
        })
      );
  }
  
  logout(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    this.isAuthenticated.set(false);
    this.user.set(null);
  }
  
  private setTokens(accessToken: string, refreshToken: string): void {
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
  }
  
  getAccessToken(): string | null {
    return localStorage.getItem('accessToken');
  }
}
```

**Step 2: Create Auth Guard**

```typescript
// src/app/core/guards/auth.guard.ts
import { Injectable, inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../../auth/services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  
  if (authService.isAuthenticated()) {
    return true;
  }
  
  router.navigate(['/login']);
  return false;
};
```

**Step 3: Create HTTP Interceptor**

```typescript
// src/app/core/interceptors/auth.interceptor.ts
import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from '../../auth/services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private authService: AuthService) {}
  
  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = this.authService.getAccessToken();
    
    if (token) {
      req = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }
    
    return next.handle(req);
  }
}
```

---

### Day 6-7: Docker Setup & Initial Build

**Goal**: Set up Docker for local development

**docker-compose.yml**

```yaml
version: '3.8'

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "YourPassword123!"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql
    networks:
      - lms-network

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    networks:
      - lms-network

  api:
    build:
      context: .
      dockerfile: src/LMS.WebAPI/Dockerfile
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=LmsDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;
    depends_on:
      - sqlserver
      - redis
    networks:
      - lms-network

  angular:
    build:
      context: ./lms-angular
      dockerfile: Dockerfile
    ports:
      - "4200:4200"
    volumes:
      - ./lms-angular/src:/app/src
    networks:
      - lms-network

volumes:
  sqlserver_data:

networks:
  lms-network:
    driver: bridge
```

**Start Development**

```bash
docker-compose up -d

# Check services
docker-compose ps

# View logs
docker-compose logs -f api
docker-compose logs -f angular
```

**Summary - End of Week 1**
✅ Complete layered architecture setup
✅ Database schema with EF Core
✅ JWT authentication system
✅ Auth endpoints (register/login)
✅ Angular auth module with guards
✅ Docker development environment

---

## WEEK 2-3: STUDENT MODULE {#week-2-3}

### Overview
Build complete student features:
- Day 1-2: Registration & login pages
- Day 3: Dashboard
- Day 4: Course catalog
- Day 5: Video player
- Day 6: Quizzes & assignments
- Day 7: Certificates

### Day 1-2: Registration & Login Angular Pages

**Step 1: Create Auth Module**

```typescript
// src/app/auth/auth.module.ts
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';

import { AuthRoutingModule } from './auth-routing.module';
import { LoginComponent } from './pages/login/login.component';
import { RegisterComponent } from './pages/register/register.component';
import { AuthInterceptor } from '../core/interceptors/auth.interceptor';

@NgModule({
  declarations: [LoginComponent, RegisterComponent],
  imports: [CommonModule, ReactiveFormsModule, HttpClientModule, AuthRoutingModule],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ]
})
export class AuthModule { }
```

**Step 2: Login Component**

```typescript
// src/app/auth/pages/login/login.component.ts
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  private authService = inject(AuthService);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  
  loginForm: FormGroup;
  loading = false;
  error: string | null = null;
  
  constructor() {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }
  
  onSubmit(): void {
    if (!this.loginForm.valid) return;
    
    this.loading = true;
    const { email, password } = this.loginForm.value;
    
    this.authService.login(email, password).subscribe({
      next: (response) => {
        if (response.success) {
          this.router.navigate(['/student/dashboard']);
        }
      },
      error: (err) => {
        this.error = err.error?.message || 'Login failed';
        this.loading = false;
      }
    });
  }
}
```

**login.component.html**

```html
<div class="login-container">
  <div class="login-card">
    <h2>Student Login</h2>
    
    <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
      <div class="form-group">
        <label>Email</label>
        <input
          type="email"
          formControlName="email"
          class="form-control"
          placeholder="Enter your email"
        />
        <small *ngIf="loginForm.get('email')?.hasError('required')">
          Email is required
        </small>
      </div>
      
      <div class="form-group">
        <label>Password</label>
        <input
          type="password"
          formControlName="password"
          class="form-control"
          placeholder="Enter your password"
        />
        <small *ngIf="loginForm.get('password')?.hasError('required')">
          Password is required
        </small>
      </div>
      
      <div *ngIf="error" class="alert alert-danger">
        {{ error }}
      </div>
      
      <button
        type="submit"
        [disabled]="!loginForm.valid || loading"
        class="btn btn-primary w-100"
      >
        {{ loading ? 'Logging in...' : 'Login' }}
      </button>
    </form>
    
    <p class="mt-3">
      Don't have an account? <a routerLink="/register">Register here</a>
    </p>
  </div>
</div>
```

### Day 3: Student Dashboard

**Step 1: Create Dashboard Service**

```csharp
// LMS.Application/Features/Student/Queries/GetDashboardQuery.cs
public class GetDashboardQuery : IRequest<DashboardDto>
{
}

public class DashboardDto
{
    public int TotalCourses { get; set; }
    public int CompletedCourses { get; set; }
    public decimal OverallProgress { get; set; }
    public List<CourseProgressDto> EnrolledCourses { get; set; }
    public List<DeadlineDto> UpcomingDeadlines { get; set; }
    public List<AnnouncementDto> RecentAnnouncements { get; set; }
}

public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    
    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        
        var enrollments = await _context.Enrollments
            .Where(e => e.StudentId == userId)
            .Include(e => e.Course)
            .ToListAsync(cancellationToken);
        
        var totalCourses = enrollments.Count;
        var completedCourses = enrollments.Count(e => e.IsCompleted);
        var overallProgress = enrollments.Any() 
            ? enrollments.Average(e => e.ProgressPercentage)
            : 0;
        
        var enrolledCourses = _mapper.Map<List<CourseProgressDto>>(enrollments);
        var deadlines = await GetUpcomingDeadlines(userId, cancellationToken);
        var announcements = await GetRecentAnnouncements(userId, cancellationToken);
        
        return new DashboardDto
        {
            TotalCourses = totalCourses,
            CompletedCourses = completedCourses,
            OverallProgress = overallProgress,
            EnrolledCourses = enrolledCourses,
            UpcomingDeadlines = deadlines,
            RecentAnnouncements = announcements
        };
    }
    
    private async Task<List<DeadlineDto>> GetUpcomingDeadlines(Guid userId, CancellationToken cancellationToken)
    {
        // Query for upcoming assignment deadlines
        return await _context.Assessments
            .Where(a => a.DueDate > DateTime.UtcNow && 
                       a.Course.Enrollments.Any(e => e.StudentId == userId))
            .OrderBy(a => a.DueDate)
            .Take(5)
            .Select(a => new DeadlineDto
            {
                Id = a.Id,
                Title = a.Title,
                DueDate = a.DueDate,
                Course = a.Course.Title
            })
            .ToListAsync(cancellationToken);
    }
    
    private async Task<List<AnnouncementDto>> GetRecentAnnouncements(Guid userId, CancellationToken cancellationToken)
    {
        // Query recent announcements from enrolled courses
        return await _context.Announcements
            .Where(a => a.Course.Enrollments.Any(e => e.StudentId == userId))
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new AnnouncementDto
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                CreatedAt = a.CreatedAt,
                CourseTitle = a.Course.Title
            })
            .ToListAsync(cancellationToken);
    }
}
```

**Step 2: Dashboard Controller**

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentController : ControllerBase
{
    private readonly IMediator _mediator;
    
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        var result = await _mediator.Send(new GetDashboardQuery());
        return Ok(result);
    }
}
```

**Step 3: Angular Dashboard Component**

```typescript
// src/app/student/pages/dashboard/dashboard.component.ts
import { Component, OnInit, inject } from '@angular/core';
import { StudentService } from '../../services/student.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  private studentService = inject(StudentService);
  
  dashboard: any;
  loading = true;
  error: string | null = null;
  
  ngOnInit(): void {
    this.loadDashboard();
  }
  
  loadDashboard(): void {
    this.studentService.getDashboard().subscribe({
      next: (data) => {
        this.dashboard = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load dashboard';
        this.loading = false;
      }
    });
  }
}
```

**dashboard.component.html**

```html
<div class="dashboard-container">
  <div class="dashboard-header">
    <h1>Welcome back!</h1>
    <p>Continue your learning journey</p>
  </div>
  
  <div *ngIf="loading" class="loading">
    <p>Loading dashboard...</p>
  </div>
  
  <div *ngIf="!loading && dashboard" class="dashboard-content">
    <!-- Statistics Cards -->
    <div class="stats-grid">
      <div class="stat-card">
        <h3>{{ dashboard.totalCourses }}</h3>
        <p>Enrolled Courses</p>
      </div>
      <div class="stat-card">
        <h3>{{ dashboard.overallProgress | percent }}</h3>
        <p>Overall Progress</p>
      </div>
      <div class="stat-card">
        <h3>{{ dashboard.completedCourses }}</h3>
        <p>Completed</p>
      </div>
    </div>
    
    <!-- Courses Section -->
    <div class="courses-section">
      <h2>My Courses</h2>
      <div class="courses-grid">
        <div *ngFor="let course of dashboard.enrolledCourses" class="course-card">
          <img [src]="course.thumbnailUrl" alt="{{ course.title }}" />
          <h3>{{ course.title }}</h3>
          <p>{{ course.teacherName }}</p>
          <div class="progress-bar">
            <div class="progress" [style.width.%]="course.progressPercentage"></div>
          </div>
          <span>{{ course.progressPercentage }}% complete</span>
          <button class="btn-primary" (click)="continueCourse(course.id)">
            Continue Learning
          </button>
        </div>
      </div>
    </div>
    
    <!-- Deadlines Section -->
    <div class="deadlines-section">
      <h2>Upcoming Deadlines</h2>
      <table>
        <thead>
          <tr>
            <th>Assignment</th>
            <th>Course</th>
            <th>Due Date</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let deadline of dashboard.upcomingDeadlines">
            <td>{{ deadline.title }}</td>
            <td>{{ deadline.course }}</td>
            <td>{{ deadline.dueDate | date }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</div>
```

---

Continue with similar patterns for:
- **Day 4**: Course Catalog with filtering
- **Day 5**: Video player with playback controls
- **Day 6**: Quiz/assignment system
- **Day 7**: Certificates and completion

Each follows the same pattern:
1. Create .NET backend (service, DTOs, controller)
2. Create Angular service and component
3. Test in browser

---

## PATTERNS & BEST PRACTICES {#patterns}

### C# Entity Configuration Pattern

```csharp
// Fluent API configuration
modelBuilder.Entity<Course>(entity =>
{
    entity.HasKey(e => e.Id);
    
    entity.Property(e => e.Title)
        .IsRequired()
        .HasMaxLength(200);
    
    entity.Property(e => e.Description)
        .IsRequired();
    
    entity.HasOne(e => e.Teacher)
        .WithMany(t => t.CreatedCourses)
        .HasForeignKey(e => e.TeacherId)
        .OnDelete(DeleteBehavior.NoAction);
    
    entity.HasMany(e => e.Enrollments)
        .WithOne(e => e.Course)
        .HasForeignKey(e => e.CourseId)
        .OnDelete(DeleteBehavior.Cascade);
    
    entity.HasIndex(e => e.Title);
    entity.HasIndex(e => e.CreatedAt);
});
```

### Angular Service Pattern with Signals

```typescript
import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class CourseService {
  private courses$ = signal<Course[]>([]);
  private loading$ = signal(false);
  
  public courses = computed(() => this.courses$());
  public isLoading = computed(() => this.loading$());
  
  constructor(private http: HttpClient) {}
  
  loadCourses(): void {
    this.loading$.set(true);
    this.http.get<Course[]>('/api/courses')
      .pipe(
        tap(courses => {
          this.courses$.set(courses);
          this.loading$.set(false);
        })
      )
      .subscribe();
  }
}
```

### Reactive Forms with Validation

```typescript
export class CourseFormComponent {
  courseForm: FormGroup;
  
  constructor(private fb: FormBuilder) {
    this.courseForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(3)]],
      description: ['', [Validators.required, Validators.minLength(10)]],
      category: ['', Validators.required],
      maxStudents: [null, [Validators.min(1), Validators.max(500)]],
      isPublished: [false]
    });
  }
  
  get title() { return this.courseForm.get('title'); }
  get description() { return this.courseForm.get('description'); }
  
  submit(): void {
    if (this.courseForm.invalid) return;
    // Submit form
  }
}
```

---

## BEST PRACTICES {#best-practices}

### .NET Core
1. **Use dependency injection** for all services
2. **Validate input** with FluentValidation or DataAnnotations
3. **Log with Serilog** for structured logging
4. **Use Entity Framework migrations** for schema changes
5. **Implement repository pattern** for data access
6. **Use async/await** for all I/O operations
7. **Validate on both client and server**
8. **Use DTOs** to avoid exposing domain entities
9. **Implement proper error handling** with custom exceptions
10. **Use pagination** for large datasets

### Angular
1. **Use Signals** for reactive state (Angular 17+)
2. **Use standalone components** (Angular 14+)
3. **Unsubscribe properly** with takeUntilDestroyed
4. **Use trackBy** in *ngFor for performance
5. **Implement OnPush** change detection strategy
6. **Lazy load feature modules** via router
7. **Use strong typing** with TypeScript strict mode
8. **Create reusable components** for common UI
9. **Implement interceptors** for cross-cutting concerns
10. **Test with Cypress** for E2E testing

---

**Continuation**: Follow this pattern for Weeks 4-8 (Teacher Module, Admin Module, Testing, Deployment)

Each week builds on the previous with increasing complexity but following the same architectural patterns.

For complete details on all features, see the BRD document.

