namespace Server.Dtos
{
    public class WootNamedFeedDto
    {
        public ICollection<WootFeedItemDto> Items { get; set; } = null!;
    }
}
