using FluentAssertions;
using LicenciamentoSoftware.Domain.Entities;
using LicenciamentoSoftware.Domain.Enums;
using LicenciamentoSoftware.Domain.Exceptions;
using LicenciamentoSoftware.Domain.ValueObjects;

namespace LicenciamentoSoftware.Domain.Tests.Entities;

public class ClienteTests
{
    private static readonly Inscricao InscricaoValida =
        new(TipoInscricao.PessoaJuridica, "11222333000181");

    private static readonly Email EmailValido = new("empresa@teste.com");

    [Fact]
    public void Criar_DadosValidos_RetornaClienteAtivo()
    {
        var cliente = Cliente.Criar("Empresa Teste LTDA", InscricaoValida, EmailValido);

        cliente.Id.Should().NotBe(Guid.Empty);
        cliente.RazaoSocial.Should().Be("Empresa Teste LTDA");
        cliente.Ativo.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Criar_RazaoSocialVazia_LancaDomainException(string? razaoSocial)
    {
        var act = () => Cliente.Criar(razaoSocial!, InscricaoValida, EmailValido);
        act.Should().Throw<DomainException>().WithMessage("*obrigatória*");
    }

    [Fact]
    public void Criar_RazaoSocialMuitoLonga_LancaDomainException()
    {
        var razaoSocial = new string('x', 201);
        var act = () => Cliente.Criar(razaoSocial, InscricaoValida, EmailValido);
        act.Should().Throw<DomainException>().WithMessage("*200*");
    }

    [Fact]
    public void Desativar_ClienteAtivo_TornaInativo()
    {
        var cliente = Cliente.Criar("Empresa Teste", InscricaoValida, EmailValido);
        cliente.Desativar();
        cliente.Ativo.Should().BeFalse();
    }

    [Fact]
    public void Desativar_ClienteJaInativo_LancaDomainException()
    {
        var cliente = Cliente.Criar("Empresa Teste", InscricaoValida, EmailValido);
        cliente.Desativar();

        var act = () => cliente.Desativar();
        act.Should().Throw<DomainException>().WithMessage("*inativo*");
    }

    [Fact]
    public void AtualizarDados_DadosValidos_AlteraPropriedades()
    {
        var cliente = Cliente.Criar("Nome Antigo", InscricaoValida, EmailValido);
        var novoEmail = new Email("novo@email.com");

        cliente.AtualizarDados("Nome Novo", novoEmail);

        cliente.RazaoSocial.Should().Be("Nome Novo");
        cliente.Email.Endereco.Should().Be("novo@email.com");
    }
}
