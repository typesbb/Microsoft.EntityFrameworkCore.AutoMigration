using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore.Migrations;

public interface IAutoMigrationsHistoryRepository
{
    bool Exists();
    Task<bool> ExistsAsync(CancellationToken cancellationToken = default);
    IReadOnlyList<AutoMigrationsHistory> GetAppliedMigrations();
    Task<IReadOnlyList<AutoMigrationsHistory>> GetAppliedMigrationsAsync(
        CancellationToken cancellationToken = default);
    string GetCreateScript();
    string GetInsertScript(AutoMigrationsHistory row);
    string GetDeleteScript(string migrationId);
}
