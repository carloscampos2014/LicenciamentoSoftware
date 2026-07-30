namespace LicenciamentoSoftware.Application.Abstractions;

/// <summary>
/// Representa o usuário autenticado na requisição atual.
/// O IdCliente (tenant) vem exclusivamente do JWT — nunca do corpo da requisição.
/// </summary>
public interface ICurrentUser
{
    Guid Id { get; }
    Guid IdCliente { get; }
    string Nome { get; }
    string Papel { get; }
    bool EstaAutenticado { get; }
}
