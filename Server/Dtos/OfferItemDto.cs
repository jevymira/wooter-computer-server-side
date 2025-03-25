namespace Server.Dtos
{
    public class OfferItemDto
    {
        public int Id { get; set; }

        public string Category { get; set; } = null!;

        public string Title { get; set; } = null!;

        public short MemoryCapacity { get; set; }

        public short StorageSize { get; set; }

        public decimal Price { get; set; }

        public bool IsSoldOut { get; set; }

        public string Url { get; set; } = null!;
    }
}
