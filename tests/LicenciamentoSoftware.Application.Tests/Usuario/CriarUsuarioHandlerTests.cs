using FluentAssertions;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Usuario.Abstractions;
using LicenciamentoSoftware.Application.Usuario.Commands;
using LicenciamentoSoftware.Application.Usuario.Handlers;
using LicenciamentoSoftware.Application.Usuario.Results;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Usuario;

public class CriarUsuarioHandlerTests
{
    private readonly IUsuarioGestaoRepository _repo = Substitute.For<IUsuarioGestaoRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();

    private CriarUsuarioHandler CriarHandler() => new(_repo, _uow, _hasher);

    private static CriarUsuarioCommand CommandValido() => new(
        Guid.NewGuid(), "João Silva", "joao@empresa.com", "Senha@123", "OperadorCliente");

    [Fact]
    public async Task Handle_DadosInvalidos_RetornaInvalido()
    {
        var command = CommandValido() with { Nome = "" };

        var resultado = await CriarHandler().HandleAsync(command);

        resultado.Should().BeOfType<CriarUsuarioResult.Invalido>();
    }

    [Fact]
    public async Task Handle_SenhaFraca_RetornaInvalido()
    {
        var command = CommandValido() with { Senha = "123" };

        var resultado = await CriarHandler().HandleAsync(command);

        resultado.Should().BeOfType<CriarUsuarioResult.Invalido>();
    }

    [Fact]
    public async Task Handle_EmailDuplicado_RetornaEmailJaExiste()
    {
        _repo.ExisteEmailAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
             .Returns(true);

        var resultado = await CriarHandler().HandleAsync(CommandValido());

        resultado.Should().BeOfType<CriarUsuarioResult.EmailJaExiste>();
    }

    [Fact]
    public async Task Handle_Sucesso_HasheSenhaEPersiste()
    {
        _repo.ExisteEmailAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
             .Returns(false);
        _hasher.Hash(Arg.Any<string>()).Returns("$2a$12$hash");
        _repo.InserirAsync(Arg.Any<Domain.Entities.Usuario>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
             .Returns(x => ((Domain.Entities.Usuario)x[0]).Id);

        var resultado = await CriarHandler().HandleAsync(CommandValido());

        resultado.Should().BeOfType<CriarUsuarioResult.Sucesso>();
        _hasher.Received(1).Hash("Senha@123");
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PapelInvalido_RetornaInvalido()
    {
        var command = CommandValido() with { Papel = "PapelInexistente" };

        var resultado = await CriarHandler().HandleAsync(command);

        resultado.Should().BeOfType<CriarUsuarioResult.Invalido>();
    }
}
