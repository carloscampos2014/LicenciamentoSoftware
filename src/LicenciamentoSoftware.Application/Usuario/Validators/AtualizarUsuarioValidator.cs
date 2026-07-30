using FluentValidation;
using LicenciamentoSoftware.Application.Usuario.Commands;

namespace LicenciamentoSoftware.Application.Usuario.Validators;

public sealed class AtualizarUsuarioValidator : AbstractValidator<AtualizarUsuarioCommand>
{
    public AtualizarUsuarioValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id do usuário é obrigatório.");

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome não pode ter mais de 200 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .MaximumLength(300).WithMessage("E-mail não pode ter mais de 300 caracteres.")
            .EmailAddress().WithMessage("E-mail inválido.");
    }
}
