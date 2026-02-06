using Dashboard.Net.AI.Models;
using Newtonsoft.Json.Linq;
using System.Net;

namespace Dashboard.Net.AI.Services
{
    public class WeatherService(HttpClient http, IConfiguration config, ILogger<WeatherService> logger) : IWeatherService
    {
        private readonly HttpClient _http = http;
        private readonly string _apiKey = config["OpenWeatherMap:ApiKey"] ?? string.Empty;
        private readonly string _mapBoxApiKey = config["Mapbox:ApiKey"] ?? string.Empty;
        //private readonly string _baseGeoUrl = config["OpenWeatherMap:GeoBaseUrl"] ?? "http://api.openweathermap.org/geo/1.0";
        private readonly string _baseMapBoxUrl = config["Mapbox:BaseUrl"] ?? "https://api.mapbox.com/search/geocode/v6";
        private readonly string _baseWeatherUrl = "http://api.openweathermap.org/data/2.5";
        private readonly string _baseXanoUrl = config["Xano:BaseUrl"] ?? string.Empty;
        private readonly ILogger<WeatherService> _logger = logger;

        public async Task<MapboxV6Response> GetAutocompleteResultsAsync(string query)
        {
            string encodedQuery = Uri.EscapeDataString(query);
            string url = $"{_baseMapBoxUrl}/forward?q={encodedQuery}";
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                url += $"&access_token={WebUtility.UrlEncode(_mapBoxApiKey)}";
            }

            using HttpClient client = new();
            try
            {
                using Stream stream = await client.GetStreamAsync(url);
                // 2. Deserialize directly from the stream
                // This is the modern, high-performance way to do it
                MapboxV6Response? result = await System.Text.Json.JsonSerializer.DeserializeAsync<MapboxV6Response>(stream);
                if (result != null)
                {
                    _ = await IncrementMapboxUsage();
                }
                return result;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }

        // Calls reverse geocoding
        public async Task<MapboxV6Response?> ReverseGeocodeAsync(double latitude, double longitude, int limit = 1)
        {
            try
            {
                string url = $"{_baseMapBoxUrl}/reverse?longitude={longitude}&latitude{latitude}";
                if (!string.IsNullOrWhiteSpace(_apiKey))
                {
                    url += $"?access_token={WebUtility.UrlEncode(_mapBoxApiKey)}";
                }

                using HttpClient client = new();
                try
                {
                    using Stream stream = await client.GetStreamAsync(url);
                    // 2. Deserialize directly from the stream
                    // This is the modern, high-performance way to do it
                    MapboxV6Response? result = await System.Text.Json.JsonSerializer.DeserializeAsync<MapboxV6Response>(stream);
                    if (result != null)
                    {
                        _ = await IncrementMapboxUsage();
                    }
                    return result;
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    return null;
                }
            }
            catch (Exception ex)
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
                string baseCurrent = _baseWeatherUrl;
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

                string url = $"{baseCurrent}/weather?lat={latitude}&lon={longitude}&units=imperial";
                if (!string.IsNullOrWhiteSpace(_apiKey))
                {
                    url += $"&appid={WebUtility.UrlEncode(_apiKey)}";
                }

                string json = await _http.GetStringAsync(url);

                JObject j = JObject.Parse(json);

                CurrentWeather current = new()
                {
                    Dt = j["dt"]?.Value<long>() ?? 0,
                    Sunrise = j["sys"]?["sunrise"]?.Value<long>() ?? 0,
                    Sunset = j["sys"]?["sunset"]?.Value<long>() ?? 0,

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
                        ? j["weather"].ToObject<List<WeatherDescription>>()!
                        : []
                };

                return current;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current weather lat={Latitude} lon={Longitude}", latitude, longitude);
                return null;
            }
        }

        private async Task<bool> IncrementMapboxUsage(int incrementBy = 1)
        {
            string url = $"{_baseXanoUrl}/Increment_Mapbox_Usage";

            try
            {
                var payload = new
                {
                    CurrentYearMonth = DateTime.UtcNow.ToString("yyyy-MM"),
                    IncrementBy = 1
                };
                HttpResponseMessage result = await _http.PostAsJsonAsync(url, payload);
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing Mapbox usage by {IncrementBy}", incrementBy);
                return false;
            }
        }
    }
}
