using AlbumViajes.Application.Auth.UseCases;
using AlbumViajes.Application.Cities.UseCases;
using AlbumViajes.Application.Music.UseCases;
using AlbumViajes.Application.Photos.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace AlbumViajes.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ListCitiesHandler>();
        services.AddScoped<GetCityHandler>();
        services.AddScoped<CreateCityHandler>();
        services.AddScoped<UpdateCityHandler>();
        services.AddScoped<DeleteCityHandler>();
        services.AddScoped<EnrichCityHandler>();

        services.AddScoped<StartPhotoPickerSessionHandler>();
        services.AddScoped<GetPhotoPickerSessionHandler>();
        services.AddScoped<ImportPickedPhotosHandler>();
        services.AddScoped<ReorderPhotosHandler>();
        services.AddScoped<CaptionPhotoHandler>();
        services.AddScoped<DeletePhotoHandler>();
        services.AddScoped<GetPhotoFileHandler>();

        services.AddScoped<SearchRadioStationsHandler>();
        services.AddScoped<AddRadioStationHandler>();
        services.AddScoped<SearchTracksHandler>();
        services.AddScoped<AddTrackHandler>();
        services.AddScoped<SetDefaultMusicSourceHandler>();
        services.AddScoped<RemoveMusicSourceHandler>();
        services.AddScoped<StreamMusicSourceHandler>();

        services.AddScoped<SignInOwnerHandler>();

        return services;
    }
}
