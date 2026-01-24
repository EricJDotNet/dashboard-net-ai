using Microsoft.Extensions.DependencyInjection;
using Dashboard.Net.AI;
using Dashboard.Net.AI.Services;

namespace Dashboard.Net.AI.Extensions
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddApplicationServices(this IServiceCollection services) 
		{
			services.AddTransient<IAuth0Service, Auth0Service>();
			services.AddHttpClient<IStockService, StockService>();
			services.AddHttpClient<IWeatherService, WeatherService>();
			return services;
		}
	}
}
