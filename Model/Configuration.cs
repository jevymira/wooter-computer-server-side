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

    /// <value>
    /// The price denominated in USD ($).
    /// </value>
    /// <remarks>
    /// Decimal seems conventional to use as the SQL Server data type because it is:
    /// (1) decimal floating-point rather than binary floating-point and
    /// (2) 128-bit, for twice the precision of the double type.
    /// For SQL-CLR type mapping, see: https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql/linq/sql-clr-type-mapping#NumericMapping
    /// </remarks>
    public decimal Price { get; set; }

    public int OfferId { get; set; }

    public virtual Offer Offer { get; set; } = null!;
}
