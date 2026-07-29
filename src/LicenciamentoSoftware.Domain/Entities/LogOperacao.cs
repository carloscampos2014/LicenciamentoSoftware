using LicenciamentoSoftware.Domain.Enums;
using LicenciamentoSoftware.Domain.Exceptions;

namespace LicenciamentoSoftware.Domain.Entities;

public sealed class LogOperacao
{
    public Guid Id { get; private set; }
    public string Entidade { get; private set; } = string.Empty;
    public Guid IdRegistro { get; private set; }
    public TipoOperacao Operacao { get; private set; }
    public DateTime DataHora { get; private set; }
    public Guid? IdUsuario { get; private set; }
    public string? CamposAlterados { get; private set; }

    private LogOperacao() { }

    public static LogOperacao Criar(
        string entidade,
        Guid idRegistro,
        TipoOperacao operacao,
        Guid? idUsuario = null,
        string? camposAlterados = null)
    {
        if (string.IsNullOrWhiteSpace(entidade))
            throw new DomainException("Entidade é obrigatória no log de operação.");

        if (entidade.Length > 100)
            throw new DomainException("Nome da entidade não pode ter mais de 100 caracteres.");

        if (idRegistro == Guid.Empty)
            throw new DomainException("IdRegistro é obrigatório no log de operação.");

        return new LogOperacao
        {
            Id = Guid.NewGuid(),
            Entidade = entidade.Trim(),
            IdRegistro = idRegistro,
            Operacao = operacao,
            DataHora = DateTime.UtcNow,
            IdUsuario = idUsuario,
            CamposAlterados = camposAlterados
        };
    }
}
