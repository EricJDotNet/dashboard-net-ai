namespace Dashboard.Net.AI.Models
{
    public record WeatherDescription
    {
        public int Id { get; init; }
        public string Main { get; init; }
        public string Description { get; init; }
        public string Icon { get; init; }
    }

    public record CurrentWeather
    {
        public long Dt { get; init; }
        public long Sunrise { get; init; }
        public long Sunset { get; init; }

        public double Temp { get; init; }
        public double FeelsLike { get; init; }

        public int Pressure { get; init; }
        public int Humidity { get; init; }
        public double DewPoint { get; init; }
        public double Uvi { get; init; }
        public int Clouds { get; init; }
        public int Visibility { get; init; }

        public double WindSpeed { get; init; }
        public int WindDeg { get; init; }
        public double? WindGust { get; init; }

        public List<WeatherDescription> Weather { get; init; }
    }
}
