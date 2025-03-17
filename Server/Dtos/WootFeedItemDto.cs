namespace Server.Dtos
{
    public class WootFeedItemDto
    {
        // matching the JSON property for the FeedItem schema at
        // https://developer.woot.com/#tocs_feeditem
        public Guid OfferId { get; set; }

        public string[] Categories { get; set; } = null!;
    }
}
