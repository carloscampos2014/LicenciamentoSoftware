namespace LicenciamentoSoftware.Application.Auth.Commands;

public sealed record AutoCadastrarClienteCommand(
    string RazaoSocial,
    int TipoInscricao,
    string NumeroInscricao,
    string EmailCliente,
    string? Telefone,
    string NomeResponsavel,
    string EmailResponsavel,
    string Senha);
