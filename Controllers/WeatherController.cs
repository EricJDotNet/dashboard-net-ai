using Dashboard.Net.AI.Models;
using Dashboard.Net.AI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Dashboard.Net.AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _weatherService;
        private readonly ILogger<WeatherController> _logger;

        public WeatherController(IWeatherService weatherService, ILogger<WeatherController> logger)
        {
            _weatherService = weatherService;
            _logger = logger;
        }

		[HttpGet("search")]
		public async Task<ActionResult> Search([FromQuery] string query)
		{
			if (string.IsNullOrWhiteSpace(query)) return BadRequest("Query is required.");

			try
			{
				var jsonResult = await _weatherService.GetAutocompleteResultsAsync(query);
				return Ok(jsonResult);
			}
			catch (HttpRequestException)
			{
				return StatusCode(502, "Error communicating with the geocoding provider.");
			}
		}

		// GET /weather/reverse?lat=...&lon=...&limit=1
		[HttpGet("reverse")]
        public async Task<ActionResult> ReverseGeocode([FromQuery] double lat, [FromQuery] double lon, [FromQuery] int limit = 1)
        {
            if (double.IsNaN(lat) || double.IsNaN(lon))
                return BadRequest(new { error = "Invalid latitude or longitude" });

            var location = await _weatherService.ReverseGeocodeAsync(lat, lon, limit);

            if (location is null)
                return NotFound(new { error = "No city found for the provided coordinates" });

            return Ok(location);
        }

        // GET /weather/current?lat=...&lon=...
        [HttpGet("current")]
        public async Task<ActionResult<CurrentWeather>> GetCurrent([FromQuery] double lat, [FromQuery] double lon)
        {
            if (double.IsNaN(lat) || double.IsNaN(lon))
                return BadRequest(new { error = "Invalid latitude or longitude" });

            var weather = await _weatherService.GetCurrentWeatherAsync(lat, lon);

            if (weather is null)
                return NotFound(new { error = "Unable to retrieve current weather for the provided coordinates" });

            return Ok(weather);
        }
    }
}
