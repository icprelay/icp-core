using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Icp.Persistence.Data.Runtime;

public sealed class RuntimeDbContextFactory
    : IDesignTimeDbContextFactory<RuntimeDbContext>
{
    public RuntimeDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RuntimeDbContext>()
            // Any SQL Server string is fine for design-time script generation
            .UseSqlServer("Server=.;Database=icprelay-runtime-db;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new RuntimeDbContext(options);
    }
}
