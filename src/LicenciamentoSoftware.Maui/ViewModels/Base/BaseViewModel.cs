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

    /// <summary>Executado quando a página aparece — sobrescrever para carregar dados.</summary>
    public virtual Task OnAppearing() => Task.CompletedTask;
}
