using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Dashboard.Net.AI.Models;

namespace Dashboard.Net.AI.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _baseGeoUrl;
        private readonly string _baseWeatherUrl = "http://api.openweathermap.org/data/2.5";
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(HttpClient http, IConfiguration config, ILogger<WeatherService> logger)
        {
            _http = http;
            _apiKey = config["OpenWeatherMap:ApiKey"] ?? string.Empty;
            _baseGeoUrl = config["OpenWeatherMap:GeoBaseUrl"] ?? "http://api.openweathermap.org/geo/1.0";
            _logger = logger;
        }

        // Calls reverse geocoding
        public async Task<string?> ReverseGeocodeAsync(double latitude, double longitude, int limit = 1)
        {
            try
            {
                var url = $"{_baseGeoUrl}/reverse?lat={latitude}&lon={longitude}&limit={limit}";
                if (!string.IsNullOrWhiteSpace(_apiKey))
                    url += $"&appid={System.Net.WebUtility.UrlEncode(_apiKey)}";

                var json = await _http.GetStringAsync(url);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind == System.Text.Json.JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    var first = root[0];
                    if (first.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        return nameProp.GetString();
                    }
                }

                return null;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error reverse geocoding lat={Latitude} lon={Longitude}", latitude, longitude);
                return null;
            }
        }

        // Calls OpenWeatherMap current weather: /data/2.5/weather?lat={lat}&lon={lon}&appid={API key}
        public async Task<CurrentWeather?> GetCurrentWeatherAsync(double latitude, double longitude)
        {
            try
            {
                var baseCurrent = _baseWeatherUrl;
                // If GeoBaseUrl was used from configuration, replace the path to point to the data API
                if (baseCurrent.Contains("/geo/1.0"))
                {
                    baseCurrent = baseCurrent.Replace("/geo/1.0", "/data/2.5");
                }
                else if (!baseCurrent.Contains("/data/2.5"))
                {
                    // default to openweathermap data API base
                    baseCurrent = "http://api.openweathermap.org/data/2.5";
                }

                var url = $"{baseCurrent}/weather?lat={latitude}&lon={longitude}&units=metric";
                if (!string.IsNullOrWhiteSpace(_apiKey))
                    url += $"&appid={System.Net.WebUtility.UrlEncode(_apiKey)}";

                var json = await _http.GetStringAsync(url);

                var j = JObject.Parse(json);

                var current = new CurrentWeather
                {
                    Dt = j["dt"]?.Value<long>() ?? 0,
                    Sunrise = j["sys"]?[("sunrise")]?.Value<long>() ?? 0,
                    Sunset = j["sys"]?[("sunset")]?.Value<long>() ?? 0,

                    Temp = j["main"]?["temp"]?.Value<double>() ?? 0.0,
                    FeelsLike = j["main"]?["feels_like"]?.Value<double>() ?? 0.0,

                    Pressure = j["main"]?["pressure"]?.Value<int>() ?? 0,
                    Humidity = j["main"]?["humidity"]?.Value<int>() ?? 0,

                    DewPoint = j["dew_point"]?.Value<double>() ?? 0.0,
                    Uvi = j["uvi"]?.Value<double>() ?? 0.0,

                    Clouds = j["clouds"]?["all"]?.Value<int>() ?? 0,
                    Visibility = j["visibility"]?.Value<int>() ?? 0,

                    WindSpeed = j["wind"]?["speed"]?.Value<double>() ?? 0.0,
                    WindDeg = j["wind"]?["deg"]?.Value<int>() ?? 0,
                    WindGust = j["wind"]?["gust"]?.Value<double?>(),

                    Weather = j["weather"] != null && j["weather"].Type == JTokenType.Array
                        ? j["weather"].ToObject<System.Collections.Generic.List<WeatherDescription>>()!
                        : new System.Collections.Generic.List<WeatherDescription>()
                };

                return current;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error getting current weather lat={Latitude} lon={Longitude}", latitude, longitude);
                return null;
            }
        }
    }
}
