using FluentValidation;
using LogInLab.Application.DTOs;
using LogInLab.Application.Interfaces;
using LogInLab.Application.Services;
using LogInLab.Application.Validators;
using LogInLab.Domain.Entities;
using LogInLab.Infrastructure.Email;
using LogInLab.Infrastructure.Persistence;
using LogInLab.Infrastructure.Persistence.Repositories;
using LogInLab.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<LogInLabDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
builder.Services.AddScoped<IValidator<ResetPasswordRequest>, ResetPasswordRequestValidator>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddSingleton<IEncryptionService, AesEncryptionService>();
builder.Services.AddSingleton<ITotpService, TotpService>();
builder.Services.AddScoped<IMfaSecretRepository, MfaSecretRepository>();
builder.Services.AddScoped<IBackupCodeRepository, BackupCodeRepository>();
builder.Services.AddScoped<IMfaService, MfaService>();
builder.Services.AddScoped<IAuthEventLogger, AuthEventLogger>();
builder.Services.AddScoped<ISessionManagementSerivce, SessionManagementService>();

builder.Services.AddHttpClient<IPasswordBreachChecker, HaveIBeenPwnedChecker>(client =>
{
    client.BaseAddress = new Uri("https://api.pwnedpasswords.com/");
    client.Timeout = TimeSpan.FromSeconds(3);
    client.DefaultRequestHeaders.Add("Add-Padding", "true");
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                string? sessionIdClaim = context.Principal?.FindFirstValue("SessionId");

                if(string.IsNullOrEmpty(sessionIdClaim) || !Guid.TryParse(sessionIdClaim, out Guid sessionId))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                ISessionRepository sessionRepository = context.HttpContext.RequestServices.GetRequiredService<ISessionRepository>();

                Session? session = await sessionRepository.GetByIdAsync(sessionId);

                bool isValid = session is not null && session.RevokedAt is null && session.ExpiresAt > DateTime.UtcNow;

                if(!isValid)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
    });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("AuthStrict", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("AuthModerate", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });

    options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
        PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            string ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit =0,
            });
        })
    );

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please wait a moment until you try again.", cancellationToken);
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
