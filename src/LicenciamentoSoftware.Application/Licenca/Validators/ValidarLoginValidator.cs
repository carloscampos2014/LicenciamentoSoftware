using FluentValidation;
using LicenciamentoSoftware.Application.Licenca.Commands;

namespace LicenciamentoSoftware.Application.Licenca.Validators;

public sealed class ValidarLoginValidator : AbstractValidator<ValidarLoginCommand>
{
    public ValidarLoginValidator()
    {
        RuleFor(x => x.IdLicenca)
            .NotEmpty().WithMessage("IdLicenca é obrigatório.");

        RuleFor(x => x.IdentificadorUsuario)
            .NotEmpty().WithMessage("IdentificadorUsuario é obrigatório.")
            .MaximumLength(300).WithMessage("IdentificadorUsuario não pode ter mais de 300 caracteres.");
    }
}
