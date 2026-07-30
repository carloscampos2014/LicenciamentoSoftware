using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Commands;
using LicenciamentoSoftware.Application.Auth.Results;
using LicenciamentoSoftware.Domain.Entities;
using LicenciamentoSoftware.Domain.Enums;

namespace LicenciamentoSoftware.Application.Auth.Handlers;

public sealed class RegistrarUsuarioHandler
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogWriter _auditLog;
    private readonly IUnitOfWork _uow;

    public RegistrarUsuarioHandler(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IAuditLogWriter auditLog,
        IUnitOfWork uow)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _auditLog = auditLog;
        _uow = uow;
    }

    public async Task<RegistrarResult> HandleAsync(
        RegistrarUsuarioCommand command,
        CancellationToken cancellationToken = default)
    {
        // Verifica se e-mail já está em uso
        var existente = await _usuarioRepository
            .BuscarPorEmailAsync(command.Email, cancellationToken);

        if (existente is not null)
            return new RegistrarResult.EmailJaEmUso();

        // Primeiro usuário do cliente vira AdministradorCliente
        var temAdmin = await _usuarioRepository
            .ExisteAdminParaClienteAsync(command.IdCliente, cancellationToken);

        var papel = temAdmin ? "OperadorCliente" : "AdministradorCliente";

        var senhaHash = _passwordHasher.Hash(command.Senha);
        var usuario = Usuario.Criar(command.IdCliente, command.Nome, senhaHash);

        await _uow.BeginAsync(cancellationToken: cancellationToken);

        try
        {
            await _usuarioRepository.SalvarAsync(usuario, papel, cancellationToken);

            var log = LogOperacao.Criar(
                entidade: "Usuario",
                idRegistro: usuario.Id,
                operacao: TipoOperacao.Insercao);

            await _auditLog.RegistrarAsync(log, cancellationToken);
            await _uow.CommitAsync(cancellationToken);
        }
        catch
        {
            await _uow.RollbackAsync(cancellationToken);
            throw;
        }

        return new RegistrarResult.Sucesso(usuario.Id, usuario.Nome, papel);
    }
}
