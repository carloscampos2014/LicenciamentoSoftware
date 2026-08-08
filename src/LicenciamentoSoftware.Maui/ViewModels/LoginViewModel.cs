using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LicenciamentoSoftware.Maui.Services;
using LicenciamentoSoftware.Maui.ViewModels.Base;

namespace LicenciamentoSoftware.Maui.ViewModels;

public partial class LoginViewModel(MauiAuthService authService) : BaseViewModel
{
    [ObservableProperty] string _email = string.Empty;
    [ObservableProperty] string _senha = string.Empty;
    [ObservableProperty] string? _erro;

    [RelayCommand]
    async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Senha))
        {
            Erro = "Preencha e-mail e senha.";
            return;
        }

        Ocupado = true;
        Erro = null;

        try
        {
            var resultado = await authService.LoginAsync(Email, Senha);

            switch (resultado)
            {
                case LoginResultado.Sucesso:
                    await Shell.Current.GoToAsync("//dashboard");
                    break;

                case LoginResultado.Requer2FA r:
                    await Shell.Current.GoToAsync(
                        $"totp?token={Uri.EscapeDataString(r.TokenTemporario)}");
                    break;

                case LoginResultado.Erro e:
                    Erro = e.Mensagem;
                    break;
            }
        }
        finally
        {
            Ocupado = false;
        }
    }

    [RelayCommand]
    static async Task IrParaCadastroAsync()
        => await Shell.Current.GoToAsync("cadastro");

    [RelayCommand]
    static async Task EsqueciSenhaAsync()
    {
        // Abre o portal web no browser — o fluxo de recuperacao e feito pelo Web
        const string url = "https://licensemanager.enzojb.com.br/esqueci-senha";
        await Launcher.OpenAsync(new Uri(url));
    }
}
