using System.Text.Json.Serialization;

namespace Server.Dtos
{
    public class WootOfferDto
    {
        [JsonPropertyName("Id")]
        public Guid WootId { get; set; }

        // FIXME: To be deprecated and removed.
        public string[] Categories { get; set; } = null!;

        public string Category { get; set; } = string.Empty;

        public string Title { get; set; } = null!;

        public string? Photo { get; set; }

        public bool IsSoldOut { get; set; }

        public string Condition { get; set; } = null!;

        public string Url { get; set; } = null!;

        public string FullTitle { get; set; } = null!;

        public ICollection<WootOfferItemDto> Items { get; set; } = new List<WootOfferItemDto>();
    }
}
