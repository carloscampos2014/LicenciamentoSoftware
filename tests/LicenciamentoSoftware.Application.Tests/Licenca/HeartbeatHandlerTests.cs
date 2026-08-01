using FluentAssertions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Handlers;
using LicenciamentoSoftware.Application.Licenca.Results;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Licenca;

public class HeartbeatHandlerTests
{
    private readonly ILicencaSessaoRepository _sessaoRepo = Substitute.For<ILicencaSessaoRepository>();
    private readonly IValidacaoLogRepository  _logRepo    = Substitute.For<IValidacaoLogRepository>();

    private HeartbeatHandler CriarHandler() =>
        new(_sessaoRepo, _logRepo, Substitute.For<ILogger<HeartbeatHandler>>());

    private static readonly Guid IdLicenca = Guid.NewGuid();
    private static readonly Guid IdSessao  = Guid.NewGuid();

    private static SessaoResult SessaoAtiva() => new(
        IdSessao, IdLicenca, "usuario@teste.com",
        DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddMinutes(-5), Ativo: true);

    // -------------------------------------------------------------------------
    // Sessão não encontrada
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_SessaoNaoEncontrada_RetornaSessaoNaoEncontrada()
    {
        _sessaoRepo.BuscarPorIdAsync(IdSessao, Arg.Any<CancellationToken>())
            .Returns((SessaoResult?)null);

        var resultado = await CriarHandler().HandleAsync(
            new HeartbeatCommand(IdLicenca, IdSessao));

        resultado.Should().BeOfType<HeartbeatResult.SessaoNaoEncontrada>();
    }

    // -------------------------------------------------------------------------
    // Tenant isolation — sessão de outra licença
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_SessaoDeOutraLicenca_RetornaAcessoNegado()
    {
        var outraLicenca = Guid.NewGuid();
        _sessaoRepo.BuscarPorIdAsync(IdSessao, Arg.Any<CancellationToken>())
            .Returns(new SessaoResult(IdSessao, outraLicenca, "u@x.com",
                DateTime.UtcNow, DateTime.UtcNow, Ativo: true));

        var resultado = await CriarHandler().HandleAsync(
            new HeartbeatCommand(IdLicenca, IdSessao));

        resultado.Should().BeOfType<HeartbeatResult.AcessoNegado>();
    }

    // -------------------------------------------------------------------------
    // Sessão encerrada
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_SessaoJaEncerrada_RetornaSessaoEncerrada()
    {
        _sessaoRepo.BuscarPorIdAsync(IdSessao, Arg.Any<CancellationToken>())
            .Returns(SessaoAtiva() with { Ativo = false });

        var resultado = await CriarHandler().HandleAsync(
            new HeartbeatCommand(IdLicenca, IdSessao));

        resultado.Should().BeOfType<HeartbeatResult.SessaoEncerrada>();
    }

    // -------------------------------------------------------------------------
    // Sucesso
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_SessaoAtiva_AtualizaAtividadeERetornaSucesso()
    {
        _sessaoRepo.BuscarPorIdAsync(IdSessao, Arg.Any<CancellationToken>())
            .Returns(SessaoAtiva());

        var resultado = await CriarHandler().HandleAsync(
            new HeartbeatCommand(IdLicenca, IdSessao));

        resultado.Should().BeOfType<HeartbeatResult.Sucesso>();
        await _sessaoRepo.Received(1)
            .AtualizarAtividadeAsync(IdSessao, Arg.Any<CancellationToken>());
    }
}
