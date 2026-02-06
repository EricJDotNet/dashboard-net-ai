namespace Dashboard.Net.AI.Services
{
    // A small DTO used to return intraday chart points to controllers
    public record IntradayPoint(long TimestampMilliseconds, double Close);
}
