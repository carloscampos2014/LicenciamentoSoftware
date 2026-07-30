using LicenciamentoSoftware.Application.Aplicacao.Commands;
using LicenciamentoSoftware.Application.Aplicacao.Results;
using LicenciamentoSoftware.Application.Common;

namespace LicenciamentoSoftware.Application.Aplicacao.Abstractions;

public interface IAplicacaoRepository
{
    Task<AplicacaoResult?> BuscarPorIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExisteTipoLicencaAsync(Guid idTipoLicenca, CancellationToken ct = default);
    Task<PagedResult<AplicacaoResult>> ListarAsync(Guid? idCliente, string? titulo, bool? ativo, int pagina, int tamanhoPagina, CancellationToken ct = default);
    Task<Guid> InserirAsync(Domain.Entities.Aplicacao aplicacao, CancellationToken ct = default);
    Task AtualizarAsync(AtualizarAplicacaoCommand command, CancellationToken ct = default);
    Task DesativarAsync(Guid id, CancellationToken ct = default);
}
