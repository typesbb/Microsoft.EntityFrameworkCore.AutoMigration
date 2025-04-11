using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.AutoMigration.Test
{
    public class UnitTest1
    {
        private readonly ServiceProvider _serviceProvider;
        public UnitTest1()
        {
            var services = new ServiceCollection();

            services.AddDbContext<TextDbContext>(options =>
            {
                options.UseNpgsql("Host=localhost;Username=postgres;Password=123456;Database=test");
                options.UseAutoMigration();
            });

            _serviceProvider = services.BuildServiceProvider();
        }
        [Fact]
        public async Task Test1Async()
        {
            var ctx = _serviceProvider.GetRequiredService<TextDbContext>();
            var m1 = ctx.Database.GetAutoMigrations();
            await ctx.Database.AutoMigrateAsync();


            var databaseCreator = ctx.GetService<IRelationalDatabaseCreator>();
            var m = ctx.Database.GetMigrations();
            await ctx.GetService<IMigrator>().MigrateAsync();
            //databaseCreator.GenerateCreateScript
            //ctx.Database.GetPendingMigrations
            //ctx.Database.GetRelationalService<IMigrationsAssembly>()
        }
    }
}