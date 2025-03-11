using System;
using System.Collections.Generic;

namespace Model;

public partial class Configuration
{
    public int Id { get; set; }

    public Guid WootId { get; set; }

    public string? Processor { get; set; }

    public short MemoryCapacity { get; set; }

    public short StorageSize { get; set; }

    public int OfferId { get; set; }

    public virtual Offer Offer { get; set; } = null!;
}
