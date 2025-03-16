using System.Text.Json.Serialization;

namespace Server.Dtos
{
    public class WootOfferDto
    {
        [JsonPropertyName("OfferId")]
        public Guid WootId { get; set; }

        public string[] Categories { get; set; } = null!;

        public string Title { get; set; } = null!;

        public string? Photo { get; set; }

        public bool IsSoldOut { get; set; }

        public string Condition { get; set; } = null!;

        public string Url { get; set; } = null!;

        public string Specs { get; set; } = null!;

        public ICollection<WootOfferItemDto> Items { get; set; } = new List<WootOfferItemDto>();
    }
}
