using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NovaDrive.Infrastructure.Persistence;

/// <summary>
/// Used only by dotnet-ef CLI tooling (migrations add / migrations script).
/// A real connection string is not required to generate migrations.
/// </summary>
public class NovaDriveDbContextFactory : IDesignTimeDbContextFactory<NovaDriveDbContext>
{
    public NovaDriveDbContext CreateDbContext(string[] args)
    {
        var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5433;Database=novadrive;Username=novadrive;Password=novadrive123";

        var options = new DbContextOptionsBuilder<NovaDriveDbContext>()
            .UseNpgsql(connStr)
            .Options;

        return new NovaDriveDbContext(options);
    }
}
