using System.Reflection;
using AlbumViajes.Application.Abstractions;
using AlbumViajes.Domain.Common;
using AlbumViajes.Infrastructure.Persistence;
using NetArchTest.Rules;

namespace AlbumViajes.Tests.Architecture;

/// <summary>
/// La regla de dependencia de la arquitectura limpia, verificada por el build.
/// Si alguien mete infraestructura en el dominio, estos tests fallan.
/// </summary>
public sealed class LayerDependencyTests
{
    private static readonly Assembly Domain = typeof(Result).Assembly;
    private static readonly Assembly Application = typeof(IClock).Assembly;
    private static readonly Assembly Infrastructure = typeof(AlbumViajesDbContext).Assembly;

    [Fact]
    public void Domain_no_depende_de_ninguna_otra_capa()
    {
        var forbidden = new[] { "AlbumViajes.Application", "AlbumViajes.Infrastructure", "AlbumViajes.Api" };

        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        AssertSuccess(result, "El dominio no puede conocer las capas externas.");
    }

    [Fact]
    public void Domain_no_conoce_frameworks_de_infraestructura()
    {
        var frameworks = new[]
        {
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "Npgsql",
            "System.Net.Http",
            "Microsoft.Extensions.DependencyInjection",
        };

        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(frameworks)
            .GetResult();

        AssertSuccess(result, "El dominio debe ser C# puro, sin frameworks.");
    }

    [Fact]
    public void Application_no_depende_de_infraestructura_ni_de_la_api()
    {
        var forbidden = new[] { "AlbumViajes.Infrastructure", "AlbumViajes.Api" };

        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        AssertSuccess(result, "La aplicacion habla con la infraestructura solo por interfaces.");
    }

    [Fact]
    public void Application_no_conoce_ef_core_ni_aspnet()
    {
        var frameworks = new[] { "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql" };

        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny(frameworks)
            .GetResult();

        AssertSuccess(result, "Los detalles de persistencia y HTTP no pertenecen a la capa de aplicacion.");
    }

    [Fact]
    public void Infrastructure_no_depende_de_la_api()
    {
        var result = Types.InAssembly(Infrastructure)
            .ShouldNot()
            .HaveDependencyOn("AlbumViajes.Api")
            .GetResult();

        AssertSuccess(result, "La infraestructura no puede depender de la capa web.");
    }

    private static void AssertSuccess(TestResult result, string because)
    {
        var offenders = result.FailingTypeNames is null ? [] : result.FailingTypeNames.ToArray();
        Assert.True(result.IsSuccessful, $"{because} Tipos que la incumplen: {string.Join(", ", offenders)}");
    }
}
