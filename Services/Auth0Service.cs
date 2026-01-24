using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;

namespace Dashboard.Net.AI.Services
{
	public interface IAuth0Service
	{
		Task<string?> LoginAsync(string username, string password);
		Task<bool> RegisterAsync(string email, string password, string firstName, string lastName);
	}

	public class Auth0Service : IAuth0Service
	{
		private readonly string _domain;
		private readonly string _clientId;
		private readonly string _clientSecret;
		private readonly string _connection = "Username-Password-Authentication";

		public Auth0Service(IConfiguration config)
		{
			_domain = config["Auth0:Domain"];
			_clientId = config["Auth0:ClientId"];
			_clientSecret = config["Auth0:ClientSecret"];
		}

		public async Task<string?> LoginAsync(string username, string password)
		{
			var client = new AuthenticationApiClient(new Uri($"https://{_domain}"));

			try
			{
				// Using Resource Owner Password Flow
				var request = new ResourceOwnerTokenRequest
				{
					Username = username,
					Password = password,
					ClientId = _clientId,
					ClientSecret = _clientSecret,
					Scope = "openid profile email",
					Audience = $"https://{_domain}/api/v2/"
				};

				var response = await client.GetTokenAsync(request);
				return response.AccessToken;
			}
			catch
			{
				return null; // Login failed
			}
		}

		public async Task<bool> RegisterAsync(string email, string password, string firstName, string lastName)
		{
			// Note: For Management API, you typically need a Machine-to-Machine token.
			// For brevity, we assume you have a way to provide the Management Client a token.
			var managementToken = await GetManagementTokenAsync();
			var client = new ManagementApiClient(managementToken, new Uri($"https://{_domain}/api/v2/"));

			var request = new UserCreateRequest
			{
				Email = email,
				Password = password,
				FirstName = firstName,
				LastName = lastName,
				NickName = "",
				FullName = firstName + " " + lastName,
				Connection = _connection,
				EmailVerified = false
			};

			try
			{
				await client.Users.CreateAsync(request);
				return true;
			}
			catch
			{
				return false;
			}
		}

		private async Task<string> GetManagementTokenAsync()
		{
			// In production, cache this token until it expires
			var client = new AuthenticationApiClient(new Uri($"https://{_domain}"));
			var tokenRequest = new ClientCredentialsTokenRequest
			{
				ClientId = _clientId,
				ClientSecret = _clientSecret,
				Audience = $"https://{_domain}/api/v2/"
			};
			var response = await client.GetTokenAsync(tokenRequest);
			return response.AccessToken;
		}
	}
}