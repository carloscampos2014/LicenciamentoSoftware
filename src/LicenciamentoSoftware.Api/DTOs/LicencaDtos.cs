namespace LicenciamentoSoftware.Api.DTOs;

// Campos específicos por tipo de licença - só o bloco correspondente ao
// IdTipoLicenca da Aplicação vinculada deve ser preenchido.

public record LicencaPeriodoDto(DateTime DataInicio, DateTime DataFim, bool RenovacaoAutomatica);

public record LicencaUsuariosDto(int QuantidadeMaxima, int MaxSessoesPorUsuario = 5, int TempoLimiteSessaoHoras = 24);

public record LicencaInstalacaoDto(int QuantidadeMaxima);

public record LicencaCreateRequest(
    Guid IdCliente,
    Guid IdClienteFinal,
    Guid IdAplicativo,
    LicencaPeriodoDto? Periodo = null,
    LicencaUsuariosDto? Usuarios = null,
    LicencaInstalacaoDto? Instalacao = null);

public record LicencaUpdateRequest(
    bool Ativo,
    LicencaPeriodoDto? Periodo = null,
    LicencaUsuariosDto? Usuarios = null,
    LicencaInstalacaoDto? Instalacao = null);

public record LicencaResponse(
    Guid Id,
    Guid IdCliente,
    Guid IdClienteFinal,
    Guid IdAplicativo,
    Guid IdTipoLicenca,
    DateTime DataCadastro,
    bool Ativo,
    LicencaPeriodoDto? Periodo,
    LicencaUsuariosDto? Usuarios,
    LicencaInstalacaoDto? Instalacao);
