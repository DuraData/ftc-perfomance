using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Auth;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Persistence.Seed;
using FTCERP.Host.Infrastructure.Security;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Application.Services;
using FTCERP.Host.Infrastructure.Health;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = problem => problem.ProblemDetails.Extensions["correlationId"] = problem.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FTCERP API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter JWT with Bearer into field",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

// Add EF Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
});

// Add ASP.NET Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 12;
    options.Password.RequiredUniqueChars = 4;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Settings
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async validationContext =>
        {
            var userId = validationContext.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessionValue = validationContext.Principal?.FindFirstValue("sid");
            var securityStamp = validationContext.Principal?.FindFirstValue("security_stamp");
            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(sessionValue, out var sessionId) || securityStamp == null)
            {
                validationContext.Fail("The access token has no governed session.");
                return;
            }
            var sessions = validationContext.HttpContext.RequestServices.GetRequiredService<IJwtService>();
            if (!await sessions.ValidateAccessSessionAsync(userId, sessionId, securityStamp, validationContext.HttpContext.Connection.RemoteIpAddress?.ToString()))
                validationContext.Fail("The session is inactive, expired, or revoked.");
        }
    };
});

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAccessControlService, AccessControlService>();
builder.Services.AddSingleton<IPerformanceUnitEngine, PerformanceUnitEngine>();
builder.Services.AddScoped<ISubmissionValueService, SubmissionValueService>();
builder.Services.AddScoped<IReportingWindowService, ReportingWindowService>();
builder.Services.AddScoped<IConfigurableWorkflowService, ConfigurableWorkflowService>();
builder.Services.AddSingleton<IEvidenceInspectionService, EvidenceInspectionService>();
builder.Services.AddHttpClient<IEvidenceMalwareScanner, HttpEvidenceMalwareScanner>(client =>
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(builder.Configuration.GetValue("EvidenceScanning:TimeoutSeconds", 30), 5, 120)));
builder.Services.AddHttpClient<INotificationChannelSender, HttpEmailNotificationSender>(client =>
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(builder.Configuration.GetValue("Notifications:Email:TimeoutSeconds", 20), 5, 120)));
builder.Services.AddScoped<IWorkflowGovernanceService, WorkflowGovernanceService>();
builder.Services.AddScoped<FileSystemEvidenceBlobStorage>();
builder.Services.AddHttpClient<HttpEvidenceBlobStorage>(client =>
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(builder.Configuration.GetValue("EvidenceStorage:TimeoutSeconds", 30), 5, 120)))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddScoped<IEvidenceBlobStorage>(services =>
{
    var provider = services.GetRequiredService<IConfiguration>()["EvidenceStorage:Provider"];
    if (string.Equals(provider, "Http", StringComparison.OrdinalIgnoreCase)) return services.GetRequiredService<HttpEvidenceBlobStorage>();
    if (string.Equals(provider, "FileSystem", StringComparison.OrdinalIgnoreCase)) return services.GetRequiredService<FileSystemEvidenceBlobStorage>();
    throw new InvalidOperationException("EvidenceStorage:Provider must be either 'Http' or 'FileSystem'.");
});
builder.Services.AddHostedService<NotificationOutboxWorker>();
builder.Services.AddHostedService<PoeDisposalWorker>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();

builder.Services.AddAuthorization();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, PermissionHandler>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ApplicationClient", policy =>
    {
        var configuredOrigins = builder.Configuration.GetSection("Security:AllowedOrigins").Get<string[]>() ?? [];
        if (configuredOrigins.Length == 0 && builder.Environment.IsDevelopment())
        {
            configuredOrigins = ["http://localhost:5173", "https://localhost:5173"];
        }

        if (configuredOrigins.Length > 0)
        {
            policy.WithOrigins(configuredOrigins)
              .AllowCredentials()
              .AllowAnyMethod()
              .AllowAnyHeader();
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("authentication", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database").AddCheck<OutboxHealthCheck>("outbox").AddCheck<EvidenceScannerHealthCheck>("evidence-scanner").AddCheck<EvidenceStorageHealthCheck>("evidence-storage").AddCheck<NotificationChannelHealthCheck>("notification-channels");

var app = builder.Build();

// Seed only when explicitly enabled. Production must be provisioned through controlled administration.
if (app.Configuration.GetValue<bool>("SeedData:Enabled"))
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
    await DbInitializer.Initialize(context, userManager, roleManager, app.Configuration);
}

// The immutable security catalogue is application infrastructure, not demo data.
// This additive/idempotent bootstrap never overwrites administrator configuration.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SecurityRegistrySeeder.SeedAsync(context);
    await SecurityRegistrySeeder.BackfillAssignmentsAsync(context);
}

// Configure the HTTP request pipeline.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages(async statusCodeContext =>
{
    var http = statusCodeContext.HttpContext;
    if (!http.Request.Path.StartsWithSegments("/api") || http.Response.HasStarted || http.Response.ContentLength.HasValue) return;
    var service = http.RequestServices.GetRequiredService<IProblemDetailsService>();
    await service.TryWriteAsync(new ProblemDetailsContext
    {
        HttpContext = http,
        ProblemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = http.Response.StatusCode,
            Title = Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(http.Response.StatusCode),
            Instance = http.Request.Path
        }
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FTCERP API v1"));
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseCors("ApplicationClient");
app.UseRateLimiter();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<MfaEnrollmentMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready");

app.MapFallback(async context =>
{
    var indexPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "index.html");

    if (File.Exists(indexPath))
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(indexPath);
        return;
    }

    context.Response.ContentType = "text/plain; charset=utf-8";
    await context.Response.WriteAsync("FTCERP frontend has not been built yet. Run 'dotnet build' or 'dotnet publish' to generate the integrated frontend inside wwwroot.");
});

app.Run();

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permissionCode)
    {
        PermissionCode = permissionCode;
    }

    public string PermissionCode { get; }
}

public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccessControlService _accessControl;

    public PermissionHandler(UserManager<ApplicationUser> userManager, IAccessControlService accessControl)
    {
        _userManager = userManager;
        _accessControl = accessControl;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return;
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive) return;
        var decision = await _accessControl.CheckPermissionAsync(user, requirement.PermissionCode);
        if (decision.Allowed) context.Succeed(requirement);
    }

}

public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private const string Prefix = "Permission:";
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            var code = policyName[Prefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(code))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
