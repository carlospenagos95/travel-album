using AlbumViajes.Application.Abstractions;
using AlbumViajes.Domain.Common;

namespace AlbumViajes.Application.Cities.UseCases;

public sealed class DeleteCityHandler(ICityRepository cities, IUnitOfWork unitOfWork)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var city = await cities.FindAsync(id, cancellationToken);
        if (city is null)
        {
            return CityErrors.NotFound(id);
        }

        cities.Remove(city);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
