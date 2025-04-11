using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore.Migrations
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static DbContextOptionsBuilder UseAutoMigration(this DbContextOptionsBuilder optionsBuilder)
        {
            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(
                optionsBuilder.Options.FindExtension<AutoMigrationsOptionsExtension>() ?? new AutoMigrationsOptionsExtension());
            return optionsBuilder;
        }
    }
}
