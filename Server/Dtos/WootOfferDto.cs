using System.Text.Json.Serialization;

namespace Server.Dtos
{
    public class WootOfferDto
    {
        [JsonPropertyName("id")]
        public Guid WootId { get; set; }

        public string Category { get; set; } = null!;

        public string Title { get; set; } = null!;

        public string? Photo { get; set; }

        public bool IsSoldOut { get; set; }

        public string Condition { get; set; } = null!;

        public string Url { get; set; } = null!;

        public ICollection<WootItemDto> Items { get; set; } = new List<WootItemDto>();
    }
}
