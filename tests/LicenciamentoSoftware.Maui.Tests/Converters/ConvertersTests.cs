using System.Globalization;

namespace LicenciamentoSoftware.Maui.Tests.Converters;

// ── Interface stub (sem dependência do MAUI) ──────────────────────────────────

internal interface IValueConverter
{
    object Convert(object? value, Type targetType, object? parameter, CultureInfo culture);
    object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture);
}

// ── Implementações que replicam a lógica dos Converters originais ─────────────

internal sealed class StringNullOrEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.IsNullOrEmpty(value?.ToString());
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

internal sealed class StringNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => !string.IsNullOrEmpty(value?.ToString());
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

internal sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}

internal sealed class TipoInscricaoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int v ? v - 1 : 1;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int idx ? idx + 1 : 2;
}

// ── Testes ────────────────────────────────────────────────────────────────────

public sealed class StringNullOrEmptyConverterTests
{
    private readonly StringNullOrEmptyConverter _sut = new();

    [Fact]
    public void Convert_NullValue_ReturnsTrue()
        => ((bool)_sut.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture))
            .Should().BeTrue();

    [Fact]
    public void Convert_EmptyString_ReturnsTrue()
        => ((bool)_sut.Convert(string.Empty, typeof(bool), null, CultureInfo.InvariantCulture))
            .Should().BeTrue();

    [Fact]
    public void Convert_NonEmptyString_ReturnsFalse()
        => ((bool)_sut.Convert("hello", typeof(bool), null, CultureInfo.InvariantCulture))
            .Should().BeFalse();

    [Fact]
    public void ConvertBack_Throws_NotSupportedException()
        => _sut.Invoking(c => c.ConvertBack(true, typeof(string), null, CultureInfo.InvariantCulture))
            .Should().Throw<NotSupportedException>();
}

public sealed class StringNotNullConverterTests
{
    private readonly StringNotNullConverter _sut = new();

    [Fact]
    public void Convert_NullValue_ReturnsFalse()
        => ((bool)_sut.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture))
            .Should().BeFalse();

    [Fact]
    public void Convert_EmptyString_ReturnsFalse()
        => ((bool)_sut.Convert(string.Empty, typeof(bool), null, CultureInfo.InvariantCulture))
            .Should().BeFalse();

    [Fact]
    public void Convert_NonEmptyString_ReturnsTrue()
        => ((bool)_sut.Convert("texto", typeof(bool), null, CultureInfo.InvariantCulture))
            .Should().BeTrue();
}

public sealed class InverseBoolConverterTests
{
    private readonly InverseBoolConverter _sut = new();

    [Fact]
    public void Convert_True_ReturnsFalse()
        => ((bool)_sut.Convert(true, typeof(bool), null, CultureInfo.InvariantCulture))
            .Should().BeFalse();

    [Fact]
    public void Convert_False_ReturnsTrue()
        => ((bool)_sut.Convert(false, typeof(bool), null, CultureInfo.InvariantCulture))
            .Should().BeTrue();

    [Fact]
    public void Convert_NonBool_ReturnsFalse()
        => ((bool)_sut.Convert("texto", typeof(bool), null, CultureInfo.InvariantCulture))
            .Should().BeFalse();

    [Fact]
    public void ConvertBack_True_ReturnsFalse()
        => ((bool)_sut.ConvertBack(true, typeof(bool), null, CultureInfo.InvariantCulture))
            .Should().BeFalse();
}

public sealed class TipoInscricaoConverterTests
{
    private readonly TipoInscricaoConverter _sut = new();

    [Theory]
    [InlineData(1, 0)]  // CPF → índice 0
    [InlineData(2, 1)]  // CNPJ → índice 1
    public void Convert_TipoInscricao_ReturnsCorrectIndex(int tipo, int expectedIndex)
        => ((int)_sut.Convert(tipo, typeof(int), null, CultureInfo.InvariantCulture))
            .Should().Be(expectedIndex);

    [Theory]
    [InlineData(0, 1)]  // índice 0 → CPF (1)
    [InlineData(1, 2)]  // índice 1 → CNPJ (2)
    public void ConvertBack_Index_ReturnsCorrectTipo(int index, int expectedTipo)
        => ((int)_sut.ConvertBack(index, typeof(int), null, CultureInfo.InvariantCulture))
            .Should().Be(expectedTipo);
}
