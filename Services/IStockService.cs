namespace Dashboard.Net.AI.Services
{
    public interface IStockService
    {
        Task<bool> IsValidSymbolAsync(string symbol);

        // Returns intraday points (timestamp in milliseconds + close price) for the specified date.
        Task<IntradayPoint[]> GetIntradayForDateAsync(string symbol, DateTime date);
    }
}
