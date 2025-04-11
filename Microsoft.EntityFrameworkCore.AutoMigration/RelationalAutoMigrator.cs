using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage;
using System.IO.Compression;
using System.Reflection;
using System.Runtime;
using System.Runtime.Loader;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.Migrations;

public class RelationalAutoMigrator : IRelationalAutoMigrator
{
    private readonly IMigrationsAssembly _migrationsAssembly;
    private readonly IAutoMigrationsHistoryRepository _historyRepository;
    private readonly IRelationalDatabaseCreator _databaseCreator;
    private readonly IMigrationsSqlGenerator _migrationsSqlGenerator;
    private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder;
    private readonly IMigrationCommandExecutor _migrationCommandExecutor;
    private readonly IRelationalConnection _connection;
    private readonly ISqlGenerationHelper _sqlGenerationHelper;
    private readonly ICurrentDbContext _currentContext;
    private readonly IModelRuntimeInitializer _modelRuntimeInitializer;
    private readonly IDesignTimeModel _designTimeModel;
    private readonly IMigrationsModelDiffer _migrationsModelDiffer;
    private readonly IMigrationsCodeGeneratorSelector _migrationsCodeGeneratorSelector;
    private readonly IMigrationsIdGenerator _migrationsIdGenerator;
    private readonly ISnapshotModelProcessor _snapshotModelProcessor;
    private readonly IDiagnosticsLogger<DbLoggerCategory.Migrations> _logger;
    private readonly IRelationalCommandDiagnosticsLogger _commandLogger;
    private readonly string _activeProvider;

    public RelationalAutoMigrator(
        ICurrentDbContext currentContext,
        IMigrationsAssembly migrationsAssembly,
        IAutoMigrationsHistoryRepository historyRepository,
        IDatabaseCreator databaseCreator,
        IMigrationsSqlGenerator migrationsSqlGenerator,
        IRawSqlCommandBuilder rawSqlCommandBuilder,
        IMigrationCommandExecutor migrationCommandExecutor,
        IRelationalConnection connection,
        ISqlGenerationHelper sqlGenerationHelper,
        IModelRuntimeInitializer modelRuntimeInitializer,
        IDiagnosticsLogger<DbLoggerCategory.Migrations> logger,
        IRelationalCommandDiagnosticsLogger commandLogger,
        IDesignTimeModel designTimeModel,
        IDatabaseProvider databaseProvider)
    {
        _currentContext = currentContext;
        _migrationsAssembly = migrationsAssembly;
        _historyRepository = historyRepository;
        _databaseCreator = (IRelationalDatabaseCreator)databaseCreator;
        _migrationsSqlGenerator = migrationsSqlGenerator;
        _rawSqlCommandBuilder = rawSqlCommandBuilder;
        _migrationCommandExecutor = migrationCommandExecutor;
        _connection = connection;
        _sqlGenerationHelper = sqlGenerationHelper;
        _modelRuntimeInitializer = modelRuntimeInitializer;
        _logger = logger;
        _commandLogger = commandLogger;
        _designTimeModel = designTimeModel;
        _activeProvider = databaseProvider.Name;

        var providerAssembly = databaseProvider.GetType().GenericTypeArguments[0].Assembly;
        var serviceCollection = new ServiceCollection()
            .AddEntityFrameworkDesignTimeServices()
            .AddDbContextDesignTimeServices(_currentContext.Context);
        ((IDesignTimeServices)Activator.CreateInstance(
                providerAssembly.GetType(
                    providerAssembly.GetCustomAttribute<DesignTimeProviderServicesAttribute>()!.TypeName,
                    throwOnError: true)!)!)
            .ConfigureDesignTimeServices(serviceCollection);
        using var services = serviceCollection.BuildServiceProvider(validateScopes: true);

        var migrationsScaffolderDependencies = services.CreateScope().ServiceProvider.GetRequiredService<MigrationsScaffolderDependencies>();

        _migrationsModelDiffer = migrationsScaffolderDependencies.MigrationsModelDiffer;
        _migrationsCodeGeneratorSelector = migrationsScaffolderDependencies.MigrationsCodeGeneratorSelector;
        _migrationsIdGenerator = migrationsScaffolderDependencies.MigrationsIdGenerator;
        _snapshotModelProcessor = migrationsScaffolderDependencies.SnapshotModelProcessor;
    }

