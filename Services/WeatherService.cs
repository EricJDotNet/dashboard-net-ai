using Auth0.ManagementApi.Models;
using Dashboard.Net.AI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dashboard.Net.AI.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
		private readonly string _mapBoxApiKey;
		private readonly string _baseGeoUrl;
		private readonly string _baseMapBoxUrl;
        private readonly string _baseWeatherUrl = "http://api.openweathermap.org/data/2.5";
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(HttpClient http, IConfiguration config, ILogger<WeatherService> logger)
        {
            _http = http;
            _apiKey = config["OpenWeatherMap:ApiKey"] ?? string.Empty;
			_mapBoxApiKey = config["Mapbox:ApiKey"] ?? string.Empty;
			_baseGeoUrl = config["OpenWeatherMap:GeoBaseUrl"] ?? "http://api.openweathermap.org/geo/1.0";
			_baseMapBoxUrl = config["Mapbox:BaseUrl"] ?? "https://api.mapbox.com/search/geocode/v6";
			_logger = logger;
        }

		public async Task<MapboxV6Response> GetAutocompleteResultsAsync(string query)
		{
			var encodedQuery = Uri.EscapeDataString(query);
			var url = $"{_baseMapBoxUrl}/forward?q={encodedQuery}";
			if (!string.IsNullOrWhiteSpace(_apiKey))
				url += $"&access_token={WebUtility.UrlEncode(_mapBoxApiKey)}";

			using (HttpClient client = new HttpClient())
			{
				try
				{
					using (Stream stream = await client.GetStreamAsync(url))
					{
						// 2. Deserialize directly from the stream
						// This is the modern, high-performance way to do it
						var result = await System.Text.Json.JsonSerializer.DeserializeAsync<MapboxV6Response>(stream);
                        return result;
					}
				}
				catch (HttpRequestException ex)
				{
					Console.WriteLine($"Error: {ex.Message}");
					return null;
				}
			}
		}

		// Calls reverse geocoding
		public async Task<MapboxV6Response?> ReverseGeocodeAsync(double latitude, double longitude, int limit = 1)
        {
            try
            {
                var url = $"{_baseMapBoxUrl}/reverse?longitude={longitude}&latitude{latitude}";
                if (!string.IsNullOrWhiteSpace(_apiKey))
                    url += $"?access_token={WebUtility.UrlEncode(_mapBoxApiKey)}";

				using (HttpClient client = new HttpClient())
				{
					try
					{
						using (Stream stream = await client.GetStreamAsync(url))
						{
							// 2. Deserialize directly from the stream
							// This is the modern, high-performance way to do it
							return await System.Text.Json.JsonSerializer.DeserializeAsync<MapboxV6Response>(stream);
						}
					}
					catch (HttpRequestException ex)
					{
						Console.WriteLine($"Error: {ex.Message}");
						return null;
					}
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

                var url = $"{baseCurrent}/weather?lat={latitude}&lon={longitude}&units=imperial";
                if (!string.IsNullOrWhiteSpace(_apiKey))
                    url += $"&appid={WebUtility.UrlEncode(_apiKey)}";

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
                        ? j["weather"].ToObject<List<WeatherDescription>>()!
                        : new List<WeatherDescription>()
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
