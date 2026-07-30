using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Usuario.Abstractions;
using LicenciamentoSoftware.Application.Usuario.Commands;
using LicenciamentoSoftware.Application.Usuario.Results;
using LicenciamentoSoftware.Application.Usuario.Validators;

namespace LicenciamentoSoftware.Application.Usuario.Handlers;

public sealed class CriarUsuarioHandler
{
    private readonly IUsuarioGestaoRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly CriarUsuarioValidator _validator;

    public CriarUsuarioHandler(
        IUsuarioGestaoRepository repo,
        IUnitOfWork uow,
        IPasswordHasher hasher)
    {
        _repo = repo;
        _uow = uow;
        _hasher = hasher;
        _validator = new CriarUsuarioValidator();
    }

    public async Task<CriarUsuarioResult> HandleAsync(
        CriarUsuarioCommand command,
        CancellationToken ct = default)
    {
        var validacao = await _validator.ValidateAsync(command, ct);
        if (!validacao.IsValid)
            return new CriarUsuarioResult.Invalido(
                validacao.Errors.Select(e => e.ErrorMessage).ToList());

        var emailDuplicado = await _repo.ExisteEmailAsync(command.Email, null, ct);
        if (emailDuplicado)
            return new CriarUsuarioResult.EmailJaExiste();

        Domain.Entities.Usuario usuario;
        try
        {
            var senhaHash = _hasher.Hash(command.Senha);
            usuario = Domain.Entities.Usuario.Criar(
                command.IdCliente, command.Nome, senhaHash, command.Email);
        }
        catch (Domain.Exceptions.DomainException ex)
        {
            return new CriarUsuarioResult.Invalido([ex.Message]);
        }

        await _uow.BeginAsync(cancellationToken: ct);
        await _repo.InserirAsync(usuario, command.Papel, ct);
        await _uow.CommitAsync(ct);

        return new CriarUsuarioResult.Sucesso(new UsuarioResult(
            usuario.Id, usuario.IdCliente, usuario.Nome,
            usuario.Email, command.Papel, usuario.Ativo));
    }
}

public abstract record CriarUsuarioResult
{
    private CriarUsuarioResult() { }
    public sealed record Sucesso(UsuarioResult Usuario) : CriarUsuarioResult;
    public sealed record Invalido(IReadOnlyList<string> Erros) : CriarUsuarioResult;
    public sealed record EmailJaExiste : CriarUsuarioResult;
}
