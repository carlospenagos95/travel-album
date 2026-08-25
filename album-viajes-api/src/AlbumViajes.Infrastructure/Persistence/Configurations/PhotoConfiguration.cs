using AlbumViajes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlbumViajes.Infrastructure.Persistence.Configurations;

internal sealed class PhotoConfiguration : IEntityTypeConfiguration<Photo>
{
    public void Configure(EntityTypeBuilder<Photo> builder)
    {
        builder.ToTable("photos");

        builder.HasKey(photo => photo.Id);

        // El identificador lo genera el dominio, no la base de datos. Decirselo a
        // EF Core no es cosmetico: si lo cree generado, al descubrir una entidad
        // nueva dentro del agregado la toma por existente y emite un UPDATE en
        // lugar de un INSERT.
        builder.Property(photo => photo.Id).ValueGeneratedNever();

        builder.Property(photo => photo.SourceMediaId).HasMaxLength(512).IsRequired();
        builder.Property(photo => photo.FileName).HasMaxLength(Photo.MaxFileNameLength).IsRequired();
        builder.Property(photo => photo.StoredPath).HasMaxLength(Photo.MaxPathLength).IsRequired();
        builder.Property(photo => photo.ThumbnailPath).HasMaxLength(Photo.MaxPathLength).IsRequired();
        builder.Property(photo => photo.Caption).HasMaxLength(Photo.MaxCaptionLength);

        // Importar dos veces la misma foto en la misma ciudad no debe ser posible
        // ni siquiera si dos peticiones corren a la vez.
        builder.HasIndex(photo => new { photo.CityId, photo.SourceMediaId })
            .IsUnique()
            .HasDatabaseName("ix_photos_city_source_media");

        builder.HasIndex(photo => new { photo.CityId, photo.SortOrder })
            .HasDatabaseName("ix_photos_city_sort_order");
    }
}