    public IEnumerable<MigrationCommand> GetMigrations()
    {
        return GetDifferences(null, _designTimeModel.Model.GetRelationalModel());
    }

    public IEnumerable<MigrationCommand> GetAppliedMigrations()
    {
        var snapshots = _historyRepository.GetAppliedMigrations().Select(x => x.Snapshot);
        var relationalModel = MergeSnapshots(snapshots);
        return GetDifferences(null, relationalModel);
    }
    public async Task<IEnumerable<MigrationCommand>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default)
    {
        var snapshots = (await _historyRepository.GetAppliedMigrationsAsync()).Select(x => x.Snapshot);
        var relationalModel = MergeSnapshots(snapshots);
        return GetDifferences(null, relationalModel);
    }

    public IEnumerable<MigrationCommand> GetPendingMigrations()
    {
        var snapshots = _historyRepository.GetAppliedMigrations().Select(x => x.Snapshot);
        var relationalModel = MergeSnapshots(snapshots);
        return GetDifferences(relationalModel, _designTimeModel.Model.GetRelationalModel());
    }

    public async Task<IEnumerable<MigrationCommand>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        var snapshots = (await _historyRepository.GetAppliedMigrationsAsync()).Select(x => x.Snapshot);
        var relationalModel = MergeSnapshots(snapshots);
        return GetDifferences(relationalModel, _designTimeModel.Model.GetRelationalModel());
    }

    public void Migrate()
    {
        if (!_historyRepository.Exists())
        {
            if (!_databaseCreator.Exists())
            {
                _databaseCreator.Create();
            }

            var command = _rawSqlCommandBuilder.Build(
                _historyRepository.GetCreateScript());

            command.ExecuteNonQuery(
                new RelationalCommandParameterObject(
                    _connection,
                    null,
                    null,
                    _currentContext.Context,
                    _commandLogger, CommandSource.Migrations));
        }

        var commandLists = GetPendingMigrations();
        _migrationCommandExecutor.ExecuteNonQuery(commandLists, _connection);
    }
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        if (!await _historyRepository.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!await _databaseCreator.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                await _databaseCreator.CreateAsync(cancellationToken).ConfigureAwait(false);
            }

            var command = _rawSqlCommandBuilder.Build(
                _historyRepository.GetCreateScript());

            await command.ExecuteNonQueryAsync(
                    new RelationalCommandParameterObject(
                        _connection,
                        null,
                        null,
                        _currentContext.Context,
                        _commandLogger, CommandSource.Migrations),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var commandLists = await GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);
        await _migrationCommandExecutor.ExecuteNonQueryAsync(commandLists, _connection, cancellationToken).ConfigureAwait(false);
    }
    private RelationalModel MergeSnapshots(IEnumerable<byte[]> snapshots)
    {
        var currentRelationalModel = _designTimeModel.Model.GetRelationalModel();
        var tempRelationalModel = new RelationalModel(_designTimeModel.Model);
        foreach (var snapshot in snapshots)
        {
            var source = DecompressSource(snapshot);
            var modelSnapshot = CompileSnapshot(source);
            var model = modelSnapshot.Model;
            model = _snapshotModelProcessor.Process(model);
            var relationalModel = (RelationalModel)model.GetRelationalModel();
            foreach (var t in relationalModel.Tables)
            {
                if (t.Value.IsExcludedFromMigrations) continue;
                var table = currentRelationalModel.FindTable(t.Key.Item1, t.Key.Item2);
                if (table != null && !table.IsExcludedFromMigrations)
                    tempRelationalModel.Tables.TryAdd(t.Key, t.Value);
            }
            foreach (var t in relationalModel.Views)
            {
                var view = currentRelationalModel.FindView(t.Key.Item1, t.Key.Item2);
                if (view != null)
                    tempRelationalModel.Views.TryAdd(t.Key, t.Value);
            }
            foreach (var t in relationalModel.Queries)
            {
                var query = currentRelationalModel.FindQuery(t.Key);
                if (query != null)
                    tempRelationalModel.Queries.TryAdd(t.Key, t.Value);
            }
            foreach (var t in relationalModel.Functions)
            {
                var function = currentRelationalModel.FindFunction(t.Key.Item1, t.Key.Item2, t.Key.Item3);
                if (function != null)
                    tempRelationalModel.Functions.TryAdd(t.Key, t.Value);
            }
            foreach (var t in relationalModel.StoredProcedures)
            {
                var storedProcedure = currentRelationalModel.FindStoredProcedure(t.Key.Item1, t.Key.Item2);
                if (storedProcedure != null)
                    tempRelationalModel.StoredProcedures.TryAdd(t.Key, t.Value);
            }
        }
        return tempRelationalModel;
    }
    private IEnumerable<MigrationCommand> GetDifferences(IRelationalModel? oldModel, IRelationalModel newModel)
    {
        var operations = _migrationsModelDiffer
            .GetDifferences(oldModel, newModel)
            .Where(x => x is not UpdateDataOperation)
            .ToList();
        var commands = _migrationsSqlGenerator.Generate(operations, _designTimeModel.Model, MigrationsSqlGenerationOptions.Default);
        if (!commands.Any())
        {
            return commands;
        }
        return commands.Concat(new[] { GenerateHistorySql() });
    }
    private MigrationCommand GenerateHistorySql()
    {
        var contextName = _currentContext.Context.GetType().FullName;
        var migrationId = _migrationsIdGenerator.GenerateId(contextName);
        var model = _currentContext.Context.GetService<IDesignTimeModel>().Model;
        var source = _migrationsCodeGeneratorSelector.Select(null).GenerateSnapshot("AutoMigrations", _currentContext.Context.GetType(), contextName, model);
        var snapshot = CompressSource(source);
        var insertCommand = _rawSqlCommandBuilder.Build(
            _historyRepository.GetInsertScript(new AutoMigrationsHistory(migrationId, ProductInfo.GetVersion(), snapshot)));

        return new MigrationCommand(insertCommand, _currentContext.Context, _commandLogger);
    }
    private byte[] CompressSource(string source)
    {
        using var ms = new MemoryStream();
        using var gzs = new GZipStream(ms, CompressionLevel.Optimal, true);
        gzs.Write(Encoding.UTF8.GetBytes(source));
        gzs.Flush();
        ms.Seek(0, SeekOrigin.Begin);
        return ms.ToArray();
    }
    private string DecompressSource(byte[] source)
    {
        if (source == null) return null;
        using var ms = new MemoryStream(source);
        using var gzs = new GZipStream(ms, CompressionMode.Decompress);
        return new StreamReader(gzs).ReadToEnd();
    }
    private ModelSnapshot CompileSnapshot(string source)
    {
        var assembles = new List<Assembly>()
        {
            typeof(object).Assembly,
            typeof(DbContext).Assembly,
            typeof(ModelSnapshot).Assembly,
            typeof(AssemblyTargetedPatchBandAttribute).Assembly,
            typeof(DeleteBehavior).Assembly,
            _migrationsAssembly.Assembly,
            _currentContext.Context.GetType().Assembly
        }
        .Union(
            AppDomain.CurrentDomain.GetAssemblies()
            .Where(x =>
                x.GetName().Name.Contains(_currentContext.Context.Database.ProviderName) ||
                x.GetName().Name == "netstandard" ||
                x.GetName().Name == "System.Runtime" ||
                _migrationsAssembly.Assembly.GetReferencedAssemblies().Any(e => e.Name == x.GetName().Name))
        );

        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);

        var compileOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default);
        var compilation = CSharpCompilation.Create("Dynamic",
            new[] { SyntaxFactory.ParseSyntaxTree(source, options) },
            assembles.Select(a => MetadataReference.CreateFromFile(a.Location)),
            compileOptions
        );
        using var ms = new MemoryStream();
        var e = compilation.Emit(ms);
        if (!e.Success)
            throw new Exception("Compilation failed:" + string.Join(Environment.NewLine, e.Diagnostics.Select(d => d.ToString())));

        ms.Seek(0, SeekOrigin.Begin);

        var context = new AssemblyLoadContext(null, true);
        var assembly = context.LoadFromStream(ms);

        var modelType = assembly.DefinedTypes.Where(t => typeof(ModelSnapshot).IsAssignableFrom(t)).Single();

        return (ModelSnapshot)Activator.CreateInstance(modelType);
    }
}
