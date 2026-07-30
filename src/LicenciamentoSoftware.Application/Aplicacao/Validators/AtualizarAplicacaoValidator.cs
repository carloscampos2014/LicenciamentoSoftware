using FluentValidation;
using LicenciamentoSoftware.Application.Aplicacao.Commands;

namespace LicenciamentoSoftware.Application.Aplicacao.Validators;

public sealed class AtualizarAplicacaoValidator : AbstractValidator<AtualizarAplicacaoCommand>
{
    public AtualizarAplicacaoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id da aplicação é obrigatório.");

        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("Título é obrigatório.")
            .MaximumLength(120).WithMessage("Título não pode ter mais de 120 caracteres.");

        RuleFor(x => x.Descricao)
            .MaximumLength(300).WithMessage("Descrição não pode ter mais de 300 caracteres.")
            .When(x => x.Descricao is not null);
    }
}
