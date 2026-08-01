using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LicenciamentoSoftware.Client.Models.Aplicacoes;
using LicenciamentoSoftware.Client.Models.ClientesFinais;
using LicenciamentoSoftware.Client.Models.Licencas;
using LicenciamentoSoftware.Maui.Services;
using LicenciamentoSoftware.Maui.ViewModels.Base;
using System.Collections.ObjectModel;

namespace LicenciamentoSoftware.Maui.ViewModels.Licencas;

/// <summary>
/// Wizard de 3 etapas para emissão de licença:
///   Passo 1 — Selecionar cliente final e aplicação
///   Passo 2 — Configurar detalhes conforme o tipo de licença
///   Passo 3 — Confirmar e exibir token gerado
/// </summary>
public partial class EmitirLicencaViewModel(MauiApiClientFactory factory) : BaseViewModel
{
    // ── Wizard ────────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoPasso1))]
    [NotifyPropertyChangedFor(nameof(NoPasso2))]
    [NotifyPropertyChangedFor(nameof(NoPasso3))]
    int _passoAtual = 1;

    public bool NoPasso1 => PassoAtual == 1;
    public bool NoPasso2 => PassoAtual == 2;
    public bool NoPasso3 => PassoAtual == 3;

    // ── Passo 1: Seleção ──────────────────────────────────────────────────────

    [ObservableProperty] ObservableCollection<ClienteFinalResult> _clientesFinais = [];
    [ObservableProperty] ObservableCollection<AplicacaoResult> _aplicacoes = [];
    [ObservableProperty] ClienteFinalResult? _clienteFinalSelecionado;
    [ObservableProperty] AplicacaoResult? _aplicacaoSelecionada;

    // ── Passo 2: Configuração ─────────────────────────────────────────────────

    // Tipo de licença detectado a partir da aplicação selecionada
    [ObservableProperty] string _tipoLicenca = string.Empty;

    // Campos Período
    [ObservableProperty] DateTime _dataInicio = DateTime.Today;
    [ObservableProperty] DateTime _dataFim = DateTime.Today.AddYears(1);
    [ObservableProperty] bool _renovacaoAutomatica;

    // Campos Usuários
    [ObservableProperty] int _quantidadeMaximaUsuarios = 10;
    [ObservableProperty] int _maxSessoesPorUsuario = 5;
    [ObservableProperty] int _tempoLimiteSessaoHoras = 24;

    // Campos Instalação
    [ObservableProperty] int _quantidadeMaximaInstalacoes = 1;

    // Token
    [ObservableProperty] bool _emitirToken;
    [ObservableProperty] int _expiracaoTokenMinutos = 525600; // 1 ano

    // Flags de visibilidade por tipo
    public bool EhPeriodo      => TipoLicenca.Contains("Period", StringComparison.OrdinalIgnoreCase)
                                || TipoLicenca.Contains("Período", StringComparison.OrdinalIgnoreCase);
    public bool EhUsuarios     => TipoLicenca.Contains("Usuário", StringComparison.OrdinalIgnoreCase)
                                || TipoLicenca.Contains("Usuario", StringComparison.OrdinalIgnoreCase);
    public bool EhInstalacao   => TipoLicenca.Contains("Instalação", StringComparison.OrdinalIgnoreCase)
                                || TipoLicenca.Contains("Instalacao", StringComparison.OrdinalIgnoreCase);
    public bool EhPermanente   => TipoLicenca.Contains("Permanente", StringComparison.OrdinalIgnoreCase);

    // ── Passo 3: Resultado ────────────────────────────────────────────────────

    [ObservableProperty] LicencaResult? _licencaEmitida;
    [ObservableProperty] string? _tokenEmitido;

    // ── Estado geral ──────────────────────────────────────────────────────────

    [ObservableProperty] string? _erro;

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    public override async Task OnAppearing()
    {
        Titulo = "Emitir Licença";
        PassoAtual = 1;
        await CarregarDadosIniciais();
    }

