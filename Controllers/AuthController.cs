using Microsoft.AspNetCore.Mvc;
using Dashboard.Net.AI.Models;
using Dashboard.Net.AI.Services;

namespace Dashboard.Net.AI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IConfiguration config, IAuth0Service authService) : ControllerBase
    {
        private readonly IAuth0Service _authService = authService;

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] SigninRequest request)
        {
            string? token = await _authService.LoginAsync(request.Email, request.Password);

            return token == null ? Unauthorized("Invalid credentials.") : Ok(new { Token = token });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegistrationRequest request)
        {
            bool success = await _authService.RegisterAsync(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName);

            return !success ? BadRequest("Could not create user.") : Ok(true);
        }

    }

}
