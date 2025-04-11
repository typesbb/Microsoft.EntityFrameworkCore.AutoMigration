using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore.Migrations;

public class AutoMigrationsOptionsExtension : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;
    public DbContextOptionsExtensionInfo Info => _info ??= new AutoMigrationsExtensionInfo(this);
    private sealed class AutoMigrationsExtensionInfo : DbContextOptionsExtensionInfo
    {
        public AutoMigrationsExtensionInfo(IDbContextOptionsExtension extension) : base(extension) { }

        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "UseAutoMigrations";

        public override int GetServiceProviderHashCode() => 0;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["AutoMigrations"] = "1";
        }

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is AutoMigrationsExtensionInfo;
    }

    public void ApplyServices(IServiceCollection services)
    {
        services
            .AddScoped<AutoMigrationsHistoryRepositoryDependencies, AutoMigrationsHistoryRepositoryDependencies>()
            .AddScoped<IAutoMigrationsHistoryRepository, AutoMigrationsHistoryRepository>()
            .AddScoped<IRelationalAutoMigrator, RelationalAutoMigrator>();
    }

    public void Validate(IDbContextOptions options)
    {

    }
}
