using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Dashboard.Net.AI.Services
{
    public class StockService : IStockService
	{
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly ILogger<StockService> _logger;

        public StockService(HttpClient http, IConfiguration config, ILogger<StockService> logger)
        {
            _http = http;
            _apiKey = config["StockData:ApiKey"] ?? string.Empty;
            _baseUrl = config["StockData:BaseUrl"] ?? "https://api.stockdata.org/v1";
            _logger = logger;

            // StockData.org typically uses an api_token query parameter. We won't add a default header here,
            // but keep the HttpClient available for requests.
        }

        // Symbol lookup using StockData.org quote endpoint (returns data array when a symbol is known)
        public async Task<bool> IsValidSymbolAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return false;

            var encoded = System.Net.WebUtility.UrlEncode(symbol);
            var url = $"{_baseUrl}/data/quote?symbols={encoded}";
            if (!string.IsNullOrWhiteSpace(_apiKey))
                url += $"&api_token={System.Net.WebUtility.UrlEncode(_apiKey)}";

            try
            {
                var json = await _http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        // Try several common property names that may hold the symbol
                        if (item.TryGetProperty("symbol", out var symProp) && symProp.ValueKind == JsonValueKind.String)
                        {
                            var s = symProp.GetString() ?? string.Empty;
                            if (string.Equals(s, symbol, System.StringComparison.OrdinalIgnoreCase))
                                return true;
                        }

                        if (item.TryGetProperty("ticker", out var tickerProp) && tickerProp.ValueKind == JsonValueKind.String)
                        {
                            var s = tickerProp.GetString() ?? string.Empty;
                            if (string.Equals(s, symbol, System.StringComparison.OrdinalIgnoreCase))
                                return true;
                        }

                        // Some APIs return a "code" or "s" field
                        if (item.TryGetProperty("code", out var codeProp) && codeProp.ValueKind == JsonValueKind.String)
                        {
                            var s = codeProp.GetString() ?? string.Empty;
                            if (string.Equals(s, symbol, System.StringComparison.OrdinalIgnoreCase))
                                return true;
                        }

                        if (item.TryGetProperty("s", out var sProp) && sProp.ValueKind == JsonValueKind.String)
                        {
                            var s = sProp.GetString() ?? string.Empty;
                            if (string.Equals(s, symbol, System.StringComparison.OrdinalIgnoreCase))
                                return true;
                        }
                    }
                }

                return false;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error calling StockData for symbol {Symbol}", symbol);
                return false;
            }
        }

        public async Task<IntradayPoint[]> GetIntradayForDateAsync(string symbol, System.DateTime date)
        {
            var localDate = date.Date;

            // Common US market hours (09:30 - 16:00) in local time. We'll assume the symbol trades in US market.
            var marketOpen = new System.DateTime(localDate.Year, localDate.Month, localDate.Day, 9, 30, 0, System.DateTimeKind.Local);
            var marketClose = new System.DateTime(localDate.Year, localDate.Month, localDate.Day, 16, 0, 0, System.DateTimeKind.Local);

            // StockData.org OHLC endpoint: use date/time range and interval=1m
            var fromIso = new System.DateTimeOffset(marketOpen).ToString("o");
            var toIso = new System.DateTimeOffset(marketClose).ToString("o");

            var encoded = System.Net.WebUtility.UrlEncode(symbol);
            var url = $"{_baseUrl}/data/intraday/adjusted?symbols={encoded}&date={marketOpen.ToString("yyyy-MM-dd")}";
            if (!string.IsNullOrWhiteSpace(_apiKey))
                url += $"&api_token={System.Net.WebUtility.UrlEncode(_apiKey)}";

            try
            {
                var json = await _http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                    return System.Array.Empty<IntradayPoint>();

                var points = new System.Collections.Generic.List<IntradayPoint>();

                foreach (var item in data.EnumerateArray())
                {
                    // Each item might represent a data point with fields like: t (unix seconds or ISO string), c or close
                    long? tsMs = null;
                    double? close = null;

                    if (item.TryGetProperty("t", out var tProp))
                    {
                        if (tProp.ValueKind == JsonValueKind.Number)
                        {
                            if (tProp.TryGetInt64(out var tSec))
                            {
                                tsMs = tSec * 1000L;
                            }
                            else if (tProp.TryGetDouble(out var tDouble))
                            {
                                tsMs = (long)(tDouble * 1000);
                            }
                        }
                        else if (tProp.ValueKind == JsonValueKind.String)
                        {
                            var str = tProp.GetString();
                            if (System.DateTimeOffset.TryParse(str, out var dto))
                                tsMs = dto.ToUnixTimeMilliseconds();
                        }
                    }
                    else if (item.TryGetProperty("timestamp", out var tsProp))
                    {
                        if (tsProp.ValueKind == JsonValueKind.Number && tsProp.TryGetInt64(out var tSec))
                            tsMs = tSec * 1000L;
                        else if (tsProp.ValueKind == JsonValueKind.String)
                        {
                            var str = tsProp.GetString();
                            if (System.DateTimeOffset.TryParse(str, out var dto))
                                tsMs = dto.ToUnixTimeMilliseconds();
                        }
                    }

                    // Close price
                    if (item.TryGetProperty("c", out var cProp) && (cProp.ValueKind == JsonValueKind.Number))
                    {
                        close = cProp.GetDouble();
                    }
                    else if (item.TryGetProperty("close", out var closeProp) && (closeProp.ValueKind == JsonValueKind.Number))
                    {
                        close = closeProp.GetDouble();
                    }
                    else if (item.TryGetProperty("price", out var priceProp) && (priceProp.ValueKind == JsonValueKind.Number))
                    {
                        close = priceProp.GetDouble();
                    }

                    if (tsMs.HasValue && close.HasValue)
                    {
                        points.Add(new IntradayPoint(tsMs.Value, close.Value));
                    }
                }

                return points.ToArray();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error fetching intraday OHLC from StockData for {Symbol} on {Date}", symbol, date);
                return System.Array.Empty<IntradayPoint>();
            }
        }

        // Note: keep private DTOs removed in favor of runtime JSON parsing above.
    }
}
