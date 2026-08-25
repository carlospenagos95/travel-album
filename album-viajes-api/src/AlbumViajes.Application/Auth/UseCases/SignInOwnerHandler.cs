using AlbumViajes.Application.Abstractions;
using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Entities;

namespace AlbumViajes.Application.Auth.UseCases;

/// <summary>
/// Cierra el ciclo de entrada con Google: comprueba que el correo esta en la
/// lista de permitidos y guarda el consentimiento para poder leer despues las
/// fotos que el usuario elija.
///
/// El refresh token solo llega la primera vez que se concede el permiso; en los
/// inicios de sesion siguientes viene vacio y se conserva el que ya estaba.
/// </summary>
public sealed class SignInOwnerHandler(
    IOwnerAllowlist allowlist,
    IGoogleAccountRepository accounts,
    ISecretProtector protector,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result> HandleAsync(string email, string? refreshToken, CancellationToken cancellationToken)
    {
        if (!allowlist.Allows(email))
        {
            return AuthErrors.NotAllowed(email);
        }

        var normalized = email.Trim().ToLowerInvariant();
        var existing = await accounts.FindByEmailAsync(normalized, cancellationToken);

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            // Google no reemite el refresh token en cada entrada. Sin uno previo
            // guardado, el album funciona salvo la importacion de fotos.
            return existing is null ? AuthErrors.ConsentIncomplete() : Result.Success();
        }

        var protectedToken = protector.Protect(refreshToken);
        var now = clock.UtcNow;

        if (existing is not null)
        {
            var renewed = existing.RenewConsent(protectedToken, now);
            if (!renewed.IsSuccess)
            {
                return renewed;
            }
        }
        else
        {
            var created = GoogleAccount.Create(normalized, protectedToken, now);
            if (!created.IsSuccess)
            {
                return created.Error!;
            }

            await accounts.AddAsync(created.Value, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
