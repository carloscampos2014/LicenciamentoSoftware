using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Aplicacao.Abstractions;
using LicenciamentoSoftware.Application.ClienteFinal.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Abstractions;
using LicenciamentoSoftware.Application.Licenca.Commands;
using LicenciamentoSoftware.Application.Licenca.Results;
using LicenciamentoSoftware.Application.Licenca.Validators;

namespace LicenciamentoSoftware.Application.Licenca.Handlers;

public sealed class EmitirLicencaHandler
{
    // UUIDs dos tipos de licença (seed V001)
    private static readonly Guid TipoPermanente = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TipoPeriodo    = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TipoUsuarios   = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TipoInstalacao = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly ILicencaGestaoRepository _licencaRepo;
    private readonly IClienteFinalRepository _clienteFinalRepo;
    private readonly IAplicacaoRepository _aplicacaoRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly EmitirTokenLicencaHandler _emitirTokenHandler;
    private readonly EmitirLicencaValidator _validator;

    public EmitirLicencaHandler(
        ILicencaGestaoRepository licencaRepo,
        IClienteFinalRepository clienteFinalRepo,
        IAplicacaoRepository aplicacaoRepo,
        IUnitOfWork uow,
        ICurrentUser currentUser,
        EmitirTokenLicencaHandler emitirTokenHandler)
    {
        _licencaRepo       = licencaRepo;
        _clienteFinalRepo  = clienteFinalRepo;
        _aplicacaoRepo     = aplicacaoRepo;
        _uow               = uow;
        _currentUser       = currentUser;
        _emitirTokenHandler = emitirTokenHandler;
        _validator         = new EmitirLicencaValidator();
    }

    public async Task<EmitirLicencaResult> HandleAsync(
        EmitirLicencaCommand command,
        CancellationToken ct = default)
    {
        // 1. Validação de entrada
        var validacao = await _validator.ValidateAsync(command, ct);
        if (!validacao.IsValid)
            return new EmitirLicencaResult.Invalido(
                validacao.Errors.Select(e => e.ErrorMessage).ToList());

        // 2. Tenant isolation (Opção A): ClienteFinal deve pertencer ao tenant do JWT
        var clienteFinal = await _clienteFinalRepo.BuscarPorIdAsync(command.IdClienteFinal, ct);
        if (clienteFinal is null)
            return new EmitirLicencaResult.ClienteFinalNaoEncontrado();
        if (clienteFinal.IdCliente != _currentUser.IdCliente)
            return new EmitirLicencaResult.AcessoNegado();

        // 3. Tenant isolation: Aplicacao deve pertencer ao mesmo tenant
        var aplicacao = await _aplicacaoRepo.BuscarPorIdAsync(command.IdAplicativo, ct);
        if (aplicacao is null)
            return new EmitirLicencaResult.AplicacaoNaoEncontrada();
        if (aplicacao.IdCliente != _currentUser.IdCliente)
            return new EmitirLicencaResult.AcessoNegado();
        if (!aplicacao.Ativo)
            return new EmitirLicencaResult.AplicacaoNaoEncontrada();

        // 4. Verificar compatibilidade do bloco de detalhe com o TipoLicenca da Aplicacao
        var incompativel = VerificarCompatibilidade(aplicacao.IdTipoLicenca, command);
        if (incompativel is not null)
            return incompativel;

        // 5. Verificar licença ativa duplicada
        var duplicada = await _licencaRepo.ExisteLicencaAtivaAsync(
            _currentUser.IdCliente, command.IdClienteFinal, command.IdAplicativo, ct);
        if (duplicada)
            return new EmitirLicencaResult.LicencaDuplicada();

        // 6. Criar entidade Licenca
        Domain.Entities.Licenca licenca;
        try
        {
            licenca = Domain.Entities.Licenca.Criar(
                _currentUser.IdCliente, command.IdClienteFinal, command.IdAplicativo);
        }
        catch (Domain.Exceptions.DomainException ex)
        {
            return new EmitirLicencaResult.Invalido([ex.Message]);
        }

        // 7. Persistir licença + detalhe dentro de UoW
        await _uow.BeginAsync(cancellationToken: ct);

        await _licencaRepo.InserirLicencaAsync(licenca, ct);
        await InserirDetalheAsync(licenca.Id, aplicacao.IdTipoLicenca, command, ct);

        await _uow.CommitAsync(ct);

        // 8. Buscar resultado completo (com detalhe)
        var licencaResult = await _licencaRepo.BuscarPorIdAsync(licenca.Id, ct);

        // 9. Emitir token se solicitado (fora da UoW principal — tem sua própria transação)
        string? tokenTexto = null;
        if (command.EmitirToken)
        {
            var tokenResult = await _emitirTokenHandler.HandleAsync(
                new Application.Licenca.Commands.EmitirTokenLicencaCommand(
                    licenca.Id, command.ExpiracaoTokenMinutos), ct);

            if (tokenResult is Application.Licenca.Results.EmitirTokenResult.Sucesso tokenSucesso)
                tokenTexto = tokenSucesso.TokenTexto;
        }

        return new EmitirLicencaResult.Sucesso(licencaResult!, tokenTexto);
    }

