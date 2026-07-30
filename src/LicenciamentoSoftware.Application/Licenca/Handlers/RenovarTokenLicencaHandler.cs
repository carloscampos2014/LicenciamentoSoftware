using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Results;
using LicenciamentoSoftware.Domain.Entities;

namespace LicenciamentoSoftware.Application.Licenca.Handlers;

/// <summary>
/// Revoga o token HMAC atual de uma licença e emite um substituto.
/// Se não houver token ativo, cria um novo (comportamento idempotente).
/// O segredo em texto é retornado uma única vez — apenas o hash é persistido.
/// </summary>
public sealed class RenovarTokenLicencaHandler
{
    private readonly ILicencaRepository _licencaRepository;
    private readonly ILicencaTokenRepository _tokenRepository;
    private readonly IHmacLicencaTokenService _hmacService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly int _defaultExpiracaoMinutos;

    /// <param name="defaultExpiracaoMinutos">
    /// Valor resolvido na camada de DI a partir de LicencaTokenSettings:DefaultExpiracaoMinutos.
    /// Padrão: 525600 (1 ano).
    /// </param>
    public RenovarTokenLicencaHandler(
        ILicencaRepository licencaRepository,
        ILicencaTokenRepository tokenRepository,
        IHmacLicencaTokenService hmacService,
        IUnitOfWork unitOfWork,
        int defaultExpiracaoMinutos = 525600)
    {
        _licencaRepository = licencaRepository;
        _tokenRepository = tokenRepository;
        _hmacService = hmacService;
        _unitOfWork = unitOfWork;
        _defaultExpiracaoMinutos = defaultExpiracaoMinutos;
    }

    public async Task<EmitirTokenResult> HandleAsync(
        RenovarTokenLicencaCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. Valida que a licença existe e está ativa
        var licenca = await _licencaRepository.BuscarPorIdAsync(
            command.IdLicenca, cancellationToken);

        if (licenca is null)
            return new EmitirTokenResult.LicencaNaoEncontrada();

        if (!licenca.Ativo)
            return new EmitirTokenResult.LicencaInativa();

        // 2. Gera novo segredo
        var segredoTexto = _hmacService.GerarSegredo();
        var segredoHash = _hmacService.HashSegredo(segredoTexto);

        var expiracaoMinutos = command.ExpiracaoMinutosOverride ?? _defaultExpiracaoMinutos;

        await _unitOfWork.BeginAsync(cancellationToken: cancellationToken);

        // 3. Se existir token ativo, atualiza em vez de criar novo registro
        var tokenExistente = await _tokenRepository.BuscarAtivoporLicencaAsync(
            command.IdLicenca, cancellationToken);

        Guid idToken;

        if (tokenExistente is not null)
        {
            // Reutiliza o mesmo registro — apenas atualiza hash, expiração e data
            await _tokenRepository.AtualizarAsync(
                tokenExistente.Id, segredoHash, expiracaoMinutos,
                DateTime.UtcNow, cancellationToken);

            idToken = tokenExistente.Id;
        }
        else
        {
            // Nenhum token ativo — cria um novo (comportamento de emissão)
            var novoToken = LicencaToken.Criar(command.IdLicenca, segredoHash, expiracaoMinutos);

            await _tokenRepository.SalvarAsync(
                novoToken.Id, novoToken.IdLicenca, novoToken.SegredoHash,
                novoToken.ExpiracaoMinutos, novoToken.CriadoEm, cancellationToken);

            idToken = novoToken.Id;
        }

        await _unitOfWork.CommitAsync(cancellationToken);

        return new EmitirTokenResult.Sucesso(
            idToken, command.IdLicenca, segredoTexto, expiracaoMinutos);
    }
}
