using System.Globalization;

namespace LicenciamentoSoftware.Maui.Converters;

/// <summary>Retorna true quando o valor é null ou string vazia.</summary>
public sealed class StringNullOrEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.IsNullOrEmpty(value?.ToString());

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Retorna true quando o valor não é null nem string vazia.</summary>
public sealed class StringNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => !string.IsNullOrEmpty(value?.ToString());

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Inverte um bool.</summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}

/// <summary>Converte TipoInscricao int (1=CPF, 2=CNPJ) para texto exibível.</summary>
public sealed class TipoInscricaoTextoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int v ? (v == 1 ? "CPF" : "CNPJ") : "CPF";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Converte TipoInscricao int (1=CPF, 2=CNPJ) para índice do Picker (0=CPF, 1=CNPJ).</summary>
public sealed class TipoInscricaoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int v ? v - 1 : 1;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int idx ? idx + 1 : 2;
}
