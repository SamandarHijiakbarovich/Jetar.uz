using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jetar.Core.Contracts;
using Jetar.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jetar.Tests.Integration;

/// <summary>
/// Haqiqiy HTTP quvuri (middleware, avtorizatsiya, kontrollerlar) bilan test qiladi.
/// Postgres o'rniga InMemory baza ishlatiladi, fon vazifalari o'chiriladi.
/// </summary>
public class JetarWebFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"jetar-it-{Guid.NewGuid()}";

    public const string TestJwtSecret = "integration_tests_secret_key_min_32_chars!!";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    static JetarWebFactory()
    {
        // Program.cs konfiguratsiyani host qurilishidan oldin o'qiydi (JWT validatsiya
        // parametrlari uchun), shuning uchun qiymatlarni muhit o'zgaruvchisi orqali beramiz.
        Environment.SetEnvironmentVariable("JETAR_Jwt__Secret", TestJwtSecret);
        Environment.SetEnvironmentVariable("JETAR_Database__AutoMigrate", "false");
        Environment.SetEnvironmentVariable("JETAR_Database__Seed", "false");
        Environment.SetEnvironmentVariable("JETAR_Payments__SandboxMode", "true");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:AutoMigrate"] = "false",
                ["Database:Seed"] = "false",
                ["Jwt:Secret"] = TestJwtSecret,
                ["Payments:SandboxMode"] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Postgres registratsiyasini butunlay olib tashlaymiz: EF bitta konteynerda
            // ikkita provayder xizmatlarini ko'rsa xato beradi.
            var npgsql = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(AppDbContext) ||
                    (d.ServiceType.FullName?.Contains("IDbContextOptionsConfiguration", StringComparison.Ordinal) ?? false) ||
                    (d.ServiceType.FullName?.Contains("Npgsql", StringComparison.Ordinal) ?? false) ||
                    (d.ImplementationType?.FullName?.Contains("Npgsql", StringComparison.Ordinal) ?? false))
                .ToList();

            foreach (var descriptor in npgsql) services.Remove(descriptor);

            // Fon vazifalari testda kerak emas.
            services.RemoveAll<IHostedService>();

            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }

    /// <summary>Yangi foydalanuvchi yaratib, token bilan tayyor klient qaytaradi.</summary>
    public async Task<(HttpClient Client, UserDto User)> CreateAuthenticatedClientAsync(
        string username, string phone)
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Test", "Foydalanuvchi", username, phone, "jetar123", null), Json);

        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(Json)
                   ?? throw new InvalidOperationException("Auth javobi bo'sh.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth.User);
    }
}
