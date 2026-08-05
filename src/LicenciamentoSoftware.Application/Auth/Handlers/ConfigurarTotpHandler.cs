using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Commands;
using LicenciamentoSoftware.Application.Auth.Results;

namespace LicenciamentoSoftware.Application.Auth.Handlers;

public sealed class ConfigurarTotpHandler
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITotpService _totpService;
    private readonly IUnitOfWork _uow;

    public ConfigurarTotpHandler(
        IUsuarioRepository usuarioRepository,
        ITotpService totpService,
        IUnitOfWork uow)
    {
        _usuarioRepository = usuarioRepository;
        _totpService = totpService;
        _uow = uow;
    }

    public async Task<ConfigurarTotpResult?> HandleAsync(
        ConfigurarTotpCommand command,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository
            .BuscarPorIdAsync(command.IdUsuario, cancellationToken);

        if (usuario is null || !usuario.Ativo)
            return null;

        var segredo = _totpService.GerarSegredo();
        var qrCodeUri = _totpService.GerarQrCodeUri(segredo, command.Email);

        // Salva como pendente — só move para totp_secret_hash quando o usuário
        // confirmar com o primeiro código gerado pelo autenticador.
        await _usuarioRepository.SalvarTotpPendenteAsync(
            usuario.Id, segredo, cancellationToken);

        return new ConfigurarTotpResult(segredo, qrCodeUri);
    }
}
