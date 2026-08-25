using AlbumViajes.Domain.Common;
using AlbumViajes.Domain.Errors;
using AlbumViajes.Domain.ValueObjects;

namespace AlbumViajes.Domain.Entities;

/// <summary>
/// Ciudad visitada. Es la raiz del agregado: fotos, sitios de interes y fuentes
/// de musica solo se modifican a traves de ella.
/// El estado nunca se asigna desde fuera; cambia por metodos con nombre de intencion.
/// </summary>
public sealed class City : Entity
{
    public const int MaxNameLength = 120;
    public const int MaxShortDescriptionLength = 2000;

    /// <summary>
    /// Tope de fotos por ciudad. No es una limitacion tecnica sino del formato:
    /// una ficha con cientos de fotos deja de ser un album y se vuelve un archivo.
    /// </summary>
    public const int MaxPhotos = 200;

    private readonly List<Photo> _photos = [];
    private readonly List<MusicSource> _musicSources = [];

    /// <summary>
    /// Solo lo usa EF Core al materializar filas: no puede enlazar un complex property
    /// como Coordinates a un parametro de constructor. El dominio nunca lo llama.
    /// </summary>
    private City()
        : base(Guid.Empty)
    {
        Name = null!;
        Country = null!;
    }

    private City(Guid id, string name, string country, CountryCode countryCode, Coordinates coordinates, DateTimeOffset createdAt)
        : base(id)
    {
        Name = name;
        Country = country;
        CountryCode = countryCode;
        Coordinates = coordinates;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public string Name { get; private set; }

    public string Country { get; private set; }

    public CountryCode CountryCode { get; private set; }

    public Coordinates Coordinates { get; private set; }

    public DateOnly? VisitedOn { get; private set; }

    public string? Notes { get; private set; }

    public long? Population { get; private set; }

    public string? PopulationSource { get; private set; }

    public string? ShortDescription { get; private set; }

    /// <summary>Comida tipica. Wikidata no la publica como dato estructurado, asi que es siempre manual.</summary>
    public string? TypicalFood { get; private set; }

    public string? WikidataId { get; private set; }

    public string? WikipediaUrl { get; private set; }

    /// <summary>Null mientras la ciudad nunca se haya enriquecido desde fuentes externas.</summary>
    public DateTimeOffset? EnrichedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Fotos en el orden en que se muestran en la galeria.</summary>
    public IReadOnlyList<Photo> Photos => _photos;

    public IReadOnlyList<MusicSource> MusicSources => _musicSources;

    /// <summary>La primera foto de la galeria, que hace de portada en el mapa y en la lista.</summary>
    public Photo? CoverPhoto => _photos.OrderBy(photo => photo.SortOrder).FirstOrDefault();

    /// <summary>La fuente que suena al abrir la ciudad, si el usuario marco alguna.</summary>
    public MusicSource? DefaultMusicSource => _musicSources.Find(source => source.IsDefault);

    public static Result<City> Create(
        string name,
        string country,
        CountryCode countryCode,
        Coordinates coordinates,
        DateTimeOffset now)
    {
        var nameResult = NormalizeRequiredText(name, MaxNameLength, "city.name", "nombre de la ciudad");
        if (!nameResult.IsSuccess)
        {
            return nameResult.Error!;
        }

        var countryResult = NormalizeRequiredText(country, MaxNameLength, "city.country", "pais");
        if (!countryResult.IsSuccess)
        {
            return countryResult.Error!;
        }

        return new City(Guid.NewGuid(), nameResult.Value, countryResult.Value, countryCode, coordinates, now);
    }

    public Result Rename(string name, string country, CountryCode countryCode, DateTimeOffset now)
    {
        var nameResult = NormalizeRequiredText(name, MaxNameLength, "city.name", "nombre de la ciudad");
        if (!nameResult.IsSuccess)
        {
            return nameResult.Error!;
        }

        var countryResult = NormalizeRequiredText(country, MaxNameLength, "city.country", "pais");
        if (!countryResult.IsSuccess)
        {
            return countryResult.Error!;
        }

        Name = nameResult.Value;
        Country = countryResult.Value;
        CountryCode = countryCode;
        Touch(now);
        return Result.Success();
    }

    public void Relocate(Coordinates coordinates, DateTimeOffset now)
    {
        Coordinates = coordinates;
        Touch(now);
    }

    public Result RecordVisit(DateOnly? visitedOn, string? notes, DateTimeOffset now)
    {
        if (visitedOn is not null && visitedOn.Value > DateOnly.FromDateTime(now.UtcDateTime))
        {
            return Error.Validation("city.visitedOn", "La fecha de visita no puede estar en el futuro.");
        }

        VisitedOn = visitedOn;
        Notes = Trim(notes);
        Touch(now);
        return Result.Success();
    }

    public Result DescribeManually(string? shortDescription, string? typicalFood, DateTimeOffset now)
    {
        if (shortDescription?.Length > MaxShortDescriptionLength)
        {
            return Error.Validation(
                "city.shortDescription",
                $"La descripcion no puede superar {MaxShortDescriptionLength} caracteres.");
        }

        ShortDescription = Trim(shortDescription);
        TypicalFood = Trim(typicalFood);
        Touch(now);
        return Result.Success();
    }

    /// <summary>
    /// Aplica los datos traidos de Wikidata y Wikipedia. La comida tipica no entra
    /// aqui a proposito: esas fuentes no la publican.
    /// </summary>
    public void ApplyEnrichment(
        long? population,
        string? populationSource,
        string? shortDescription,
        string? wikidataId,
        string? wikipediaUrl,
        DateTimeOffset now)
    {
        Population = population;
        PopulationSource = Trim(populationSource);
        ShortDescription = Trim(shortDescription) ?? ShortDescription;
        WikidataId = Trim(wikidataId);
        WikipediaUrl = Trim(wikipediaUrl);
        EnrichedAt = now;
        Touch(now);
    }

    /// <summary>
    /// Permite que la importacion sea idempotente: si el usuario vuelve a elegir
    /// una foto que ya esta en el album, se salta en vez de duplicarla.
    /// </summary>
    public bool AlreadyHasPhotoFrom(string sourceMediaId) =>
        _photos.Exists(photo => photo.SourceMediaId == sourceMediaId);

    public Result<Photo> AddPhoto(
        string sourceMediaId,
        string fileName,
        string storedPath,
        string thumbnailPath,
        int width,
        int height,
        DateTimeOffset? takenAt,
        DateTimeOffset now)
    {
        if (_photos.Count >= MaxPhotos)
        {
            return Error.Validation("city.photoLimit", $"Una ciudad no puede tener mas de {MaxPhotos} fotos.");
        }

        if (AlreadyHasPhotoFrom(sourceMediaId))
        {
            return Error.Conflict("city.photoDuplicate", "Esa foto ya esta en el album de la ciudad.");
        }

        var nextPosition = _photos.Count == 0 ? 0 : _photos.Max(photo => photo.SortOrder) + 1;

        var photo = new Photo(
            Guid.NewGuid(),
            Id,
            sourceMediaId,
            Truncate(fileName, Photo.MaxFileNameLength),
            storedPath,
            thumbnailPath,
            width,
            height,
            takenAt,
            nextPosition,
            now);

        _photos.Add(photo);
        Touch(now);

        return photo;
    }

    /// <summary>
    /// Devuelve la foto quitada para que quien llama borre tambien sus archivos:
    /// la entidad no sabe nada del disco.
    /// </summary>
    public Result<Photo> RemovePhoto(Guid photoId, DateTimeOffset now)
    {
        var photo = _photos.Find(candidate => candidate.Id == photoId);

        if (photo is null)
        {
            return Error.NotFound("photo.notFound", "Esa foto no pertenece a la ciudad.");
        }

        _photos.Remove(photo);
        Renumber();
        Touch(now);

        return photo;
    }

    public Result ReorderPhotos(IReadOnlyList<Guid> orderedPhotoIds, DateTimeOffset now)
    {
        var byId = _photos.ToDictionary(photo => photo.Id);

        if (orderedPhotoIds.Count != byId.Count
            || orderedPhotoIds.Distinct().Count() != orderedPhotoIds.Count
            || !orderedPhotoIds.All(byId.ContainsKey))
        {
            return Error.Validation(
                "photo.order",
                "El nuevo orden debe listar exactamente una vez cada foto de la ciudad.");
        }

        for (var position = 0; position < orderedPhotoIds.Count; position++)
        {
            byId[orderedPhotoIds[position]].MoveTo(position);
        }

        Touch(now);
        return Result.Success();
    }

    public Result CaptionPhoto(Guid photoId, string? caption, DateTimeOffset now)
    {
        var photo = _photos.Find(candidate => candidate.Id == photoId);

        if (photo is null)
        {
            return Error.NotFound("photo.notFound", "Esa foto no pertenece a la ciudad.");
        }

        var described = photo.Describe(caption);
        if (!described.IsSuccess)
        {
            return described;
        }

        Touch(now);
        return Result.Success();
    }

    public Result<MusicSource> AddRadioStation(string stationUuid, string stationName, string streamUrl, DateTimeOffset now)
    {
        var source = MusicSource.ForRadio(Id, stationUuid, stationName, streamUrl, now);

        return source.IsSuccess ? Attach(source.Value, now) : source.Error!;
    }

    public Result<MusicSource> AddYouTubeVideo(string urlOrId, string? title, DateTimeOffset now)
    {
        var source = MusicSource.ForYouTube(Id, urlOrId, title, now);

        return source.IsSuccess ? Attach(source.Value, now) : source.Error!;
    }

    public Result RemoveMusicSource(Guid musicSourceId, DateTimeOffset now)
    {
        var source = _musicSources.Find(candidate => candidate.Id == musicSourceId);

        if (source is null)
        {
            return Error.NotFound("music.notFound", "Esa fuente de musica no pertenece a la ciudad.");
        }

        _musicSources.Remove(source);

        // La ciudad nunca se queda sin fuente por defecto teniendo fuentes: la
        // primera que quede toma el relevo.
        if (source.IsDefault && _musicSources.Count > 0)
        {
            _musicSources[0].MarkAsDefault(true);
        }

        Touch(now);
        return Result.Success();
    }

    public Result SetDefaultMusicSource(Guid musicSourceId, DateTimeOffset now)
    {
        if (!_musicSources.Exists(source => source.Id == musicSourceId))
        {
            return Error.NotFound("music.notFound", "Esa fuente de musica no pertenece a la ciudad.");
        }

        foreach (var source in _musicSources)
        {
            source.MarkAsDefault(source.Id == musicSourceId);
        }

        Touch(now);
        return Result.Success();
    }

    private Result<MusicSource> Attach(MusicSource source, DateTimeOffset now)
    {
        // La primera fuente que se agrega es la que suena: obliga a que una ciudad
        // con musica tenga siempre una por defecto.
        source.MarkAsDefault(_musicSources.Count == 0);
        _musicSources.Add(source);
        Touch(now);

        return source;
    }

    /// <summary>Cierra los huecos que deja un borrado para que el orden siga siendo 0..n-1.</summary>
    private void Renumber()
    {
        var position = 0;

        foreach (var photo in _photos.OrderBy(photo => photo.SortOrder))
        {
            photo.MoveTo(position++);
        }
    }

    private void Touch(DateTimeOffset now) => UpdatedAt = now;

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Truncate(string value, int maxLength)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static Result<string> NormalizeRequiredText(string value, int maxLength, string code, string label)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return Error.Validation(code, $"El {label} es obligatorio.");
        }

        if (trimmed.Length > maxLength)
        {
            return Error.Validation(code, $"El {label} no puede superar {maxLength} caracteres.");
        }

        return trimmed;
    }
}
