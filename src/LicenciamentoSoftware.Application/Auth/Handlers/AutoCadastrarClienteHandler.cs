using FluentValidation;
using LicenciamentoSoftware.Application.Abstractions;
using LicenciamentoSoftware.Application.Auth.Commands;
using LicenciamentoSoftware.Application.Auth.Results;
using LicenciamentoSoftware.Application.Cliente.Abstractions;
using LicenciamentoSoftware.Domain.Enums;
using LicenciamentoSoftware.Domain.ValueObjects;

namespace LicenciamentoSoftware.Application.Auth.Handlers;

/// <summary>
/// Cria um novo Cliente + primeiro Usuário (AdministradorCliente) em uma única transação.
/// Endpoint público — equivalente ao self-service de cadastro da plataforma.
/// </summary>
public sealed class AutoCadastrarClienteHandler
{
    private readonly IClienteRepository _clienteRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogWriter _auditLog;
    private readonly IUnitOfWork _uow;

    public AutoCadastrarClienteHandler(
        IClienteRepository clienteRepo,
        IUsuarioRepository usuarioRepo,
        IPasswordHasher passwordHasher,
        IAuditLogWriter auditLog,
        IUnitOfWork uow)
    {
        _clienteRepo    = clienteRepo;
        _usuarioRepo    = usuarioRepo;
        _passwordHasher = passwordHasher;
        _auditLog       = auditLog;
        _uow            = uow;
    }

    public async Task<AutoCadastrarClienteResult> HandleAsync(
        AutoCadastrarClienteCommand command,
        CancellationToken ct = default)
    {
        // 1. Validação de entrada
        var erros = Validar(command);
        if (erros.Count > 0)
            return new AutoCadastrarClienteResult.Invalido(erros);

        // 2. Unicidade da inscrição
        var inscricaoDuplicada = await _clienteRepo.ExisteInscricaoAsync(
            command.TipoInscricao, command.NumeroInscricao, null, ct);
        if (inscricaoDuplicada)
            return new AutoCadastrarClienteResult.InscricaoJaExiste();

        // 3. Unicidade do e-mail do responsável
        var emailExistente = await _usuarioRepo.BuscarPorEmailAsync(
            command.EmailResponsavel, ct);
        if (emailExistente is not null)
            return new AutoCadastrarClienteResult.EmailJaEmUso();

        // 4. Montar entidades — value objects validam invariantes internamente
        Domain.Entities.Cliente cliente;
        Domain.Entities.Usuario usuario;
        try
        {
            var inscricao = new Inscricao(
                (TipoInscricao)command.TipoInscricao, command.NumeroInscricao);
            var emailCliente = new Email(command.EmailCliente);
            var telefone = command.Telefone is not null
                ? new Telefone(command.Telefone) : null;

            cliente = Domain.Entities.Cliente.Criar(
                command.RazaoSocial, inscricao, emailCliente, telefone);

            var senhaHash = _passwordHasher.Hash(command.Senha);
            usuario = Domain.Entities.Usuario.Criar(
                cliente.Id, command.NomeResponsavel,
                senhaHash, command.EmailResponsavel);
        }
        catch (Domain.Exceptions.DomainException ex)
        {
            return new AutoCadastrarClienteResult.Invalido([ex.Message]);
        }

        // 5. Persistir em uma única transação
        await _uow.BeginAsync(cancellationToken: ct);
        try
        {
            await _clienteRepo.InserirAsync(cliente, ct);

            // Primeiro usuário do cliente → AdministradorCliente
            await _usuarioRepo.SalvarAsync(usuario, "AdministradorCliente", ct);

            var logCliente = Domain.Entities.LogOperacao.Criar(
                "Cliente", cliente.Id, Domain.Enums.TipoOperacao.Insercao);
            var logUsuario = Domain.Entities.LogOperacao.Criar(
                "Usuario", usuario.Id, Domain.Enums.TipoOperacao.Insercao);

            await _auditLog.RegistrarAsync(logCliente, ct);
            await _auditLog.RegistrarAsync(logUsuario, ct);

            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }

        return new AutoCadastrarClienteResult.Sucesso(cliente.Id, usuario.Id);
    }

    private static List<string> Validar(AutoCadastrarClienteCommand c)
    {
        var erros = new List<string>();

        if (string.IsNullOrWhiteSpace(c.RazaoSocial))
            erros.Add("Razão Social é obrigatória.");
        else if (c.RazaoSocial.Length > 200)
            erros.Add("Razão Social deve ter no máximo 200 caracteres.");

        if (string.IsNullOrWhiteSpace(c.NumeroInscricao))
            erros.Add("CPF/CNPJ é obrigatório.");

        if (string.IsNullOrWhiteSpace(c.EmailCliente))
            erros.Add("E-mail da empresa é obrigatório.");

        if (string.IsNullOrWhiteSpace(c.NomeResponsavel))
            erros.Add("Nome do responsável é obrigatório.");

        if (string.IsNullOrWhiteSpace(c.EmailResponsavel))
            erros.Add("E-mail do responsável é obrigatório.");

        if (string.IsNullOrWhiteSpace(c.Senha) || c.Senha.Length < 8)
            erros.Add("Senha deve ter no mínimo 8 caracteres.");

        return erros;
    }
}
