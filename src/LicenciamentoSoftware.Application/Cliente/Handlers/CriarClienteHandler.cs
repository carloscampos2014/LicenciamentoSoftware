using FluentValidation;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Cliente.Abstractions;
using LicenciamentoSoftware.Application.Cliente.Commands;
using LicenciamentoSoftware.Application.Cliente.Results;
using LicenciamentoSoftware.Application.Cliente.Validators;
using LicenciamentoSoftware.Domain.Enums;
using LicenciamentoSoftware.Domain.ValueObjects;

namespace LicenciamentoSoftware.Application.Cliente.Handlers;

public sealed class CriarClienteHandler
{
    private readonly IClienteRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly CriarClienteValidator _validator;

    public CriarClienteHandler(IClienteRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
        _validator = new CriarClienteValidator();
    }

    public async Task<CriarClienteResult> HandleAsync(
        CriarClienteCommand command,
        CancellationToken ct = default)
    {
        // 1. Validação de entrada
        var validacao = await _validator.ValidateAsync(command, ct);
        if (!validacao.IsValid)
            return new CriarClienteResult.Invalido(
                validacao.Errors.Select(e => e.ErrorMessage).ToList());

        // 2. Unicidade da inscrição
        var inscricaoDuplicada = await _repo.ExisteInscricaoAsync(
            command.TipoInscricao, command.NumeroInscricao, null, ct);
        if (inscricaoDuplicada)
            return new CriarClienteResult.InscricaoJaExiste();

        // 3. Montar entidade — value objects validam internamente
        Domain.Entities.Cliente cliente;
        try
        {
            var inscricao = new Inscricao((TipoInscricao)command.TipoInscricao, command.NumeroInscricao);
            var email = new Email(command.Email);
            var telefone = command.Telefone is not null ? new Telefone(command.Telefone) : null;
            cliente = Domain.Entities.Cliente.Criar(command.RazaoSocial, inscricao, email, telefone);
        }
        catch (Domain.Exceptions.DomainException ex)
        {
            return new CriarClienteResult.Invalido([ex.Message]);
        }

        // 4. Persistir
        await _uow.BeginAsync(cancellationToken: ct);
        await _repo.InserirAsync(cliente, ct);
        await _uow.CommitAsync(ct);

        return new CriarClienteResult.Sucesso(new ClienteResult(
            cliente.Id,
            cliente.RazaoSocial,
            (int)cliente.Inscricao.Tipo,
            cliente.Inscricao.Numero,
            cliente.Email.Endereco,
            cliente.Telefone?.Numero,
            cliente.Ativo));
    }
}

public abstract record CriarClienteResult
{
    private CriarClienteResult() { }
    public sealed record Sucesso(ClienteResult Cliente) : CriarClienteResult;
    public sealed record Invalido(IReadOnlyList<string> Erros) : CriarClienteResult;
    public sealed record InscricaoJaExiste : CriarClienteResult;
}
