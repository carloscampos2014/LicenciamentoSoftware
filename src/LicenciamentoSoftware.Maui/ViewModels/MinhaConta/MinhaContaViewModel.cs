using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LicenciamentoSoftware.Maui.Services;
using LicenciamentoSoftware.Maui.ViewModels.Base;

namespace LicenciamentoSoftware.Maui.ViewModels.MinhaConta;

public partial class MinhaContaViewModel(
    MauiApiClientFactory factory,
    MauiAuthService authService) : BaseViewModel
{
    // ── 2FA ───────────────────────────────────────────────────────────────────

    [ObservableProperty] bool? _totpAtivo;
    [ObservableProperty] bool _exibirSetup2FA;
    [ObservableProperty] bool _exibirDesativar2FA;
    [ObservableProperty] string? _segredoTotp;
    [ObservableProperty] string? _erroTotp;
    [ObservableProperty] string? _sucessoTotp;
    [ObservableProperty] string _codigoConfirmacaoTotp = string.Empty;
    [ObservableProperty] string _codigoDesativarTotp   = string.Empty;

    // ── Alterar senha ─────────────────────────────────────────────────────────

    [ObservableProperty] string _senhaAtual        = string.Empty;
    [ObservableProperty] string _novaSenha         = string.Empty;
    [ObservableProperty] string _confirmacaoSenha  = string.Empty;
    [ObservableProperty] string? _erroSenha;
    [ObservableProperty] bool _sucessoSenha;

    // ─────────────────────────────────────────────────────────────────────────

    protected override async Task OnCarregarAsync()
    {
        Titulo = "Minha Conta";
        Ocupado = true;
        ErroTotp = null;
        ErroSenha = null;
        SucessoSenha = false;
        SucessoTotp = null;

        try
        {
            TotpAtivo = await factory.Totp.BuscarStatusAsync();
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

            SegredoTotp    = segredo;
            ExibirSetup2FA = true;
        }
        finally { Ocupado = false; }
    }

    [RelayCommand]
    async Task ConfirmarSetup2FAAsync()
    {
        if (CodigoConfirmacaoTotp.Length != 6) return;
        ErroTotp = null;
        Ocupado  = true;
        try
        {
            var (sucesso, erro) = await factory.Totp.ConfirmarAsync(CodigoConfirmacaoTotp);
            if (sucesso)
            {
                TotpAtivo             = true;
                ExibirSetup2FA        = false;
                SegredoTotp           = null;
                CodigoConfirmacaoTotp = string.Empty;
                SucessoTotp           = "Autenticação de dois fatores ativada com sucesso.";
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
        ExibirSetup2FA        = false;
        SegredoTotp           = null;
        CodigoConfirmacaoTotp = string.Empty;
        ErroTotp              = null;
    }

    [RelayCommand]
    async Task CopiarSegredoAsync()
    {
        if (SegredoTotp is null) return;
        await Clipboard.SetTextAsync(SegredoTotp);
    }

    // ── 2FA — Desativar ───────────────────────────────────────────────────────

    [RelayCommand]
    void MostrarDesativar2FA()
    {
        CodigoDesativarTotp = string.Empty;
        ErroTotp            = null;
        ExibirDesativar2FA  = true;
    }

    [RelayCommand]
    async Task ConfirmarDesativar2FAAsync()
    {
        if (CodigoDesativarTotp.Length != 6) return;
        ErroTotp = null;
        Ocupado  = true;
        try
        {
            var (sucesso, erro) = await factory.Totp.DesativarAsync(CodigoDesativarTotp);
            if (sucesso)
            {
                TotpAtivo           = false;
                ExibirDesativar2FA  = false;
                CodigoDesativarTotp = string.Empty;
                SucessoTotp         = "Autenticação de dois fatores desativada.";
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
        ExibirDesativar2FA  = false;
        CodigoDesativarTotp = string.Empty;
        ErroTotp            = null;
    }

    // ── Alterar senha ─────────────────────────────────────────────────────────

    [RelayCommand]
    async Task AlterarSenhaAsync()
    {
        ErroSenha    = null;
        SucessoSenha = false;

        if (string.IsNullOrWhiteSpace(SenhaAtual))
            { ErroSenha = "Informe a senha atual."; return; }
        if (string.IsNullOrWhiteSpace(NovaSenha) || NovaSenha.Length < 8)
            { ErroSenha = "A nova senha deve ter pelo menos 8 caracteres."; return; }
        if (NovaSenha != ConfirmacaoSenha)
            { ErroSenha = "As senhas não conferem."; return; }

        Ocupado = true;
        try
        {
            var (sucesso, erro) = await factory.Auth.AlterarSenhaAsync(
                SenhaAtual, NovaSenha, ConfirmacaoSenha);

            if (sucesso)
            {
                SucessoSenha     = true;
                SenhaAtual       = string.Empty;
                NovaSenha        = string.Empty;
                ConfirmacaoSenha = string.Empty;
            }
            else
            {
                ErroSenha = erro ?? "Erro ao alterar senha. Tente novamente.";
            }
        }
        catch
        {
            ErroSenha = "Não foi possível conectar ao servidor.";
        }
        finally { Ocupado = false; }
    }
}
