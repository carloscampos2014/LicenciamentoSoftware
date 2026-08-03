namespace LicenciamentoSoftware.Maui.Controls;

/// <summary>
/// Popup de confirmação reutilizável.
/// Uso: chamar ShowAsync() e aguardar o bool de retorno (true = confirmou).
/// </summary>
public partial class ConfirmPopup : ContentView
{
    private TaskCompletionSource<bool>? _tcs;

    public ConfirmPopup()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Exibe o popup e aguarda a resposta do usuário.
    /// </summary>
    /// <param name="titulo">Título do diálogo.</param>
    /// <param name="mensagem">Mensagem de confirmação.</param>
    /// <param name="textoBotaoConfirmar">Texto do botão de confirmação (padrão: "Confirmar").</param>
    /// <returns>True se o usuário confirmou, false se cancelou.</returns>
    public Task<bool> ShowAsync(
        string titulo,
        string mensagem,
        string textoBotaoConfirmar = "Confirmar")
    {
        LabelTitulo.Text = titulo;
        LabelMensagem.Text = mensagem;
        BotaoConfirmar.Text = textoBotaoConfirmar;

        _tcs = new TaskCompletionSource<bool>();
        IsVisible = true;

        return _tcs.Task;
    }

    private void OnConfirmarClicked(object? sender, EventArgs e)
    {
        IsVisible = false;
        _tcs?.TrySetResult(true);
    }

    private void OnCancelarClicked(object? sender, EventArgs e)
    {
        IsVisible = false;
        _tcs?.TrySetResult(false);
    }
}
