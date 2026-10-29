namespace IRISA.Backend.Dtos;

/// <summary>Person data and lab results returned by a national-code lookup.</summary>
public sealed record PersonLookupResponse(
    string NationalCode,
    string FullName,
    DateOnly? BirthDate,
    string? Gender,
    DateTimeOffset LastSyncedAtUtc,
    bool IsStale,
    string DataSource,
    IReadOnlyList<LabResultResponse> LabResults);

/// <summary>One laboratory result in a person lookup response.</summary>
public sealed record LabResultResponse(
    string TestName,
    string? ResultValue,
    string? Unit,
    string? ReferenceRange,
    DateTimeOffset PerformedAtUtc);

/// <summary>A synthetic person available in the local demonstration source database.</summary>
public sealed record DemoPersonResponse(string NationalCode, string FullName);
