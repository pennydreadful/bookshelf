using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NzbDrone.Core.MetadataSource.GoogleBooks
{
    public class GoogleBooksVolumeResponse
    {
        [JsonPropertyName("items")]
        public List<GoogleBooksVolume> Items { get; set; }

        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }
    }

    public class GoogleBooksVolume
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("volumeInfo")]
        public GoogleBooksVolumeInfo VolumeInfo { get; set; }

        [JsonPropertyName("saleInfo")]
        public GoogleBooksSaleInfo SaleInfo { get; set; }
    }

    public class GoogleBooksVolumeInfo
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("subtitle")]
        public string Subtitle { get; set; }

        [JsonPropertyName("authors")]
        public List<string> Authors { get; set; }

        [JsonPropertyName("publisher")]
        public string Publisher { get; set; }

        [JsonPropertyName("publishedDate")]
        public string PublishedDate { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("industryIdentifiers")]
        public List<GoogleBooksIndustryIdentifier> IndustryIdentifiers { get; set; }

        [JsonPropertyName("pageCount")]
        public int? PageCount { get; set; }

        [JsonPropertyName("averageRating")]
        public decimal? AverageRating { get; set; }

        [JsonPropertyName("ratingsCount")]
        public int? RatingsCount { get; set; }

        [JsonPropertyName("categories")]
        public List<string> Categories { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("printType")]
        public string PrintType { get; set; }

        [JsonPropertyName("infoLink")]
        public string InfoLink { get; set; }

        [JsonPropertyName("previewLink")]
        public string PreviewLink { get; set; }

        [JsonPropertyName("imageLinks")]
        public GoogleBooksImageLinks ImageLinks { get; set; }
    }

    public class GoogleBooksSaleInfo
    {
        [JsonPropertyName("isEbook")]
        public bool? IsEbook { get; set; }
    }

    public class GoogleBooksIndustryIdentifier
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("identifier")]
        public string Identifier { get; set; }
    }

    public class GoogleBooksImageLinks
    {
        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; }

        [JsonPropertyName("smallThumbnail")]
        public string SmallThumbnail { get; set; }
    }
}
