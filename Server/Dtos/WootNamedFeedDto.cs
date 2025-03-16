using Microsoft.AspNetCore.Http.Features;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace Server.Dtos
{
    public class WootNamedFeedDto
    {
        [JsonPropertyName("Items")]
        public ICollection<WootOfferDto> Offers { get; set; }
    }
}
