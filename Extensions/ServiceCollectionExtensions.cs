using Dashboard.Net.AI.Services;

namespace Dashboard.Net.AI.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            _ = services.AddTransient<IAuth0Service, Auth0Service>();
            _ = services.AddHttpClient<IStockService, StockService>();
            _ = services.AddHttpClient<IWeatherService, WeatherService>();
            return services;
        }
    }
}
