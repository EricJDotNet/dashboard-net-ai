using System.Text.Json.Serialization;

namespace Dashboard.Net.AI.Models
{
    public class MapboxV6Response
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("features")]
        public List<MapboxFeature> Features { get; set; } = [];

        [JsonPropertyName("attribution")]
        public string Attribution { get; set; } = string.Empty;
    }

    public class MapboxFeature
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("geometry")]
        public Geometry Geometry { get; set; } = new();

        [JsonPropertyName("properties")]
        public FeatureProperties Properties { get; set; } = new();
    }

    public class Geometry
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("coordinates")]
        public double[] Coordinates { get; set; } = []; // [Lon, Lat]
    }

    public class FeatureProperties
    {
        [JsonPropertyName("mapbox_id")]
        public string MapboxId { get; set; } = string.Empty;

        [JsonPropertyName("feature_type")]
        public string FeatureType { get; set; } = string.Empty;

        [JsonPropertyName("full_address")]
        public string FullAddress { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("place_formatted")]
        public string PlaceFormatted { get; set; } = string.Empty;

        [JsonPropertyName("context")]
        public AddressContext Context { get; set; } = new();

        [JsonPropertyName("coordinates")]
        public CoordinateProperties Coordinates { get; set; } = new();
    }

    public class AddressContext
    {
        [JsonPropertyName("street")]
        public ContextItem? Street { get; set; }

        [JsonPropertyName("postcode")]
        public ContextItem? Postcode { get; set; }

        [JsonPropertyName("place")]
        public ContextItem? Place { get; set; }

        [JsonPropertyName("region")]
        public ContextItem? Region { get; set; }

        [JsonPropertyName("country")]
        public ContextItem? Country { get; set; }
    }

    public class ContextItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("mapbox_id")]
        public string MapboxId { get; set; } = string.Empty;
    }

    public class CoordinateProperties
    {
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }
    }
}
