namespace LicenciamentoSoftware.Client.Models.Clientes;

/// <summary>Request para encerramento de conta da empresa.</summary>
public sealed record EncerrarContaRequest(
    string SenhaAtual,
    bool ExclusaoImediata = false);
