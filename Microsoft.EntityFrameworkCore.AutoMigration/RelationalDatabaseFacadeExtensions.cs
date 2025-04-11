using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.EntityFrameworkCore.Migrations;

public static class RelationalDatabaseFacadeExtensions
{
    [RequiresDynamicCode("Migrations operations are not supported with NativeAOT Use a migration bundle or an alternate way of executing migration operations.")]
    public static IEnumerable<MigrationCommand> GetAutoMigrations(this DatabaseFacade databaseFacade)
    {
        return databaseFacade.GetRelationalService<IRelationalAutoMigrator>().GetMigrations();
    }
    [RequiresDynamicCode("Migrations operations are not supported with NativeAOT Use a migration bundle or an alternate way of executing migration operations.")]
    public static IEnumerable<MigrationCommand> GetAppliedAutoMigrations(this DatabaseFacade databaseFacade)
    {
        return databaseFacade.GetRelationalService<IRelationalAutoMigrator>().GetAppliedMigrations();
    }
    [RequiresDynamicCode("Migrations operations are not supported with NativeAOT Use a migration bundle or an alternate way of executing migration operations.")]
    public static async Task<IEnumerable<MigrationCommand>> GetAppliedAutoMigrationsAsync(this DatabaseFacade databaseFacade, CancellationToken cancellationToken = default(CancellationToken))
    {
        return await databaseFacade.GetRelationalService<IRelationalAutoMigrator>().GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
    }
    [RequiresDynamicCode("Migrations operations are not supported with NativeAOT Use a migration bundle or an alternate way of executing migration operations.")]
    public static IEnumerable<MigrationCommand> GetPendingAutoMigrations(this DatabaseFacade databaseFacade)
    {
        return databaseFacade.GetRelationalService<IRelationalAutoMigrator>().GetMigrations().Except(databaseFacade.GetAppliedAutoMigrations());
    }
    [RequiresDynamicCode("Migrations operations are not supported with NativeAOT Use a migration bundle or an alternate way of executing migration operations.")]
    public static async Task<IEnumerable<MigrationCommand>> GetPendingAutoMigrationsAsync(this DatabaseFacade databaseFacade, CancellationToken cancellationToken = default(CancellationToken))
    {
        IEnumerable<MigrationCommand> migrations = databaseFacade.GetRelationalService<IRelationalAutoMigrator>().GetMigrations();
        return migrations.Except(await databaseFacade.GetRelationalService<IRelationalAutoMigrator>().GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
    }
    [RequiresDynamicCode("Migrations operations are not supported with NativeAOT Use a migration bundle or an alternate way of executing migration operations.")]
    public static void AutoMigrate(this DatabaseFacade databaseFacade)
    {
        databaseFacade.GetRelationalService<IRelationalAutoMigrator>().Migrate();
    }
    [RequiresDynamicCode("Migrations operations are not supported with NativeAOT Use a migration bundle or an alternate way of executing migration operations.")]
    public static Task AutoMigrateAsync(this DatabaseFacade databaseFacade, CancellationToken cancellationToken = default(CancellationToken))
    {
        return databaseFacade.GetRelationalService<IRelationalAutoMigrator>().MigrateAsync(cancellationToken);
    }
    private static TService GetRelationalService<TService>(this IInfrastructure<IServiceProvider> databaseFacade)
    {
        var service = databaseFacade.Instance.GetService<TService>();
        return service == null
            ? throw new InvalidOperationException(RelationalStrings.RelationalNotInUse)
            : service;
    }
}
