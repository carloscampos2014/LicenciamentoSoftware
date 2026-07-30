using FluentValidation;
using LicenciamentoSoftware.Application.Usuario.Commands;

namespace LicenciamentoSoftware.Application.Usuario.Validators;

public sealed class CriarUsuarioValidator : AbstractValidator<CriarUsuarioCommand>
{
    private static readonly string[] PapeisValidos =
        ["AdministradorPlataforma", "AdministradorCliente", "OperadorCliente", "Leitor"];

    public CriarUsuarioValidator()
    {
        RuleFor(x => x.IdCliente)
            .NotEmpty().WithMessage("IdCliente é obrigatório.");

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome não pode ter mais de 200 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .MaximumLength(300).WithMessage("E-mail não pode ter mais de 300 caracteres.")
            .EmailAddress().WithMessage("E-mail inválido.");

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(8).WithMessage("Senha deve ter pelo menos 8 caracteres.");

        RuleFor(x => x.Papel)
            .Must(p => PapeisValidos.Contains(p))
            .WithMessage($"Papel inválido. Valores aceitos: {string.Join(", ", PapeisValidos)}.");
    }
}
