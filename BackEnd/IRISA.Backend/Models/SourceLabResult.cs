namespace IRISA.Backend.Models;

public sealed class SourceLabResult
{
    public Guid Id { get; set; }

    public Guid SourcePersonId { get; set; }

    public required string TestName { get; set; }

    public string? ResultValue { get; set; }

    public string? Unit { get; set; }

    public string? ReferenceRange { get; set; }

    public DateTimeOffset PerformedAtUtc { get; set; }

    public SourcePerson SourcePerson { get; set; } = null!;
}
