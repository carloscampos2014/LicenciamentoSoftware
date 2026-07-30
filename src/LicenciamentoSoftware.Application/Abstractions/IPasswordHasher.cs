namespace LicenciamentoSoftware.Application.Abstractions;

/// <summary>
/// Porta para hashing e verificação de senhas.
/// A implementação usa BCrypt na Infrastructure.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string senha);
    bool Verificar(string senha, string hash);
}
