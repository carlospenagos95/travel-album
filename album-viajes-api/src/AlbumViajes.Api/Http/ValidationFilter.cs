using FluentValidation;

namespace AlbumViajes.Api.Http;

/// <summary>
/// Ejecuta el validador de FluentValidation del cuerpo antes de llegar al handler,
/// para que ningun endpoint tenga que repetir el mismo bloque de validacion.
/// </summary>
internal sealed class ValidationFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter
    where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();

        if (request is null)
        {
            return Results.Problem(
                title: "request.missing",
                detail: "El cuerpo de la peticion es obligatorio.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var validation = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);

        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        return await next(context);
    }
}
