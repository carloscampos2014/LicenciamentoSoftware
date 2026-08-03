using CommunityToolkit.Mvvm.ComponentModel;

namespace LicenciamentoSoftware.Maui.ViewModels.Base;

/// <summary>
/// ViewModel base com propriedades comuns a todas as telas.
/// </summary>
public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NaoOcupado))]
    bool _ocupado;

    [ObservableProperty]
    string _titulo = string.Empty;

    public bool NaoOcupado => !Ocupado;

    /// <summary>
    /// True após o primeiro OnAppearing bem-sucedido.
    /// Use para evitar recarregar dados ao voltar para a tela.
    /// </summary>
    protected bool Carregado { get; private set; }

    /// <summary>
    /// Executado quando a página aparece.
    /// Chama OnCarregarAsync apenas na primeira vez (ou se forçado).
    /// </summary>
    public async Task OnAppearing(bool forcarRecarga = false)
    {
        if (Carregado && !forcarRecarga) return;
        await OnCarregarAsync();
        Carregado = true;
    }

    /// <summary>Sobrescrever para carregar dados da tela.</summary>
    protected virtual Task OnCarregarAsync() => Task.CompletedTask;

    /// <summary>Força recarga na próxima visita à tela.</summary>
    public void ResetarCarregado() => Carregado = false;
}
