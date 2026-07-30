using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.ClienteFinal.Abstractions;
using LicenciamentoSoftware.Application.ClienteFinal.Commands;
using LicenciamentoSoftware.Application.ClienteFinal.Results;
using LicenciamentoSoftware.Application.ClienteFinal.Validators;
using LicenciamentoSoftware.Domain.Enums;
using LicenciamentoSoftware.Domain.ValueObjects;

namespace LicenciamentoSoftware.Application.ClienteFinal.Handlers;

public sealed class CriarClienteFinalHandler
{
    private readonly IClienteFinalRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly CriarClienteFinalValidator _validator;

    public CriarClienteFinalHandler(IClienteFinalRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
        _validator = new CriarClienteFinalValidator();
    }

    public async Task<CriarClienteFinalResult> HandleAsync(
        CriarClienteFinalCommand command, CancellationToken ct = default)
    {
        var validacao = await _validator.ValidateAsync(command, ct);
        if (!validacao.IsValid)
            return new CriarClienteFinalResult.Invalido(
                validacao.Errors.Select(e => e.ErrorMessage).ToList());

        var inscricaoDuplicada = await _repo.ExisteInscricaoAsync(
            command.IdCliente, command.TipoInscricao, command.NumeroInscricao, null, ct);
        if (inscricaoDuplicada)
            return new CriarClienteFinalResult.InscricaoJaExiste();

        Domain.Entities.ClienteFinal clienteFinal;
        try
        {
            var inscricao = new Inscricao((TipoInscricao)command.TipoInscricao, command.NumeroInscricao);
            var email = new Email(command.Email);
            var telefone = command.Telefone is not null ? new Telefone(command.Telefone) : null;
            clienteFinal = Domain.Entities.ClienteFinal.Criar(
                command.IdCliente, command.RazaoSocial, inscricao, email, telefone);
        }
        catch (Domain.Exceptions.DomainException ex)
        {
            return new CriarClienteFinalResult.Invalido([ex.Message]);
        }

        await _uow.BeginAsync(cancellationToken: ct);
        await _repo.InserirAsync(clienteFinal, ct);
        await _uow.CommitAsync(ct);

        return new CriarClienteFinalResult.Sucesso(new ClienteFinalResult(
            clienteFinal.Id, clienteFinal.IdCliente, clienteFinal.RazaoSocial,
            (int)clienteFinal.Inscricao.Tipo, clienteFinal.Inscricao.Numero,
            clienteFinal.Email.Endereco, clienteFinal.Telefone?.Numero, clienteFinal.Ativo));
    }
}

public abstract record CriarClienteFinalResult
{
    private CriarClienteFinalResult() { }
    public sealed record Sucesso(ClienteFinalResult ClienteFinal) : CriarClienteFinalResult;
    public sealed record Invalido(IReadOnlyList<string> Erros) : CriarClienteFinalResult;
    public sealed record InscricaoJaExiste : CriarClienteFinalResult;
}
