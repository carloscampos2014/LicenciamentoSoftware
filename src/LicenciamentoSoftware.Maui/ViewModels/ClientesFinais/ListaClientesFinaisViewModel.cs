using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LicenciamentoSoftware.Client.Models.ClientesFinais;
using LicenciamentoSoftware.Maui.Services;
using LicenciamentoSoftware.Maui.ViewModels.Base;
using System.Collections.ObjectModel;

namespace LicenciamentoSoftware.Maui.ViewModels.ClientesFinais;

public partial class ListaClientesFinaisViewModel(MauiApiClientFactory factory) : BaseViewModel
{
    // ── Estado da lista ───────────────────────────────────────────────────────

    [ObservableProperty] ObservableCollection<ClienteFinalResult> _itens = [];
    [ObservableProperty] string _busca = string.Empty;
    [ObservableProperty] bool? _filtroAtivo = null;
    [ObservableProperty] int _totalRegistros;
    [ObservableProperty] int _paginaAtual = 1;
    [ObservableProperty] bool _temMaisPaginas;
    [ObservableProperty] string? _erro;

    // ── Formulário ────────────────────────────────────────────────────────────

    [ObservableProperty] bool _exibirFormulario;
    [ObservableProperty] Guid? _idEdicao;
    [ObservableProperty] string _formRazaoSocial = string.Empty;
    [ObservableProperty] int _formTipoInscricao = 1;
    [ObservableProperty] string _formNumeroInscricao = string.Empty;
    [ObservableProperty] string _formEmail = string.Empty;
    [ObservableProperty] string _formTelefone = string.Empty;
    [ObservableProperty] string? _erroFormulario;

    private const int TamanhoPagina = 20;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    protected override async Task OnCarregarAsync()
    {
        Titulo = "Clientes Finais";
        await RecarregarAsync();
    }

    // ── Comandos ──────────────────────────────────────────────────────────────

    /// <summary>Recarga completa — limpa lista e busca página 1.</summary>
    [RelayCommand]
    async Task RecarregarAsync()
    {
        if (!await _semaphore.WaitAsync(0)) return; // já está carregando
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

    /// <summary>Carrega próxima página — só chamado pelo threshold da CollectionView.</summary>
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
    async Task BuscarAsync() => await RecarregarAsync();

    private async Task BuscarPaginaInternaAsync()
    {
        try
        {
            var resultado = await factory.ClienteFinal.ListarAsync(
                razaoSocial: string.IsNullOrWhiteSpace(Busca) ? null : Busca,
                ativo: FiltroAtivo,
                pagina: PaginaAtual,
                tamanhoPagina: TamanhoPagina);

            if (resultado is null) return;

            TotalRegistros = resultado.Total;

            foreach (var item in resultado.Itens)
                Itens.Add(item);

            TemMaisPaginas = Itens.Count < resultado.Total;
        }
        catch (Exception ex)
        {
            Erro = $"Erro ao carregar clientes: {ex.Message}";
        }
    }

    // ── Desativar ─────────────────────────────────────────────────────────────

    [RelayCommand]
    async Task DesativarAsync(ClienteFinalResult item)
    {
        var (sucesso, erro) = await factory.ClienteFinal.DesativarAsync(item.Id);
        if (sucesso) await RecarregarAsync();
        else Erro = erro ?? "Erro ao desativar.";
    }

    // ── Formulário ────────────────────────────────────────────────────────────

    [RelayCommand]
    void AbrirFormularioCriar()
    {
        IdEdicao = null;
        FormRazaoSocial = string.Empty;
        FormTipoInscricao = 1;
        FormNumeroInscricao = string.Empty;
        FormEmail = string.Empty;
        FormTelefone = string.Empty;
        ErroFormulario = null;
        ExibirFormulario = true;
    }

    [RelayCommand]
    void AbrirFormularioEditar(ClienteFinalResult item)
    {
        IdEdicao = item.Id;
        FormRazaoSocial = item.RazaoSocial;
        FormTipoInscricao = item.TipoInscricao;
        FormNumeroInscricao = item.NumeroInscricao;
        FormEmail = item.Email;
        FormTelefone = item.Telefone ?? string.Empty;
        ErroFormulario = null;
        ExibirFormulario = true;
    }

    [RelayCommand]
    void FecharFormulario() => ExibirFormulario = false;

    [RelayCommand]
    async Task SalvarAsync()
    {
        if (string.IsNullOrWhiteSpace(FormRazaoSocial) ||
            string.IsNullOrWhiteSpace(FormNumeroInscricao) ||
            string.IsNullOrWhiteSpace(FormEmail))
        {
            ErroFormulario = "Preencha os campos obrigatórios.";
            return;
        }

        Ocupado = true;
        ErroFormulario = null;

        try
        {
            bool sucesso;
            string? erro;

            if (IdEdicao is null)
            {
                var req = new CriarClienteFinalRequest(
                    FormRazaoSocial, FormTipoInscricao, FormNumeroInscricao,
                    FormEmail, string.IsNullOrWhiteSpace(FormTelefone) ? null : FormTelefone);
                (sucesso, _, erro, _) = await factory.ClienteFinal.CriarAsync(req);
            }
            else
            {
                var req = new AtualizarClienteFinalRequest(
                    FormRazaoSocial, FormEmail,
                    string.IsNullOrWhiteSpace(FormTelefone) ? null : FormTelefone);
                (sucesso, _, erro, _) = await factory.ClienteFinal.AtualizarAsync(IdEdicao.Value, req);
            }

            if (sucesso)
            {
                ExibirFormulario = false;
                await RecarregarAsync();
            }
            else
            {
                ErroFormulario = erro ?? "Erro ao salvar.";
            }
        }
        catch (Exception ex)
        {
            ErroFormulario = ex.Message;
        }
        finally
        {
            Ocupado = false;
        }
    }
}
