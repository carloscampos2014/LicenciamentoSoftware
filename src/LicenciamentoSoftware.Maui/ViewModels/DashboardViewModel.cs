using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LicenciamentoSoftware.Client.Models.Dashboard;
using LicenciamentoSoftware.Maui.Services;
using LicenciamentoSoftware.Maui.ViewModels.Base;

namespace LicenciamentoSoftware.Maui.ViewModels;

public partial class DashboardViewModel(
    MauiApiClientFactory factory,
    MauiAuthService authService) : BaseViewModel
{
    // ── Resumo ────────────────────────────────────────────────────────────────

    [ObservableProperty] int _totalClientesAtivos;
    [ObservableProperty] int _totalAplicacoesAtivas;
    [ObservableProperty] int _totalLicencasAtivas;
    [ObservableProperty] int _totalLicencasInativas;
    [ObservableProperty] int _licencasExpirandoEm7Dias;
    [ObservableProperty] int _sessoesAtivasAgora;
    [ObservableProperty] int _tokensExpirandoEm7Dias;
    [ObservableProperty] int _novasLicencas30Dias;
    [ObservableProperty] int _novosClientes30Dias;

    // Tipos de licença
    [ObservableProperty] int _licencasPermanente;
    [ObservableProperty] int _licencasPorPeriodo;
    [ObservableProperty] int _licencasPorUsuarios;
    [ObservableProperty] int _licencasPorInstalacao;

    // ── Alertas ───────────────────────────────────────────────────────────────

    [ObservableProperty] IReadOnlyList<SessaoInativaAlerta> _sessoesInativas = [];
    [ObservableProperty] IReadOnlyList<LicencaLimiteAlerta> _licencasNoLimite = [];
    [ObservableProperty] long _totalErrosValidacao;

    // ── Estado ────────────────────────────────────────────────────────────────

    [ObservableProperty] string? _erro;
    [ObservableProperty] string _nomeUsuario = string.Empty;

    public bool TemSessoesInativas => SessoesInativas.Count > 0;
    public bool TemLicencasNoLimite => LicencasNoLimite.Count > 0;
    public bool TemErrosValidacao => TotalErrosValidacao > 0;

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    public override async Task OnAppearing()
    {
        Titulo = "Dashboard";
        NomeUsuario = authService.Nome ?? string.Empty;
        await CarregarAsync();
    }

    [RelayCommand]
    async Task CarregarAsync()
    {
        Ocupado = true;
        Erro = null;

        try
        {
            // Carrega resumo e alertas em paralelo
            var resumoTask  = factory.Dashboard.BuscarResumoAsync();
            var alertasTask = factory.Dashboard.BuscarAlertasAsync();

            await Task.WhenAll(resumoTask, alertasTask);

            var resumo  = resumoTask.Result;
            var alertas = alertasTask.Result;

            if (resumo is not null)
                AplicarResumo(resumo);

            if (alertas is not null)
                AplicarAlertas(alertas);
        }
        catch (Exception ex)
        {
            Erro = $"Erro ao carregar dashboard: {ex.Message}";
        }
        finally
        {
            Ocupado = false;
        }
    }

    [RelayCommand]
    async Task LogoutAsync()
    {
        await authService.LogoutAsync();
        await Shell.Current.GoToAsync("//login");
    }

    // ── Auxiliares ────────────────────────────────────────────────────────────

    private void AplicarResumo(DashboardResumoResult r)
    {
        TotalClientesAtivos      = r.TotalClientesFinaisAtivos;
        TotalAplicacoesAtivas    = r.TotalAplicacoesAtivas;
        TotalLicencasAtivas      = r.TotalLicencasAtivas;
        TotalLicencasInativas    = r.TotalLicencasInativas;
        LicencasExpirandoEm7Dias = r.LicencasExpirandoEm7Dias;
        SessoesAtivasAgora       = r.SessoesAtivasAgora;
        TokensExpirandoEm7Dias   = r.TokensExpirandoEm7Dias;
        NovasLicencas30Dias      = r.NovasLicencasUltimos30Dias;
        NovosClientes30Dias      = r.NovosClientesFinaisUltimos30Dias;

        LicencasPermanente    = r.LicencasPorTipo.Permanente;
        LicencasPorPeriodo    = r.LicencasPorTipo.PorPeriodo;
        LicencasPorUsuarios   = r.LicencasPorTipo.PorUsuarios;
        LicencasPorInstalacao = r.LicencasPorTipo.PorInstalacao;
    }

    private void AplicarAlertas(DashboardAlertasResult a)
    {
        SessoesInativas      = a.SessoesInativas;
        LicencasNoLimite     = a.LicencasNoLimite;
        TotalErrosValidacao  = a.ErrosValidacao.TotalErros;

        OnPropertyChanged(nameof(TemSessoesInativas));
        OnPropertyChanged(nameof(TemLicencasNoLimite));
        OnPropertyChanged(nameof(TemErrosValidacao));
    }
}
