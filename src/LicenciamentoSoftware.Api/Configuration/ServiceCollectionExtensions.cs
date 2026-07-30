using LicenciamentoSoftware.Api.Middleware;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Handlers;
using LicenciamentoSoftware.Application.Licenca.Handlers;
using LicenciamentoSoftware.Infrastructure.Identity;
using LicenciamentoSoftware.Infrastructure.Persistence;
using LicenciamentoSoftware.Infrastructure.Persistence.Repositories;
using LicenciamentoSoftware.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

namespace LicenciamentoSoftware.Api.Configuration;

/// <summary>
/// Extensões de registro de serviços da API.
/// Mantém o Program.cs limpo — cada grupo de serviços tem seu próprio método.
/// </summary>
internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddHttpContextAccessor();

        // ProblemDetails + handler centralizado de exceções não tratadas.
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddApiAuthentication(configuration);
        services.AddApiAuthorization();
        services.AddApiHealthChecks();
        services.AddApiRateLimiting(configuration);
        services.AddAntiReplayOptions(configuration);
        services.AddInfrastructureServices(configuration);
        services.AddApplicationHandlers();

        return services;
    }

    private static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var secret = configuration["JwtSettings:Secret"] ?? string.Empty;
        var emissor = configuration["JwtSettings:Emissor"] ?? "LicenciamentoSoftware";
        var audiencia = configuration["JwtSettings:Audiencia"] ?? "LicenciamentoSoftware";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    // Lê o secret em tempo de validação via IssuerSigningKeyResolver
                    // para evitar falha na startup quando secret está vazio no appsettings base
                    IssuerSigningKeyResolver = (_, _, _, _) =>
                    {
                        var currentSecret = configuration["JwtSettings:Secret"] ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(currentSecret))
                            return [];
                        return [new SymmetricSecurityKey(Encoding.UTF8.GetBytes(currentSecret))];
                    },
                    ValidateIssuer = true,
                    ValidIssuer = emissor,
                    ValidateAudience = true,
                    ValidAudience = audiencia,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };
            });

        return services;
    }

    private static IServiceCollection AddApiAuthorization(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdministradorPlataforma",
                p => p.RequireRole("AdministradorPlataforma"));
            options.AddPolicy("AdministradorCliente",
                p => p.RequireRole("AdministradorPlataforma", "AdministradorCliente"));
            options.AddPolicy("OperadorCliente",
                p => p.RequireRole("AdministradorPlataforma", "AdministradorCliente", "OperadorCliente"));
            options.AddPolicy("Leitor",
                p => p.RequireRole("AdministradorPlataforma", "AdministradorCliente", "OperadorCliente", "Leitor"));
        });

        return services;
    }

    private static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbConnectionFactory — usa IConfiguration para ler a connection string em tempo de uso
        // Necessário para suportar injeção de config em testes sem registrar a string no startup
        services.AddSingleton<DbConnectionFactory>(sp =>
        {
            var cs = sp.GetRequiredService<IConfiguration>()
                .GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurado.");
            return new DbConnectionFactory(cs);
        });

        // UnitOfWork scoped — uma transação por request
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositórios
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ILicencaRepository, LicencaRepository>();
        services.AddScoped<ILicencaTokenRepository, LicencaTokenRepository>();
        services.AddScoped<INonceRepository, NonceRepository>();

        // Auditoria
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();

        // Segurança
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ITotpService, TotpService>();
        services.AddSingleton<IHmacLicencaTokenService, HmacLicencaTokenService>();
        services.AddSingleton<IClock, SystemClock>();

        // ICurrentUser — lê claims do HttpContext
        services.AddScoped<ICurrentUser, CurrentUser>();

        // DatabaseMigrator — também lazy via IConfiguration
        services.AddSingleton<DatabaseMigrator>(sp =>
        {
            var cs = sp.GetRequiredService<IConfiguration>()
                .GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurado.");
            return new DatabaseMigrator(cs);
        });

        return services;
    }

    private static IServiceCollection AddApplicationHandlers(
        this IServiceCollection services)
    {
        services.AddScoped<LoginHandler>();
        services.AddScoped<VerificarTotpHandler>();
        services.AddScoped<RefreshTokenHandler>();
        services.AddScoped<LogoutHandler>();
        services.AddScoped<RegistrarUsuarioHandler>();
        services.AddScoped<ConfigurarTotpHandler>();

        // Handlers Fase 4 — resolve defaultExpiracaoMinutos a partir de IConfiguration
        services.AddScoped<EmitirTokenLicencaHandler>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var expiracaoMinutos = int.TryParse(
                config["LicencaTokenSettings:DefaultExpiracaoMinutos"], out var min) ? min : 525600;
            return new EmitirTokenLicencaHandler(
                sp.GetRequiredService<ILicencaRepository>(),
                sp.GetRequiredService<ILicencaTokenRepository>(),
                sp.GetRequiredService<IHmacLicencaTokenService>(),
                sp.GetRequiredService<IUnitOfWork>(),
                expiracaoMinutos);
        });

        services.AddScoped<RenovarTokenLicencaHandler>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var expiracaoMinutos = int.TryParse(
                config["LicencaTokenSettings:DefaultExpiracaoMinutos"], out var min) ? min : 525600;
            return new RenovarTokenLicencaHandler(
                sp.GetRequiredService<ILicencaRepository>(),
                sp.GetRequiredService<ILicencaTokenRepository>(),
                sp.GetRequiredService<IHmacLicencaTokenService>(),
                sp.GetRequiredService<IUnitOfWork>(),
                expiracaoMinutos);
        });

        return services;
    }

    private static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var requisicoesPorMinuto = int.TryParse(
            configuration["RateLimiting:ValidacaoRequisicoesPorMinuto"], out var rpm) ? rpm : 60;

        services.AddRateLimiter(options =>
        {
            // Política para endpoints de validação — sliding window por IP
            options.AddSlidingWindowLimiter("validacao", limiterOptions =>
            {
                limiterOptions.PermitLimit = requisicoesPorMinuto;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.SegmentsPerWindow = 6; // janelas de 10 segundos
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }

    private static IServiceCollection AddAntiReplayOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AntiReplayOptions>(opts =>
        {
            opts.JanelaMinutos = int.TryParse(
                configuration["LicencaTokenSettings:AntiReplayJanelaMinutos"], out var min) ? min : 5;
        });

        return services;
    }

    private static IServiceCollection AddApiHealthChecks(
        this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("API operacional"));

        return services;
    }
}
