using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore.Migrations;

public sealed record AutoMigrationsHistoryRepositoryDependencies
{
    [EntityFrameworkInternal]
    public AutoMigrationsHistoryRepositoryDependencies(
        IRelationalDatabaseCreator databaseCreator,
        IRawSqlCommandBuilder rawSqlCommandBuilder,
        IRelationalConnection connection,
        IDbContextOptions options,
        IMigrationsModelDiffer modelDiffer,
        IMigrationsSqlGenerator migrationsSqlGenerator,
        ISqlGenerationHelper sqlGenerationHelper,
        IConventionSetBuilder conventionSetBuilder,
        ModelDependencies modelDependencies,
        IRelationalTypeMappingSource typeMappingSource,
        ICurrentDbContext currentContext,
        IModelRuntimeInitializer modelRuntimeInitializer,
        IRelationalCommandDiagnosticsLogger commandLogger)
    {
        DatabaseCreator = databaseCreator;
        RawSqlCommandBuilder = rawSqlCommandBuilder;
        Connection = connection;
        Options = options;
        ModelDiffer = modelDiffer;
        MigrationsSqlGenerator = migrationsSqlGenerator;
        SqlGenerationHelper = sqlGenerationHelper;
        ConventionSetBuilder = conventionSetBuilder;
        ModelDependencies = modelDependencies;
        TypeMappingSource = typeMappingSource;
        CurrentContext = currentContext;
        ModelRuntimeInitializer = modelRuntimeInitializer;
        CommandLogger = commandLogger;
    }

    /// <summary>
    ///     The database creator.
    /// </summary>
    public IRelationalDatabaseCreator DatabaseCreator { get; init; }

    /// <summary>
    ///     A command builder for building raw SQL commands.
    /// </summary>
    public IRawSqlCommandBuilder RawSqlCommandBuilder { get; init; }

    /// <summary>
    ///     The connection to the database.
    /// </summary>
    public IRelationalConnection Connection { get; init; }

    /// <summary>
    ///     Options for the current context instance.
    /// </summary>
    public IDbContextOptions Options { get; init; }

    /// <summary>
    ///     The model differ.
    /// </summary>
    public IMigrationsModelDiffer ModelDiffer { get; init; }

    /// <summary>
    ///     The SQL generator for Migrations operations.
    /// </summary>
    public IMigrationsSqlGenerator MigrationsSqlGenerator { get; init; }

    /// <summary>
    ///     Helpers for generating update SQL.
    /// </summary>
    public ISqlGenerationHelper SqlGenerationHelper { get; init; }

    /// <summary>
    ///     The core convention set to use when creating the model.
    /// </summary>
    public IConventionSetBuilder ConventionSetBuilder { get; init; }

    /// <summary>
    ///     The model dependencies.
    /// </summary>
    public ModelDependencies ModelDependencies { get; init; }

    /// <summary>
    ///     The type mapper.
    /// </summary>
    public IRelationalTypeMappingSource TypeMappingSource { get; init; }

    /// <summary>
    ///     Contains the <see cref="DbContext" /> currently in use.
    /// </summary>
    public ICurrentDbContext CurrentContext { get; init; }

    /// <summary>
    ///     The model runtime initializer
    /// </summary>
    public IModelRuntimeInitializer ModelRuntimeInitializer { get; init; }

    /// <summary>
    ///     The command logger
    /// </summary>
    public IRelationalCommandDiagnosticsLogger CommandLogger { get; init; }
}
