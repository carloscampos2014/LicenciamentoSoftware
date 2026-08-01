using CommunityToolkit.Mvvm.ComponentModel;

namespace LicenciamentoSoftware.Maui.Tests.ViewModels;

// ── Implementação local do BaseViewModel (sem dependência do MAUI) ────────────

public abstract partial class BaseViewModelTestable : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NaoOcupado))]
    bool _ocupado;

    [ObservableProperty]
    string _titulo = string.Empty;

    public bool NaoOcupado => !Ocupado;

    public virtual Task OnAppearing() => Task.CompletedTask;
}

file sealed partial class ConcreteViewModel : BaseViewModelTestable
{
    public int AppearingCount { get; private set; }

    public override Task OnAppearing()
    {
        AppearingCount++;
        return Task.CompletedTask;
    }
}

// ── Testes ────────────────────────────────────────────────────────────────────

public sealed class BaseViewModelTests
{
    [Fact]
    public void NaoOcupado_WhenOcupadoFalse_ReturnsTrue()
    {
        var vm = new ConcreteViewModel();
        vm.NaoOcupado.Should().BeTrue();
    }

    [Fact]
    public void NaoOcupado_WhenOcupadoTrue_ReturnsFalse()
    {
        var vm = new ConcreteViewModel { Ocupado = true };
        vm.NaoOcupado.Should().BeFalse();
    }

    [Fact]
    public void Ocupado_WhenSet_RaisesPropertyChangedForNaoOcupado()
    {
        var vm = new ConcreteViewModel();
        var changedProperties = new List<string?>();
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        vm.Ocupado = true;

        changedProperties.Should().Contain(nameof(vm.Ocupado));
        changedProperties.Should().Contain(nameof(vm.NaoOcupado));
    }

    [Fact]
    public void Titulo_DefaultValue_IsEmptyString()
    {
        var vm = new ConcreteViewModel();
        vm.Titulo.Should().BeEmpty();
    }

    [Fact]
    public async Task OnAppearing_WhenCalled_ExecutesOverride()
    {
        var vm = new ConcreteViewModel();
        await vm.OnAppearing();
        vm.AppearingCount.Should().Be(1);
    }
}
