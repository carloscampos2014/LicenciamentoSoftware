using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Results;
using LicenciamentoSoftware.Domain.Entities;

namespace LicenciamentoSoftware.Application.Licenca.Handlers;

/// <summary>
/// Emite um token HMAC-SHA256 para uma licença ativa.
/// O segredo em texto é retornado uma única vez — apenas o hash é persistido.
/// </summary>
public sealed class EmitirTokenLicencaHandler
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
    public EmitirTokenLicencaHandler(
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
        EmitirTokenLicencaCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. Valida que a licença existe e está ativa
        var licenca = await _licencaRepository.BuscarPorIdAsync(
            command.IdLicenca, cancellationToken);

        if (licenca is null)
            return new EmitirTokenResult.LicencaNaoEncontrada();

        if (!licenca.Ativo)
            return new EmitirTokenResult.LicencaInativa();

        // 2. Garante que não existe token ativo — use RenovarToken para substituir
        var tokenExistente = await _tokenRepository.BuscarAtivoporLicencaAsync(
            command.IdLicenca, cancellationToken);

        if (tokenExistente is not null)
            return new EmitirTokenResult.TokenJaExiste();

        // 3. Gera segredo (texto puro — exibido apenas nesta resposta)
        var segredoTexto = _hmacService.GerarSegredo();
        var segredoHash = _hmacService.HashSegredo(segredoTexto);

        var expiracaoMinutos = command.ExpiracaoMinutosOverride ?? _defaultExpiracaoMinutos;

        // 4. Cria a entidade e persiste dentro da UoW
        var token = LicencaToken.Criar(command.IdLicenca, segredoHash, expiracaoMinutos);

        await _unitOfWork.BeginAsync(cancellationToken: cancellationToken);

        await _tokenRepository.SalvarAsync(
            token.Id, token.IdLicenca, token.SegredoHash,
            token.ExpiracaoMinutos, token.CriadoEm, cancellationToken);

        await _unitOfWork.CommitAsync(cancellationToken);

        return new EmitirTokenResult.Sucesso(
            token.Id, token.IdLicenca, segredoTexto, token.ExpiracaoMinutos);
    }
}
