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
    // ── Estado da lista ───────────────────────────────────────────────────────

    [ObservableProperty] ObservableCollection<LicencaResult> _itens = [];
    [ObservableProperty] bool? _filtroAtivo = null;
    [ObservableProperty] int _totalRegistros;
    [ObservableProperty] int _paginaAtual = 1;
    [ObservableProperty] bool _temMaisPaginas;
    [ObservableProperty] string? _erro;

    // ── Filtros ───────────────────────────────────────────────────────────────

    [ObservableProperty] ObservableCollection<ClienteFinalResult> _clientesFinais = [];
    [ObservableProperty] ClienteFinalResult? _clienteFinalSelecionado;

    // ── Detalhe expandido ─────────────────────────────────────────────────────

    [ObservableProperty] LicencaResult? _licencaSelecionada;
    [ObservableProperty] bool _exibirDetalhe;

    private const int TamanhoPagina = 20;

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    public override async Task OnAppearing()
    {
        Titulo = "Licenças";
        await Task.WhenAll(CarregarClientesFinaisAsync(), CarregarAsync());
    }

    // ── Filtro de clientes ────────────────────────────────────────────────────

    private async Task CarregarClientesFinaisAsync()
    {
        try
        {
            var resultado = await factory.ClienteFinal.ListarAsync(ativo: true, tamanhoPagina: 100);
            ClientesFinais.Clear();
            ClientesFinais.Add(new ClienteFinalResult(Guid.Empty, Guid.Empty, "Todos", 1, "", "", null, true));

            if (resultado?.Itens is not null)
                foreach (var c in resultado.Itens)
                    ClientesFinais.Add(c);

            ClienteFinalSelecionado = ClientesFinais.FirstOrDefault();
        }
        catch { /* não bloqueia */ }
    }

    // ── Comandos de lista ─────────────────────────────────────────────────────

    [RelayCommand]
    async Task CarregarAsync()
    {
        PaginaAtual = 1;
        Itens.Clear();
        await BuscarPaginaAsync();
    }

    [RelayCommand]
    async Task CarregarMaisAsync()
    {
        if (!TemMaisPaginas || Ocupado) return;
        PaginaAtual++;
        await BuscarPaginaAsync();
    }

    [RelayCommand]
    async Task FiltrarAsync() => await CarregarAsync();

    private async Task BuscarPaginaAsync()
    {
        Ocupado = true;
        Erro = null;

        try
        {
            var idCliente = ClienteFinalSelecionado?.Id == Guid.Empty
                ? (Guid?)null
                : ClienteFinalSelecionado?.Id;

            var resultado = await factory.Licenca.ListarAsync(
                idClienteFinal: idCliente,
                ativo: FiltroAtivo,
                pagina: PaginaAtual,
                tamanhoPagina: TamanhoPagina);

            if (resultado is null) return;

            TotalRegistros = resultado.Total;
            TemMaisPaginas = Itens.Count + resultado.Itens.Count < resultado.Total;

            foreach (var item in resultado.Itens)
                Itens.Add(item);
        }
        catch (Exception ex)
        {
            Erro = $"Erro ao carregar licenças: {ex.Message}";
        }
        finally
        {
            Ocupado = false;
        }
    }

    // ── Detalhe ───────────────────────────────────────────────────────────────

    [RelayCommand]
    async Task VerDetalheAsync(LicencaResult item)
    {
        try
        {
            // Carrega detalhes completos (sessões, instalações, token)
            LicencaSelecionada = await factory.Licenca.BuscarPorIdAsync(item.Id) ?? item;
            ExibirDetalhe = true;
        }
        catch
        {
            LicencaSelecionada = item;
            ExibirDetalhe = true;
        }
    }

    [RelayCommand]
    void FecharDetalhe()
    {
        ExibirDetalhe = false;
        LicencaSelecionada = null;
    }

    // ── Ações sobre licença ───────────────────────────────────────────────────

    [RelayCommand]
    async Task DesativarAsync(LicencaResult item)
    {
        var (sucesso, erro) = await factory.Licenca.DesativarAsync(item.Id);

        if (sucesso)
            await CarregarAsync();
        else
            Erro = erro ?? "Erro ao desativar licença.";
    }

    [RelayCommand]
    async Task EncerrarSessaoAsync((Guid idLicenca, Guid idSessao) args)
    {
        var (sucesso, erro) = await factory.Licenca.EncerrarSessaoAsync(args.idLicenca, args.idSessao);

        if (sucesso && LicencaSelecionada is not null)
            await VerDetalheAsync(LicencaSelecionada);
        else if (!sucesso)
            Erro = erro ?? "Erro ao encerrar sessão.";
    }

    [RelayCommand]
    async Task LiberarInstalacaoAsync((Guid idLicenca, Guid idInstalacao) args)
    {
        var (sucesso, erro) = await factory.Licenca.LiberarInstalacaoAsync(args.idLicenca, args.idInstalacao);

        if (sucesso && LicencaSelecionada is not null)
            await VerDetalheAsync(LicencaSelecionada);
        else if (!sucesso)
            Erro = erro ?? "Erro ao liberar instalação.";
    }

    [RelayCommand]
    async Task RenovarTokenAsync(Guid idLicenca)
    {
        var (sucesso, tokenTexto, erro) = await factory.Licenca.RenovarTokenAsync(idLicenca);

        if (!sucesso)
            Erro = erro ?? "Erro ao renovar token.";
        else if (LicencaSelecionada is not null)
            await VerDetalheAsync(LicencaSelecionada);
    }

    // ── Navegar para emitir ───────────────────────────────────────────────────

    [RelayCommand]
    static async Task EmitirNovaAsync()
        => await Shell.Current.GoToAsync("licencas/emitir");
}
