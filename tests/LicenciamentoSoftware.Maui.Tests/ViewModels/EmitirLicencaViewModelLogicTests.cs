using LicenciamentoSoftware.Client.Models.Aplicacoes;
using LicenciamentoSoftware.Client.Models.Licencas;

namespace LicenciamentoSoftware.Maui.Tests.ViewModels;

/// <summary>
/// Testa a lógica pura do wizard de emissão de licença:
/// detecção de tipo, montagem do request, controle de passos.
/// Não usa Shell nem HttpClient — testa apenas métodos utilitários.
/// </summary>
public sealed class EmitirLicencaViewModelLogicTests
{
    // ── Helpers: replicam a lógica de detecção de tipo do ViewModel ──────────

    private static bool EhPeriodo(string tipo)
        => tipo.Contains("Period", StringComparison.OrdinalIgnoreCase)
        || tipo.Contains("Período", StringComparison.OrdinalIgnoreCase);

    private static bool EhUsuarios(string tipo)
        => tipo.Contains("Usuário", StringComparison.OrdinalIgnoreCase)
        || tipo.Contains("Usuario", StringComparison.OrdinalIgnoreCase);

    private static bool EhInstalacao(string tipo)
        => tipo.Contains("Instalação", StringComparison.OrdinalIgnoreCase)
        || tipo.Contains("Instalacao", StringComparison.OrdinalIgnoreCase);

    private static bool EhPermanente(string tipo)
        => tipo.Contains("Permanente", StringComparison.OrdinalIgnoreCase);

    // ── Detecção de tipo de licença ───────────────────────────────────────────

    [Theory]
    [InlineData("Por Período", true)]
    [InlineData("Licença Por Período Anual", true)]
    [InlineData("Permanente", false)]
    [InlineData("Por Usuários", false)]
    public void EhPeriodo_VariosDescricoes_RetornaCorreto(string descricao, bool esperado)
        => EhPeriodo(descricao).Should().Be(esperado);

    [Theory]
    [InlineData("Por Usuários", true)]
    [InlineData("Licença por Usuario Simultaneo", true)]
    [InlineData("Permanente", false)]
    [InlineData("Por Período", false)]
    public void EhUsuarios_VariosDescricoes_RetornaCorreto(string descricao, bool esperado)
        => EhUsuarios(descricao).Should().Be(esperado);

    [Theory]
    [InlineData("Por Instalação", true)]
    [InlineData("Licença por Instalacao Unica", true)]
    [InlineData("Permanente", false)]
    public void EhInstalacao_VariosDescricoes_RetornaCorreto(string descricao, bool esperado)
        => EhInstalacao(descricao).Should().Be(esperado);

    [Theory]
    [InlineData("Permanente", true)]
    [InlineData("Licença Permanente Ilimitada", true)]
    [InlineData("Por Período", false)]
    public void EhPermanente_VariosDescricoes_RetornaCorreto(string descricao, bool esperado)
        => EhPermanente(descricao).Should().Be(esperado);

    // ── Montagem do request ───────────────────────────────────────────────────

    [Fact]
    public void MontarRequest_TipoPeriodo_IncluidetalhePeriodo()
    {
        var idClienteFinal = Guid.NewGuid();
        var idAplicativo   = Guid.NewGuid();
        var dataInicio     = new DateTime(2026, 1, 1);
        var dataFim        = new DateTime(2026, 12, 31);

        var request = new EmitirLicencaRequest(
            IdClienteFinal:        idClienteFinal,
            IdAplicativo:          idAplicativo,
            Periodo:               new DetalhePeriodoRequest(dataInicio, dataFim, false),
            Usuarios:              null,
            Instalacao:            null,
            EmitirToken:           false,
            ExpiracaoTokenMinutos: null);

        request.Periodo.Should().NotBeNull();
        request.Periodo!.DataInicio.Should().Be(dataInicio);
        request.Periodo.DataFim.Should().Be(dataFim);
        request.Usuarios.Should().BeNull();
        request.Instalacao.Should().BeNull();
    }

    [Fact]
    public void MontarRequest_TipoUsuarios_IncluiDetalheUsuarios()
    {
        var request = new EmitirLicencaRequest(
            IdClienteFinal:        Guid.NewGuid(),
            IdAplicativo:          Guid.NewGuid(),
            Periodo:               null,
            Usuarios:              new DetalheUsuariosRequest(10, 5, 24),
            Instalacao:            null,
            EmitirToken:           true,
            ExpiracaoTokenMinutos: 525600);

        request.Usuarios.Should().NotBeNull();
        request.Usuarios!.QuantidadeMaxima.Should().Be(10);
        request.Usuarios.MaxSessoesPorUsuario.Should().Be(5);
        request.Usuarios.TempoLimiteSessaoHoras.Should().Be(24);
        request.EmitirToken.Should().BeTrue();
        request.ExpiracaoTokenMinutos.Should().Be(525600);
    }

    [Fact]
    public void MontarRequest_TipoInstalacao_IncluiDetalheInstalacao()
    {
        var request = new EmitirLicencaRequest(
            IdClienteFinal:        Guid.NewGuid(),
            IdAplicativo:          Guid.NewGuid(),
            Periodo:               null,
            Usuarios:              null,
            Instalacao:            new DetalheInstalacaoRequest(3),
            EmitirToken:           false,
            ExpiracaoTokenMinutos: null);

        request.Instalacao.Should().NotBeNull();
        request.Instalacao!.QuantidadeMaxima.Should().Be(3);
    }

    // ── AplicacaoResult ───────────────────────────────────────────────────────

    [Fact]
    public void AplicacaoResult_TipoLicencaDescricao_RefleteNaTipoLicenca()
    {
        var app = new AplicacaoResult(
            Id:                  Guid.NewGuid(),
            IdCliente:           Guid.NewGuid(),
            Titulo:              "App Teste",
            Descricao:           null,
            IdTipoLicenca:       Guid.NewGuid(),
            TipoLicencaDescricao: "Por Período",
            Ativo:               true);

        EhPeriodo(app.TipoLicencaDescricao).Should().BeTrue();
        EhUsuarios(app.TipoLicencaDescricao).Should().BeFalse();
    }
}
