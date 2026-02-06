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

    public class Auth0Service(IConfiguration config) : IAuth0Service
    {
        private readonly string _domain = config["Auth0:Domain"];
        private readonly string _clientId = config["Auth0:ClientId"];
        private readonly string _clientSecret = config["Auth0:ClientSecret"];
        private readonly string _connection = "Username-Password-Authentication";

        public async Task<string?> LoginAsync(string username, string password)
        {
            AuthenticationApiClient client = new(new Uri($"https://{_domain}"));

            try
            {
                // Using Resource Owner Password Flow
                ResourceOwnerTokenRequest request = new()
                {
                    Username = username,
                    Password = password,
                    ClientId = _clientId,
                    ClientSecret = _clientSecret,
                    Scope = "openid profile email",
                    Audience = $"https://{_domain}/api/v2/"
                };

                AccessTokenResponse response = await client.GetTokenAsync(request);
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
            string managementToken = await GetManagementTokenAsync();
            ManagementApiClient client = new(managementToken, new Uri($"https://{_domain}/api/v2/"));

            UserCreateRequest request = new()
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
                _ = await client.Users.CreateAsync(request);
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
            AuthenticationApiClient client = new(new Uri($"https://{_domain}"));
            ClientCredentialsTokenRequest tokenRequest = new()
            {
                ClientId = _clientId,
                ClientSecret = _clientSecret,
                Audience = $"https://{_domain}/api/v2/"
            };
            AccessTokenResponse response = await client.GetTokenAsync(tokenRequest);
            return response.AccessToken;
        }
    }
}
