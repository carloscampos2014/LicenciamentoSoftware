using FluentAssertions;
using LicenciamentoSoftware.Application.Usuario.Commands;
using LicenciamentoSoftware.Application.Usuario.Validators;

namespace LicenciamentoSoftware.Application.Tests.Usuario;

public class CriarUsuarioValidatorTests
{
    private readonly CriarUsuarioValidator _sut = new();

    private static CriarUsuarioCommand Valido() => new(
        Guid.NewGuid(), "João Silva", "joao@empresa.com", "Senha@123", "OperadorCliente");

    [Fact]
    public async Task Validar_DadosValidos_Sucesso()
    {
        var resultado = await _sut.ValidateAsync(Valido());
        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validar_IdClienteVazio_Invalido()
    {
        var resultado = await _sut.ValidateAsync(Valido() with { IdCliente = Guid.Empty });
        resultado.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validar_NomeVazio_Invalido(string nome)
    {
        var resultado = await _sut.ValidateAsync(Valido() with { Nome = nome });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validar_NomeMaiorQue200_Invalido()
    {
        var resultado = await _sut.ValidateAsync(Valido() with { Nome = new string('A', 201) });
        resultado.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("invalido")]
    [InlineData("semArroba")]
    [InlineData("")]
    public async Task Validar_EmailInvalido_Invalido(string email)
    {
        var resultado = await _sut.ValidateAsync(Valido() with { Email = email });
        resultado.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("123")]
    [InlineData("curta")]
    [InlineData("1234567")]
    public async Task Validar_SenhaMenorQue8_Invalido(string senha)
    {
        var resultado = await _sut.ValidateAsync(Valido() with { Senha = senha });
        resultado.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("PapelInvalido")]
    [InlineData("")]
    [InlineData("admin")]
    public async Task Validar_PapelInvalido_Invalido(string papel)
    {
        var resultado = await _sut.ValidateAsync(Valido() with { Papel = papel });
        resultado.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("AdministradorPlataforma")]
    [InlineData("AdministradorCliente")]
    [InlineData("OperadorCliente")]
    [InlineData("Leitor")]
    public async Task Validar_PapeisValidos_Sucesso(string papel)
    {
        var resultado = await _sut.ValidateAsync(Valido() with { Papel = papel });
        resultado.IsValid.Should().BeTrue();
    }
}
