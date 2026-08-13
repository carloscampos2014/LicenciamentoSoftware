using FluentValidation;
using LicenciamentoSoftware.Application.Licenca.Commands;

namespace LicenciamentoSoftware.Application.Licenca.Validators;

public sealed class EditarDetalhesInstalacaoValidator : AbstractValidator<EditarDetalhesInstalacaoCommand>
{
    public EditarDetalhesInstalacaoValidator()
    {
        RuleFor(x => x.IdLicenca)
            .NotEmpty().WithMessage("IdLicenca é obrigatório.");

        RuleFor(x => x.QuantidadeMaxima)
            .GreaterThan(0).WithMessage("Quantidade máxima de instalações deve ser maior que zero.");
    }
}
