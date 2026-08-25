using AlbumViajes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlbumViajes.Infrastructure.Persistence.Configurations;

internal sealed class GoogleAccountConfiguration : IEntityTypeConfiguration<GoogleAccount>
{
    public void Configure(EntityTypeBuilder<GoogleAccount> builder)
    {
        builder.ToTable("google_accounts");

        builder.HasKey(account => account.Id);

        // El identificador lo genera el dominio, no la base de datos. Decirselo a
        // EF Core no es cosmetico: si lo cree generado, al descubrir una entidad
        // nueva dentro del agregado la toma por existente y emite un UPDATE en
        // lugar de un INSERT.
        builder.Property(account => account.Id).ValueGeneratedNever();

        builder.Property(account => account.Email)
            .HasMaxLength(GoogleAccount.MaxEmailLength)
            .IsRequired();

        // El token va cifrado con Data Protection, asi que ocupa bastante mas que
        // el original: no se le pone tope de longitud.
        builder.Property(account => account.ProtectedRefreshToken).IsRequired();

        builder.HasIndex(account => account.Email)
            .IsUnique()
            .HasDatabaseName("ix_google_accounts_email");
    }
}
