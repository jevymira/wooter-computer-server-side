namespace Server.Dtos;

public class BookmarkDto
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public int ConfigurationId { get; set; }

    public string Title { get; set; } = null!;

    public string Photo { get; set; } = null!;

    public short MemoryCapacity { get; set; }

    public short StorageSize { get; set; }

    public decimal Price { get; set; }

    public bool IsSoldOut { get; set; }
}
