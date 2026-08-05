namespace LicenciamentoSoftware.Application.Cliente.Commands;

/// <summary>
/// Encerra a conta da empresa do tenant autenticado.
/// </summary>
/// <param name="IdCliente">ID do cliente (tenant) — vem do JWT, nunca do body.</param>
/// <param name="SenhaAtual">Senha do AdministradorCliente para confirmação.</param>
/// <param name="ExclusaoImediata">
/// Se verdadeiro, agenda exclusão física para agora (job executa em até 24h).
/// Se falso, agenda para 90 dias após o encerramento.
/// </param>
/// <param name="IdUsuario">ID do usuário autenticado — para verificação de senha.</param>
public sealed record EncerrarContaEmpresaCommand(
    Guid IdCliente,
    Guid IdUsuario,
    string SenhaAtual,
    bool ExclusaoImediata);
