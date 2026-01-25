using System.Threading.Tasks;
using System.Text.Json;
using Dashboard.Net.AI.Models;

namespace Dashboard.Net.AI.Services
{
    public interface IWeatherService
    {
        // Reverse geocode latitude/longitude to a city name. Returns null if not found.
        Task<MapboxV6Response?> ReverseGeocodeAsync(double latitude, double longitude, int limit = 1);

        // Get current weather from OpenWeatherMap for the given coordinates.
        Task<CurrentWeather?> GetCurrentWeatherAsync(double latitude, double longitude);

        Task<MapboxV6Response> GetAutocompleteResultsAsync(string query);

	}
}
