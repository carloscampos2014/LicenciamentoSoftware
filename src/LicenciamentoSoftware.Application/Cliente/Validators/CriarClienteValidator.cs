using FluentValidation;
using LicenciamentoSoftware.Application.Cliente.Commands;

namespace LicenciamentoSoftware.Application.Cliente.Validators;

public sealed class CriarClienteValidator : AbstractValidator<CriarClienteCommand>
{
    public CriarClienteValidator()
    {
        RuleFor(x => x.RazaoSocial)
            .NotEmpty().WithMessage("Razão social é obrigatória.")
            .MaximumLength(200).WithMessage("Razão social não pode ter mais de 200 caracteres.");

        RuleFor(x => x.TipoInscricao)
            .InclusiveBetween(1, 2).WithMessage("Tipo de inscrição deve ser 1 (PF) ou 2 (PJ).");

        RuleFor(x => x.NumeroInscricao)
            .NotEmpty().WithMessage("Número de inscrição é obrigatório.")
            .MaximumLength(20).WithMessage("Número de inscrição não pode ter mais de 20 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .MaximumLength(300).WithMessage("E-mail não pode ter mais de 300 caracteres.")
            .EmailAddress().WithMessage("E-mail inválido.");

        RuleFor(x => x.Telefone)
            .MaximumLength(15).WithMessage("Telefone não pode ter mais de 15 caracteres.")
            .When(x => x.Telefone is not null);
    }
}
