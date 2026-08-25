using System.Reflection;
using AlbumViajes.Domain.Common;
using NetArchTest.Rules;

namespace AlbumViajes.Tests.Architecture;

/// <summary>
/// Convenciones que mantienen el modelo rico: sin ellas, el dominio degenera en
/// clases con setters publicos y la logica se escapa a los servicios.
/// </summary>
public sealed class DomainConventionTests
{
    private static readonly Assembly Domain = typeof(Result).Assembly;

    [Fact]
    public void Las_entidades_son_selladas()
    {
        var result = Types.InAssembly(Domain)
            .That()
            .Inherit(typeof(Entity))
            .Should()
            .BeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful, "Las entidades deben ser sealed: la herencia no es parte de este modelo.");
    }

    [Fact]
    public void Las_entidades_no_exponen_setters_publicos()
    {
        var mutableProperties = Domain.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(Entity)))
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Where(property => property.SetMethod?.IsPublic == true)
            .Select(property => $"{property.DeclaringType!.Name}.{property.Name}")
            .ToArray();

        Assert.True(
            mutableProperties.Length == 0,
            $"El estado solo cambia por metodos con nombre de intencion. Setters publicos encontrados: {string.Join(", ", mutableProperties)}");
    }
}
