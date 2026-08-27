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

namespace UTH.Library.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LibraryDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("LibraryDatabase")
                ?? throw new InvalidOperationException("ConnectionStrings:LibraryDatabase is required.");
            options.UseNpgsql(connectionString);
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
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddSingleton<RsaJwtKeyProvider>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddHostedService<JwtKeyValidationHostedService>();
        services.AddHostedService<IdentitySeeder>();
        services.AddScoped<ITodoRepository, TodoRepository>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IBorrowingRepository, BorrowingRepository>();
        return services;
    }
}
