using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore.Migrations;

public interface IRelationalAutoMigrator
{
    IEnumerable<MigrationCommand> GetMigrations();
    IEnumerable<MigrationCommand> GetAppliedMigrations();
    Task<IEnumerable<MigrationCommand>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default(CancellationToken));
    IEnumerable<MigrationCommand> GetPendingMigrations();
    Task<IEnumerable<MigrationCommand>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default(CancellationToken));
    void Migrate();
    Task MigrateAsync(CancellationToken cancellationToken = default);
    //string GenerateScript(MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default);
}