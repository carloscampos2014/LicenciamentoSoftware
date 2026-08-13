using FluentValidation;
using LicenciamentoSoftware.Application.Licenca.Commands;

namespace LicenciamentoSoftware.Application.Licenca.Validators;

public sealed class EditarDetalhesUsuariosValidator : AbstractValidator<EditarDetalhesUsuariosCommand>
{
    public EditarDetalhesUsuariosValidator()
    {
        RuleFor(x => x.IdLicenca)
            .NotEmpty().WithMessage("IdLicenca é obrigatório.");

        RuleFor(x => x.QuantidadeMaxima)
            .GreaterThan(0).WithMessage("Quantidade máxima de usuários deve ser maior que zero.");

        RuleFor(x => x.MaxSessoesPorUsuario)
            .GreaterThan(0).WithMessage("Máximo de sessões por usuário deve ser maior que zero.");
    }
}
