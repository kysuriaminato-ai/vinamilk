using System.Text;
using System.Threading.RateLimiting;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using VinamilkHRM.Application;
using VinamilkHRM.Infrastructure;
using VinamilkHRM.Infrastructure.Data;
using VinamilkHRM.WebApi.Filters;
using VinamilkHRM.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Serilog (Console + Rolling File)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/vinamilk-api-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Đăng ký Application và Infrastructure
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 3. Cấu hình JWT Bearer Authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "Vinamilk_Enterprise_Secure_Secret_Key_For_Jwt_Authentication_2026";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "VinamilkHRM";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "VinamilkHRM.Clients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 4. Rate Limiting cho /api/v1/auth
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 15;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

// 5. CORS dành riêng cho Portal PHP
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost", "http://localhost:8080", "http://127.0.0.1", "http://127.0.0.1:8080" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("PortalCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 6. Global Exception Handler RFC 7807 ProblemDetails
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 7. Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<VinamilkHRMDbContext>("Database");

// 8. OpenAPI & Controllers
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();

var app = builder.Build();

// Pipeline xử lý yêu cầu HTTP
app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Vinamilk Enterprise HRM API Reference";
        options.Theme = ScalarTheme.Purple;
    });
}

app.UseRouting();
app.UseCors("PortalCorsPolicy");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard bảo vệ chỉ cho SystemAdmin
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
});

// Tự động seed 10 vai trò và 10 tài khoản demo ngẫu nhiên trong Development
if (app.Environment.IsDevelopment() || args.Contains("--seed"))
{
    try
    {
        await DatabaseSeeder.SeedIdentityAsync(app.Services);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Không thể chạy SeedIdentityAsync trên CSDL hiện tại.");
    }
}

app.MapControllers();
app.MapHub<VinamilkHRM.WebApi.Hubs.NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health");

app.Run();

// Dùng cho WebApplicationFactory trong Integration Tests
public partial class Program { }
