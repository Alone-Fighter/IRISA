using IRISA.Backend.Dtos;

namespace IRISA.Backend.Services;

public interface IPersonLookupService
{
    Task<PersonLookupResult> FindAsync(string nationalCode, CancellationToken cancellationToken);
    Task<PersonLookupResult> RefreshFromSourceAsync(string nationalCode, CancellationToken cancellationToken);
}

public sealed record PersonLookupResult(
    PersonLookupResponse? Person,
    bool SourceUnavailable);
