using FluentAssertions;
using LicenciamentoSoftware.Application.Cliente.Commands;
using LicenciamentoSoftware.Application.Cliente.Validators;

namespace LicenciamentoSoftware.Application.Tests.Cliente;

public class CriarClienteValidatorTests
{
    private readonly CriarClienteValidator _sut = new();

    private static CriarClienteCommand Valido() => new(
        "Empresa Teste Ltda", 2, "11222333000181", "contato@empresa.com", "11999999999");

    [Fact]
    public async Task Validar_DadosValidos_Sucesso()
    {
        var resultado = await _sut.ValidateAsync(Valido());
        resultado.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validar_RazaoSocialVazia_Invalido(string razaoSocial)
    {
        var resultado = await _sut.ValidateAsync(Valido() with { RazaoSocial = razaoSocial });
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CriarClienteCommand.RazaoSocial));
    }

    [Fact]
    public async Task Validar_RazaoSocialMaiorQue200_Invalido()
    {
        var resultado = await _sut.ValidateAsync(Valido() with { RazaoSocial = new string('A', 201) });
        resultado.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(-1)]
    public async Task Validar_TipoInscricaoForaDoRange_Invalido(int tipo)
    {
        var resultado = await _sut.ValidateAsync(Valido() with { TipoInscricao = tipo });
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CriarClienteCommand.TipoInscricao));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validar_NumeroInscricaoVazio_Invalido(string numero)
    {
        var resultado = await _sut.ValidateAsync(Valido() with { NumeroInscricao = numero });
        resultado.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("email-invalido")]
    [InlineData("sem-arroba")]
    [InlineData("")]
    public async Task Validar_EmailInvalido_Invalido(string email)
    {
        var resultado = await _sut.ValidateAsync(Valido() with { Email = email });
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CriarClienteCommand.Email));
    }

    [Fact]
    public async Task Validar_TelefoneNulo_Valido()
    {
        var resultado = await _sut.ValidateAsync(Valido() with { Telefone = null });
        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validar_TelefoneMaiorQue15_Invalido()
    {
        var resultado = await _sut.ValidateAsync(Valido() with { Telefone = new string('1', 16) });
        resultado.IsValid.Should().BeFalse();
    }
}
