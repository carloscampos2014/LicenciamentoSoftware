using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Jobs;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Jobs;

public class NotificarExpiracaoJobTests
{
    private readonly ILicencaGestaoRepository  _licencaRepo     = Substitute.For<ILicencaGestaoRepository>();
    private readonly ILicencaTokenRepository   _tokenRepo       = Substitute.For<ILicencaTokenRepository>();
    private readonly IUsuarioRepository        _usuarioRepo     = Substitute.For<IUsuarioRepository>();
    private readonly IEmailService             _email           = Substitute.For<IEmailService>();
    private readonly IEmailTemplateRenderer    _renderer        = Substitute.For<IEmailTemplateRenderer>();
    private readonly IClock                    _clock           = Substitute.For<IClock>();

    private NotificarExpiracaoJob CriarJob(int diasAntecedencia = 7) =>
        new(_licencaRepo, _tokenRepo, _usuarioRepo, _email, _renderer, _clock,
            NullLogger<NotificarExpiracaoJob>.Instance, diasAntecedencia);

    private static readonly DateTime Agora =
        new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid IdCliente = Guid.NewGuid();

    private static readonly AdminClienteInfo Admin =
        new(IdCliente, "admin@empresa.com", "Carlos Admin");

    // -------------------------------------------------------------------------
    // Notificações de licença
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Execute_LicencaProximaVencimento_EnviaEmail()
    {
        _clock.UtcNow.Returns(Agora);
        _renderer.Renderizar(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>())
            .Returns("<html>corpo</html>");

        var licenca = new LicencaPeriodoJobInfo(
            Guid.NewGuid(), IdCliente, "App Teste",
            Agora.AddYears(-1), Agora.AddDays(5), false);

        _licencaRepo.BuscarLicencasProximasVencimentoAsync(Agora, 7, Arg.Any<CancellationToken>())
            .Returns(new List<LicencaPeriodoJobInfo> { licenca });
        _licencaRepo.BuscarLicencasPeriodoVencidasAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<LicencaPeriodoJobInfo>());
        _tokenRepo.BuscarTokensProximosVencimentoAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<LicencaTokenJobInfo>());
        _usuarioRepo.BuscarEmailAdminPorClienteAsync(IdCliente, Arg.Any<CancellationToken>())
            .Returns(Admin);

        await CriarJob().ExecuteAsync();

        await _email.Received(1).EnviarAsync(
            "admin@empresa.com",
            Arg.Is<string>(s => s.Contains("App Teste")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_SemAdminParaCliente_NaoEnviaEmail()
    {
        _clock.UtcNow.Returns(Agora);

        var licenca = new LicencaPeriodoJobInfo(
            Guid.NewGuid(), IdCliente, "App Sem Admin",
            Agora.AddYears(-1), Agora.AddDays(3), false);

        _licencaRepo.BuscarLicencasProximasVencimentoAsync(Agora, 7, Arg.Any<CancellationToken>())
            .Returns(new List<LicencaPeriodoJobInfo> { licenca });
        _licencaRepo.BuscarLicencasPeriodoVencidasAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<LicencaPeriodoJobInfo>());
        _tokenRepo.BuscarTokensProximosVencimentoAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<LicencaTokenJobInfo>());

        // Admin não encontrado
        _usuarioRepo.BuscarEmailAdminPorClienteAsync(IdCliente, Arg.Any<CancellationToken>())
            .Returns((AdminClienteInfo?)null);

        await CriarJob().ExecuteAsync();

        await _email.DidNotReceive().EnviarAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Notificações de token
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Execute_TokenProximoVencimento_EnviaEmail()
    {
        _clock.UtcNow.Returns(Agora);
        _renderer.Renderizar(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>())
            .Returns("<html>token</html>");

        var token = new LicencaTokenJobInfo(
            Guid.NewGuid(), Guid.NewGuid(), IdCliente, "App Token",
            ExpiracaoMinutos: 525600,
            CriadoEm: Agora.AddDays(-358), Ativo: true);

        _licencaRepo.BuscarLicencasProximasVencimentoAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<LicencaPeriodoJobInfo>());
        _licencaRepo.BuscarLicencasPeriodoVencidasAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<LicencaPeriodoJobInfo>());
        _tokenRepo.BuscarTokensProximosVencimentoAsync(7, Arg.Any<CancellationToken>())
            .Returns(new List<LicencaTokenJobInfo> { token });
        _usuarioRepo.BuscarEmailAdminPorClienteAsync(IdCliente, Arg.Any<CancellationToken>())
            .Returns(Admin);

        await CriarJob().ExecuteAsync();

        await _email.Received(1).EnviarAsync(
            "admin@empresa.com",
            Arg.Is<string>(s => s.Contains("App Token")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_FalhaEnvioEmail_NaoPropagaExcecao()
    {
        _clock.UtcNow.Returns(Agora);

        var licenca = new LicencaPeriodoJobInfo(
            Guid.NewGuid(), IdCliente, "App Falha",
            Agora.AddYears(-1), Agora.AddDays(2), false);

        _licencaRepo.BuscarLicencasProximasVencimentoAsync(Agora, 7, Arg.Any<CancellationToken>())
            .Returns(new List<LicencaPeriodoJobInfo> { licenca });
        _licencaRepo.BuscarLicencasPeriodoVencidasAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<LicencaPeriodoJobInfo>());
        _tokenRepo.BuscarTokensProximosVencimentoAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<LicencaTokenJobInfo>());
        _usuarioRepo.BuscarEmailAdminPorClienteAsync(IdCliente, Arg.Any<CancellationToken>())
            .Returns(Admin);
        _renderer.Renderizar(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>())
            .Returns("<html/>");

        // Simula falha no envio de e-mail
        _email.EnviarAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("SMTP indisponível")));

        // Job deve absorver a exceção (fire-and-forget por licença)
        var act = () => CriarJob().ExecuteAsync();
        await act.Should().NotThrowAsync();
    }
}
