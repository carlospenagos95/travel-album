namespace AlbumViajes.Domain.Common;

/// <summary>Raiz de las entidades: identidad por Id, no por valor.</summary>
public abstract class Entity
{
    protected Entity(Guid id) => Id = id;

    public Guid Id { get; private set; }

    public override bool Equals(object? obj) =>
        obj is Entity other && other.GetType() == GetType() && other.Id == Id;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
