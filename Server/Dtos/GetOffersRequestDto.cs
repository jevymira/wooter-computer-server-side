namespace Server.Dtos;

public class GetOffersRequestDto
{
    /// <summary>
    /// Computer category (e.g., "desktop")
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// System RAM in GB.
    /// </summary>
    public List<short>? Memory { get; set; }

    /// <summary>
    /// System storage in GB.
    /// </summary>
    public List<short>? Storage { get; set; }

    public string? SortOrder { get; set; }
}
