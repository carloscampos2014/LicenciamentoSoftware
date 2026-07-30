using System.Data;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Results;
using LicenciamentoSoftware.Application.Licenca.Validators;

namespace LicenciamentoSoftware.Application.Licenca.Handlers;

/// <summary>
/// Valida o acesso de um usuário a uma licença.
/// <para>
/// Fluxo por tipo:
/// <list type="bullet">
///   <item><b>Permanente</b> — autoriza diretamente, sem criar sessão.</item>
///   <item><b>Por Período</b> — valida <c>data_fim &gt;= now()</c>, autoriza sem criar sessão.</item>
///   <item><b>Por Usuários</b> — transação serializável: verifica limites e abre sessão.</item>
///   <item><b>Por Instalação</b> — retorna <see cref="ValidarLoginResult.TipoLicencaIncompativel"/>.</item>
/// </list>
/// </para>
/// </summary>
public sealed class ValidarLoginHandler
{
    private readonly IValidacaoLicencaRepository _validacaoRepo;
    private readonly ILicencaSessaoRepository _sessaoRepo;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly ValidarLoginValidator _validator;

    public ValidarLoginHandler(
        IValidacaoLicencaRepository validacaoRepo,
        ILicencaSessaoRepository sessaoRepo,
        IUnitOfWork uow,
        IClock clock)
    {
        _validacaoRepo = validacaoRepo;
        _sessaoRepo    = sessaoRepo;
        _uow           = uow;
        _clock         = clock;
        _validator     = new ValidarLoginValidator();
    }

    public async Task<ValidarLoginResult> HandleAsync(
        ValidarLoginCommand command,
        CancellationToken ct = default)
    {
        // 1. Validação de entrada
        var validacao = await _validator.ValidateAsync(command, ct);
        if (!validacao.IsValid)
            return new ValidarLoginResult.Invalido(
                validacao.Errors.Select(e => e.ErrorMessage).ToList());

        // 2. Carrega licença com detalhe em uma única query
        var licenca = await _validacaoRepo.BuscarParaValidacaoAsync(command.IdLicenca, ct);
        if (licenca is null)
            return new ValidarLoginResult.LicencaNaoEncontrada();

        if (!licenca.Ativo)
            return new ValidarLoginResult.LicencaInativa();

        // 3. Ramifica por tipo de licença
        if (licenca.IdTipoLicenca == TiposLicencaIds.Permanente)
            return new ValidarLoginResult.Sucesso(IdSessao: null);

        if (licenca.IdTipoLicenca == TiposLicencaIds.Periodo)
            return ValidarPeriodo(licenca);

        if (licenca.IdTipoLicenca == TiposLicencaIds.Usuarios)
            return await ValidarUsuariosAsync(licenca, command.IdentificadorUsuario, ct);

        // Por Instalação — não suportado neste endpoint
        return new ValidarLoginResult.TipoLicencaIncompativel(
            "Licença Por Instalação não suporta validação de login. Use o endpoint POST /validacao/instalacao.");
    }

    // -------------------------------------------------------------------------
    // Helpers privados
    // -------------------------------------------------------------------------

    private ValidarLoginResult ValidarPeriodo(LicencaValidacaoInfo licenca)
    {
        var agora = _clock.UtcNow;

        if (licenca.DataFim is null || licenca.DataFim.Value < agora)
            return new ValidarLoginResult.LicencaExpirada();

        return new ValidarLoginResult.Sucesso(IdSessao: null);
    }

    private async Task<ValidarLoginResult> ValidarUsuariosAsync(
        LicencaValidacaoInfo licenca,
        string identificadorUsuario,
        CancellationToken ct)
    {
        var quantidadeMaxima      = licenca.QuantidadeMaximaUsuarios!.Value;
        var maxSessoesPorUsuario  = licenca.MaxSessoesPorUsuario!.Value;

        // Transação serializável: leitura + inserção atômica para evitar race condition
        await _uow.BeginAsync(IsolationLevel.Serializable, ct);

        try
        {
            // 4a. Verificar limite global
            var totalAtivas = await _sessaoRepo.ContarAtivasPorLicencaAsync(licenca.Id, ct);
            if (totalAtivas >= quantidadeMaxima)
            {
                await _uow.RollbackAsync(ct);
                return new ValidarLoginResult.LimiteUsuariosAtingido(quantidadeMaxima);
            }

            // 4b. Verificar limite por usuário
            var sessoesDoUsuario = await _sessaoRepo.ContarAtivasPorUsuarioAsync(
                licenca.Id, identificadorUsuario, ct);
            if (sessoesDoUsuario >= maxSessoesPorUsuario)
            {
                await _uow.RollbackAsync(ct);
                return new ValidarLoginResult.LimiteSessionsPorUsuarioAtingido(maxSessoesPorUsuario);
            }

            // 4c. Criar e persistir sessão
            Domain.Entities.LicencaSessao sessao;
            try
            {
                sessao = Domain.Entities.LicencaSessao.Criar(licenca.Id, identificadorUsuario);
            }
            catch (Domain.Exceptions.DomainException ex)
            {
                await _uow.RollbackAsync(ct);
                return new ValidarLoginResult.Invalido([ex.Message]);
            }

            await _sessaoRepo.InserirAsync(sessao, ct);
            await _uow.CommitAsync(ct);

            return new ValidarLoginResult.Sucesso(IdSessao: sessao.Id);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }
}
