Auto migration for EntityFrameworkCore8.0+,only relational databases are supported.

# Usage
## 1. Enable auto migration
Just call extension method DbContextOptionsBuilder.UseAutoMigration() to enable auto migration.
``` csharp
services.AddDbContext<MyDbContext>(options =>
{
    options.UseNpgsql("Host=;Username=;Password=;Database=");
    options.UseAutoMigration();
});
```
or
``` csharp
public class MyDbContext : DbContext
{
   protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
   {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseAutoMigration();
    }
}
```
## 2. Execute migration
Must call DatabaseFacade.AutoMigrate() or DatabaseFacade.AutoMigrateAsync() to execute migration.
``` csharp
var ctx = _serviceProvider.GetRequiredService<MyDbContext>();
await ctx.Database.AutoMigrateAsync();
```
Or
``` csharp
var ctx = _serviceProvider.GetRequiredService<MyDbContext>();
await ctx.GetService<IRelationalAutoMigrator>().MigrateAsync();
```
## 3. Other useful methods
``` csharp
var ctx = _serviceProvider.GetRequiredService<MyDbContext>();
var migrations = ctx.Database.GetMigrations();// get migrations of current dbcontext model.
var migrations = await ctx.Database.GetAppliedMigrationsAsync();// get migrations of applied dbcontext model.
var migrations = await ctx.Database.GetPendingMigrationsAsync();// get migrations of pending dbcontext model.
```

