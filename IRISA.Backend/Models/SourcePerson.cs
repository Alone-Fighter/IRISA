namespace IRISA.Backend.Models;

public sealed class SourcePerson
{
    public Guid Id { get; set; }

    public required string NationalCode { get; set; }

    public required string FullName { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? Gender { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<SourceLabResult> LabResults { get; set; } = [];
}
