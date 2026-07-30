using FluentValidation;
using LicenciamentoSoftware.Application.ClienteFinal.Commands;

namespace LicenciamentoSoftware.Application.ClienteFinal.Validators;

public sealed class AtualizarClienteFinalValidator : AbstractValidator<AtualizarClienteFinalCommand>
{
    public AtualizarClienteFinalValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id do cliente final é obrigatório.");

        RuleFor(x => x.RazaoSocial)
            .NotEmpty().WithMessage("Razão social é obrigatória.")
            .MaximumLength(200).WithMessage("Razão social não pode ter mais de 200 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .MaximumLength(300).WithMessage("E-mail não pode ter mais de 300 caracteres.")
            .EmailAddress().WithMessage("E-mail inválido.");

        RuleFor(x => x.Telefone)
            .MaximumLength(15).WithMessage("Telefone não pode ter mais de 15 caracteres.")
            .When(x => x.Telefone is not null);
    }
}
