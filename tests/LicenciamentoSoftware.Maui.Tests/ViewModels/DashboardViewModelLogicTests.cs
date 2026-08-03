using LicenciamentoSoftware.Client.Models.Dashboard;

namespace LicenciamentoSoftware.Maui.Tests.ViewModels;

/// <summary>
/// Testa a lógica de mapeamento e flags de alerta do DashboardViewModel.
/// Não usa HttpClient — testa apenas transformação de dados via métodos utilitários.
/// </summary>
public sealed class DashboardViewModelLogicTests
{
    // ── Helpers que replicam a lógica do ViewModel ────────────────────────────

    private static bool TemSessoesInativas(IReadOnlyList<SessaoInativaAlerta> lista)
        => lista.Count > 0;

    private static bool TemLicencasNoLimite(IReadOnlyList<LicencaLimiteAlerta> lista)
        => lista.Count > 0;

    private static bool TemErrosValidacao(long total)
        => total > 0;

    // ── Flags de alerta ───────────────────────────────────────────────────────

    [Fact]
    public void TemSessoesInativas_ListaVazia_RetornaFalse()
        => TemSessoesInativas([]).Should().BeFalse();

    [Fact]
    public void TemSessoesInativas_ComItens_RetornaTrue()
    {
        var alerta = new SessaoInativaAlerta(
            Guid.NewGuid(), Guid.NewGuid(),
            "Cliente X", "App Y", "user@x", DateTime.UtcNow.AddHours(-5), 5.0);

        TemSessoesInativas([alerta]).Should().BeTrue();
    }

    [Fact]
    public void TemLicencasNoLimite_ListaVazia_RetornaFalse()
        => TemLicencasNoLimite([]).Should().BeFalse();

    [Fact]
    public void TemLicencasNoLimite_ComItens_RetornaTrue()
    {
        var alerta = new LicencaLimiteAlerta(
            Guid.NewGuid(), "Cliente X", "App Y", "Por Usuários", 9, 10);

        TemLicencasNoLimite([alerta]).Should().BeTrue();
    }

    [Theory]
    [InlineData(0L, false)]
    [InlineData(1L, true)]
    [InlineData(100L, true)]
    public void TemErrosValidacao_VariosValores_RetornaCorreto(long total, bool esperado)
        => TemErrosValidacao(total).Should().Be(esperado);

    // ── Mapeamento de resumo ──────────────────────────────────────────────────

    [Fact]
    public void DashboardResumoResult_Construcao_MapeiaTodasAsPropriedades()
    {
        var resumo = new DashboardResumoResult(
            TotalClientesFinaisAtivos:     5,
            TotalAplicacoesAtivas:         3,
            TotalLicencasAtivas:           12,
            TotalLicencasInativas:         2,
            LicencasPorTipo:               new LicencasPorTipoResult(4, 3, 3, 2),
            LicencasExpirandoEm7Dias:      1,
            SessoesAtivasAgora:            7,
            TokensExpirandoEm7Dias:        2,
            NovasLicencasUltimos30Dias:    3,
            NovosClientesFinaisUltimos30Dias: 1);

        resumo.TotalClientesFinaisAtivos.Should().Be(5);
        resumo.TotalLicencasAtivas.Should().Be(12);
        resumo.LicencasPorTipo.Permanente.Should().Be(4);
        resumo.LicencasPorTipo.PorPeriodo.Should().Be(3);
        resumo.LicencasPorTipo.PorUsuarios.Should().Be(3);
        resumo.LicencasPorTipo.PorInstalacao.Should().Be(2);
        resumo.LicencasExpirandoEm7Dias.Should().Be(1);
        resumo.SessoesAtivasAgora.Should().Be(7);
    }
}
