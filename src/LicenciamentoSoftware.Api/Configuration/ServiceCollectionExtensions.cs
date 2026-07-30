using LicenciamentoSoftware.Api.Middleware;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Handlers;
using LicenciamentoSoftware.Infrastructure.Identity;
using LicenciamentoSoftware.Infrastructure.Persistence;
using LicenciamentoSoftware.Infrastructure.Persistence.Repositories;
using LicenciamentoSoftware.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
        services.AddInfrastructureServices(configuration);
        services.AddApplicationHandlers();

        return services;
    }

    private static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var secret = configuration["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("JwtSettings:Secret não configurado.");

        var emissor = configuration["JwtSettings:Emissor"] ?? "LicenciamentoSoftware";
        var audiencia = configuration["JwtSettings:Audiencia"] ?? "LicenciamentoSoftware";
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = chave,
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
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurado.");

        // DbConnectionFactory singleton — cria conexões Npgsql
        services.AddSingleton(new DbConnectionFactory(connectionString));

        // UnitOfWork scoped — uma transação por request
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositórios
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // Auditoria
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();

        // Segurança
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ITotpService, TotpService>();
        services.AddSingleton<IClock, SystemClock>();

        // ICurrentUser — lê claims do HttpContext
        services.AddScoped<ICurrentUser, CurrentUser>();

        // Aplica migrations na inicialização
        services.AddSingleton(_ => new DatabaseMigrator(connectionString));

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
