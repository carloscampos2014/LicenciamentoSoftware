using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Jobs;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Handlers;
using LicenciamentoSoftware.Application.Licenca.Results;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Jobs;

public class RotacionarTokensJobTests
{
    private readonly ILicencaTokenRepository _tokenRepo = Substitute.For<ILicencaTokenRepository>();
    private readonly ILicencaRepository _licencaRepo = Substitute.For<ILicencaRepository>();
    private readonly ILicencaTokenRepository _tokenRepoHandler = Substitute.For<ILicencaTokenRepository>();
    private readonly IHmacLicencaTokenService _hmac = Substitute.For<IHmacLicencaTokenService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private RenovarTokenLicencaHandler CriarRenovarHandler() =>
        new(_licencaRepo, _tokenRepoHandler, _hmac, _uow);

    private RotacionarTokensLicencaJob CriarJob(int diasAntecedencia = 7) =>
        new(_tokenRepo, CriarRenovarHandler(),
            NullLogger<RotacionarTokensLicencaJob>.Instance, diasAntecedencia);

    private static readonly Guid IdLicenca = Guid.NewGuid();

    [Fact]
    public async Task Execute_TokenProximoDoVencimento_RenovaToken()
    {
        var token = new LicencaTokenJobInfo(
            Guid.NewGuid(), IdLicenca, Guid.NewGuid(),
            "App Z", ExpiracaoMinutos: 525600,
            CriadoEm: DateTime.UtcNow.AddDays(-360), Ativo: true);

        _tokenRepo.BuscarTokensProximosVencimentoAsync(7, Arg.Any<CancellationToken>())
            .Returns(new List<LicencaTokenJobInfo> { token });

        // Configura o handler de renovação para retornar sucesso
        _licencaRepo.BuscarPorIdAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(new LicencaInfo(IdLicenca, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), true));
        _tokenRepoHandler.BuscarAtivoporLicencaAsync(IdLicenca, Arg.Any<CancellationToken>())
            .Returns(new LicencaTokenInfo(token.IdToken, IdLicenca, "hash", 525600, DateTime.UtcNow, true));
        _hmac.GerarSegredo().Returns("novo-segredo-gerado");
        _hmac.HashSegredo(Arg.Any<string>()).Returns("hash-novo");

        await CriarJob().ExecuteAsync();

        await _tokenRepo.Received(1)
            .BuscarTokensProximosVencimentoAsync(7, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_NenhumTokenProximo_NaoRenova()
    {
        _tokenRepo.BuscarTokensProximosVencimentoAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<LicencaTokenJobInfo>());

        await CriarJob().ExecuteAsync();

        // Nenhuma interação com o handler de renovação
        await _licencaRepo.DidNotReceive()
            .BuscarPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
