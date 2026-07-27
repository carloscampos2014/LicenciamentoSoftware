namespace LicenciamentoSoftware.Api.DTOs;

public record ValidarLoginRequest(Guid IdCliente, Guid IdClienteFinal, Guid IdAplicativo, string IdentificadorUsuario);
public record ValidarInstalacaoRequest(Guid IdCliente, Guid IdClienteFinal, Guid IdAplicativo, string IdentificadorMaquina);
public record HeartbeatRequest(Guid SessaoId);
public record LogoutRequest(Guid SessaoId);

public record ValidacaoResponse(bool Liberado, string Mensagem, Guid? SessaoId = null);
