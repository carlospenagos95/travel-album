using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Errors;

namespace AlbumViajes.Domain.Entities;

/// <summary>
/// Foto de una ciudad, ya copiada al servidor. No se guarda el enlace de Google
/// Photos a proposito: el <c>baseUrl</c> que devuelve la Picker API caduca a la
/// hora, asi que el album dejaria de verse solo.
/// Vive dentro del agregado <see cref="City"/>: solo la ciudad la crea o la mueve.
/// </summary>
public sealed class Photo : Entity
{
    public const int MaxCaptionLength = 500;
    public const int MaxFileNameLength = 260;
    public const int MaxPathLength = 400;

    /// <summary>Solo lo usa EF Core al materializar filas. El dominio nunca lo llama.</summary>
    private Photo()
        : base(Guid.Empty)
    {
        SourceMediaId = null!;
        FileName = null!;
        StoredPath = null!;
        ThumbnailPath = null!;
    }

    internal Photo(
        Guid id,
        Guid cityId,
        string sourceMediaId,
        string fileName,
        string storedPath,
        string thumbnailPath,
        int width,
        int height,
        DateTimeOffset? takenAt,
        int sortOrder,
        DateTimeOffset createdAt)
        : base(id)
    {
        CityId = cityId;
        SourceMediaId = sourceMediaId;
        FileName = fileName;
        StoredPath = storedPath;
        ThumbnailPath = thumbnailPath;
        Width = width;
        Height = height;
        TakenAt = takenAt;
        SortOrder = sortOrder;
        CreatedAt = createdAt;
    }

    public Guid CityId { get; private set; }

    /// <summary>Id del elemento en Google Photos. Evita importar dos veces la misma foto.</summary>
    public string SourceMediaId { get; private set; }

    /// <summary>Nombre original, solo para mostrarlo. El archivo en disco se nombra por GUID.</summary>
    public string FileName { get; private set; }

    public string StoredPath { get; private set; }

    public string ThumbnailPath { get; private set; }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public DateTimeOffset? TakenAt { get; private set; }

    public string? Caption { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    internal void MoveTo(int sortOrder) => SortOrder = sortOrder;

    internal Result Describe(string? caption)
    {
        if (caption?.Length > MaxCaptionLength)
        {
            return Error.Validation("photo.caption", $"El pie de foto no puede superar {MaxCaptionLength} caracteres.");
        }

        Caption = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim();
        return Result.Success();
    }
}
