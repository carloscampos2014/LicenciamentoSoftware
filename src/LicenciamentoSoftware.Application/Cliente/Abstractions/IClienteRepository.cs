using LicenciamentoSoftware.Application.Cliente.Commands;
using LicenciamentoSoftware.Application.Cliente.Results;
using LicenciamentoSoftware.Application.Common;

namespace LicenciamentoSoftware.Application.Cliente.Abstractions;

public interface IClienteRepository
{
    Task<ClienteResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExisteInscricaoAsync(int tipoInscricao, string numeroInscricao, Guid? ignorarId = null, CancellationToken ct = default);
    Task<PagedResult<ClienteResult>> ListarAsync(string? razaoSocial, bool? ativo, int pagina, int tamanhoPagina, CancellationToken ct = default);
    Task<Guid> InserirAsync(Domain.Entities.Cliente cliente, CancellationToken ct = default);
    Task AtualizarAsync(AtualizarClienteCommand command, CancellationToken ct = default);
    Task DesativarAsync(Guid id, CancellationToken ct = default);
}
