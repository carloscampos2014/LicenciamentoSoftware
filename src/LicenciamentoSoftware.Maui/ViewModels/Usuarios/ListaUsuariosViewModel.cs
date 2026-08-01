using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LicenciamentoSoftware.Client.Models.Usuarios;
using LicenciamentoSoftware.Maui.Services;
using LicenciamentoSoftware.Maui.ViewModels.Base;
using System.Collections.ObjectModel;

namespace LicenciamentoSoftware.Maui.ViewModels.Usuarios;

public partial class ListaUsuariosViewModel(MauiApiClientFactory factory) : BaseViewModel
{
    // ── Estado da lista ───────────────────────────────────────────────────────

    [ObservableProperty] ObservableCollection<UsuarioResult> _itens = [];
    [ObservableProperty] string _busca = string.Empty;
    [ObservableProperty] bool? _filtroAtivo = null;
    [ObservableProperty] int _totalRegistros;
    [ObservableProperty] int _paginaAtual = 1;
    [ObservableProperty] bool _temMaisPaginas;
    [ObservableProperty] string? _erro;

    // ── Formulário ────────────────────────────────────────────────────────────

    [ObservableProperty] bool _exibirFormulario;
    [ObservableProperty] Guid? _idEdicao;
    [ObservableProperty] string _formNome = string.Empty;
    [ObservableProperty] string _formEmail = string.Empty;
    [ObservableProperty] string _formSenha = string.Empty;
    [ObservableProperty] string _formPapel = "Operador";
    [ObservableProperty] string? _erroFormulario;

    public IReadOnlyList<string> PapeisDisponiveis { get; } = ["Administrador", "Operador"];

    private const int TamanhoPagina = 20;

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    public override async Task OnAppearing()
    {
        Titulo = "Usuários";
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
            var resultado = await factory.Usuario.ListarAsync(
                nome: string.IsNullOrWhiteSpace(Busca) ? null : Busca,
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
            Erro = $"Erro ao carregar usuários: {ex.Message}";
        }
        finally
        {
            Ocupado = false;
        }
    }

    // ── Desativar ─────────────────────────────────────────────────────────────

    [RelayCommand]
    async Task DesativarAsync(UsuarioResult item)
    {
        var (sucesso, erro) = await factory.Usuario.DesativarAsync(item.Id);

        if (sucesso)
            await CarregarAsync();
        else
            Erro = erro ?? "Erro ao desativar usuário.";
    }

    // ── Formulário ────────────────────────────────────────────────────────────

    [RelayCommand]
    void AbrirFormularioCriar()
    {
        IdEdicao = null;
        FormNome = string.Empty;
        FormEmail = string.Empty;
        FormSenha = string.Empty;
        FormPapel = "Operador";
        ErroFormulario = null;
        ExibirFormulario = true;
    }

    [RelayCommand]
    void AbrirFormularioEditar(UsuarioResult item)
    {
        IdEdicao = item.Id;
        FormNome = item.Nome;
        FormEmail = item.Email;
        FormSenha = string.Empty;   // senha não é carregada
        FormPapel = item.Papel;
        ErroFormulario = null;
        ExibirFormulario = true;
    }

    [RelayCommand]
    void FecharFormulario() => ExibirFormulario = false;

    [RelayCommand]
    async Task SalvarAsync()
    {
        if (string.IsNullOrWhiteSpace(FormNome) || string.IsNullOrWhiteSpace(FormEmail))
        {
            ErroFormulario = "Nome e e-mail são obrigatórios.";
            return;
        }

        if (IdEdicao is null && string.IsNullOrWhiteSpace(FormSenha))
        {
            ErroFormulario = "Senha é obrigatória ao criar usuário.";
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
                var req = new CriarUsuarioRequest(FormNome, FormEmail, FormSenha, FormPapel);
                (sucesso, _, erro, _) = await factory.Usuario.CriarAsync(req);
            }
            else
            {
                var req = new AtualizarUsuarioRequest(FormNome, FormEmail);
                (sucesso, _, erro, _) = await factory.Usuario.AtualizarAsync(IdEdicao.Value, req);
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
