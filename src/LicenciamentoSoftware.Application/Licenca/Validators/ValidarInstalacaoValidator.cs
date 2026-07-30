using FluentValidation;
using LicenciamentoSoftware.Application.Licenca.Commands;

namespace LicenciamentoSoftware.Application.Licenca.Validators;

public sealed class ValidarInstalacaoValidator : AbstractValidator<ValidarInstalacaoCommand>
{
    public ValidarInstalacaoValidator()
    {
        RuleFor(x => x.IdLicenca)
            .NotEmpty().WithMessage("IdLicenca é obrigatório.");

        RuleFor(x => x.IdentificadorMaquina)
            .NotEmpty().WithMessage("IdentificadorMaquina é obrigatório.")
            .MaximumLength(300).WithMessage("IdentificadorMaquina não pode ter mais de 300 caracteres.");
    }
}
