using LicenciamentoSoftware.Api.DTOs;

namespace LicenciamentoSoftware.Api.Services;

public interface ILicencaValidacaoService
{
    Task<ValidacaoResponse> ValidarLoginAsync(ValidarLoginRequest request);
    Task<ValidacaoResponse> HeartbeatAsync(HeartbeatRequest request);
    Task<ValidacaoResponse> LogoutAsync(LogoutRequest request);
    Task<ValidacaoResponse> ValidarInstalacaoAsync(ValidarInstalacaoRequest request);
}
