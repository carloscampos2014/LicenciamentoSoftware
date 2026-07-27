namespace LicenciamentoSoftware.Api.DTOs;

public record UsuarioCreateRequest(Guid IdCliente, string Nome);
public record UsuarioUpdateRequest(string Nome, bool Ativo);
public record UsuarioResponse(Guid Id, Guid IdCliente, string Nome, bool Ativo);
