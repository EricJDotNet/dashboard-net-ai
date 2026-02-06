using System.Text.Json;

namespace Dashboard.Net.AI.Services
{
    public class StockService(HttpClient http, IConfiguration config, ILogger<StockService> logger) : IStockService
    {
        private readonly HttpClient _http = http;
        private readonly string _apiKey = config["StockData:ApiKey"] ?? string.Empty;
        private readonly string _baseUrl = config["StockData:BaseUrl"] ?? "https://api.stockdata.org/v1";
        private readonly ILogger<StockService> _logger = logger;

        // Symbol lookup using StockData.org quote endpoint (returns data array when a symbol is known)
        public async Task<bool> IsValidSymbolAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return false;
            }

            string encoded = System.Net.WebUtility.UrlEncode(symbol);
            string url = $"{_baseUrl}/data/quote?symbols={encoded}";
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                url += $"&api_token={System.Net.WebUtility.UrlEncode(_apiKey)}";
            }

            try
            {
                string json = await _http.GetStringAsync(url);
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("data", out JsonElement data) && data.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in data.EnumerateArray())
                    {
                        // Try several common property names that may hold the symbol
                        if (item.TryGetProperty("symbol", out JsonElement symProp) && symProp.ValueKind == JsonValueKind.String)
                        {
                            string s = symProp.GetString() ?? string.Empty;
                            if (string.Equals(s, symbol, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }

                        if (item.TryGetProperty("ticker", out JsonElement tickerProp) && tickerProp.ValueKind == JsonValueKind.String)
                        {
                            string s = tickerProp.GetString() ?? string.Empty;
                            if (string.Equals(s, symbol, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }

                        // Some APIs return a "code" or "s" field
                        if (item.TryGetProperty("code", out JsonElement codeProp) && codeProp.ValueKind == JsonValueKind.String)
                        {
                            string s = codeProp.GetString() ?? string.Empty;
                            if (string.Equals(s, symbol, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }

                        if (item.TryGetProperty("s", out JsonElement sProp) && sProp.ValueKind == JsonValueKind.String)
                        {
                            string s = sProp.GetString() ?? string.Empty;
                            if (string.Equals(s, symbol, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling StockData for symbol {Symbol}", symbol);
                return false;
            }
        }

        public async Task<IntradayPoint[]> GetIntradayForDateAsync(string symbol, DateTime date)
        {
            DateTime localDate = date.Date;

            // Common US market hours (09:30 - 16:00) in local time. We'll assume the symbol trades in US market.
            DateTime marketOpen = new(localDate.Year, localDate.Month, localDate.Day, 9, 30, 0, DateTimeKind.Local);
            DateTime marketClose = new(localDate.Year, localDate.Month, localDate.Day, 16, 0, 0, DateTimeKind.Local);

            // StockData.org OHLC endpoint: use date/time range and interval=1m
            _ = new DateTimeOffset(marketOpen).ToString("o");
            _ = new DateTimeOffset(marketClose).ToString("o");

            string encoded = System.Net.WebUtility.UrlEncode(symbol);
            string url = $"{_baseUrl}/data/intraday/adjusted?symbols={encoded}&date={marketOpen:yyyy-MM-dd}";
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                url += $"&api_token={System.Net.WebUtility.UrlEncode(_apiKey)}";
            }

            try
            {
                string json = await _http.GetStringAsync(url);
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("data", out JsonElement data) || data.ValueKind != JsonValueKind.Array)
                {
                    return [];
                }

                List<IntradayPoint> points = [];

                foreach (JsonElement item in data.EnumerateArray())
                {
                    // Each item might represent a data point with fields like: t (unix seconds or ISO string), c or close
                    long? tsMs = null;
                    double? close = null;

                    if (item.TryGetProperty("t", out JsonElement tProp))
                    {
                        if (tProp.ValueKind == JsonValueKind.Number)
                        {
                            if (tProp.TryGetInt64(out long tSec))
                            {
                                tsMs = tSec * 1000L;
                            }
                            else if (tProp.TryGetDouble(out double tDouble))
                            {
                                tsMs = (long)(tDouble * 1000);
                            }
                        }
                        else if (tProp.ValueKind == JsonValueKind.String)
                        {
                            string? str = tProp.GetString();
                            if (DateTimeOffset.TryParse(str, out DateTimeOffset dto))
                            {
                                tsMs = dto.ToUnixTimeMilliseconds();
                            }
                        }
                    }
                    else if (item.TryGetProperty("timestamp", out JsonElement tsProp))
                    {
                        if (tsProp.ValueKind == JsonValueKind.Number && tsProp.TryGetInt64(out long tSec))
                        {
                            tsMs = tSec * 1000L;
                        }
                        else if (tsProp.ValueKind == JsonValueKind.String)
                        {
                            string? str = tsProp.GetString();
                            if (DateTimeOffset.TryParse(str, out DateTimeOffset dto))
                            {
                                tsMs = dto.ToUnixTimeMilliseconds();
                            }
                        }
                    }

                    // Close price
                    if (item.TryGetProperty("c", out JsonElement cProp) && (cProp.ValueKind == JsonValueKind.Number))
                    {
                        close = cProp.GetDouble();
                    }
                    else if (item.TryGetProperty("close", out JsonElement closeProp) && (closeProp.ValueKind == JsonValueKind.Number))
                    {
                        close = closeProp.GetDouble();
                    }
                    else if (item.TryGetProperty("price", out JsonElement priceProp) && (priceProp.ValueKind == JsonValueKind.Number))
                    {
                        close = priceProp.GetDouble();
                    }

                    if (tsMs.HasValue && close.HasValue)
                    {
                        points.Add(new IntradayPoint(tsMs.Value, close.Value));
                    }
                }

                return [.. points];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching intraday OHLC from StockData for {Symbol} on {Date}", symbol, date);
                return [];
            }
        }

        // Note: keep private DTOs removed in favor of runtime JSON parsing above.
    }
}
