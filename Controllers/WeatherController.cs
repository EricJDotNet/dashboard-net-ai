using Dashboard.Net.AI.Models;
using Dashboard.Net.AI.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Net.AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController(IWeatherService weatherService, ILogger<WeatherController> logger) : ControllerBase
    {
        private readonly IWeatherService _weatherService = weatherService;

        [HttpGet("search")]
        public async Task<ActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Query is required.");
            }

            try
            {
                MapboxV6Response jsonResult = await _weatherService.GetAutocompleteResultsAsync(query);
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
            {
                return BadRequest(new { error = "Invalid latitude or longitude" });
            }

            MapboxV6Response? location = await _weatherService.ReverseGeocodeAsync(lat, lon, limit);

            return location is null ? NotFound(new { error = "No city found for the provided coordinates" }) : Ok(location);
        }

        // GET /weather/current?lat=...&lon=...
        [HttpGet("current")]
        public async Task<ActionResult<CurrentWeather>> GetCurrent([FromQuery] double lat, [FromQuery] double lon)
        {
            if (double.IsNaN(lat) || double.IsNaN(lon))
            {
                return BadRequest(new { error = "Invalid latitude or longitude" });
            }

            CurrentWeather? weather = await _weatherService.GetCurrentWeatherAsync(lat, lon);

            return weather is null ? (ActionResult<CurrentWeather>)NotFound(new { error = "Unable to retrieve current weather for the provided coordinates" }) : (ActionResult<CurrentWeather>)Ok(weather);
        }
    }
}
