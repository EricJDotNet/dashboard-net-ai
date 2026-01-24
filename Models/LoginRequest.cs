using System.ComponentModel.DataAnnotations;

namespace Dashboard.Net.AI.Models
{

	public class SigninRequest
	{
		[Required]
		[EmailAddress]
		public string Email { get; set; } = string.Empty;

		[Required]
		public string Password { get; set; } = string.Empty;
	}

	public class RegistrationRequest
	{
		[Required]
		[EmailAddress]
		public string Email { get; set; } = string.Empty;

		[Required]
		[MinLength(8)]
		public string Password { get; set; } = string.Empty;

		[Required]
		public string FirstName { get; set; } = string.Empty;

		[Required]
		public string LastName { get; set; } = string.Empty;
	}
}
