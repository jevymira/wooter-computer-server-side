namespace Server.Dtos
{
    public class WootOfferItemDto
    {
        public ICollection<WootOfferItemAttributeDto> Attributes { get; set; } = new List<WootOfferItemAttributeDto>();

        public Guid Id { get; set; }
    }
}
