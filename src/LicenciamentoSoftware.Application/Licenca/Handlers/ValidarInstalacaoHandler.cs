using System.Data;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Results;
using LicenciamentoSoftware.Application.Licenca.Validators;

namespace LicenciamentoSoftware.Application.Licenca.Handlers;

/// <summary>
/// Valida e registra a instalação de um software em uma máquina.
/// <para>
/// Exclusivo para licenças do tipo <b>Por Instalação</b>.
/// A operação é <b>idempotente</b>: máquina já registrada retorna
/// <see cref="ValidarInstalacaoResult.Sucesso"/> com <c>JaRegistrada = true</c>.
/// </para>
/// <para>
/// Usa transação serializável para evitar race condition ao registrar a última vaga.
/// </para>
/// </summary>
public sealed class ValidarInstalacaoHandler
{
    private readonly IValidacaoLicencaRepository _validacaoRepo;
    private readonly ILicencaInstalacaoRepository _instalacaoRepo;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly ValidarInstalacaoValidator _validator;

    public ValidarInstalacaoHandler(
        IValidacaoLicencaRepository validacaoRepo,
        ILicencaInstalacaoRepository instalacaoRepo,
        IUnitOfWork uow,
        IClock clock)
    {
        _validacaoRepo  = validacaoRepo;
        _instalacaoRepo = instalacaoRepo;
        _uow            = uow;
        _clock          = clock;
        _validator      = new ValidarInstalacaoValidator();
    }

    public async Task<ValidarInstalacaoResult> HandleAsync(
        ValidarInstalacaoCommand command,
        CancellationToken ct = default)
    {
        // 1. Validação de entrada
        var validacao = await _validator.ValidateAsync(command, ct);
        if (!validacao.IsValid)
            return new ValidarInstalacaoResult.Invalido(
                validacao.Errors.Select(e => e.ErrorMessage).ToList());

        // 2. Carrega licença com detalhe em uma única query
        var licenca = await _validacaoRepo.BuscarParaValidacaoAsync(command.IdLicenca, ct);
        if (licenca is null)
            return new ValidarInstalacaoResult.LicencaNaoEncontrada();

        if (!licenca.Ativo)
            return new ValidarInstalacaoResult.LicencaInativa();

        // 3. Apenas licenças Por Instalação
        if (licenca.IdTipoLicenca != TiposLicencaIds.Instalacao)
            return new ValidarInstalacaoResult.TipoLicencaIncompativel(
                "Esta licença não é do tipo Por Instalação. Use o endpoint POST /validacao/login.");

        // 4. Verificar expiração de período (se a licença Por Instalação tiver período associado)
        if (licenca.DataFim is not null && licenca.DataFim.Value < _clock.UtcNow)
            return new ValidarInstalacaoResult.LicencaExpirada();

        var quantidadeMaxima = licenca.QuantidadeMaximaInstalacoes!.Value;

        // 5. Transação serializável: idempotência + verificação de limite + inserção atômica
        await _uow.BeginAsync(IsolationLevel.Serializable, ct);

        try
        {
            // 5a. Idempotência: máquina já registrada?
            var existente = await _instalacaoRepo.BuscarRegistradaAtivaAsync(
                command.IdLicenca, command.IdentificadorMaquina, ct);

            if (existente is not null)
            {
                await _uow.RollbackAsync(ct);
                return new ValidarInstalacaoResult.Sucesso(existente.Id, JaRegistrada: true);
            }

            // 5b. Verificar limite
            var totalAtivas = await _instalacaoRepo.ContarAtivasAsync(command.IdLicenca, ct);
            if (totalAtivas >= quantidadeMaxima)
            {
                await _uow.RollbackAsync(ct);
                return new ValidarInstalacaoResult.LimiteInstalacoesAtingido(quantidadeMaxima);
            }

            // 5c. Registrar nova instalação
            Domain.Entities.LicencaInstalacaoRegistrada instalacao;
            try
            {
                instalacao = Domain.Entities.LicencaInstalacaoRegistrada.Registrar(
                    command.IdLicenca, command.IdentificadorMaquina);
            }
            catch (Domain.Exceptions.DomainException ex)
            {
                await _uow.RollbackAsync(ct);
                return new ValidarInstalacaoResult.Invalido([ex.Message]);
            }

            await _instalacaoRepo.InserirRegistradaAsync(instalacao, ct);
            await _uow.CommitAsync(ct);

            return new ValidarInstalacaoResult.Sucesso(instalacao.Id, JaRegistrada: false);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }
}
