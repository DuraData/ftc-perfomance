using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FTCERP.Host.Infrastructure.Persistence;

public sealed class ApplicationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string DesignTimeConnectionString =
        "Server=localhost;Database=OPMS_DesignTime;Integrated Security=true;TrustServerCertificate=true";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(DesignTimeConnectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}