    // -------------------------------------------------------------------------
    // Helpers privados
    // -------------------------------------------------------------------------

    private static EmitirLicencaResult.TipoLicencaIncompativel? VerificarCompatibilidade(
        Guid idTipoLicenca, EmitirLicencaCommand command)
    {
        if (idTipoLicenca == TipoPermanente)
        {
            if (command.Periodo is not null || command.Usuarios is not null || command.Instalacao is not null)
                return new EmitirLicencaResult.TipoLicencaIncompativel(
                    "Licença Permanente não aceita blocos de detalhe.");
        }
        else if (idTipoLicenca == TipoPeriodo)
        {
            if (command.Periodo is null)
                return new EmitirLicencaResult.TipoLicencaIncompativel(
                    "Licença Por Período requer o bloco Periodo.");
        }
        else if (idTipoLicenca == TipoUsuarios)
        {
            if (command.Usuarios is null)
                return new EmitirLicencaResult.TipoLicencaIncompativel(
                    "Licença Por Usuários requer o bloco Usuarios.");
        }
        else if (idTipoLicenca == TipoInstalacao)
        {
            if (command.Instalacao is null)
                return new EmitirLicencaResult.TipoLicencaIncompativel(
                    "Licença Por Instalação requer o bloco Instalacao.");
        }

        return null;
    }

    private async Task InserirDetalheAsync(
        Guid idLicenca, Guid idTipoLicenca,
        EmitirLicencaCommand command, CancellationToken ct)
    {
        if (idTipoLicenca == TipoPeriodo && command.Periodo is not null)
        {
            var periodo = Domain.Entities.LicencaPeriodo.Criar(
                idLicenca,
                command.Periodo.DataInicio,
                command.Periodo.DataFim,
                command.Periodo.RenovacaoAutomatica);
            await _licencaRepo.InserirDetalhePeriodoAsync(periodo, ct);
        }
        else if (idTipoLicenca == TipoUsuarios && command.Usuarios is not null)
        {
            var usuarios = Domain.Entities.LicencaUsuarios.Criar(
                idLicenca,
                command.Usuarios.QuantidadeMaxima,
                command.Usuarios.MaxSessoesPorUsuario,
                command.Usuarios.TempoLimiteSessaoHoras);
            await _licencaRepo.InserirDetalheUsuariosAsync(usuarios, ct);
        }
        else if (idTipoLicenca == TipoInstalacao && command.Instalacao is not null)
        {
            var instalacao = Domain.Entities.LicencaInstalacao.Criar(
                idLicenca, command.Instalacao.QuantidadeMaxima);
            await _licencaRepo.InserirDetalheInstalacaoAsync(instalacao, ct);
        }
        // TipoPermanente: sem detalhe a inserir
    }
}

public abstract record EmitirLicencaResult
{
    private EmitirLicencaResult() { }

    public sealed record Sucesso(
        LicencaResult Licenca,
        string? TokenTexto) : EmitirLicencaResult;

    public sealed record Invalido(IReadOnlyList<string> Erros) : EmitirLicencaResult;
    public sealed record AcessoNegado : EmitirLicencaResult;
    public sealed record ClienteFinalNaoEncontrado : EmitirLicencaResult;
    public sealed record AplicacaoNaoEncontrada : EmitirLicencaResult;
    public sealed record TipoLicencaIncompativel(string Motivo) : EmitirLicencaResult;
    public sealed record LicencaDuplicada : EmitirLicencaResult;
}
