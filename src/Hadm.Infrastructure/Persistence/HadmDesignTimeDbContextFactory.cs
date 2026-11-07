using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hadm.Infrastructure.Persistence;

public sealed class HadmDesignTimeDbContextFactory : IDesignTimeDbContextFactory<HadmDbContext>
{
    public HadmDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Hadm")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=Hadm;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<HadmDbContext>().UseSqlServer(connection).Options;
        return new HadmDbContext(options);
    }
}
