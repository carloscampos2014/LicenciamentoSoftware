using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LicenciamentoSoftware.Client.Models.Aplicacoes;
using LicenciamentoSoftware.Client.Models.TiposLicenca;
using LicenciamentoSoftware.Maui.Services;
using LicenciamentoSoftware.Maui.ViewModels.Base;
using System.Collections.ObjectModel;

namespace LicenciamentoSoftware.Maui.ViewModels.Aplicacoes;

public partial class ListaAplicacoesViewModel(MauiApiClientFactory factory) : BaseViewModel
{
    // ── Estado da lista ───────────────────────────────────────────────────────

    [ObservableProperty] ObservableCollection<AplicacaoResult> _itens = [];
    [ObservableProperty] string _busca = string.Empty;
    [ObservableProperty] bool? _filtroAtivo = null;
    [ObservableProperty] int _totalRegistros;
    [ObservableProperty] int _paginaAtual = 1;
    [ObservableProperty] bool _temMaisPaginas;
    [ObservableProperty] string? _erro;

    // ── Formulário ────────────────────────────────────────────────────────────

    [ObservableProperty] bool _exibirFormulario;
    [ObservableProperty] Guid? _idEdicao;
    [ObservableProperty] string _formTitulo = string.Empty;
    [ObservableProperty] string _formDescricao = string.Empty;
    [ObservableProperty] TipoLicencaResult? _formTipoLicencaSelecionado;
    [ObservableProperty] ObservableCollection<TipoLicencaResult> _tiposLicenca = [];
    [ObservableProperty] string? _erroFormulario;

    private const int TamanhoPagina = 20;

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    public override async Task OnAppearing()
    {
        Titulo = "Aplicações";
        await Task.WhenAll(CarregarTiposLicencaAsync(), CarregarAsync());
    }

    // ── Tipos de licença ──────────────────────────────────────────────────────

    private async Task CarregarTiposLicencaAsync()
    {
        try
        {
            var tipos = await factory.TipoLicenca.ListarAsync();
            TiposLicenca.Clear();

            if (tipos is null) return;

            foreach (var t in tipos)
                TiposLicenca.Add(t);

            FormTipoLicencaSelecionado = TiposLicenca.FirstOrDefault();
        }
        catch { /* não bloqueia o carregamento da lista */ }
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
            var resultado = await factory.Aplicacao.ListarAsync(
                titulo: string.IsNullOrWhiteSpace(Busca) ? null : Busca,
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
            Erro = $"Erro ao carregar aplicações: {ex.Message}";
        }
        finally
        {
            Ocupado = false;
        }
    }

    // ── Desativar ─────────────────────────────────────────────────────────────

    [RelayCommand]
    async Task DesativarAsync(AplicacaoResult item)
    {
        var (sucesso, erro) = await factory.Aplicacao.DesativarAsync(item.Id);

        if (sucesso)
            await CarregarAsync();
        else
            Erro = erro ?? "Erro ao desativar aplicação.";
    }

    // ── Formulário ────────────────────────────────────────────────────────────

    [RelayCommand]
    void AbrirFormularioCriar()
    {
        IdEdicao = null;
        FormTitulo = string.Empty;
        FormDescricao = string.Empty;
        FormTipoLicencaSelecionado = TiposLicenca.FirstOrDefault();
        ErroFormulario = null;
        ExibirFormulario = true;
    }

    [RelayCommand]
    void AbrirFormularioEditar(AplicacaoResult item)
    {
        IdEdicao = item.Id;
        FormTitulo = item.Titulo;
        FormDescricao = item.Descricao ?? string.Empty;
        FormTipoLicencaSelecionado = TiposLicenca.FirstOrDefault(t => t.Id == item.IdTipoLicenca);
        ErroFormulario = null;
        ExibirFormulario = true;
    }

    [RelayCommand]
    void FecharFormulario() => ExibirFormulario = false;

    [RelayCommand]
    async Task SalvarAsync()
    {
        if (string.IsNullOrWhiteSpace(FormTitulo))
        {
            ErroFormulario = "Título é obrigatório.";
            return;
        }

        if (FormTipoLicencaSelecionado is null)
        {
            ErroFormulario = "Selecione um tipo de licença.";
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
                var req = new CriarAplicacaoRequest(
                    FormTitulo,
                    string.IsNullOrWhiteSpace(FormDescricao) ? null : FormDescricao,
                    FormTipoLicencaSelecionado.Id);
                (sucesso, _, erro, _) = await factory.Aplicacao.CriarAsync(req);
            }
            else
            {
                var req = new AtualizarAplicacaoRequest(
                    FormTitulo,
                    string.IsNullOrWhiteSpace(FormDescricao) ? null : FormDescricao);
                (sucesso, _, erro, _) = await factory.Aplicacao.AtualizarAsync(IdEdicao.Value, req);
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
