using FluentAssertions;
using LicenciamentoSoftware.Application.Aplicacao.Commands;
using LicenciamentoSoftware.Application.Aplicacao.Validators;

namespace LicenciamentoSoftware.Application.Tests.Aplicacao;

public class CriarAplicacaoValidatorTests
{
    private readonly CriarAplicacaoValidator _sut = new();

    private static CriarAplicacaoCommand Valido() => new(
        Guid.NewGuid(), "Meu Software", Guid.NewGuid(), null);

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
    public async Task Validar_TituloVazio_Invalido(string titulo)
    {
        var resultado = await _sut.ValidateAsync(Valido() with { Titulo = titulo });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validar_TituloMaiorQue120_Invalido()
    {
        var resultado = await _sut.ValidateAsync(Valido() with { Titulo = new string('A', 121) });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validar_IdTipoLicencaVazio_Invalido()
    {
        var resultado = await _sut.ValidateAsync(Valido() with { IdTipoLicenca = Guid.Empty });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validar_DescricaoMaiorQue300_Invalido()
    {
        var resultado = await _sut.ValidateAsync(Valido() with { Descricao = new string('A', 301) });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validar_DescricaoNula_Valido()
    {
        var resultado = await _sut.ValidateAsync(Valido() with { Descricao = null });
        resultado.IsValid.Should().BeTrue();
    }
}
