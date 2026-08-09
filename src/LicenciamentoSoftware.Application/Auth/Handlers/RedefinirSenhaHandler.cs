using LicenciamentoSoftware.Application.Abstractions;

namespace LicenciamentoSoftware.Application.Auth.Handlers;

public abstract record RedefinirSenhaResult
{
    public sealed record Sucesso : RedefinirSenhaResult;
    public sealed record TokenInvalidoOuExpirado : RedefinirSenhaResult;
    public sealed record Invalido(IReadOnlyList<string> Erros) : RedefinirSenhaResult;
}

public sealed record RedefinirSenhaCommand(
    string Token,
    string NovaSenha,
    string ConfirmacaoNovaSenha);

/// <summary>
/// Redefine a senha do usuário usando o token recebido por e-mail.
/// Valida: token existe, não expirou, não foi usado.
/// Após sucesso: marca o token como usado e revoga todos os refresh tokens.
/// </summary>
public sealed class RedefinirSenhaHandler
{
    private readonly IRecuperacaoSenhaRepository _recuperacaoRepo;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public RedefinirSenhaHandler(
        IRecuperacaoSenhaRepository recuperacaoRepo,
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork uow,
        IClock clock)
    {
        _recuperacaoRepo   = recuperacaoRepo;
        _usuarioRepository = usuarioRepository;
        _passwordHasher    = passwordHasher;
        _uow               = uow;
        _clock             = clock;
    }

    public async Task<RedefinirSenhaResult> HandleAsync(
        RedefinirSenhaCommand command,
        CancellationToken ct = default)
    {
        // 1. Validações básicas
        var erros = new List<string>();
        if (string.IsNullOrWhiteSpace(command.NovaSenha) || command.NovaSenha.Length < 8)
            erros.Add("A nova senha deve ter pelo menos 8 caracteres.");
        if (command.NovaSenha != command.ConfirmacaoNovaSenha)
            erros.Add("A confirmação da nova senha não confere.");
        if (erros.Count > 0)
            return new RedefinirSenhaResult.Invalido(erros);

        // 2. Calcular hash do token recebido e buscar no banco
        var tokenHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(command.Token))).ToLowerInvariant();

        var tokenRecord = await _recuperacaoRepo.BuscarPorHashAsync(tokenHash, ct);
        if (tokenRecord is null || tokenRecord.ExpiraEm < _clock.UtcNow)
            return new RedefinirSenhaResult.TokenInvalidoOuExpirado();

        // 3. Definir nova senha e revogar tokens
        var novoHash = _passwordHasher.Hash(command.NovaSenha);

        await _uow.BeginAsync(cancellationToken: ct);
        try
        {
            await _recuperacaoRepo.MarcarComoUsadoAsync(tokenRecord.Id, ct);
            await _usuarioRepository.DefinirSenhaAsync(tokenRecord.IdUsuario, novoHash, ct);
            await _usuarioRepository.RevogarTodosRefreshTokensAsync(tokenRecord.IdUsuario, ct);
            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }

        return new RedefinirSenhaResult.Sucesso();
    }
}
