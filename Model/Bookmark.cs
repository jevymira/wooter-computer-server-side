using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model;

[Table("Bookmark")] // Override EF Core plural naming convention.
public class Bookmark
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(WooterComputerUser))]
    public int UserId { get; set; }

    [ForeignKey(nameof(HardwareConfiguration))]
    public int ConfigurationId { get; set; }

    public HardwareConfiguration HardwareConfiguration { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
