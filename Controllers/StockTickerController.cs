using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Dashboard.Net.AI.Services;

namespace Dashboard.Net.AI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class StockTickerController : ControllerBase
	{
		private readonly ILogger<StockTickerController> _logger;
		private readonly IStockService _finnhub;

		public StockTickerController(ILogger<StockTickerController> logger, IStockService finnhub)
		{
			_logger = logger;
			_finnhub = finnhub;
		}

		/// <summary>
		/// Verifies whether a stock symbol has a valid format and exists according to Finnhub.
		/// </summary>
		/// <param name="symbol">Stock ticker symbol to validate (e.g. MSFT, BRK.B).</param>
		/// <returns>Object with normalized symbol and IsValid flag (+ optional reason).</returns>
		[HttpGet("validate/{symbol}")]
		public async Task<IActionResult> ValidateSymbol(string? symbol)
		{
			if (string.IsNullOrWhiteSpace(symbol))
			{
				return BadRequest(new { Symbol = symbol, IsValid = false, Reason = "Symbol is required." });
			}

			var normalized = symbol.Trim().ToUpperInvariant();

			// Basic format validation:
			// - Allow letters, numbers, dot and dash (covers tickers like BRK.B and RDS-A)
			// - Length between 1 and 7 characters (adjust if you need wider support)
			var regex = new Regex(@"^[A-Z0-9\.\-]{1,7}$", RegexOptions.Compiled);
			if (!regex.IsMatch(normalized))
			{
				_logger.LogInformation("Invalid symbol format: {Symbol}", symbol);
				return Ok(new { Symbol = symbol, IsValid = false, Reason = "Invalid format." });
			}

			var exists = await _finnhub.IsValidSymbolAsync(normalized);
			if (!exists)
			{
				_logger.LogInformation("Symbol not found in Finnhub: {Symbol}", normalized);
				return Ok(new { Symbol = normalized, IsValid = false, Reason = "Symbol not found." });
			}

			_logger.LogInformation("Symbol validated with Finnhub: {Symbol}", normalized);
			return Ok(new { Symbol = normalized, IsValid = true });
		}

		/// <summary>
		/// Returns today's intraday close prices for the requested symbol formatted for a line chart.
		/// </summary>
		[HttpGet("intraday/{symbol}")]
		public async Task<IActionResult> GetTodayIntraday(string? symbol)
		{
			if (string.IsNullOrWhiteSpace(symbol))
			{
				return BadRequest(new { Symbol = symbol, Points = new object[0], Reason = "Symbol is required." });
			}

			var normalized = symbol.Trim().ToUpperInvariant();

			var regex = new Regex(@"^[A-Z0-9\.\-]{1,7}$", RegexOptions.Compiled);
			if (!regex.IsMatch(normalized))
			{
				return Ok(new { Symbol = normalized, Points = new object[0], Reason = "Invalid symbol format." });
			}

			var exists = await _finnhub.IsValidSymbolAsync(normalized);
			if (!exists)
			{
				return Ok(new { Symbol = normalized, Points = new object[0], Reason = "Symbol not found." });
			}

			var today = System.DateTime.Now.Date;
			var points = await _finnhub.GetIntradayForDateAsync(normalized, today);

			// Return simple payload: an array of { timestamp: milliseconds, close: number }
			return Ok(new { Symbol = normalized, Points = points });
		}
	}
}
