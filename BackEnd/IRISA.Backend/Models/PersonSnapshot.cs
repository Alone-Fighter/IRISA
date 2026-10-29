using System.ComponentModel.DataAnnotations;

namespace IRISA.Backend.Models;

public sealed class PersonSnapshot
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(10, MinimumLength = 10)]
    public required string NationalCode { get; set; }

    [Required]
    [StringLength(200)]
    public required string FullName { get; set; }

    public DateOnly? BirthDate { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }

    public DateTimeOffset LastSyncedAtUtc { get; set; }

    public DateTimeOffset? SourceUpdatedAtUtc { get; set; }

    public ICollection<LabResultSnapshot> LabResults { get; set; } = [];
}
