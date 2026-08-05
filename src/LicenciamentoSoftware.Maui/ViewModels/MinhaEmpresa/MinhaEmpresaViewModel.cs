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

    // ── 2FA ───────────────────────────────────────────────────────────────────

    [ObservableProperty] bool? _totpAtivo;
    [ObservableProperty] bool _exibirSetup2FA;
    [ObservableProperty] bool _exibirDesativar2FA;
    [ObservableProperty] string? _segredoTotp;
    [ObservableProperty] string? _erroTotp;
    [ObservableProperty] string? _sucessoTotp;
    [ObservableProperty] string _codigoConfirmacaoTotp = string.Empty;
    [ObservableProperty] string _codigoDesativarTotp   = string.Empty;

    // ── Encerrar conta ────────────────────────────────────────────────────────

    [ObservableProperty] bool _exibirModalEncerrar;
    [ObservableProperty] string _senhaConfirmacao   = string.Empty;
    [ObservableProperty] bool _exclusaoImediata;
    [ObservableProperty] string? _erroEncerrar;

    // ─────────────────────────────────────────────────────────────────────────

    protected override async Task OnCarregarAsync()
    {
        Titulo = "Minha Conta";
        Ocupado = true;
        ErroSalvar = null;
        ErroTotp = null;

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

            TotpAtivo = await factory.Totp.BuscarStatusAsync();
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

    // ── 2FA — Ativar ──────────────────────────────────────────────────────────

    [RelayCommand]
    async Task IniciarSetup2FAAsync()
    {
        ErroTotp = null;
        SucessoTotp = null;
        Ocupado = true;
        try
        {
            var idUsuario = authService.ObterIdUsuario();
            var email     = authService.ObterEmail() ?? authService.Nome ?? string.Empty;
            if (idUsuario is null) { ErroTotp = "Sessão inválida."; return; }

            var (segredo, _, erro) = await factory.Totp.IniciarSetupAsync(idUsuario.Value, email);

            if (erro is not null) { ErroTotp = erro; return; }

            SegredoTotp = segredo;
            ExibirSetup2FA = true;
        }
        finally { Ocupado = false; }
    }

    [RelayCommand]
    async Task ConfirmarSetup2FAAsync()
    {
        if (CodigoConfirmacaoTotp.Length != 6) return;
        ErroTotp = null;
        Ocupado = true;
        try
        {
            var (sucesso, erro) = await factory.Totp.ConfirmarAsync(CodigoConfirmacaoTotp);
            if (sucesso)
            {
                TotpAtivo = true;
                ExibirSetup2FA = false;
                SegredoTotp = null;
                CodigoConfirmacaoTotp = string.Empty;
                SucessoTotp = "Autenticação de dois fatores ativada com sucesso.";
            }
            else
            {
                ErroTotp = erro ?? "Código inválido.";
            }
        }
        finally { Ocupado = false; }
    }

    [RelayCommand]
    void CancelarSetup2FA()
    {
        ExibirSetup2FA = false;
        SegredoTotp = null;
        CodigoConfirmacaoTotp = string.Empty;
        ErroTotp = null;
    }

    // ── 2FA — Desativar ───────────────────────────────────────────────────────

    [RelayCommand]
    void MostrarDesativar2FA()
    {
        CodigoDesativarTotp = string.Empty;
        ErroTotp = null;
        ExibirDesativar2FA = true;
    }

    [RelayCommand]
    async Task ConfirmarDesativar2FAAsync()
    {
        if (CodigoDesativarTotp.Length != 6) return;
        ErroTotp = null;
        Ocupado = true;
        try
        {
            var (sucesso, erro) = await factory.Totp.DesativarAsync(CodigoDesativarTotp);
            if (sucesso)
            {
                TotpAtivo = false;
                ExibirDesativar2FA = false;
                CodigoDesativarTotp = string.Empty;
                SucessoTotp = "Autenticação de dois fatores desativada.";
            }
            else
            {
                ErroTotp = erro ?? "Código inválido.";
            }
        }
        finally { Ocupado = false; }
    }

    [RelayCommand]
    void CancelarDesativar2FA()
    {
        ExibirDesativar2FA = false;
        CodigoDesativarTotp = string.Empty;
        ErroTotp = null;
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
                // Logout — conta encerrada, sessão deve ser encerrada
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

    /// <summary>Copia o segredo TOTP para a área de transferência.</summary>
    [RelayCommand]
    async Task CopiarSegredoAsync()
    {
        if (SegredoTotp is null) return;
        await Clipboard.SetTextAsync(SegredoTotp);
    }
}
