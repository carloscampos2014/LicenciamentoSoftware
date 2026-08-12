using LicenciamentoSoftware.Admin;
using LicenciamentoSoftware.Application.Auth.Handlers;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Infrastructure.Email;
using LicenciamentoSoftware.Infrastructure.Persistence;
using LicenciamentoSoftware.Infrastructure.Persistence.Repositories;
using LicenciamentoSoftware.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// ── Porta exclusivamente localhost — nunca exposta pelo Nginx/ufw ─────────────
builder.WebHost.UseUrls("http://localhost:5020");

// ── Conexão com o banco (lazy — validada apenas ao usar) ──────────────────────
builder.Services.AddSingleton<DbConnectionFactory>(sp =>
{
    var cs = sp.GetRequiredService<IConfiguration>()
               .GetConnectionString("DefaultConnection")
               ?? throw new InvalidOperationException(
                   "ConnectionStrings:DefaultConnection não configurado. " +
                   "Defina a variável de ambiente ConnectionStrings__DefaultConnection.");
    return new DbConnectionFactory(cs);
});

// ── HTTP Basic Auth ───────────────────────────────────────────────────────────
builder.Services.AddAuthentication("BasicAuth")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>("BasicAuth", null);

builder.Services.AddAuthorization();

// ── Repositório de métricas ───────────────────────────────────────────────────
builder.Services.AddScoped<AdminMetricasRepository>();

// ── Dependências para o ResetarTotpAdminHandler ───────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ResetarTotpAdminHandler>();

// ── Dependências para os handlers de reset 2FA via solicitação ────────────────
builder.Services.AddScoped<ISolicitacaoReset2FARepository, SolicitacaoReset2FARepository>();

// Serviços de e-mail e infraestrutura necessários pelo AprovarReset2FAHandler
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddSingleton<IEmailTemplateRenderer, TemplateRenderer>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddSingleton<IClock, LicenciamentoSoftware.Infrastructure.Security.SystemClock>();

builder.Services.AddScoped<AprovarReset2FAHandler>();

// ── HTTP client para verificar health dos serviços ───────────────────────────
builder.Services.AddHttpClient("health", c =>
{
    c.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

// Banner de inicialização
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("=================================================");
Console.WriteLine("  LicenciamentoSoftware — Painel Admin");
Console.WriteLine($"  URL:    http://localhost:5020");
Console.WriteLine($"  Usuário: {builder.Configuration["Admin:Usuario"] ?? "admin"}");
Console.WriteLine("=================================================");
Console.ResetColor();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", AdminController.Index).RequireAuthorization();
app.MapPost("/backup/executar", AdminController.ExecutarBackup).RequireAuthorization();
app.MapGet("/usuarios", AdminController.ListarUsuarios).RequireAuthorization();
app.MapPost("/usuarios/{id}/reset-2fa", AdminController.ResetarTotp).RequireAuthorization();
app.MapGet("/reset-2fa/pendentes", AdminController.ListarSolicitacoesPendentes).RequireAuthorization();
app.MapPost("/reset-2fa/{id}/aprovar", AdminController.AprovarSolicitacaoReset).RequireAuthorization();
app.MapPost("/reset-2fa/{id}/rejeitar", AdminController.RejeitarSolicitacaoReset).RequireAuthorization();
app.MapGet("/validacoes",                AdminController.ListarValidacoes).RequireAuthorization();
app.MapGet("/sessoes",                   AdminController.ListarSessoes).RequireAuthorization();
app.MapPost("/sessoes/{id}/encerrar",    AdminController.EncerrarSessao).RequireAuthorization();
app.MapGet("/instalacoes",               AdminController.ListarInstalacoes).RequireAuthorization();
app.MapPost("/instalacoes/{id}/liberar", AdminController.LiberarInstalacao).RequireAuthorization();
app.MapGet("/health", () => Results.Ok("Admin operacional"));

app.Run();
