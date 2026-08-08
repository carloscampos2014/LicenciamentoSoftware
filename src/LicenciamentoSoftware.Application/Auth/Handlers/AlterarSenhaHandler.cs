using LicenciamentoSoftware.Application.Abstractions;

namespace LicenciamentoSoftware.Application.Auth.Handlers;

public abstract record AlterarSenhaResult
{
    public sealed record Sucesso : AlterarSenhaResult;
    public sealed record SenhaAtualIncorreta : AlterarSenhaResult;
    public sealed record UsuarioNaoEncontrado : AlterarSenhaResult;
    public sealed record Invalido(IReadOnlyList<string> Erros) : AlterarSenhaResult;
}

public sealed record AlterarSenhaCommand(
    Guid IdUsuario,
    string SenhaAtual,
    string NovaSenha,
    string ConfirmacaoNovaSenha);

/// <summary>
/// Altera a senha do usuário autenticado.
/// Valida a senha atual com BCrypt, define a nova senha e revoga todos os refresh tokens
/// para forçar re-login em todos os dispositivos.
/// </summary>
public sealed class AlterarSenhaHandler
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _uow;

    public AlterarSenhaHandler(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork uow)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher    = passwordHasher;
        _uow               = uow;
    }

    public async Task<AlterarSenhaResult> HandleAsync(
        AlterarSenhaCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. Validações básicas
        var erros = new List<string>();
        if (string.IsNullOrWhiteSpace(command.NovaSenha) || command.NovaSenha.Length < 8)
            erros.Add("A nova senha deve ter pelo menos 8 caracteres.");
        if (command.NovaSenha != command.ConfirmacaoNovaSenha)
            erros.Add("A confirmação da nova senha não confere.");
        if (erros.Count > 0)
            return new AlterarSenhaResult.Invalido(erros);

        // 2. Buscar usuário
        var usuario = await _usuarioRepository.BuscarPorIdAsync(command.IdUsuario, cancellationToken);
        if (usuario is null || !usuario.Ativo)
            return new AlterarSenhaResult.UsuarioNaoEncontrado();

        // 3. Validar senha atual
        if (usuario.SenhaHash is null || !_passwordHasher.Verificar(command.SenhaAtual, usuario.SenhaHash))
            return new AlterarSenhaResult.SenhaAtualIncorreta();

        // 4. Calcular novo hash e persistir
        var novoHash = _passwordHasher.Hash(command.NovaSenha);

        await _uow.BeginAsync(cancellationToken: cancellationToken);
        try
        {
            await _usuarioRepository.DefinirSenhaAsync(command.IdUsuario, novoHash, cancellationToken);
            // Revoga todos os refresh tokens — força re-login em todos os dispositivos
            await _usuarioRepository.RevogarTodosRefreshTokensAsync(command.IdUsuario, cancellationToken);
            await _uow.CommitAsync(cancellationToken);
        }
        catch
        {
            await _uow.RollbackAsync(cancellationToken);
            throw;
        }

        return new AlterarSenhaResult.Sucesso();
    }
}
