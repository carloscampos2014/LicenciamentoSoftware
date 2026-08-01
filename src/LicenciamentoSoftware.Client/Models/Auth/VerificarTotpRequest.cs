namespace LicenciamentoSoftware.Client.Models.Auth;

public sealed record VerificarTotpRequest(string TokenTemporario, string Codigo);
