namespace LicenciamentoSoftware.Client.Models.Auth;

public sealed record AutoCadastroRequest(
    string RazaoSocial,
    int TipoInscricao,
    string NumeroInscricao,
    string EmailCliente,
    string? Telefone,
    string NomeResponsavel,
    string EmailResponsavel,
    string Senha);
