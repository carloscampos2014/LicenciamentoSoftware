using LicenciamentoSoftware.Application.Abstractions;

namespace LicenciamentoSoftware.Application.Auth.Handlers;

public abstract record ConfirmarReset2FAResult
{
    public sealed record Sucesso(Guid IdSolicitacao) : ConfirmarReset2FAResult;
    public sealed record TokenInvalidoOuExpirado : ConfirmarReset2FAResult;
}

/// <summary>
/// Passo 2 do reset de 2FA: valida o token recebido por e-mail e cria a solicitação Pendente.
/// Após este passo o Admin precisa aprovar para o reset ser executado.
/// </summary>
public sealed class ConfirmarReset2FAHandler
{
    private readonly ISolicitacaoReset2FARepository _solicitacaoRepo;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public ConfirmarReset2FAHandler(
        ISolicitacaoReset2FARepository solicitacaoRepo,
        IUnitOfWork uow,
        IClock clock)
    {
        _solicitacaoRepo = solicitacaoRepo;
        _uow             = uow;
        _clock           = clock;
    }

    public async Task<ConfirmarReset2FAResult> HandleAsync(
        string token, CancellationToken ct = default)
    {
        var tokenHash = System.Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

        var registro = await _solicitacaoRepo.BuscarTokenAsync(tokenHash, ct);

        if (registro is null || registro.ExpiraEm < _clock.UtcNow)
            return new ConfirmarReset2FAResult.TokenInvalidoOuExpirado();

        await _uow.BeginAsync(cancellationToken: ct);
        Guid idSolicitacao;
        try
        {
            idSolicitacao = await _solicitacaoRepo.ConfirmarECriarSolicitacaoAsync(registro.Id, ct);
            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }

        return new ConfirmarReset2FAResult.Sucesso(idSolicitacao);
    }
}
