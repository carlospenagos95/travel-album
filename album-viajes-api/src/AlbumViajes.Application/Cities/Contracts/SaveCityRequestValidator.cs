using FluentValidation;

namespace AlbumViajes.Application.Cities.Contracts;

/// <summary>
/// Valida la forma del request antes de tocar el dominio: campos obligatorios y
/// longitudes. Las invariantes de negocio siguen viviendo en las entidades y los
/// value objects, no aqui.
/// </summary>
public sealed class SaveCityRequestValidator : AbstractValidator<SaveCityRequest>
{
    public SaveCityRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("El nombre de la ciudad es obligatorio.")
            .MaximumLength(120);

        RuleFor(request => request.Country)
            .NotEmpty().WithMessage("El pais es obligatorio.")
            .MaximumLength(120);

        RuleFor(request => request.CountryCode)
            .NotEmpty().WithMessage("El codigo de pais es obligatorio.")
            .Length(2).WithMessage("El codigo de pais debe tener 2 letras, por ejemplo CO.");

        RuleFor(request => request.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("La latitud debe estar entre -90 y 90.");

        RuleFor(request => request.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("La longitud debe estar entre -180 y 180.");

        RuleFor(request => request.Notes).MaximumLength(4000);
        RuleFor(request => request.ShortDescription).MaximumLength(2000);
        RuleFor(request => request.TypicalFood).MaximumLength(4000);
    }
}
