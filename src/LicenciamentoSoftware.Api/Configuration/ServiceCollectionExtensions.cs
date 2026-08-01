using LicenciamentoSoftware.Api.Middleware;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Aplicacao.Abstractions;
using LicenciamentoSoftware.Application.Aplicacao.Handlers;
using LicenciamentoSoftware.Application.Auth.Handlers;
using LicenciamentoSoftware.Application.Cliente.Abstractions;using LicenciamentoSoftware.Application.Cliente.Handlers;
using LicenciamentoSoftware.Application.ClienteFinal.Abstractions;
using LicenciamentoSoftware.Application.ClienteFinal.Handlers;
using LicenciamentoSoftware.Application.Jobs;
using LicenciamentoSoftware.Application.Dashboard.Abstractions;
using LicenciamentoSoftware.Application.Dashboard.Handlers;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Handlers;using LicenciamentoSoftware.Application.TipoLicenca.Abstractions;
using LicenciamentoSoftware.Application.TipoLicenca.Handlers;
using LicenciamentoSoftware.Application.Usuario.Abstractions;
using LicenciamentoSoftware.Application.Usuario.Handlers;
using LicenciamentoSoftware.Infrastructure.Email;
using LicenciamentoSoftware.Infrastructure.Identity;
using LicenciamentoSoftware.Infrastructure.Jobs;
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
        services.AddApiCors(configuration);
        services.AddApiHealthChecks();
        services.AddApiRateLimiting(configuration);
        services.AddAntiReplayOptions(configuration);
        services.AddInfrastructureServices(configuration);
        services.AddApplicationHandlers();
        services.AddGestaoHandlers();
        services.AddJobServices(configuration);

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
            // Por enquanto todos os usuários autenticados têm acesso total ao seu tenant.
            // A separação por papel (AdministradorCliente vs UsuarioFinal) será implementada
            // como feature separada quando o portal do cliente final for desenvolvido.
            var policyAutenticado = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy("AdministradorCliente",  policyAutenticado);
            options.AddPolicy("OperadorCliente",       policyAutenticado);
            options.AddPolicy("Leitor",                policyAutenticado);
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
        // Fase 5 — repositórios de gestão
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IUsuarioGestaoRepository, UsuarioGestaoRepository>();
        services.AddScoped<IClienteFinalRepository, ClienteFinalRepository>();
        services.AddScoped<IAplicacaoRepository, AplicacaoRepository>();
        services.AddScoped<ITipoLicencaRepository, TipoLicencaRepository>();
        // Fase 6 — repositórios de licença
        services.AddScoped<ILicencaGestaoRepository, LicencaGestaoRepository>();
        services.AddScoped<ILicencaSessaoRepository, LicencaSessaoRepository>();
        services.AddScoped<ILicencaInstalacaoRepository, LicencaInstalacaoRepository>();
        // Fase 7 — repositório de validação
        services.AddScoped<IValidacaoLicencaRepository, ValidacaoLicencaRepository>();

        // Fase 9.1 — repositório de log de validação
        services.AddScoped<IValidacaoLogRepository, ValidacaoLogRepository>();

        // Fase 9.1 — repositório do dashboard
        services.AddScoped<IDashboardRepository, DashboardRepository>();

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
        services.AddScoped<AutoCadastrarClienteHandler>();

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

    private static IServiceCollection AddGestaoHandlers(
        this IServiceCollection services)
    {
        // Cliente
        services.AddScoped<CriarClienteHandler>();
        services.AddScoped<AtualizarClienteHandler>();
        services.AddScoped<DesativarClienteHandler>();
        services.AddScoped<BuscarClientePorIdHandler>();
        services.AddScoped<ListarClientesHandler>();
        // Usuario
        services.AddScoped<CriarUsuarioHandler>();
        services.AddScoped<AtualizarUsuarioHandler>();
        services.AddScoped<DesativarUsuarioHandler>();
        services.AddScoped<BuscarUsuarioPorIdHandler>();
        services.AddScoped<ListarUsuariosHandler>();
        // ClienteFinal
        services.AddScoped<CriarClienteFinalHandler>();
        services.AddScoped<AtualizarClienteFinalHandler>();
        services.AddScoped<DesativarClienteFinalHandler>();
        services.AddScoped<BuscarClienteFinalPorIdHandler>();
        services.AddScoped<ListarClientesFinaisHandler>();
        // Aplicacao
        services.AddScoped<CriarAplicacaoHandler>();
        services.AddScoped<AtualizarAplicacaoHandler>();
        services.AddScoped<DesativarAplicacaoHandler>();
        services.AddScoped<BuscarAplicacaoPorIdHandler>();
        services.AddScoped<ListarAplicacoesHandler>();
        // TipoLicenca
        services.AddScoped<ListarTiposLicencaHandler>();
        services.AddScoped<BuscarTipoLicencaPorIdHandler>();

        // Fase 6 — handlers de licença
        services.AddScoped<EmitirLicencaHandler>();
        services.AddScoped<BuscarLicencaPorIdHandler>();
        services.AddScoped<ListarLicencasHandler>();
        services.AddScoped<DesativarLicencaHandler>();
        services.AddScoped<RenovarPeriodoHandler>();
        services.AddScoped<EncerrarSessaoHandler>();
        services.AddScoped<LiberarInstalacaoHandler>();

        // Fase 9.1 — handlers do dashboard
        services.AddScoped<BuscarDashboardResumoHandler>();
        services.AddScoped<BuscarDashboardAlertasHandler>();

        // Fase 7 — handlers de validação
        services.AddScoped<ValidarLoginHandler>();
        services.AddScoped<HeartbeatHandler>();
        services.AddScoped<LogoutValidacaoHandler>();
        services.AddScoped<ValidarInstalacaoHandler>();
        return services;
    }

    private static IServiceCollection AddApiCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var origensPermitidas = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            // Política para o BFF (Web.Server) — permite cookies e JWT
            options.AddPolicy("BffPolicy", policy =>
            {
                if (origensPermitidas.Length > 0)
                    policy.WithOrigins(origensPermitidas);
                else
                    policy.SetIsOriginAllowed(_ => true); // fallback dev sem config

                policy.AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials(); // necessário para cookies HttpOnly via BFF
            });
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

    private static IServiceCollection AddJobServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configurações de jobs e e-mail
        services.Configure<JobSettings>(configuration.GetSection("JobSettings"));
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        // Serviços de e-mail
        services.AddSingleton<IEmailTemplateRenderer, TemplateRenderer>();
        services.AddScoped<IEmailService, SmtpEmailService>();

        // Jobs — scoped para que repositórios sejam resolvidos por escopo de execução
        var jobSettings = configuration.GetSection("JobSettings").Get<JobSettings>() ?? new JobSettings();

        services.AddScoped<EncerrarSessoesInativasJob>(sp =>
            new EncerrarSessoesInativasJob(
                sp.GetRequiredService<ILicencaSessaoRepository>(),
                sp.GetRequiredService<IClock>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<EncerrarSessoesInativasJob>>(),
                jobSettings.SessoesInativasLimiteHoras));

        services.AddScoped<ExpirarLicencasPeriodoJob>(sp =>
            new ExpirarLicencasPeriodoJob(
                sp.GetRequiredService<ILicencaGestaoRepository>(),
                sp.GetRequiredService<IClock>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ExpirarLicencasPeriodoJob>>()));

        services.AddScoped<RenovarLicencasAutomaticasJob>(sp =>
            new RenovarLicencasAutomaticasJob(
                sp.GetRequiredService<ILicencaGestaoRepository>(),
                sp.GetRequiredService<IClock>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RenovarLicencasAutomaticasJob>>(),
                jobSettings.DiasAntecedenciaNotificacao));

        services.AddScoped<RotacionarTokensLicencaJob>(sp =>
            new RotacionarTokensLicencaJob(
                sp.GetRequiredService<ILicencaTokenRepository>(),
                sp.GetRequiredService<RenovarTokenLicencaHandler>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RotacionarTokensLicencaJob>>(),
                jobSettings.DiasAntecedenciaNotificacao));

        services.AddScoped<NotificarExpiracaoJob>(sp =>
            new NotificarExpiracaoJob(
                sp.GetRequiredService<ILicencaGestaoRepository>(),
                sp.GetRequiredService<ILicencaTokenRepository>(),
                sp.GetRequiredService<IUsuarioRepository>(),
                sp.GetRequiredService<IEmailService>(),
                sp.GetRequiredService<IEmailTemplateRenderer>(),
                sp.GetRequiredService<IClock>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<NotificarExpiracaoJob>>(),
                jobSettings.DiasAntecedenciaNotificacao));

        // BackgroundService orquestrador
        services.AddHostedService<JobScheduler>();

        return services;
    }
}
