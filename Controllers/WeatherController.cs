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

        // GET /weather/reverse?lat=...&lon=...&limit=1
        [HttpGet("reverse")]
        public async Task<ActionResult<object>> ReverseGeocode([FromQuery] double lat, [FromQuery] double lon, [FromQuery] int limit = 1)
        {
            if (double.IsNaN(lat) || double.IsNaN(lon))
                return BadRequest(new { error = "Invalid latitude or longitude" });

            var city = await _weatherService.ReverseGeocodeAsync(lat, lon, limit);

            if (city is null)
                return NotFound(new { error = "No city found for the provided coordinates" });

            return Ok(new { city });
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
