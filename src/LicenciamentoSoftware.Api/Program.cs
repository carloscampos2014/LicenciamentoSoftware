using LicenciamentoSoftware.Api.Configuration;
using Serilog;

// Configura Serilog antes de qualquer coisa para capturar erros de startup.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando LicenciamentoSoftware.Api");

    var builder = WebApplication.CreateBuilder(args);

    // --- Logging ---
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration)
                     .ReadFrom.Services(services)
                     .Enrich.FromLogContext()
                     .Enrich.WithMachineName()
                     .Enrich.WithEnvironmentName());

    // --- Serviços da API ---
    builder.Services.AddApiServices(builder.Configuration);

    var app = builder.Build();

    // --- Pipeline HTTP ---
    app.UseDatabaseMigrations();
    app.UseApiPipeline();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "API encerrada inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}

// Necessário para WebApplicationFactory nos testes de integração.
public partial class Program { }
