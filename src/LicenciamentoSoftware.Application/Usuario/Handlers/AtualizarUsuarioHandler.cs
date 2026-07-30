using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Usuario.Abstractions;
using LicenciamentoSoftware.Application.Usuario.Commands;
using LicenciamentoSoftware.Application.Usuario.Results;
using LicenciamentoSoftware.Application.Usuario.Validators;

namespace LicenciamentoSoftware.Application.Usuario.Handlers;

public sealed class AtualizarUsuarioHandler
{
    private readonly IUsuarioGestaoRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly AtualizarUsuarioValidator _validator;

    public AtualizarUsuarioHandler(IUsuarioGestaoRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
        _validator = new AtualizarUsuarioValidator();
    }

    public async Task<AtualizarUsuarioResult> HandleAsync(
        AtualizarUsuarioCommand command,
        CancellationToken ct = default)
    {
        var validacao = await _validator.ValidateAsync(command, ct);
        if (!validacao.IsValid)
            return new AtualizarUsuarioResult.Invalido(
                validacao.Errors.Select(e => e.ErrorMessage).ToList());

        var existente = await _repo.BuscarPorIdAsync(command.Id, ct);
        if (existente is null)
            return new AtualizarUsuarioResult.NaoEncontrado();

        var emailDuplicado = await _repo.ExisteEmailAsync(command.Email, command.Id, ct);
        if (emailDuplicado)
            return new AtualizarUsuarioResult.EmailJaExiste();

        await _uow.BeginAsync(cancellationToken: ct);
        await _repo.AtualizarAsync(command, ct);
        await _uow.CommitAsync(ct);

        var atualizado = await _repo.BuscarPorIdAsync(command.Id, ct);
        return new AtualizarUsuarioResult.Sucesso(atualizado!);
    }
}

public abstract record AtualizarUsuarioResult
{
    private AtualizarUsuarioResult() { }
    public sealed record Sucesso(UsuarioResult Usuario) : AtualizarUsuarioResult;
    public sealed record Invalido(IReadOnlyList<string> Erros) : AtualizarUsuarioResult;
    public sealed record NaoEncontrado : AtualizarUsuarioResult;
    public sealed record EmailJaExiste : AtualizarUsuarioResult;
}
