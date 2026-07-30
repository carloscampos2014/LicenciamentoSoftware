using FluentAssertions;
using LicenciamentoSoftware.Api.Middleware;
using LicenciamentoSoftware.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LicenciamentoSoftware.Application.Tests.Middleware;

public class AntiReplayMiddlewareTests
{
    private readonly INonceRepository _nonceRepo = Substitute.For<INonceRepository>();
    private readonly AntiReplayOptions _options = new() { JanelaMinutos = 5 };

    private AntiReplayMiddleware CriarMiddleware(RequestDelegate? next = null)
    {
        next ??= _ => Task.CompletedTask;
        return new AntiReplayMiddleware(
            next,
            NullLogger<AntiReplayMiddleware>.Instance,
            Options.Create(_options));
    }

    private static DefaultHttpContext CriarContexto(string? timestamp = null, string? nonce = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();

        if (timestamp is not null)
            ctx.Request.Headers["X-Timestamp"] = timestamp;
        if (nonce is not null)
            ctx.Request.Headers["X-Nonce"] = nonce;

        return ctx;
    }

    private static string TimestampValido() =>
        DateTimeOffset.UtcNow.ToString("O");

    // -------------------------------------------------------------------------
    // Timestamp ausente / inválido
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Invoke_SemTimestamp_Retorna400()
    {
        var ctx = CriarContexto(nonce: "nonce-valido");
        await CriarMiddleware().InvokeAsync(ctx, _nonceRepo);
        ctx.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Invoke_TimestampMalformado_Retorna400()
    {
        var ctx = CriarContexto(timestamp: "nao-e-uma-data", nonce: "nonce-valido");
        await CriarMiddleware().InvokeAsync(ctx, _nonceRepo);
        ctx.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Invoke_TimestampAntigo_ForaJanela_Retorna400()
    {
        var tsAntigo = DateTimeOffset.UtcNow.AddMinutes(-10).ToString("O");
        var ctx = CriarContexto(timestamp: tsAntigo, nonce: "nonce-valido");
        await CriarMiddleware().InvokeAsync(ctx, _nonceRepo);
        ctx.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Invoke_TimestampFuturo_ForaJanela_Retorna400()
    {
        var tsFuturo = DateTimeOffset.UtcNow.AddMinutes(10).ToString("O");
        var ctx = CriarContexto(timestamp: tsFuturo, nonce: "nonce-valido");
        await CriarMiddleware().InvokeAsync(ctx, _nonceRepo);
        ctx.Response.StatusCode.Should().Be(400);
    }

    // -------------------------------------------------------------------------
    // Nonce ausente / inválido
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Invoke_SemNonce_Retorna400()
    {
        var ctx = CriarContexto(timestamp: TimestampValido());
        await CriarMiddleware().InvokeAsync(ctx, _nonceRepo);
        ctx.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Invoke_NonceMaiorQue128Chars_Retorna400()
    {
        var nonceLongo = new string('x', 129);
        var ctx = CriarContexto(timestamp: TimestampValido(), nonce: nonceLongo);
        await CriarMiddleware().InvokeAsync(ctx, _nonceRepo);
        ctx.Response.StatusCode.Should().Be(400);
    }

    // -------------------------------------------------------------------------
    // Anti-replay
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Invoke_NonceDuplicado_Retorna400()
    {
        _nonceRepo.ExisteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var ctx = CriarContexto(timestamp: TimestampValido(), nonce: "nonce-duplicado");
        await CriarMiddleware().InvokeAsync(ctx, _nonceRepo);

        ctx.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Invoke_NonceDuplicado_NaoRegistraNovamente()
    {
        _nonceRepo.ExisteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var ctx = CriarContexto(timestamp: TimestampValido(), nonce: "nonce-duplicado");
        await CriarMiddleware().InvokeAsync(ctx, _nonceRepo);

        await _nonceRepo.DidNotReceive().RegistrarAsync(
            Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Sucesso
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Invoke_DadosValidos_ChamaNext()
    {
        _nonceRepo.ExisteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var nextChamado = false;
        var middleware = CriarMiddleware(_ =>
        {
            nextChamado = true;
            return Task.CompletedTask;
        });

        var ctx = CriarContexto(timestamp: TimestampValido(), nonce: "nonce-unico");
        await middleware.InvokeAsync(ctx, _nonceRepo);

        nextChamado.Should().BeTrue();
    }

    [Fact]
    public async Task Invoke_DadosValidos_RegistraNonce()
    {
        _nonceRepo.ExisteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var ctx = CriarContexto(timestamp: TimestampValido(), nonce: "nonce-registrar");
        await CriarMiddleware().InvokeAsync(ctx, _nonceRepo);

        await _nonceRepo.Received(1).RegistrarAsync(
            "nonce-registrar", Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invoke_TimestampNaJanela_NaoRetorna400()
    {
        _nonceRepo.ExisteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Exatamente no limite — 4min 59s no passado (dentro da janela de 5min)
        var ts = DateTimeOffset.UtcNow.AddSeconds(-299).ToString("O");
        var ctx = CriarContexto(timestamp: ts, nonce: "nonce-limite");
        await CriarMiddleware().InvokeAsync(ctx, _nonceRepo);

        ctx.Response.StatusCode.Should().NotBe(400);
    }
}
