using AlbumViajes.Api.Auth;
using AlbumViajes.Api.Endpoints;
using AlbumViajes.Application;
using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Cities.Contracts;
using AlbumViajes.Infrastructure;
using AlbumViajes.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string WebCorsPolicy = "web";

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddValidatorsFromAssemblyContaining<SaveCityRequestValidator>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddOwnerAuthentication(builder.Configuration);

// El puerto ICurrentUser se resuelve desde la cookie de la peticion en curso.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

builder.Services.AddCors(options => options.AddPolicy(WebCorsPolicy, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddHealthChecks().AddDbContextCheck<AlbumViajesDbContext>();

// La API solo la alcanza el proxy que tiene delante: no publica puertos fuera de
// la red interna de Docker. Por eso se aceptan sus cabeceras sin restringir la
// lista de proxies conocidos, que por defecto solo admite loopback y dejaria
// fuera al contenedor web.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// El TLS lo termina el proxy, asi que la peticion llega por http. Sin traducir
// sus cabeceras, la API construiria el redirect_uri de Google como http y Google
// lo rechazaria. Va antes que la autenticacion, que ya depende del esquema.
app.UseForwardedHeaders();

// En desarrollo interesa ver la excepcion completa; en produccion, solo un
// ProblemDetails sin detalles internos.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
}
app.UseStatusCodePages();
app.UseCors(WebCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapCityEndpoints();
app.MapPhotoEndpoints();
app.MapMusicEndpoints();

await ApplyMigrationsAsync(app);

await app.RunAsync();

// Las migraciones se aplican al arrancar para que el contenedor quede listo sin
// un paso manual. Con un solo replica de la API esto es seguro.
static async Task ApplyMigrationsAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AlbumViajesDbContext>();
    await dbContext.Database.MigrateAsync();
}

/// <summary>Expuesto para que los tests de integracion puedan levantar la API con WebApplicationFactory.</summary>
public partial class Program;
