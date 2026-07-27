using LicenciamentoSoftware.Api.DTOs;
using LicenciamentoSoftware.Domain.Entities;
using LicenciamentoSoftware.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LicenciamentoSoftware.Api.Services;

public class LicencaValidacaoService : ILicencaValidacaoService
{
    private readonly LicenciamentoDbContext _db;

    public LicencaValidacaoService(LicenciamentoDbContext db)
    {
        _db = db;
    }

    public async Task<ValidacaoResponse> ValidarLoginAsync(ValidarLoginRequest request)
    {
        var licenca = await _db.Licencas
            .Include(l => l.Aplicativo)
            .Include(l => l.Usuarios)
            .FirstOrDefaultAsync(l =>
                l.IdCliente == request.IdCliente &&
                l.IdClienteFinal == request.IdClienteFinal &&
                l.IdAplicativo == request.IdAplicativo &&
                l.Ativo);

        if (licenca is null || licenca.Aplicativo is null)
            return new ValidacaoResponse(false, "Licença não encontrada ou inativa.");

        var tipoLicencaId = licenca.Aplicativo.IdTipoLicenca;

        // Tipos que não envolvem controle de sessão respondem de forma direta.
        if (tipoLicencaId == TipoLicenca.Permanente)
            return new ValidacaoResponse(true, "Licença permanente - acesso liberado.");

        if (tipoLicencaId == TipoLicenca.PorPeriodo)
        {
            var periodo = await _db.LicencasPeriodo.FirstOrDefaultAsync(p => p.LicencaId == licenca.Id);
            if (periodo is null || periodo.DataFim < DateTime.UtcNow)
                return new ValidacaoResponse(false, "Licença expirada.");

            return new ValidacaoResponse(true, "Licença dentro do período de vigência.");
        }

        if (tipoLicencaId != TipoLicenca.PorUsuarios)
            return new ValidacaoResponse(false, "Tipo de licença incompatível com o endpoint de login.");

        var config = licenca.Usuarios;
        if (config is null)
            return new ValidacaoResponse(false, "Configuração de licença Por Usuários não encontrada.");

        // 1) Limite de sessões simultâneas do MESMO usuário.
        var sessoesDoUsuario = await _db.LicencasSessao.CountAsync(s =>
            s.LicencaId == licenca.Id &&
            s.IdentificadorUsuario == request.IdentificadorUsuario &&
            s.Ativo);

        if (sessoesDoUsuario >= config.MaxSessoesPorUsuario)
            return new ValidacaoResponse(false,
                $"Limite de sessões simultâneas ({config.MaxSessoesPorUsuario}) atingido para o usuário '{request.IdentificadorUsuario}'.");

        // 2) Se for um usuário novo (sem sessão ativa), checar limite de usuários DISTINTOS.
        if (sessoesDoUsuario == 0)
        {
            var usuariosDistintosAtivos = await _db.LicencasSessao
                .Where(s => s.LicencaId == licenca.Id && s.Ativo)
                .Select(s => s.IdentificadorUsuario)
                .Distinct()
                .CountAsync();

            if (usuariosDistintosAtivos >= config.QuantidadeMaxima)
                return new ValidacaoResponse(false,
                    $"Limite de usuários simultâneos ({config.QuantidadeMaxima}) atingido.");
        }

        var sessao = new LicencaSessao
        {
            Id = Guid.NewGuid(),
            LicencaId = licenca.Id,
            IdentificadorUsuario = request.IdentificadorUsuario,
            DataLogin = DateTime.UtcNow,
            DataUltimaAtividade = DateTime.UtcNow,
            Ativo = true
        };

        _db.LicencasSessao.Add(sessao);
        await _db.SaveChangesAsync();

        return new ValidacaoResponse(true, "Login autorizado.", sessao.Id);
    }

    public async Task<ValidacaoResponse> HeartbeatAsync(HeartbeatRequest request)
    {
        var sessao = await _db.LicencasSessao.FirstOrDefaultAsync(s => s.Id == request.SessaoId && s.Ativo);

        if (sessao is null)
            return new ValidacaoResponse(false, "Sessão não encontrada ou já encerrada.");

        sessao.DataUltimaAtividade = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new ValidacaoResponse(true, "Heartbeat registrado.");
    }

    public async Task<ValidacaoResponse> LogoutAsync(LogoutRequest request)
    {
        var sessao = await _db.LicencasSessao.FirstOrDefaultAsync(s => s.Id == request.SessaoId && s.Ativo);

        if (sessao is null)
            return new ValidacaoResponse(false, "Sessão não encontrada ou já encerrada.");

        sessao.Ativo = false;
        await _db.SaveChangesAsync();

        return new ValidacaoResponse(true, "Sessão encerrada com sucesso.");
    }

    public async Task<ValidacaoResponse> ValidarInstalacaoAsync(ValidarInstalacaoRequest request)
    {
        var licenca = await _db.Licencas
            .Include(l => l.Aplicativo)
            .Include(l => l.Instalacao)
            .FirstOrDefaultAsync(l =>
                l.IdCliente == request.IdCliente &&
                l.IdClienteFinal == request.IdClienteFinal &&
                l.IdAplicativo == request.IdAplicativo &&
                l.Ativo);

        if (licenca is null || licenca.Aplicativo is null)
            return new ValidacaoResponse(false, "Licença não encontrada ou inativa.");

        if (licenca.Aplicativo.IdTipoLicenca != TipoLicenca.PorInstalacao)
            return new ValidacaoResponse(false, "Tipo de licença incompatível com o endpoint de instalação.");

        var config = licenca.Instalacao;
        if (config is null)
            return new ValidacaoResponse(false, "Configuração de licença Por Instalação não encontrada.");

        // Máquina já autorizada -> sempre libera.
        var jaRegistrada = await _db.LicencasInstalacaoRegistrada.AnyAsync(r =>
            r.LicencaId == licenca.Id &&
            r.IdentificadorMaquina == request.IdentificadorMaquina &&
            r.Ativo);

        if (jaRegistrada)
            return new ValidacaoResponse(true, "Instalação já autorizada.");

        // Máquina nova -> checar limite de instalações distintas.
        var instalacoesAtivas = await _db.LicencasInstalacaoRegistrada.CountAsync(r =>
            r.LicencaId == licenca.Id && r.Ativo);

        if (instalacoesAtivas >= config.QuantidadeMaxima)
            return new ValidacaoResponse(false,
                $"Limite de instalações ({config.QuantidadeMaxima}) atingido.");

        _db.LicencasInstalacaoRegistrada.Add(new LicencaInstalacaoRegistrada
        {
            Id = Guid.NewGuid(),
            LicencaId = licenca.Id,
            IdentificadorMaquina = request.IdentificadorMaquina,
            DataRegistro = DateTime.UtcNow,
            Ativo = true
        });
        await _db.SaveChangesAsync();

        return new ValidacaoResponse(true, "Instalação autorizada.");
    }
}
