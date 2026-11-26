using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QueryAI.Application.Abstractions.Persistence.Interfaces;
using QueryAI.Application.Abstractions.Security.Interfaces;
using QueryAI.Domain.Common.Interfaces;
using QueryAI.Infrastructure.Common.Services;
using QueryAI.Infrastructure.Persistence.Contexts;
using QueryAI.Infrastructure.Persistence.Repositories;
using QueryAI.Infrastructure.Persistence.Transactions;
using QueryAI.Infrastructure.Security.Services;
using QueryAI.Infrastructure.Startup.HostedServices;

namespace QueryAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, Func<IServiceProvider, string> connectionStringFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionStringFactory);

        services.AddDbContext<AppDbContext>((sp, options) => ConfigureDbContext(options, connectionStringFactory(sp)));
        services.AddDataProtection()
            .SetApplicationName("QueryAI")
            .PersistKeysToDbContext<AppDbContext>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
        services.AddScoped<ISecurityAuditWriter, SecurityAuditWriter>();

        services.AddHostedService<DatabaseMigrator>();
        services.AddHostedService<InitialAdminSeeder>();

        return services;
    }

    internal static void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history"))
            .UseSnakeCaseNamingConvention();
}
