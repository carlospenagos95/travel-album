using AlbumViajes.Domain.Entities;
using AlbumViajes.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlbumViajes.Infrastructure.Persistence.Configurations;

internal sealed class MusicSourceConfiguration : IEntityTypeConfiguration<MusicSource>
{
    public void Configure(EntityTypeBuilder<MusicSource> builder)
    {
        builder.ToTable("music_sources");

        builder.HasKey(source => source.Id);

        // El identificador lo genera el dominio, no la base de datos. Decirselo a
        // EF Core no es cosmetico: si lo cree generado, al descubrir una entidad
        // nueva dentro del agregado la toma por existente y emite un UPDATE en
        // lugar de un INSERT.
        builder.Property(source => source.Id).ValueGeneratedNever();

        // El tipo se guarda como texto: una fila legible vale mas que el byte
        // que ahorraria un entero, y sobrevive a que se reordene el enum.
        builder.Property(source => source.Kind)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(source => source.Label).HasMaxLength(MusicSource.MaxLabelLength).IsRequired();
        builder.Property(source => source.RadioStationUuid).HasMaxLength(64);
        builder.Property(source => source.RadioStreamUrl).HasMaxLength(StreamUrl.MaxLength);
        builder.Property(source => source.YouTubeVideoId).HasMaxLength(32);

        // "Solo una fuente por ciudad suena al abrirla" lo garantiza el agregado,
        // que se carga y se guarda entero, y no un indice unico parcial: cambiar
        // de fuente actualiza dos filas en el mismo lote y PostgreSQL no permite
        // aplazar la comprobacion de un indice parcial, asi que el guardado
        // fallaba o no segun el orden que eligiera EF Core.
        builder.HasIndex(source => source.CityId)
            .HasDatabaseName("ix_music_sources_city");
    }
}
