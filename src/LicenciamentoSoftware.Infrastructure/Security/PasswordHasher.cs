using LicenciamentoSoftware.Application.Abstractions;

namespace LicenciamentoSoftware.Infrastructure.Security;

/// <summary>
/// Implementação de hash de senha usando BCrypt.
/// Work factor 12 — balanceia segurança e performance.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string senha) =>
        BCrypt.Net.BCrypt.HashPassword(senha, workFactor: 12);

    public bool Verificar(string senha, string hash) =>
        BCrypt.Net.BCrypt.Verify(senha, hash);
}
