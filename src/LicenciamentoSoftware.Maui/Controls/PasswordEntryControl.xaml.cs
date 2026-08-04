namespace LicenciamentoSoftware.Maui.Controls;

public partial class PasswordEntryControl : Grid
{
    // ── BindableProperty: Value ───────────────────────────────────────────────

    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value),
        typeof(string),
        typeof(PasswordEntryControl),
        string.Empty,
        BindingMode.TwoWay,
        propertyChanged: OnValueChanged);

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    // ── BindableProperty: Placeholder ────────────────────────────────────────

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
        nameof(Placeholder),
        typeof(string),
        typeof(PasswordEntryControl),
        "••••••••",
        propertyChanged: (bindable, _, newVal) =>
        {
            var ctrl = (PasswordEntryControl)bindable;
            ctrl.SenhaEntry.Placeholder = (string)newVal;
        });

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    // ── BindableProperty: ReturnCommand ──────────────────────────────────────

    public static readonly BindableProperty ReturnCommandProperty = BindableProperty.Create(
        nameof(ReturnCommand),
        typeof(System.Windows.Input.ICommand),
        typeof(PasswordEntryControl),
        null,
        propertyChanged: (bindable, _, newVal) =>
        {
            var ctrl = (PasswordEntryControl)bindable;
            ctrl.SenhaEntry.ReturnCommand = (System.Windows.Input.ICommand?)newVal;
        });

    public System.Windows.Input.ICommand? ReturnCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(ReturnCommandProperty);
        set => SetValue(ReturnCommandProperty, value);
    }

    // ── Construtor ────────────────────────────────────────────────────────────

    public PasswordEntryControl()
    {
        InitializeComponent();

        // Sincroniza Value quando o usuário digita
        SenhaEntry.TextChanged += (_, e) =>
        {
            if (Value != e.NewTextValue)
                Value = e.NewTextValue ?? string.Empty;
        };
    }

    // ── Toggle mostrar/ocultar ────────────────────────────────────────────────

    private void OnToggleClicked(object? sender, EventArgs e)
    {
        SenhaEntry.IsPassword = !SenhaEntry.IsPassword;
        ToggleButton.Text = SenhaEntry.IsPassword ? "👁" : "🙈";
    }

    // ── Sync de Value → Entry ─────────────────────────────────────────────────

    private static void OnValueChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var ctrl = (PasswordEntryControl)bindable;
        var newText = (string)newValue;
        if (ctrl.SenhaEntry.Text != newText)
            ctrl.SenhaEntry.Text = newText;
    }
}
