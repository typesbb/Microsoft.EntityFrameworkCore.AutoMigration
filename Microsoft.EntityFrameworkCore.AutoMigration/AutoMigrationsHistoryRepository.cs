using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore.Migrations;

public class AutoMigrationsHistoryRepository : IAutoMigrationsHistoryRepository
{
    public const string DefaultTableName = "__EFAutoMigrationsHistory";
    private readonly IHistoryRepository _historyRepository;
    private IModel? _model;
    private string? _migrationIdColumnName;
    private string? _productVersionColumnName;
    private string? _snapshotColumnName;
    public AutoMigrationsHistoryRepository(
        AutoMigrationsHistoryRepositoryDependencies dependencies,
        IHistoryRepository historyRepository)
    {
        Dependencies = dependencies;
        _historyRepository = historyRepository;
    }
    protected virtual AutoMigrationsHistoryRepositoryDependencies Dependencies { get; }
    protected virtual string MigrationIdColumnName
        => _migrationIdColumnName ??= EnsureModel()
            .FindEntityType(typeof(AutoMigrationsHistory))!
            .FindProperty(nameof(AutoMigrationsHistory.MigrationId))!
            .GetColumnName();
    protected virtual string ProductVersionColumnName
        => _productVersionColumnName ??= EnsureModel()
            .FindEntityType(typeof(AutoMigrationsHistory))!
            .FindProperty(nameof(AutoMigrationsHistory.ProductVersion))!
            .GetColumnName();
    protected virtual string SnapshotColumnName
        => _snapshotColumnName ??= EnsureModel()
            .FindEntityType(typeof(AutoMigrationsHistory))!
            .FindProperty(nameof(AutoMigrationsHistory.Snapshot))!
            .GetColumnName();

    private IModel EnsureModel()
    {
        if (_model == null)
        {
            var conventionSet = Dependencies.ConventionSetBuilder.CreateConventionSet();

            conventionSet.Remove(typeof(DbSetFindingConvention));
            conventionSet.Remove(typeof(RelationalDbFunctionAttributeConvention));

            var modelBuilder = new ModelBuilder(conventionSet);
            modelBuilder.Entity<AutoMigrationsHistory>(
                x =>
                {
                    ConfigureTable(x);
                    x.ToTable(DefaultTableName);
                });

            _model = Dependencies.ModelRuntimeInitializer.Initialize(
                (IModel)modelBuilder.Model, designTime: true, validationLogger: null);
        }

        return _model;
    }
    protected virtual void ConfigureTable(EntityTypeBuilder<AutoMigrationsHistory> history)
    {
        history.ToTable(DefaultTableName);
        history.HasKey(h => h.MigrationId);
        history.Property(h => h.MigrationId).HasMaxLength(150);
        history.Property(h => h.ProductVersion).HasMaxLength(32).IsRequired();
        history.Property(h => h.Snapshot).HasMaxLength(2097152).IsRequired();
    }

    public bool Exists()
    {
        return _historyRepository.Exists();
    }

    public Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        return _historyRepository.ExistsAsync(cancellationToken);
    }

    public virtual string GetCreateScript()
    {
        var model = EnsureModel();

        var operations = Dependencies.ModelDiffer.GetDifferences(null, model.GetRelationalModel());
        var commandList = Dependencies.MigrationsSqlGenerator.Generate(operations, model);

        return _historyRepository.GetCreateScript() + Environment.NewLine + string.Concat(commandList.Select(c => c.CommandText));
    }
    public virtual IReadOnlyList<AutoMigrationsHistory> GetAppliedMigrations()
    {
        var rows = new List<AutoMigrationsHistory>();

        if (_historyRepository.Exists())
        {
            var command = Dependencies.RawSqlCommandBuilder.Build(GetAppliedMigrationsSql);

            using var reader = command.ExecuteReader(
                new RelationalCommandParameterObject(
                    Dependencies.Connection,
                    null,
                    null,
                    Dependencies.CurrentContext.Context,
                    Dependencies.CommandLogger, CommandSource.Migrations));
            while (reader.Read())
            {
                rows.Add(new AutoMigrationsHistory(reader.DbDataReader.GetString(0), reader.DbDataReader.GetString(1), reader.DbDataReader.GetFieldValue<byte[]>(2)));
            }
        }

        return rows;
    }
    public virtual async Task<IReadOnlyList<AutoMigrationsHistory>> GetAppliedMigrationsAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = new List<AutoMigrationsHistory>();

        if (await _historyRepository.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            var command = Dependencies.RawSqlCommandBuilder.Build(GetAppliedMigrationsSql);

            var reader = await command.ExecuteReaderAsync(
                new RelationalCommandParameterObject(
                    Dependencies.Connection,
                    null,
                    null,
                    Dependencies.CurrentContext.Context,
                    Dependencies.CommandLogger, CommandSource.Migrations),
                cancellationToken).ConfigureAwait(false);

            await using var _ = reader.ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                rows.Add(new AutoMigrationsHistory(reader.DbDataReader.GetString(0), reader.DbDataReader.GetString(1), reader.DbDataReader.GetFieldValue<byte[]>(2)));
            }
        }

        return rows;
    }
    protected virtual string GetAppliedMigrationsSql
        => new StringBuilder()
            .Append("SELECT ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(MigrationIdColumnName))
            .Append(", ")
            .AppendLine(Dependencies.SqlGenerationHelper.DelimitIdentifier(ProductVersionColumnName))
            .Append(", ")
            .AppendLine(Dependencies.SqlGenerationHelper.DelimitIdentifier(SnapshotColumnName))
            .Append("FROM ")
            .AppendLine(Dependencies.SqlGenerationHelper.DelimitIdentifier(DefaultTableName))
            .Append("ORDER BY ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(MigrationIdColumnName))
            .Append(" DESC")
            .Append(Dependencies.SqlGenerationHelper.StatementTerminator)
            .ToString();
    public virtual string GetInsertScript(AutoMigrationsHistory row)
    {
        var stringTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));
        var bytesTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(byte[]));

        return new StringBuilder().Append("INSERT INTO ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(DefaultTableName))
            .Append(" (")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(MigrationIdColumnName))
            .Append(", ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(ProductVersionColumnName))
            .Append(", ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(SnapshotColumnName))
            .AppendLine(")")
            .Append("VALUES (")
            .Append(stringTypeMapping.GenerateSqlLiteral(row.MigrationId))
            .Append(", ")
            .Append(stringTypeMapping.GenerateSqlLiteral(row.ProductVersion))
            .Append(", ")
            .Append(bytesTypeMapping.GenerateSqlLiteral(row.Snapshot))
            .Append(')')
            .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
            .ToString();
    }
    public virtual string GetDeleteScript(string migrationId)
    {
        var stringTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));

        return new StringBuilder().Append("DELETE FROM ")
            .AppendLine(Dependencies.SqlGenerationHelper.DelimitIdentifier(DefaultTableName))
            .Append("WHERE ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(MigrationIdColumnName))
            .Append(" = ")
            .Append(stringTypeMapping.GenerateSqlLiteral(migrationId))
            .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
            .ToString();
    }
}
