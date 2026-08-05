using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Commands;
using LicenciamentoSoftware.Application.Auth.Results;
using LicenciamentoSoftware.Application.Cliente.Abstractions;

namespace LicenciamentoSoftware.Application.Auth.Handlers;

public sealed class LoginHandler
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IClock _clock;

    // Token temporário de desafio 2FA expira em 5 minutos
    private static readonly TimeSpan _desafioExpiracao = TimeSpan.FromMinutes(5);

    public LoginHandler(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtService,
        IRefreshTokenRepository refreshTokenRepository,
        IClienteRepository clienteRepository,
        IClock clock)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _refreshTokenRepository = refreshTokenRepository;
        _clienteRepository = clienteRepository;
        _clock = clock;
    }

    public async Task<AuthResult> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository
            .BuscarPorEmailAsync(command.Email, cancellationToken);

        if (usuario is null || !usuario.Ativo)
            return new AuthResult.Negado("Credenciais inválidas.");

        if (!_passwordHasher.Verificar(command.Senha, usuario.SenhaHash))
            return new AuthResult.Negado("Credenciais inválidas.");

        // Verifica se a empresa (tenant) do usuário está ativa.
        // Conta encerrada: usuários ficam desativados mas verificamos o cliente
        // como defesa em profundidade — impede login mesmo que um usuário tenha
        // sido reativado manualmente sem restaurar o cliente.
        var cliente = await _clienteRepository.BuscarPorIdAsync(usuario.IdCliente, cancellationToken);
        if (cliente is null || !cliente.Ativo)
            return new AuthResult.Negado("Credenciais inválidas.");

        // Se 2FA habilitado, emite token temporário de desafio
        if (usuario.TotpSecretHash is not null)
        {
            // Token temporário contém apenas o ID do usuário — não é JWT completo
            var tokenPar = _jwtService.GerarTokenPar(
                usuario.Id, usuario.IdCliente, usuario.Nome, "Desafio2FA", usuario.Email);

            return new AuthResult.Requer2FA(tokenPar.AccessToken);
        }

        return await EmitirTokenCompletoAsync(usuario, cancellationToken);
    }

    internal async Task<AuthResult> EmitirTokenCompletoAsync(
        Domain.Entities.Usuario usuario,
        CancellationToken cancellationToken)
    {
        // Busca o papel do usuário no repositório
        var papel = await _usuarioRepository
            .BuscarPapelAsync(usuario.Id, cancellationToken);

        var tokenPar = _jwtService.GerarTokenPar(
            usuario.Id, usuario.IdCliente, usuario.Nome, papel, usuario.Email);

        var refreshHash = tokenPar.RefreshToken;
        var expiracao = _clock.UtcNow.AddDays(30);

        await _refreshTokenRepository.SalvarAsync(
            usuario.Id, refreshHash, expiracao, cancellationToken);

        return new AuthResult.Sucesso(
            tokenPar.AccessToken,
            tokenPar.RefreshToken,
            tokenPar.AccessTokenExpiracao,
            usuario.Nome,
            papel);
    }
}
