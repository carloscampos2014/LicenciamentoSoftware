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

    protected bool Carregado { get; private set; }

    public async Task OnAppearing(bool forcarRecarga = false)
    {
        if (Carregado && !forcarRecarga) return;
        await OnCarregarAsync();
        Carregado = true;
    }

    protected virtual Task OnCarregarAsync() => Task.CompletedTask;

    public void ResetarCarregado() => Carregado = false;
}

file sealed partial class ConcreteViewModel : BaseViewModelTestable
{
    public int AppearingCount { get; private set; }

    protected override Task OnCarregarAsync()
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

    [Fact]
    public async Task OnAppearing_WhenCalledTwice_LoadsOnlyOnce()
    {
        var vm = new ConcreteViewModel();
        await vm.OnAppearing();
        await vm.OnAppearing();
        vm.AppearingCount.Should().Be(1);
    }

    [Fact]
    public async Task OnAppearing_ForcarRecarga_LoadsAgain()
    {
        var vm = new ConcreteViewModel();
        await vm.OnAppearing();
        await vm.OnAppearing(forcarRecarga: true);
        vm.AppearingCount.Should().Be(2);
    }
}