    private async Task CarregarDadosIniciais()
    {
        Ocupado = true;
        Erro = null;

        try
        {
            var clientesTask  = factory.ClienteFinal.ListarAsync(ativo: true, tamanhoPagina: 100);
            var aplicacoesTask = factory.Aplicacao.ListarAsync(ativo: true, tamanhoPagina: 100);

            await Task.WhenAll(clientesTask, aplicacoesTask);

            ClientesFinais.Clear();
            if (clientesTask.Result?.Itens is not null)
                foreach (var c in clientesTask.Result.Itens)
                    ClientesFinais.Add(c);

            Aplicacoes.Clear();
            if (aplicacoesTask.Result?.Itens is not null)
                foreach (var a in aplicacoesTask.Result.Itens)
                    Aplicacoes.Add(a);

            ClienteFinalSelecionado = ClientesFinais.FirstOrDefault();
            AplicacaoSelecionada    = Aplicacoes.FirstOrDefault();
            AtualizarTipoLicenca();
        }
        catch (Exception ex)
        {
            Erro = $"Erro ao carregar dados: {ex.Message}";
        }
        finally
        {
            Ocupado = false;
        }
    }

    // ── Navegação entre passos ────────────────────────────────────────────────

    [RelayCommand]
    void Avancar()
    {
        if (PassoAtual == 1)
        {
            if (ClienteFinalSelecionado is null || AplicacaoSelecionada is null)
            {
                Erro = "Selecione o cliente final e a aplicação.";
                return;
            }
            AtualizarTipoLicenca();
        }

        Erro = null;
        PassoAtual++;
    }

    [RelayCommand]
    void Voltar()
    {
        if (PassoAtual > 1)
            PassoAtual--;
    }

    // ── Emitir ────────────────────────────────────────────────────────────────

    [RelayCommand]
    async Task EmitirAsync()
    {
        if (AplicacaoSelecionada is null || ClienteFinalSelecionado is null)
        {
            Erro = "Dados incompletos.";
            return;
        }

        Ocupado = true;
        Erro = null;

        try
        {
            var request = MontarRequest();
            var (sucesso, licenca, tokenTexto, erro, _) = await factory.Licenca.EmitirAsync(request);

            if (sucesso)
            {
                LicencaEmitida = licenca;
                TokenEmitido   = tokenTexto;
                PassoAtual     = 3;
            }
            else
            {
                Erro = erro ?? "Erro ao emitir licença.";
            }
        }
        catch (Exception ex)
        {
            Erro = ex.Message;
        }
        finally
        {
            Ocupado = false;
        }
    }

    [RelayCommand]
    static async Task VoltarParaListaAsync()
        => await Shell.Current.GoToAsync("..");

    // ── Auxiliares ────────────────────────────────────────────────────────────

    private void AtualizarTipoLicenca()
    {
        TipoLicenca = AplicacaoSelecionada?.TipoLicencaDescricao ?? string.Empty;
        OnPropertyChanged(nameof(EhPeriodo));
        OnPropertyChanged(nameof(EhUsuarios));
        OnPropertyChanged(nameof(EhInstalacao));
        OnPropertyChanged(nameof(EhPermanente));
    }

    private EmitirLicencaRequest MontarRequest()
    {
        DetalhePeriodoRequest?    periodo    = null;
        DetalheUsuariosRequest?   usuarios   = null;
        DetalheInstalacaoRequest? instalacao = null;

        if (EhPeriodo)
            periodo = new DetalhePeriodoRequest(DataInicio, DataFim, RenovacaoAutomatica);

        if (EhUsuarios)
            usuarios = new DetalheUsuariosRequest(
                QuantidadeMaximaUsuarios, MaxSessoesPorUsuario, TempoLimiteSessaoHoras);

        if (EhInstalacao)
            instalacao = new DetalheInstalacaoRequest(QuantidadeMaximaInstalacoes);

        return new EmitirLicencaRequest(
            IdClienteFinal:        ClienteFinalSelecionado!.Id,
            IdAplicativo:          AplicacaoSelecionada!.Id,
            Periodo:               periodo,
            Usuarios:              usuarios,
            Instalacao:            instalacao,
            EmitirToken:           EmitirToken,
            ExpiracaoTokenMinutos: EmitirToken ? ExpiracaoTokenMinutos : null);
    }

    partial void OnAplicacaoSelecionadaChanged(AplicacaoResult? value)
        => AtualizarTipoLicenca();
}
