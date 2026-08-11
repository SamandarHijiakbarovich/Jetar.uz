using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jetar.Infrastructure.Data;

/// <summary>
/// <c>dotnet ef migrations add ...</c> uchun. Jonli baza kerak emas —
/// faqat provayder turi muhim.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("JETAR_ConnectionStrings__Postgres")
                         ?? "Host=localhost;Port=5432;Database=jetar;Username=jetar;Password=jetar";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection)
            .Options;

        return new AppDbContext(options);
    }
}
