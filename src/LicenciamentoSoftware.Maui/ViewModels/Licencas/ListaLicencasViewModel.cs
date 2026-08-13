using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LicenciamentoSoftware.Client.Models.ClientesFinais;
using LicenciamentoSoftware.Client.Models.Licencas;
using LicenciamentoSoftware.Maui.Services;
using LicenciamentoSoftware.Maui.ViewModels.Base;
using System.Collections.ObjectModel;

namespace LicenciamentoSoftware.Maui.ViewModels.Licencas;

public partial class ListaLicencasViewModel(MauiApiClientFactory factory) : BaseViewModel
{
    [ObservableProperty] ObservableCollection<LicencaResult> _itens = [];
    [ObservableProperty] bool? _filtroAtivo = null;
    [ObservableProperty] int _totalRegistros;
    [ObservableProperty] int _paginaAtual = 1;
    [ObservableProperty] bool _temMaisPaginas;
    [ObservableProperty] string? _erro;

    [ObservableProperty] ObservableCollection<ClienteFinalResult> _clientesFinais = [];
    [ObservableProperty] ClienteFinalResult? _clienteFinalSelecionado;

    [ObservableProperty] LicencaResult? _licencaSelecionada;
    [ObservableProperty] bool _exibirDetalhe;
    [ObservableProperty] string _textoBotaoCopiarId = "Copiar";

    // Edição de detalhes (issue #219)
    [ObservableProperty] int _editQtdUsuarios;
    [ObservableProperty] int _editMaxSessoes;
    [ObservableProperty] int _editQtdInstalacoes;
    [ObservableProperty] bool _editRenovacaoAuto;

    private const int TamanhoPagina = 20;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    protected override async Task OnCarregarAsync()
    {
        Titulo = "Licenças";
        await Task.WhenAll(CarregarClientesFinaisAsync(), RecarregarAsync());
    }

    private async Task CarregarClientesFinaisAsync()
    {
        try
        {
            var resultado = await factory.ClienteFinal.ListarAsync(ativo: true, tamanhoPagina: 100);
            ClientesFinais.Clear();
            ClientesFinais.Add(new ClienteFinalResult(Guid.Empty, Guid.Empty, "Todos", 1, "", "", null, true));
            if (resultado?.Itens is not null)
                foreach (var c in resultado.Itens) ClientesFinais.Add(c);
            ClienteFinalSelecionado = ClientesFinais.FirstOrDefault();
        }
        catch { }
    }

    [RelayCommand]
    async Task RecarregarAsync()
    {
        if (!await _semaphore.WaitAsync(0)) return;
        try
        {
            Ocupado = true;
            Erro = null;
            PaginaAtual = 1;
            Itens.Clear();
            TemMaisPaginas = false;
            await BuscarPaginaInternaAsync();
        }
        finally
        {
            Ocupado = false;
            _semaphore.Release();
        }
    }

    [RelayCommand]
    async Task CarregarMaisAsync()
    {
        if (!TemMaisPaginas) return;
        if (!await _semaphore.WaitAsync(0)) return;
        try
        {
            Ocupado = true;
            PaginaAtual++;
            await BuscarPaginaInternaAsync();
        }
        finally
        {
            Ocupado = false;
            _semaphore.Release();
        }
    }

    [RelayCommand]
    async Task FiltrarAsync() => await RecarregarAsync();

    private async Task BuscarPaginaInternaAsync()
    {
        try
        {
            var idCliente = ClienteFinalSelecionado?.Id == Guid.Empty
                ? (Guid?)null : ClienteFinalSelecionado?.Id;

            var resultado = await factory.Licenca.ListarAsync(
                idClienteFinal: idCliente,
                ativo: FiltroAtivo,
                pagina: PaginaAtual,
                tamanhoPagina: TamanhoPagina);

            if (resultado is null) return;

            TotalRegistros = resultado.Total;
            foreach (var item in resultado.Itens) Itens.Add(item);
            TemMaisPaginas = Itens.Count < resultado.Total;
        }
        catch (Exception ex)
        {
            Erro = $"Erro ao carregar licenças: {ex.Message}";
        }
    }

