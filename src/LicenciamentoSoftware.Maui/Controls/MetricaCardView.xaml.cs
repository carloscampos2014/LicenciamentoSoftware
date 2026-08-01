namespace LicenciamentoSoftware.Maui.Controls;

public partial class MetricaCardView : ContentView
{
    // ── Bindable Properties ───────────────────────────────────────────────────

    public static readonly BindableProperty TituloProperty =
        BindableProperty.Create(nameof(Titulo), typeof(string), typeof(MetricaCardView), string.Empty);

    public static readonly BindableProperty ValorProperty =
        BindableProperty.Create(nameof(Valor), typeof(string), typeof(MetricaCardView), "0");

    public static readonly BindableProperty SubtituloProperty =
        BindableProperty.Create(nameof(Subtitulo), typeof(string), typeof(MetricaCardView), null);

    public static readonly BindableProperty CorValorProperty =
        BindableProperty.Create(nameof(CorValor), typeof(Color), typeof(MetricaCardView),
            Color.FromArgb("#1e293b")); // TextPrimary default

    // ── Properties ───────────────────────────────────────────────────────────

    public string Titulo
    {
        get => (string)GetValue(TituloProperty);
        set => SetValue(TituloProperty, value);
    }

    public string Valor
    {
        get => (string)GetValue(ValorProperty);
        set => SetValue(ValorProperty, value);
    }

    public string? Subtitulo
    {
        get => (string?)GetValue(SubtituloProperty);
        set => SetValue(SubtituloProperty, value);
    }

    public Color CorValor
    {
        get => (Color)GetValue(CorValorProperty);
        set => SetValue(CorValorProperty, value);
    }

    public MetricaCardView()
    {
        InitializeComponent();
    }
}
