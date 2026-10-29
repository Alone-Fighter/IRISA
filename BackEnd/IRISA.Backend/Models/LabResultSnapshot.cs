using System.ComponentModel.DataAnnotations;

namespace IRISA.Backend.Models;

public sealed class LabResultSnapshot
{
    public Guid Id { get; set; }

    public Guid PersonSnapshotId { get; set; }

    [StringLength(100)]
    public string? SourceRecordId { get; set; }

    [Required]
    [StringLength(200)]
    public required string TestName { get; set; }

    [StringLength(2000)]
    public string? ResultValue { get; set; }

    [StringLength(50)]
    public string? Unit { get; set; }

    [StringLength(200)]
    public string? ReferenceRange { get; set; }

    public DateTimeOffset? PerformedAtUtc { get; set; }

    public DateTimeOffset RetrievedAtUtc { get; set; }

    public PersonSnapshot PersonSnapshot { get; set; } = null!;
}
