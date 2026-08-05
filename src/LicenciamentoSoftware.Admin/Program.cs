using LicenciamentoSoftware.Admin;
using LicenciamentoSoftware.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// ── Porta exclusivamente localhost — nunca exposta pelo Nginx/ufw ─────────────
builder.WebHost.UseUrls("http://localhost:5020");

// ── Conexão com o banco ───────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurado.");

builder.Services.AddSingleton(new DbConnectionFactory(connectionString));

// ── HTTP Basic Auth ───────────────────────────────────────────────────────────
builder.Services.AddAuthentication("BasicAuth")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>("BasicAuth", null);

builder.Services.AddAuthorization();

// ── Repositório de métricas ───────────────────────────────────────────────────
builder.Services.AddScoped<AdminMetricasRepository>();

// ── HTTP client para verificar health dos serviços ───────────────────────────
builder.Services.AddHttpClient("health", c =>
{
    c.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", AdminController.Index).RequireAuthorization();
app.MapPost("/backup/executar", AdminController.ExecutarBackup).RequireAuthorization();
app.MapGet("/health", () => Results.Ok("Admin operacional"));

app.Run();
