using NetArchTest.Rules;
using FluentAssertions;

namespace LicenciamentoSoftware.Application.Tests.Architecture;

/// <summary>
/// Testes de arquitetura que verificam a regra de dependências da Clean Architecture.
/// Garantem que as camadas internas nunca referenciem camadas externas.
/// </summary>
public class ArchitectureTests
{
    private const string DomainAssembly = "LicenciamentoSoftware.Domain";
    private const string ApplicationAssembly = "LicenciamentoSoftware.Application";
    private const string InfrastructureAssembly = "LicenciamentoSoftware.Infrastructure";
    private const string ApiAssembly = "LicenciamentoSoftware.Api";

    [Fact]
    public void Domain_NaoDeveDependerDeNenhumProjeto_DaSolucao()
    {
        // O Domain é o núcleo — não pode conhecer nenhuma outra camada.
        var resultado = Types.InAssembly(typeof(LicenciamentoSoftware.Domain.DomainAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationAssembly,
                InfrastructureAssembly,
                ApiAssembly)
            .GetResult();

        resultado.IsSuccessful.Should().BeTrue(
            because: "Domain não pode depender de Application, Infrastructure ou Api");
    }

    [Fact]
    public void Application_NaoDeveDependerDeInfrastructure()
    {
        // Application orquestra casos de uso mas não conhece detalhes de persistência.
        var resultado = Types.InAssembly(typeof(LicenciamentoSoftware.Application.ApplicationAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureAssembly)
            .GetResult();

        resultado.IsSuccessful.Should().BeTrue(
            because: "Application não pode depender de Infrastructure");
    }

    [Fact]
    public void Application_NaoDeveDependerDeApi()
    {
        // Application não pode conhecer controllers, HttpContext ou qualquer contrato HTTP.
        var resultado = Types.InAssembly(typeof(LicenciamentoSoftware.Application.ApplicationAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiAssembly)
            .GetResult();

        resultado.IsSuccessful.Should().BeTrue(
            because: "Application não pode depender de Api");
    }

    [Fact]
    public void Domain_NaoDeveDependerDeApplication()
    {
        var resultado = Types.InAssembly(typeof(LicenciamentoSoftware.Domain.DomainAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApplicationAssembly)
            .GetResult();

        resultado.IsSuccessful.Should().BeTrue(
            because: "Domain não pode depender de Application");
    }
}
