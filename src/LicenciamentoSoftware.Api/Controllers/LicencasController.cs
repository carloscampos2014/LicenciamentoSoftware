using LicenciamentoSoftware.Api.DTOs;
using LicenciamentoSoftware.Domain.Entities;
using LicenciamentoSoftware.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicenciamentoSoftware.Api.Controllers;

[ApiController]
[Route("api/licencas")]
public class LicencasController : ControllerBase
{
    private readonly LicenciamentoDbContext _db;
    public LicencasController(LicenciamentoDbContext db) => _db = db;

    private static LicencaResponse ToResponse(Licenca l) => new(
        l.Id, l.IdCliente, l.IdClienteFinal, l.IdAplicativo,
        l.Aplicativo!.IdTipoLicenca, l.DataCadastro, l.Ativo,
        l.Periodo is null ? null : new LicencaPeriodoDto(l.Periodo.DataInicio, l.Periodo.DataFim, l.Periodo.RenovacaoAutomatica),
        l.Usuarios is null ? null : new LicencaUsuariosDto(l.Usuarios.QuantidadeMaxima, l.Usuarios.MaxSessoesPorUsuario, l.Usuarios.TempoLimiteSessaoHoras),
        l.Instalacao is null ? null : new LicencaInstalacaoDto(l.Instalacao.QuantidadeMaxima));

