using FluentValidation;
using LicenciamentoSoftware.Application.Licenca.Commands;

namespace LicenciamentoSoftware.Application.Licenca.Validators;

public sealed class EmitirLicencaValidator : AbstractValidator<EmitirLicencaCommand>
{
    // UUIDs dos tipos de licença (seed V001)
    private static readonly Guid TipoPermanente   = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TipoPeriodo      = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TipoUsuarios     = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TipoInstalacao   = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public EmitirLicencaValidator()
    {
        RuleFor(x => x.IdClienteFinal)
            .NotEmpty().WithMessage("IdClienteFinal é obrigatório.");

        RuleFor(x => x.IdAplicativo)
            .NotEmpty().WithMessage("IdAplicativo é obrigatório.");

        // Exatamente um bloco de detalhe ou nenhum (Permanente não requer detalhe)
        RuleFor(x => x)
            .Must(x => ContaDetalheUnico(x))
            .WithMessage("Informe exatamente um bloco de detalhe (Periodo, Usuarios ou Instalacao), ou nenhum para licença Permanente.");

        // Período: datas obrigatórias e coerentes
        When(x => x.Periodo is not null, () =>
        {
            RuleFor(x => x.Periodo!.DataFim)
                .GreaterThan(x => x.Periodo!.DataInicio)
                .WithMessage("DataFim do período deve ser posterior a DataInicio.");

            RuleFor(x => x.Periodo!.DataInicio)
                .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
                .WithMessage("DataInicio não pode ser no passado.");
        });

        // Usuários: quantidades positivas
        When(x => x.Usuarios is not null, () =>
        {
            RuleFor(x => x.Usuarios!.QuantidadeMaxima)
                .GreaterThan(0).WithMessage("QuantidadeMaxima de usuários deve ser maior que zero.");

            RuleFor(x => x.Usuarios!.MaxSessoesPorUsuario)
                .GreaterThan(0).WithMessage("MaxSessoesPorUsuario deve ser maior que zero.");

            RuleFor(x => x.Usuarios!.TempoLimiteSessaoHoras)
                .GreaterThan(0).WithMessage("TempoLimiteSessaoHoras deve ser maior que zero.");
        });

        // Instalação: quantidade positiva
        When(x => x.Instalacao is not null, () =>
        {
            RuleFor(x => x.Instalacao!.QuantidadeMaxima)
                .GreaterThan(0).WithMessage("QuantidadeMaxima de instalações deve ser maior que zero.");
        });

        // Token: expiração positiva se informada
        When(x => x.ExpiracaoTokenMinutos.HasValue, () =>
        {
            RuleFor(x => x.ExpiracaoTokenMinutos!.Value)
                .GreaterThan(0).WithMessage("ExpiracaoTokenMinutos deve ser maior que zero.");
        });
    }

    private static bool ContaDetalheUnico(EmitirLicencaCommand x)
    {
        var count = (x.Periodo is not null ? 1 : 0)
                  + (x.Usuarios is not null ? 1 : 0)
                  + (x.Instalacao is not null ? 1 : 0);
        // 0 = Permanente (sem detalhe), 1 = um detalhe
        return count <= 1;
    }
}
