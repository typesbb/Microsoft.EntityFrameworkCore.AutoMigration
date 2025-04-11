using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore.Migrations;

public class AutoMigrationsHistory
{
    public AutoMigrationsHistory(string migrationId, string productVersion, byte[] snapshot)
    {
        MigrationId = migrationId;
        ProductVersion = productVersion;
        Snapshot = snapshot;
    }
    public virtual string MigrationId { get; }
    public virtual string ProductVersion { get; }
    public virtual byte[] Snapshot { get; }
}
