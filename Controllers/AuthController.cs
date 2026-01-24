using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dashboard.Net.AI.Models;
using Dashboard.Net.AI.Services;

namespace Dashboard.Net.AI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AuthController : ControllerBase
	{
		private readonly IConfiguration _config;
		private readonly IAuth0Service _authService;

		public AuthController(IConfiguration config, IAuth0Service authService)
		{
			_config = config;
			_authService = authService;
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] SigninRequest request)
		{
			var token = await _authService.LoginAsync(request.Email, request.Password);

			if (token == null)
				return Unauthorized("Invalid credentials.");

			return Ok(new { Token = token });
		}

		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] RegistrationRequest request)
		{
			var success = await _authService.RegisterAsync(
				request.Email,
				request.Password,
				request.FirstName,
				request.LastName);

			if (!success)
				return BadRequest("Could not create user.");

			return Ok(true);
		}

	}

}
