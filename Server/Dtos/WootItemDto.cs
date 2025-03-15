namespace Server.Dtos
{
    public class WootItemDto
    {
        public ICollection<WootAttributeDto> Attributes { get; set; } = new List<WootAttributeDto>();

        public Guid Id { get; set; }
    }
}