    [RelayCommand]
    async Task VerDetalheAsync(LicencaResult item)
    {
        try
        {
            LicencaSelecionada = await factory.Licenca.BuscarPorIdAsync(item.Id) ?? item;
        }
        catch
        {
            LicencaSelecionada = item;
        }

        // Inicializar campos de edição
        EditQtdUsuarios = LicencaSelecionada.Usuarios?.QuantidadeMaxima ?? 10;
        EditMaxSessoes = LicencaSelecionada.Usuarios?.MaxSessoesPorUsuario ?? 5;
        EditQtdInstalacoes = LicencaSelecionada.Instalacao?.QuantidadeMaxima ?? 1;
        EditRenovacaoAuto = LicencaSelecionada.Periodo?.RenovacaoAutomatica ?? false;
        ExibirDetalhe = true;
    }

    [RelayCommand]
    void FecharDetalhe()
    {
        ExibirDetalhe = false;
        LicencaSelecionada = null;
    }

    [RelayCommand]
    async Task DesativarAsync(LicencaResult item)
    {
        var (sucesso, erro) = await factory.Licenca.DesativarAsync(item.Id);
        if (sucesso) await RecarregarAsync();
        else Erro = erro ?? "Erro ao desativar licença.";
    }

    [RelayCommand]
    async Task EncerrarSessaoAsync((Guid idLicenca, Guid idSessao) args)
    {
        var (sucesso, erro) = await factory.Licenca.EncerrarSessaoAsync(args.idLicenca, args.idSessao);
        if (sucesso && LicencaSelecionada is not null) await VerDetalheAsync(LicencaSelecionada);
        else if (!sucesso) Erro = erro ?? "Erro ao encerrar sessão.";
    }

    [RelayCommand]
    async Task RenovarTokenAsync(Guid idLicenca)
    {
        var (sucesso, _, erro) = await factory.Licenca.RenovarTokenAsync(idLicenca);
        if (!sucesso) Erro = erro ?? "Erro ao renovar token.";
        else if (LicencaSelecionada is not null) await VerDetalheAsync(LicencaSelecionada);
    }

    [RelayCommand]
    static async Task EmitirNovaAsync()
        => await Shell.Current.GoToAsync("licencas/emitir");

    [RelayCommand]
    async Task CopiarIdLicencaAsync()
    {
        if (LicencaSelecionada is null) return;
        await Clipboard.SetTextAsync(LicencaSelecionada.Id.ToString().ToUpperInvariant());
        TextoBotaoCopiarId = "✓ Copiado";
        await Task.Delay(2000);
        TextoBotaoCopiarId = "Copiar";
    }

    // ── Edição de detalhes (issue #219) ────────────────

    [RelayCommand]
    async Task SalvarDetalhesUsuariosAsync()
    {
        if (LicencaSelecionada is null) return;
        Erro = null;
        var (sucesso, erro) = await factory.Licenca.EditarDetalhesUsuariosAsync(
            LicencaSelecionada.Id, EditQtdUsuarios, EditMaxSessoes);
        if (sucesso) await VerDetalheAsync(LicencaSelecionada);
        else Erro = erro ?? "Erro ao salvar detalhes de usuários.";
    }

    [RelayCommand]
    async Task SalvarDetalhesInstalacaoAsync()
    {
        if (LicencaSelecionada is null) return;
        Erro = null;
        var (sucesso, erro) = await factory.Licenca.EditarDetalhesInstalacaoAsync(
            LicencaSelecionada.Id, EditQtdInstalacoes);
        if (sucesso) await VerDetalheAsync(LicencaSelecionada);
        else Erro = erro ?? "Erro ao salvar detalhes de instalação.";
    }

    [RelayCommand]
    async Task SalvarRenovacaoAutomaticaAsync()
    {
        if (LicencaSelecionada is null) return;
        Erro = null;
        var (sucesso, erro) = await factory.Licenca.EditarRenovacaoAutomaticaAsync(
            LicencaSelecionada.Id, EditRenovacaoAuto);
        if (sucesso) await VerDetalheAsync(LicencaSelecionada);
        else Erro = erro ?? "Erro ao salvar renovação automática.";
    }
}
