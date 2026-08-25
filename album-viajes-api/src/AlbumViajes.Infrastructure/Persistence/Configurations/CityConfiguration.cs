using AlbumViajes.Domain.Entities;
using AlbumViajes.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlbumViajes.Infrastructure.Persistence.Configurations;

/// <summary>
/// El mapeo relacional vive aqui, no en la entidad: el dominio no sabe que existe
/// una base de datos.
/// </summary>
internal sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("cities");

        builder.HasKey(city => city.Id);

        // El identificador lo genera el dominio, no la base de datos. Decirselo a
        // EF Core no es cosmetico: si lo cree generado, al descubrir una entidad
        // nueva dentro del agregado la toma por existente y emite un UPDATE en
        // lugar de un INSERT.
        builder.Property(city => city.Id).ValueGeneratedNever();

        builder.Property(city => city.Name)
            .HasMaxLength(City.MaxNameLength)
            .IsRequired();

        builder.Property(city => city.Country)
            .HasMaxLength(City.MaxNameLength)
            .IsRequired();

        builder.Property(city => city.CountryCode)
            .HasConversion(
                code => code.Value,
                value => CountryCode.Create(value).Value)
            .HasMaxLength(2)
            .IsRequired();

        builder.ComplexProperty(city => city.Coordinates, coordinates =>
        {
            coordinates.Property(point => point.Latitude).HasColumnName("latitude").IsRequired();
            coordinates.Property(point => point.Longitude).HasColumnName("longitude").IsRequired();
        });

        builder.Property(city => city.Notes).HasMaxLength(4000);
        builder.Property(city => city.ShortDescription).HasMaxLength(City.MaxShortDescriptionLength);
        builder.Property(city => city.TypicalFood).HasMaxLength(4000);
        builder.Property(city => city.PopulationSource).HasMaxLength(200);
        builder.Property(city => city.WikidataId).HasMaxLength(32);
        builder.Property(city => city.WikipediaUrl).HasMaxLength(500);

        // Fotos y musica pertenecen al agregado: se cargan y se borran con la
        // ciudad, y solo se llega a ellas a traves de ella. EF Core escribe en la
        // lista privada, no en la propiedad de solo lectura que ve el dominio.
        builder.HasMany(city => city.Photos)
            .WithOne()
            .HasForeignKey(photo => photo.CityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(city => city.Photos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(city => city.MusicSources)
            .WithOne()
            .HasForeignKey(source => source.CityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(city => city.MusicSources).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Red de seguridad contra duplicados a nivel de base de datos. La comprobacion
        // que produce el mensaje de error legible vive en CityRepository y es
        // insensible a mayusculas; este indice atrapa las carreras entre peticiones.
        builder.HasIndex(city => new { city.Name, city.CountryCode })
            .IsUnique()
            .HasDatabaseName("ix_cities_name_country_code");
    }
}
