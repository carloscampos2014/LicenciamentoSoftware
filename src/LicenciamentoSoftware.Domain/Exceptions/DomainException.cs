namespace LicenciamentoSoftware.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando uma invariante de domínio é violada.
/// Não deve ser capturada dentro do domínio — sobe até a camada de aplicação.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
