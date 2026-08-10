using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LicenciamentoSoftware.Client.Models.Clientes;
using LicenciamentoSoftware.Maui.Services;
using LicenciamentoSoftware.Maui.ViewModels.Base;

namespace LicenciamentoSoftware.Maui.ViewModels.MinhaEmpresa;

public partial class MinhaEmpresaViewModel(
    MauiApiClientFactory factory,
    MauiAuthService authService) : BaseViewModel
{
    // ── Dados da empresa ──────────────────────────────────────────────────────

    [ObservableProperty] ClienteResult? _empresa;
    [ObservableProperty] string _formRazaoSocial  = string.Empty;
    [ObservableProperty] string _formEmail        = string.Empty;
    [ObservableProperty] string? _formTelefone;
    [ObservableProperty] string? _erroSalvar;
    [ObservableProperty] bool _sucessoSalvar;

    // ── Encerrar conta ────────────────────────────────────────────────────────

    [ObservableProperty] bool _exibirModalEncerrar;
    [ObservableProperty] string _senhaConfirmacao   = string.Empty;
    [ObservableProperty] bool _exclusaoImediata;
    [ObservableProperty] string? _erroEncerrar;

    // ─────────────────────────────────────────────────────────────────────────

    protected override async Task OnCarregarAsync()
    {
        Titulo = "Minha Empresa";
        Ocupado = true;
        ErroSalvar = null;

        try
        {
            var idCliente = authService.ObterIdCliente();
            if (idCliente is not null)
            {
                Empresa = await factory.Cliente.BuscarPorIdAsync(idCliente.Value);
                if (Empresa is not null)
                {
                    FormRazaoSocial = Empresa.RazaoSocial;
                    FormEmail       = Empresa.Email;
                    FormTelefone    = Empresa.Telefone;
                }
            }
        }
        finally
        {
            Ocupado = false;
        }
    }

    // ── Salvar empresa ────────────────────────────────────────────────────────

    [RelayCommand]
    async Task SalvarEmpresaAsync()
    {
        if (string.IsNullOrWhiteSpace(FormRazaoSocial) || string.IsNullOrWhiteSpace(FormEmail))
        {
            ErroSalvar = "Razão social e e-mail são obrigatórios.";
            return;
        }

        Ocupado = true;
        ErroSalvar = null;
        SucessoSalvar = false;

        try
        {
            var idCliente = authService.ObterIdCliente();
            if (idCliente is null) { ErroSalvar = "Sessão inválida."; return; }

            var (sucesso, erro) = await factory.Cliente.AtualizarAsync(
                idCliente.Value,
                new AtualizarClienteRequest(
                    FormRazaoSocial,
                    FormEmail,
                    string.IsNullOrWhiteSpace(FormTelefone) ? null : FormTelefone));

            if (sucesso)
            {
                SucessoSalvar = true;
                Empresa = await factory.Cliente.BuscarPorIdAsync(idCliente.Value);
            }
            else
            {
                ErroSalvar = erro ?? "Erro ao salvar.";
            }
        }
        finally
        {
            Ocupado = false;
        }
    }

    // ── Encerrar conta ────────────────────────────────────────────────────────

    [RelayCommand]
    void AbrirModalEncerrar()
    {
        SenhaConfirmacao = string.Empty;
        ExclusaoImediata = false;
        ErroEncerrar = null;
        ExibirModalEncerrar = true;
    }

    [RelayCommand]
    void FecharModalEncerrar() => ExibirModalEncerrar = false;

    [RelayCommand]
    async Task ConfirmarEncerrarAsync()
    {
        if (string.IsNullOrWhiteSpace(SenhaConfirmacao)) return;
        ErroEncerrar = null;
        Ocupado = true;
        try
        {
            var idCliente = authService.ObterIdCliente();
            if (idCliente is null) { ErroEncerrar = "Sessão inválida."; return; }

            var (sucesso, erro) = await factory.Cliente.EncerrarContaAsync(
                idCliente.Value,
                new EncerrarContaRequest(SenhaConfirmacao, ExclusaoImediata));

            if (sucesso)
            {
                await authService.LogoutAsync();
                await Shell.Current.GoToAsync("//login");
            }
            else
            {
                ErroEncerrar = erro ?? "Erro ao encerrar conta.";
            }
        }
        finally { Ocupado = false; }
    }
}
