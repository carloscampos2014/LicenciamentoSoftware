using FluentAssertions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Validators;

namespace LicenciamentoSoftware.Application.Tests.Licenca;

public class EmitirLicencaValidatorTests
{
    private readonly EmitirLicencaValidator _sut = new();

    private static readonly Guid IdClienteFinal = Guid.NewGuid();
    private static readonly Guid IdAplicativo   = Guid.NewGuid();

    private static EmitirLicencaCommand Valido(
        DetalhePeriodoCommand? periodo = null,
        DetalheUsuariosCommand? usuarios = null,
        DetalheInstalacaoCommand? instalacao = null) =>
        new(IdClienteFinal, IdAplicativo, periodo, usuarios, instalacao);

    // -------------------------------------------------------------------------
    // IDs obrigatórios
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Validar_IdClienteFinalVazio_Invalido()
    {
        var resultado = await _sut.ValidateAsync(Valido() with { IdClienteFinal = Guid.Empty });
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(EmitirLicencaCommand.IdClienteFinal));
    }

    [Fact]
    public async Task Validar_IdAplicativoVazio_Invalido()
    {
        var resultado = await _sut.ValidateAsync(Valido() with { IdAplicativo = Guid.Empty });
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(EmitirLicencaCommand.IdAplicativo));
    }

    // -------------------------------------------------------------------------
    // Regra: 0 ou 1 bloco de detalhe
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Validar_SemDetalhe_Valido_Permanente()
    {
        var resultado = await _sut.ValidateAsync(Valido());
        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validar_DoisDetalhes_Invalido()
    {
        var cmd = Valido(
            periodo: new DetalhePeriodoCommand(DateTime.UtcNow.Date, DateTime.UtcNow.AddYears(1)),
            usuarios: new DetalheUsuariosCommand(10));

        var resultado = await _sut.ValidateAsync(cmd);
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validar_TresDetalhes_Invalido()
    {
        var cmd = Valido(
            periodo: new DetalhePeriodoCommand(DateTime.UtcNow.Date, DateTime.UtcNow.AddYears(1)),
            usuarios: new DetalheUsuariosCommand(10),
            instalacao: new DetalheInstalacaoCommand(5));

        var resultado = await _sut.ValidateAsync(cmd);
        resultado.IsValid.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // Período
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Validar_PeriodoValido_Sucesso()
    {
        var cmd = Valido(periodo: new DetalhePeriodoCommand(
            DateTime.UtcNow.Date, DateTime.UtcNow.AddYears(1)));
        var resultado = await _sut.ValidateAsync(cmd);
        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validar_DataFimAnteriorDataInicio_Invalido()
    {
        var cmd = Valido(periodo: new DetalhePeriodoCommand(
            DateTime.UtcNow.Date.AddDays(10), DateTime.UtcNow.Date.AddDays(5)));
        var resultado = await _sut.ValidateAsync(cmd);
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validar_DataFimIgualDataInicio_Invalido()
    {
        var data = DateTime.UtcNow.Date.AddDays(5);
        var cmd = Valido(periodo: new DetalhePeriodoCommand(data, data));
        var resultado = await _sut.ValidateAsync(cmd);
        resultado.IsValid.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // Usuários
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Validar_UsuariosQuantidadeZero_Invalido()
    {
        var cmd = Valido(usuarios: new DetalheUsuariosCommand(0));
        var resultado = await _sut.ValidateAsync(cmd);
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validar_UsuariosValidos_Sucesso()
    {
        var cmd = Valido(usuarios: new DetalheUsuariosCommand(10, 3, 12));
        var resultado = await _sut.ValidateAsync(cmd);
        resultado.IsValid.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // Instalação
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Validar_InstalacaoQuantidadeZero_Invalido()
    {
        var cmd = Valido(instalacao: new DetalheInstalacaoCommand(0));
        var resultado = await _sut.ValidateAsync(cmd);
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validar_InstalacaoValida_Sucesso()
    {
        var cmd = Valido(instalacao: new DetalheInstalacaoCommand(5));
        var resultado = await _sut.ValidateAsync(cmd);
        resultado.IsValid.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // Token
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Validar_ExpiracaoTokenZero_Invalido()
    {
        var cmd = Valido() with { EmitirToken = true, ExpiracaoTokenMinutos = 0 };
        var resultado = await _sut.ValidateAsync(cmd);
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validar_ExpiracaoTokenPositiva_Valido()
    {
        var cmd = Valido() with { EmitirToken = true, ExpiracaoTokenMinutos = 1440 };
        var resultado = await _sut.ValidateAsync(cmd);
        resultado.IsValid.Should().BeTrue();
    }
}
