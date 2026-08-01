using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LicenciamentoSoftware.Client.Models.Auth;
using LicenciamentoSoftware.Maui.Services;
using LicenciamentoSoftware.Maui.ViewModels.Base;

namespace LicenciamentoSoftware.Maui.ViewModels;

public partial class CadastroViewModel(MauiApiClientFactory factory) : BaseViewModel
{
    [ObservableProperty] string _razaoSocial    = string.Empty;
    [ObservableProperty] int    _tipoInscricao  = 2;
    [ObservableProperty] string _numeroInscricao = string.Empty;
    [ObservableProperty] string _emailCliente   = string.Empty;
    [ObservableProperty] string _telefone       = string.Empty;
    [ObservableProperty] string _nomeResponsavel = string.Empty;
    [ObservableProperty] string _emailResponsavel = string.Empty;
    [ObservableProperty] string _senha          = string.Empty;
    [ObservableProperty] string _confirmarSenha = string.Empty;
    [ObservableProperty] string? _erro;
    [ObservableProperty] bool   _sucesso;

    [RelayCommand]
    async Task CadastrarAsync()
    {
        if (Senha != ConfirmarSenha) { Erro = "As senhas não conferem."; return; }
        if (string.IsNullOrWhiteSpace(RazaoSocial) ||
            string.IsNullOrWhiteSpace(NumeroInscricao) ||
            string.IsNullOrWhiteSpace(EmailCliente) ||
            string.IsNullOrWhiteSpace(NomeResponsavel) ||
            string.IsNullOrWhiteSpace(EmailResponsavel) ||
            string.IsNullOrWhiteSpace(Senha))
        {
            Erro = "Preencha todos os campos obrigatórios.";
            return;
        }

        Ocupado = true;
        Erro = null;

        try
        {
            var (ok, errMsg, erros) = await factory.Auth.CadastrarAsync(new AutoCadastroRequest(
                RazaoSocial, TipoInscricao, NumeroInscricao,
                EmailCliente, string.IsNullOrWhiteSpace(Telefone) ? null : Telefone,
                NomeResponsavel, EmailResponsavel, Senha));

            if (ok) Sucesso = true;
            else    Erro    = errMsg
                           ?? (erros is not null ? string.Join("; ", erros) : null)
                           ?? "Erro ao cadastrar.";
        }
        finally
        {
            Ocupado = false;
        }
    }

    [RelayCommand]
    static async Task IrParaLoginAsync()
        => await Shell.Current.GoToAsync("//login");
}