    private IQueryable<Licenca> QueryComDetalhes() => _db.Licencas
        .Include(l => l.Aplicativo)
        .Include(l => l.Periodo)
        .Include(l => l.Usuarios)
        .Include(l => l.Instalacao);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LicencaResponse>>> Listar(
        [FromQuery] Guid? idCliente, [FromQuery] Guid? idClienteFinal, [FromQuery] Guid? idAplicativo,
        [FromQuery] bool incluirInativos = false)
    {
        var query = QueryComDetalhes();
        if (idCliente.HasValue) query = query.Where(l => l.IdCliente == idCliente);
        if (idClienteFinal.HasValue) query = query.Where(l => l.IdClienteFinal == idClienteFinal);
        if (idAplicativo.HasValue) query = query.Where(l => l.IdAplicativo == idAplicativo);
        if (!incluirInativos) query = query.Where(l => l.Ativo);

        var itens = await query.OrderByDescending(l => l.DataCadastro).ToListAsync();
        return Ok(itens.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LicencaResponse>> ObterPorId(Guid id)
    {
        var licenca = await QueryComDetalhes().FirstOrDefaultAsync(l => l.Id == id);
        if (licenca is null) return NotFound();
        return Ok(ToResponse(licenca));
    }

    [HttpPost]
    public async Task<ActionResult<LicencaResponse>> Criar(LicencaCreateRequest request)
    {
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == request.IdCliente && c.Ativo);
        if (cliente is null) return BadRequest("Cliente informado não existe ou está inativo.");

        var clienteFinal = await _db.ClientesFinais.FirstOrDefaultAsync(c => c.Id == request.IdClienteFinal && c.Ativo);
        if (clienteFinal is null) return BadRequest("Cliente Final informado não existe ou está inativo.");
        if (clienteFinal.IdCliente != request.IdCliente) return BadRequest("Cliente Final não pertence ao Cliente informado.");

        var aplicativo = await _db.Aplicacoes.FirstOrDefaultAsync(a => a.Id == request.IdAplicativo && a.Ativo);
        if (aplicativo is null) return BadRequest("Aplicativo informado não existe ou está inativo.");
        if (aplicativo.IdCliente != request.IdCliente) return BadRequest("Aplicativo não pertence ao Cliente informado.");

        var erroDetalhe = ValidarBlocoDetalhe(aplicativo.IdTipoLicenca, request.Periodo, request.Usuarios, request.Instalacao);
        if (erroDetalhe is not null) return BadRequest(erroDetalhe);

        var licenca = new Licenca
        {
            Id = Guid.NewGuid(),
            IdCliente = request.IdCliente,
            IdClienteFinal = request.IdClienteFinal,
            IdAplicativo = request.IdAplicativo,
            DataCadastro = DateTime.UtcNow,
            Ativo = true
        };

        AdicionarDetalhe(licenca, aplicativo.IdTipoLicenca, request.Periodo, request.Usuarios, request.Instalacao);

        _db.Licencas.Add(licenca);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Violação da constraint de licença ativa única para Cliente+ClienteFinal+Aplicativo.
            return Conflict("Já existe uma licença ativa para essa combinação de Cliente, Cliente Final e Aplicativo.");
        }

        licenca.Aplicativo = aplicativo;
        return CreatedAtAction(nameof(ObterPorId), new { id = licenca.Id }, ToResponse(licenca));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LicencaResponse>> Atualizar(Guid id, LicencaUpdateRequest request)
    {
        var licenca = await QueryComDetalhes().FirstOrDefaultAsync(l => l.Id == id);
        if (licenca is null) return NotFound();

        var erroDetalhe = ValidarBlocoDetalhe(licenca.Aplicativo!.IdTipoLicenca, request.Periodo, request.Usuarios, request.Instalacao, exigirBloco: false);
        if (erroDetalhe is not null) return BadRequest(erroDetalhe);

        licenca.Ativo = request.Ativo;

        if (request.Periodo is not null && licenca.Periodo is not null)
        {
            licenca.Periodo.DataInicio = request.Periodo.DataInicio;
            licenca.Periodo.DataFim = request.Periodo.DataFim;
            licenca.Periodo.RenovacaoAutomatica = request.Periodo.RenovacaoAutomatica;
        }

        if (request.Usuarios is not null && licenca.Usuarios is not null)
        {
            licenca.Usuarios.QuantidadeMaxima = request.Usuarios.QuantidadeMaxima;
            licenca.Usuarios.MaxSessoesPorUsuario = request.Usuarios.MaxSessoesPorUsuario;
            licenca.Usuarios.TempoLimiteSessaoHoras = request.Usuarios.TempoLimiteSessaoHoras;
        }

        if (request.Instalacao is not null && licenca.Instalacao is not null)
        {
            licenca.Instalacao.QuantidadeMaxima = request.Instalacao.QuantidadeMaxima;
        }

        await _db.SaveChangesAsync();
        return Ok(ToResponse(licenca));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Desativar(Guid id)
    {
        var licenca = await _db.Licencas.FindAsync(id);
        if (licenca is null) return NotFound();

        licenca.Ativo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static string? ValidarBlocoDetalhe(
        Guid idTipoLicenca, LicencaPeriodoDto? periodo, LicencaUsuariosDto? usuarios, LicencaInstalacaoDto? instalacao,
        bool exigirBloco = true)
    {
        var blocosPreenchidos = new[] { periodo is not null, usuarios is not null, instalacao is not null }.Count(b => b);

        if (idTipoLicenca == TipoLicenca.Permanente)
            return blocosPreenchidos > 0 ? "Licença Permanente não deve receber blocos de detalhe (Periodo/Usuarios/Instalacao)." : null;

        if (idTipoLicenca == TipoLicenca.PorPeriodo)
        {
            if (usuarios is not null || instalacao is not null) return "Aplicativo é do tipo Por Período - envie apenas o bloco 'Periodo'.";
            if (exigirBloco && periodo is null) return "Aplicativo é do tipo Por Período - o bloco 'Periodo' é obrigatório.";
            return null;
        }

        if (idTipoLicenca == TipoLicenca.PorUsuarios)
        {
            if (periodo is not null || instalacao is not null) return "Aplicativo é do tipo Por Usuários - envie apenas o bloco 'Usuarios'.";
            if (exigirBloco && usuarios is null) return "Aplicativo é do tipo Por Usuários - o bloco 'Usuarios' é obrigatório.";
            return null;
        }

        if (idTipoLicenca == TipoLicenca.PorInstalacao)
        {
            if (periodo is not null || usuarios is not null) return "Aplicativo é do tipo Por Instalação - envie apenas o bloco 'Instalacao'.";
            if (exigirBloco && instalacao is null) return "Aplicativo é do tipo Por Instalação - o bloco 'Instalacao' é obrigatório.";
            return null;
        }

        return "Tipo de licença do Aplicativo não reconhecido.";
    }

    private static void AdicionarDetalhe(
        Licenca licenca, Guid idTipoLicenca,
        LicencaPeriodoDto? periodo, LicencaUsuariosDto? usuarios, LicencaInstalacaoDto? instalacao)
    {
        if (idTipoLicenca == TipoLicenca.PorPeriodo && periodo is not null)
        {
            licenca.Periodo = new LicencaPeriodo
            {
                Id = Guid.NewGuid(),
                LicencaId = licenca.Id,
                DataInicio = periodo.DataInicio,
                DataFim = periodo.DataFim,
                RenovacaoAutomatica = periodo.RenovacaoAutomatica
            };
        }
        else if (idTipoLicenca == TipoLicenca.PorUsuarios && usuarios is not null)
        {
            licenca.Usuarios = new LicencaUsuarios
            {
                Id = Guid.NewGuid(),
                LicencaId = licenca.Id,
                QuantidadeMaxima = usuarios.QuantidadeMaxima,
                MaxSessoesPorUsuario = usuarios.MaxSessoesPorUsuario,
                TempoLimiteSessaoHoras = usuarios.TempoLimiteSessaoHoras
            };
        }
        else if (idTipoLicenca == TipoLicenca.PorInstalacao && instalacao is not null)
        {
            licenca.Instalacao = new LicencaInstalacao
            {
                Id = Guid.NewGuid(),
                LicencaId = licenca.Id,
                QuantidadeMaxima = instalacao.QuantidadeMaxima
            };
        }
    }
}
