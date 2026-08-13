using System.Data;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Results;
using LicenciamentoSoftware.Application.Licenca.Validators;
using Microsoft.Extensions.Logging;

namespace LicenciamentoSoftware.Application.Licenca.Handlers;

/// <summary>
/// Valida o acesso de um usuário a uma licença.
/// </summary>
public sealed class ValidarLoginHandler
{
    private readonly IValidacaoLicencaRepository _validacaoRepo;
    private readonly ILicencaSessaoRepository _sessaoRepo;
    private readonly IValidacaoLogRepository _logRepo;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly ValidarLoginValidator _validator;
    private readonly ILogger<ValidarLoginHandler> _logger;

    private static readonly Action<ILogger, Guid, Exception?> _logFalha =
        LoggerMessage.Define<Guid>(LogLevel.Warning,
            new EventId(1, "FalhaGravarLog"),
            "Falha ao gravar validacao_log para licença {IdLicenca}");

    public ValidarLoginHandler(
        IValidacaoLicencaRepository validacaoRepo,
        ILicencaSessaoRepository sessaoRepo,
        IValidacaoLogRepository logRepo,
        IUnitOfWork uow,
        IClock clock,
        ILogger<ValidarLoginHandler> logger)
    {
        _validacaoRepo = validacaoRepo;
        _sessaoRepo    = sessaoRepo;
        _logRepo       = logRepo;
        _uow           = uow;
        _clock         = clock;
        _logger        = logger;
        _validator     = new ValidarLoginValidator();
    }

    public async Task<ValidarLoginResult> HandleAsync(
        ValidarLoginCommand command,
        CancellationToken ct = default)
    {
        var validacao = await _validator.ValidateAsync(command, ct);
        if (!validacao.IsValid)
            return new ValidarLoginResult.Invalido(
                validacao.Errors.Select(e => e.ErrorMessage).ToList());

        var licenca = await _validacaoRepo.BuscarParaValidacaoAsync(command.IdLicenca, ct);
        if (licenca is null)
        {
            await GravarLogAsync(command.IdLicenca, TipoOperacaoValidacao.Login, "erro",
                MotivoErroValidacao.LicenceNaoEncontrada, command.IpOrigem, ct);
            return new ValidarLoginResult.LicencaNaoEncontrada();
        }

        if (!licenca.Ativo)
        {
            await GravarLogAsync(command.IdLicenca, TipoOperacaoValidacao.Login, "erro",
                MotivoErroValidacao.LicencaInativa, command.IpOrigem, ct);
            return new ValidarLoginResult.LicencaInativa();
        }

        if (licenca.IdTipoLicenca == TiposLicencaIds.Permanente)
        {
            await GravarLogAsync(command.IdLicenca, TipoOperacaoValidacao.Login, "sucesso",
                null, command.IpOrigem, ct);
            return new ValidarLoginResult.Sucesso(IdSessao: null);
        }

        if (licenca.IdTipoLicenca == TiposLicencaIds.Periodo)
        {
            var r = ValidarPeriodo(licenca);
            var motivo = r is ValidarLoginResult.LicencaExpirada ? MotivoErroValidacao.LicencaExpirada : null;
            await GravarLogAsync(command.IdLicenca, TipoOperacaoValidacao.Login,
                motivo is null ? "sucesso" : "erro", motivo, command.IpOrigem, ct);
            return r;
        }

        if (licenca.IdTipoLicenca == TiposLicencaIds.Usuarios)
            return await ValidarUsuariosAsync(licenca, command.IdentificadorUsuario,
                command.IpOrigem, ct);

        await GravarLogAsync(command.IdLicenca, TipoOperacaoValidacao.Login, "erro",
            MotivoErroValidacao.InstalacaoInvalida, command.IpOrigem, ct);
        return new ValidarLoginResult.TipoLicencaIncompativel(
            "Licença Por Instalação não suporta validação de login. Use o endpoint POST /validacao/instalacao.");
    }

    private ValidarLoginResult ValidarPeriodo(LicencaValidacaoInfo licenca)
    {
        if (licenca.DataFim is null || licenca.DataFim.Value < _clock.UtcNow)
            return new ValidarLoginResult.LicencaExpirada();
        return new ValidarLoginResult.Sucesso(IdSessao: null);
    }

    private async Task<ValidarLoginResult> ValidarUsuariosAsync(
        LicencaValidacaoInfo licenca,
        string identificadorUsuario,
        string? ipOrigem,
        CancellationToken ct)
    {
        var quantidadeMaxima     = licenca.QuantidadeMaximaUsuarios!.Value;
        var maxSessoesPorUsuario = licenca.MaxSessoesPorUsuario!.Value;

        await _uow.BeginAsync(IsolationLevel.Serializable, ct);
        try
        {
            var totalAtivas = await _sessaoRepo.ContarUsuariosDistintosAtivosPorLicencaAsync(licenca.Id, ct);
            if (totalAtivas >= quantidadeMaxima)
            {
                await _uow.RollbackAsync(ct);
                await GravarLogAsync(licenca.Id, TipoOperacaoValidacao.Login, "erro",
                    MotivoErroValidacao.LimiteExcedido, ipOrigem, ct);
                return new ValidarLoginResult.LimiteUsuariosAtingido(quantidadeMaxima);
            }

            var sessoesDoUsuario = await _sessaoRepo.ContarAtivasPorUsuarioAsync(
                licenca.Id, identificadorUsuario, ct);
            if (sessoesDoUsuario >= maxSessoesPorUsuario)
            {
                await _uow.RollbackAsync(ct);
                await GravarLogAsync(licenca.Id, TipoOperacaoValidacao.Login, "erro",
                    MotivoErroValidacao.LimiteExcedido, ipOrigem, ct);
                return new ValidarLoginResult.LimiteSessionsPorUsuarioAtingido(maxSessoesPorUsuario);
            }

            Domain.Entities.LicencaSessao sessao;
            try { sessao = Domain.Entities.LicencaSessao.Criar(licenca.Id, identificadorUsuario); }
            catch (Domain.Exceptions.DomainException ex)
            {
                await _uow.RollbackAsync(ct);
                return new ValidarLoginResult.Invalido([ex.Message]);
            }

            await _sessaoRepo.InserirAsync(sessao, ct);
            await _uow.CommitAsync(ct);

            await GravarLogAsync(licenca.Id, TipoOperacaoValidacao.Login, "sucesso",
                null, ipOrigem, ct);
            return new ValidarLoginResult.Sucesso(IdSessao: sessao.Id);
        }
        catch { await _uow.RollbackAsync(ct); throw; }
    }

    private async Task GravarLogAsync(
        Guid idLicenca, string tipoOperacao, string resultado,
        string? motivoErro, string? ipOrigem, CancellationToken ct)
    {
        try
        {
            await _logRepo.InserirAsync(idLicenca, tipoOperacao, resultado,
                motivoErro, ipOrigem, ct);
        }
        catch (Exception ex) { _logFalha(_logger, idLicenca, ex); }
    }
}
