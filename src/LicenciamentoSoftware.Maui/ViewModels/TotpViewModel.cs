using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LicenciamentoSoftware.Maui.Services;
using LicenciamentoSoftware.Maui.ViewModels.Base;

namespace LicenciamentoSoftware.Maui.ViewModels;

[QueryProperty(nameof(TokenTemporario), "token")]
public partial class TotpViewModel(MauiAuthService authService) : BaseViewModel
{
    [ObservableProperty] string _tokenTemporario = string.Empty;
    [ObservableProperty] string _codigo = string.Empty;
    [ObservableProperty] string? _erro;

    [RelayCommand]
    async Task VerificarAsync()
    {
        if (Codigo.Length != 6)
        {
            Erro = "O código deve ter 6 dígitos.";
            return;
        }

        Ocupado = true;
        Erro = null;

        try
        {
            var resultado = await authService.VerificarTotpAsync(TokenTemporario, Codigo);

            switch (resultado)
            {
                case LoginResultado.Sucesso:
                    await Shell.Current.GoToAsync("//dashboard");
                    break;

                case LoginResultado.Erro e:
                    Erro = e.Mensagem;
                    Codigo = string.Empty;
                    break;
            }
        }
        finally
        {
            Ocupado = false;
        }
    }

    [RelayCommand]
    static async Task VoltarAsync()
        => await Shell.Current.GoToAsync("//login");

    [RelayCommand]
    async Task NaoTenhoAcessoAsync()
    {
        // Abre o portal web passando o token temporario para identificar o usuario
        var url = $"https://licensemanager.enzojb.com.br/reset-2fa-solicitar?token={Uri.EscapeDataString(TokenTemporario)}";
        await Launcher.OpenAsync(new Uri(url));
    }
}
