using System;
using System.Collections.Generic;

namespace Model;

public partial class Offer
{
    public int Id { get; set; }

    public Guid WootId { get; set; }

    public string Category { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Photo { get; set; }

    public bool IsSoldOut { get; set; }

    public string Condition { get; set; } = null!;

    public string Url { get; set; } = null!;

    public virtual ICollection<Configuration> Configurations { get; set; } = new List<Configuration>();
}
