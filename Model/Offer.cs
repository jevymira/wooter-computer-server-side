using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model;

[Index(nameof(WootId))] // nameof: type safety
public partial class Offer
{
    [Key]
    public int Id { get; set; }

    public Guid WootId { get; set; }

    public string Category { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Photo { get; set; } = null!;

    public bool IsSoldOut { get; set; }

    public string Condition { get; set; } = null!;

    public string Url { get; set; } = null!;

    [InverseProperty("Offer")]
    public virtual ICollection<HardwareConfiguration> Configurations { get; set; } = new List<HardwareConfiguration>();
}
