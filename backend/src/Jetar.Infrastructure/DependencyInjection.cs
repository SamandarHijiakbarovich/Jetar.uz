using Jetar.Core.Interfaces;
using Jetar.Core.Options;
using Jetar.Infrastructure.Data;
using Jetar.Infrastructure.External;
using Jetar.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jetar.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddJetarInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // ── Sozlamalar ────────────────────────────────────────────────────
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        services.Configure<PlatformOptions>(config.GetSection(PlatformOptions.SectionName));
        services.Configure<PaymentOptions>(config.GetSection(PaymentOptions.SectionName));
        services.Configure<StorageOptions>(config.GetSection(StorageOptions.SectionName));
        services.Configure<TelegramOptions>(config.GetSection(TelegramOptions.SectionName));

        // ── Ma'lumotlar bazasi ────────────────────────────────────────────
        var connection = config.GetConnectionString("Postgres")
                         ?? "Host=localhost;Port=5432;Database=jetar;Username=jetar;Password=jetar";

        services.AddDbContext<AppDbContext>(o =>
        {
            o.UseNpgsql(connection, npgsql => npgsql.EnableRetryOnFailure(3));
            if (config.GetValue<bool>("Database:DetailedErrors")) o.EnableDetailedErrors().EnableSensitiveDataLogging();
        });

        // ── Kesh: Redis mavjud bo'lsa o'sha, aks holda xotira ──────────────
        var redis = config.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redis))
        {
            services.AddStackExchangeRedisCache(o =>
            {
                o.Configuration = redis;
                o.InstanceName = "jetar:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddHttpClient();

        // ── Servislar ─────────────────────────────────────────────────────
        services.AddSingleton<IClock, SystemClock>();

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IListingService, ListingService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IEscrowService, EscrowService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IRatingService, RatingService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        // SignalR ulanmagan bo'lsa chat baribir ishlaydi (API qatlami buni almashtiradi).
        services.AddSingleton<IChatBroadcaster, NullChatBroadcaster>();

        // ── To'lov provayderlari ──────────────────────────────────────────
        services.AddScoped<IPaymentGateway, ClickGateway>();
        services.AddScoped<IPaymentGateway, PaymeGateway>();
        services.AddScoped<IPaymentGateway, UzumGateway>();
        services.AddScoped<IPaymentGateway, ApelsinGateway>();

        // ── Fon vazifalari ────────────────────────────────────────────────
        services.AddHostedService<EscrowBackgroundWorker>();

        return services;
    }
}
