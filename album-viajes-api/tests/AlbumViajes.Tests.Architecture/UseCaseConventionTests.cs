using System.Reflection;
using AlbumViajes.Application.Abstractions;

namespace AlbumViajes.Tests.Architecture;

/// <summary>
/// Un caso de uso es una clase con un solo metodo publico. Sin esta regla, los
/// handlers acaban acumulando operaciones y vuelven a ser los servicios de
/// ochocientas lineas que la arquitectura pretendia evitar.
/// </summary>
public sealed class UseCaseConventionTests
{
    private static readonly Assembly Application = typeof(IClock).Assembly;

    [Fact]
    public void Cada_handler_expone_un_unico_metodo_publico()
    {
        var offenders = Handlers()
            .Select(handler => new
            {
                handler.Name,
                Methods = handler
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(method => !method.IsSpecialName)
                    .ToArray(),
            })
            .Where(handler => handler.Methods.Length != 1)
            .Select(handler => $"{handler.Name} ({handler.Methods.Length})")
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"Un caso de uso = una operacion. Handlers con otro numero de metodos publicos: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void El_metodo_de_cada_handler_se_llama_HandleAsync()
    {
        var offenders = Handlers()
            .SelectMany(handler => handler
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName && method.Name != "HandleAsync")
                .Select(method => $"{handler.Name}.{method.Name}"))
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"El punto de entrada de un caso de uso siempre es HandleAsync: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Cada_handler_recibe_un_token_de_cancelacion()
    {
        var offenders = Handlers()
            .SelectMany(handler => handler
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Where(method => method.GetParameters().All(parameter => parameter.ParameterType != typeof(CancellationToken)))
                .Select(method => $"{handler.Name}.{method.Name}"))
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"Toda operacion que sale a la base o a la red debe poder cancelarse: {string.Join(", ", offenders)}");
    }

    private static Type[] Handlers() =>
        Application.GetTypes()
            .Where(type => type.IsClass && type.IsPublic && type.Name.EndsWith("Handler", StringComparison.Ordinal))
            .ToArray();
}
