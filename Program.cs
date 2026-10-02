using FTCERP.Host.Domain.Entities;
using FTCERP.Host.Infrastructure.Auth;
using FTCERP.Host.Infrastructure.Persistence;
using FTCERP.Host.Infrastructure.Persistence.Seed;
using FTCERP.Host.Infrastructure.Security;
using FTCERP.Host.Domain.Services;
using FTCERP.Host.Application.Services;
using FTCERP.Host.Infrastructure.Health;
using FTCERP.Host.Infrastructure.Observability;
using FTCERP.Host.Infrastructure.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
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
builder.Services.AddControllers(options => options.Filters.Add<ApiFailureProblemDetailsFilter>());
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options => options.InvalidModelStateResponseFactory = ApiProblemDetails.InvalidModelStateResponse);
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = problem => ApiProblemDetails.ApplyDefaults(problem.ProblemDetails, problem.HttpContext));
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
    c.OperationFilter<IdempotencyOperationFilter>();
    c.OperationFilter<ProblemDetailsOperationFilter>();
});

// Add EF Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseConfiguredDatabase(builder.Configuration);
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
    // Per-municipality enforcement applies the configured threshold at or before this safe ceiling.
    options.Lockout.MaxFailedAccessAttempts = 21;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddPasswordValidator<CompromisedPasswordValidator>()
.AddPasswordValidator<MunicipalityPasswordPolicyValidator>();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromMinutes(Math.Clamp(builder.Configuration.GetValue("Authentication:PasswordReset:TokenLifetimeMinutes", 30), 5, 1440)));

// Configure JWT Settings
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
var enterpriseOptions = builder.Configuration.GetSection(EnterpriseAuthenticationOptions.SectionName).Get<EnterpriseAuthenticationOptions>() ?? new EnterpriseAuthenticationOptions();
if (!enterpriseOptions.PostLoginPath.StartsWith('/') || !enterpriseOptions.FailurePath.StartsWith('/'))
    throw new InvalidOperationException("Enterprise authentication redirects must be local application paths.");
var enterpriseProviders = new EnterpriseProviderRegistry(enterpriseOptions.Providers);
builder.Services.Configure<EnterpriseAuthenticationOptions>(builder.Configuration.GetSection(EnterpriseAuthenticationOptions.SectionName));
builder.Services.AddSingleton<IEnterpriseProviderRegistry>(enterpriseProviders);

var authentication = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
});
authentication.AddCookie(FTCERP.Host.API.Controllers.EnterpriseAuthController.ExternalCookieScheme, options =>
{
    options.Cookie.Name = "opms_enterprise_external";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
});
authentication.AddJwtBearer(options =>
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
        OnMessageReceived = messageContext =>
        {
            var authorization = messageContext.Request.Headers.Authorization.ToString();
            if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                && messageContext.Request.Cookies.TryGetValue(AuthCookiePolicy.AccessCookieName, out var cookieToken))
                messageContext.Token = cookieToken;
            return Task.CompletedTask;
        },
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
foreach (var provider in enterpriseProviders.Providers)
{
    authentication.AddOpenIdConnect(EnterpriseProviderRegistry.Scheme(provider.Code), options =>
    {
        options.SignInScheme = FTCERP.Host.API.Controllers.EnterpriseAuthController.ExternalCookieScheme;
        options.Authority = provider.Authority;
        options.ClientId = provider.ClientId;
        options.ClientSecret = provider.ClientSecret;
        options.CallbackPath = provider.CallbackPath;
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = false;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.TokenValidationParameters.ValidateIssuer = true;
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "roles";
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        foreach (var scope in provider.Scopes) options.Scope.Add(scope);
    });
}

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthenticationPolicyResolver, AuthenticationPolicyResolver>();
builder.Services.AddScoped<IEnterpriseAuthenticationService, EnterpriseAuthenticationService>();
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
builder.Services.AddScoped<IPasswordResetNotifier, PasswordResetNotifier>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<ICompromisedPasswordLookup, PwnedPasswordLookup>(client =>
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(builder.Configuration.GetValue("Authentication:PasswordProtection:TimeoutSeconds", 5), 2, 30)));
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
builder.Services.AddSingleton<OperationalTelemetry>();
builder.Services.Configure<IdempotencyOptions>(builder.Configuration.GetSection(IdempotencyOptions.SectionName));
builder.Services.AddHostedService<IdempotencyCleanupWorker>();

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
    options.AddPolicy("api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
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

builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database").AddCheck<OutboxHealthCheck>("outbox").AddCheck<EvidenceScannerHealthCheck>("evidence-scanner").AddCheck<EvidenceStorageHealthCheck>("evidence-storage").AddCheck<NotificationChannelHealthCheck>("notification-channels").AddCheck<CompromisedPasswordHealthCheck>("compromised-passwords").AddCheck<OperationalTelemetryHealthCheck>("operational-telemetry");

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:EnsureCreated"))
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("Database:EnsureCreated is restricted to the Development environment.");

    await using var initializationScope = app.Services.CreateAsyncScope();
    var initializationContext = initializationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (!initializationContext.Database.IsSqlite())
        throw new InvalidOperationException("Database:EnsureCreated is supported only for disposable local SQLite development databases.");
    await initializationContext.Database.EnsureCreatedAsync();
}

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
        ProblemDetails = ApiProblemDetails.Create(http, http.Response.StatusCode)
    });
});
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestTelemetryMiddleware>();
app.UseMiddleware<CsrfProtectionMiddleware>();

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

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseRateLimiter();
app.UseMiddleware<PasswordChangeMiddleware>();
app.UseMiddleware<MfaEnrollmentMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();
app.UseMiddleware<IdempotencyMiddleware>();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers().RequireRateLimiting("api");
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = HealthResponseWriter.WriteAsync
});

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
