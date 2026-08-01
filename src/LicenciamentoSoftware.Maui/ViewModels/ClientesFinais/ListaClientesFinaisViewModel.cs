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
    [ObservableProperty] bool? _filtroAtivo = null;   // null = todos
    [ObservableProperty] int _totalRegistros;
    [ObservableProperty] int _paginaAtual = 1;
    [ObservableProperty] bool _temMaisPaginas;
    [ObservableProperty] string? _erro;

    // ── Formulário de criação/edição ──────────────────────────────────────────

    [ObservableProperty] bool _exibirFormulario;
    [ObservableProperty] Guid? _idEdicao;
    [ObservableProperty] string _formRazaoSocial = string.Empty;
    [ObservableProperty] int _formTipoInscricao = 1;           // 1=CPF, 2=CNPJ
    [ObservableProperty] string _formNumeroInscricao = string.Empty;
    [ObservableProperty] string _formEmail = string.Empty;
    [ObservableProperty] string _formTelefone = string.Empty;
    [ObservableProperty] string? _erroFormulario;

    private const int TamanhoPagina = 20;

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    public override async Task OnAppearing()
    {
        Titulo = "Clientes Finais";
        await CarregarAsync();
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
    async Task BuscarAsync() => await CarregarAsync();

    private async Task BuscarPaginaAsync()
    {
        Ocupado = true;
        Erro = null;

        try
        {
            var resultado = await factory.ClienteFinal.ListarAsync(
                razaoSocial: string.IsNullOrWhiteSpace(Busca) ? null : Busca,
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
            Erro = $"Erro ao carregar clientes: {ex.Message}";
        }
        finally
        {
            Ocupado = false;
        }
    }

    // ── Desativar ─────────────────────────────────────────────────────────────

    [RelayCommand]
    async Task DesativarAsync(ClienteFinalResult item)
    {
        var (sucesso, erro) = await factory.ClienteFinal.DesativarAsync(item.Id);

        if (sucesso)
            await CarregarAsync();
        else
            Erro = erro ?? "Erro ao desativar cliente.";
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
                await CarregarAsync();
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
