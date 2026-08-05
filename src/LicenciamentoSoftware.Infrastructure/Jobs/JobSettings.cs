namespace LicenciamentoSoftware.Infrastructure.Jobs;

/// <summary>
/// Configurações dos jobs agendados — lidas de <c>JobSettings</c> no appsettings.json.
/// </summary>
public sealed class JobSettings
{
    /// <summary>Delay inicial em segundos antes do primeiro ciclo de jobs (padrão: 30s).</summary>
    public int DelayInicialSegundos { get; set; } = 30;

    /// <summary>Intervalo em minutos para o job de encerramento de sessões inativas (padrão: 5 min).</summary>
    public int SessoesInativasIntervaloMinutos { get; set; } = 5;

    /// <summary>Intervalo em minutos para o job de expiração de licenças Por Período (padrão: 60 min).</summary>
    public int ExpiracaoLicencasIntervaloMinutos { get; set; } = 60;

    /// <summary>Intervalo em minutos para o job de renovação automática de licenças (padrão: 60 min).</summary>
    public int RenovacaoAutomaticaIntervaloMinutos { get; set; } = 60;

    /// <summary>Intervalo em minutos para o job de rotação automática de tokens (padrão: 720 min = 12h).</summary>
    public int RotacaoTokensIntervaloMinutos { get; set; } = 720;

    /// <summary>Intervalo em minutos para o job de notificação por e-mail (padrão: 1440 min = 24h).</summary>
    public int NotificacaoIntervaloMinutos { get; set; } = 1440;

    /// <summary>Dias de antecedência para considerar licença/token "próximo do vencimento" (padrão: 7 dias).</summary>
    public int DiasAntecedenciaNotificacao { get; set; } = 7;

    /// <summary>Horas sem heartbeat para considerar sessão inativa (padrão: 24h).</summary>
    public int SessoesInativasLimiteHoras { get; set; } = 24;

    /// <summary>Intervalo em minutos para o job de exclusão física de empresas encerradas (padrão: 1440 min = 24h).</summary>
    public int ExclusaoEmpresasIntervaloMinutos { get; set; } = 1440;
}
