using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using UTH.Library.Application.Abstractions.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Infrastructure.Persistence;
using UTH.Library.Infrastructure.Persistence.Repositories;
using UTH.Library.Infrastructure.Identity;
using UTH.Library.Application.Features.Settings;
using UTH.Library.Infrastructure.Settings;
using UTH.Library.Application.Features.Notifications.Adapters;
using UTH.Library.Application.Features.Notifications;
using UTH.Library.Infrastructure.Notifications;
using Pgvector.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.AI;
using UTH.Library.Infrastructure.AI;

namespace UTH.Library.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDataProtection();
        services.AddOptions<AuditRetentionOptions>()
            .Bind(configuration.GetSection(AuditRetentionOptions.SectionName))
            .Validate(options => options.RetentionDays is >= 30 and <= 3650, "Audit:RetentionDays phải nằm trong khoảng từ 30 đến 3650.")
            .ValidateOnStart();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddDbContext<LibraryDbContext>((provider, options) =>
        {
            var connectionString = configuration.GetConnectionString("LibraryDatabase")
                ?? throw new InvalidOperationException("Cấu hình ConnectionStrings:LibraryDatabase là bắt buộc.");
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector());
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            options.AddInterceptors(provider.GetRequiredService<AuditSaveChangesInterceptor>());
        });
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(JwtOptions.IsValid, "Invalid JWT configuration.")
            .ValidateOnStart();
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<ApplicationRole>()
            .AddSignInManager()
            .AddEntityFrameworkStores<LibraryDbContext>()
            .AddDefaultTokenProviders();
        services.AddOptions<Argon2PasswordHasherOptions>()
            .Bind(configuration.GetSection(Argon2PasswordHasherOptions.SectionName))
            .Validate(Argon2PasswordHasherOptions.IsValid, "Invalid Argon2 password hasher configuration.")
            .ValidateOnStart();
        services.AddScoped<IPasswordHasher<ApplicationUser>, Argon2PasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthorizationStateService, AuthorizationStateService>();
        services.AddScoped<ICurrentProfileService, CurrentProfileService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IAccessAccountService, AccessAccountService>();
        services.AddScoped<IEmployeeAccountLifecycle, EmployeeAccountLifecycle>();
        services.AddScoped<IRolePermissionManagementService, RolePermissionManagementService>();
        services.AddSingleton<RsaJwtKeyProvider>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddHostedService<JwtKeyValidationHostedService>();
        services.AddHostedService<IdentitySeeder>();
        services.AddHostedService<AuditRetentionService>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IBookSemanticSearchRepository, BookSemanticSearchRepository>();
        services.AddOptions<AiEmbeddingOptions>()
            .Bind(configuration.GetSection(AiEmbeddingOptions.SectionName))
            .Validate(AiEmbeddingOptions.IsValid,
                "AI provider, model, base URL hoặc số chiều embedding không hợp lệ. Database yêu cầu 1536 chiều.")
            .ValidateOnStart();
        services.AddHttpClient<OpenAiEmbeddingService>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<AiEmbeddingOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IEmbeddingService>(provider => provider.GetRequiredService<OpenAiEmbeddingService>());
        services.AddScoped<IBorrowingRepository, BorrowingRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IViolationRepository, ViolationRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<ICirculationPolicyRepository, CirculationPolicyRepository>();
        services.AddMemoryCache();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ISystemConfigurationService, SystemConfigurationService>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IStockReceiptRepository, StockReceiptRepository>();
        services.AddScoped<IBookCopyRepository, BookCopyRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IInventoryAuditRepository, InventoryAuditRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<ISavedFilterRepository, SavedFilterRepository>();
        services.AddScoped<ITodoRepository, TodoRepository>();
        services.AddScoped<INotificationSenderAdapter, InAppNotificationSenderAdapter>();
        services.AddScoped<INotificationSenderAdapter, EmailNotificationSenderAdapter>();
        services.AddScoped<ISmtpSettingsProvider, SmtpSettingsProvider>();
        services.AddScoped<ISmtpAdministrationService, SmtpAdministrationService>();
        services.AddHostedService<EmailOutboxWorker>();
        return services;
    }
}
