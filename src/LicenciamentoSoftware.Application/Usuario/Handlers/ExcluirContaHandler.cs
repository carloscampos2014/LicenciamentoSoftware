using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Cliente.Abstractions;
using LicenciamentoSoftware.Application.Usuario.Commands;
using LicenciamentoSoftware.Application.Usuario.Results;

namespace LicenciamentoSoftware.Application.Usuario.Handlers;

/// <summary>
/// LGPD Art. 18 — anonimiza os dados pessoais do titular.
///
/// Comportamento por papel:
///   - AdministradorCliente: conta permanece ativa, nome/email substituídos pelos dados da empresa.
///   - Demais papéis: conta desativada + anonimização completa.
///
/// Em ambos os casos: senha, totp_secret e todos os refresh tokens são revogados.
/// Logs de auditoria são preservados (obrigação legal).
/// </summary>
public sealed class ExcluirContaHandler
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IClienteRepository _clienteRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _uow;

    public ExcluirContaHandler(
        IUsuarioRepository usuarioRepo,
        IClienteRepository clienteRepo,
        IPasswordHasher passwordHasher,
        IUnitOfWork uow)
    {
        _usuarioRepo    = usuarioRepo;
        _clienteRepo    = clienteRepo;
        _passwordHasher = passwordHasher;
        _uow            = uow;
    }

    public async Task<ExcluirContaResult> HandleAsync(
        ExcluirContaCommand command,
        CancellationToken ct = default)
    {
        // 1. Buscar usuário
        var usuario = await _usuarioRepo.BuscarPorIdAsync(command.IdUsuario, ct);
        if (usuario is null)
            return new ExcluirContaResult.NaoEncontrado();

        // 2. Verificar senha
        if (!_passwordHasher.Verificar(command.SenhaAtual, usuario.SenhaHash))
            return new ExcluirContaResult.SenhaInvalida();

        // 3. Verificar papel
        var papel = await _usuarioRepo.BuscarPapelAsync(command.IdUsuario, ct);
        var ehAdmin = papel == "AdministradorCliente";

        // 4. Se for o único admin ativo, bloquear exclusão
        if (ehAdmin)
        {
            var existeOutroAdmin = await ExisteOutroAdminAsync(command.IdCliente, command.IdUsuario, ct);
            if (!existeOutroAdmin)
                return new ExcluirContaResult.UltimoAdministrador();
        }

        // 5. Buscar dados da empresa para substituição (admin) ou usar genérico (demais)
        string nomeSubstituto;
        string emailSubstituto;

        if (ehAdmin)
        {
            var cliente = await _clienteRepo.BuscarPorIdAsync(command.IdCliente, ct);
            nomeSubstituto  = cliente?.RazaoSocial ?? "Empresa";
            emailSubstituto = cliente?.Email       ?? $"empresa-{command.IdCliente}@anonimizado.local";
        }
        else
        {
            nomeSubstituto  = "Usuário Removido";
            emailSubstituto = $"removido-{command.IdUsuario}@anonimizado.local";
        }

        // 6. Persistir em transação
        await _uow.BeginAsync(cancellationToken: ct);
        try
        {
            await _usuarioRepo.AnonimizarAsync(
                command.IdUsuario, nomeSubstituto, emailSubstituto, ct);

            if (!ehAdmin)
                await _usuarioRepo.DesativarUsuarioAsync(command.IdUsuario, ct);

            await _usuarioRepo.RevogarTodosRefreshTokensAsync(command.IdUsuario, ct);

            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }

        return new ExcluirContaResult.Sucesso();
    }

    private async Task<bool> ExisteOutroAdminAsync(
        Guid idCliente, Guid idUsuarioExcluindo, CancellationToken ct)
    {
        // Reutiliza ExisteAdminParaClienteAsync mas precisa excluir o próprio usuário.
        // Como não há método específico, verificamos contando admins ativos != idUsuarioExcluindo.
        return await _usuarioRepo.ExisteOutroAdminAsync(idCliente, idUsuarioExcluindo, ct);
    }
}
