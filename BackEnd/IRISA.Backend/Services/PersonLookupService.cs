using IRISA.Backend.Data;
using IRISA.Backend.Dtos;
using IRISA.Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace IRISA.Backend.Services;

public sealed class PersonLookupService(
    SourceDataContext sourceContext,
    ApplicationDbContext applicationContext,
    IConfiguration configuration,
    ILogger<PersonLookupService> logger) : IPersonLookupService
{
    public async Task<PersonLookupResult> FindAsync(
        string nationalCode,
        CancellationToken cancellationToken)
    {
        if (configuration.GetValue<bool>("DemoData:SimulateSourceUnavailable"))
        {
            logger.LogWarning("Demo mode is simulating an unavailable source database.");
            return await FindFromCacheAsync(nationalCode, cancellationToken);
        }

        try
        {
            var sourcePerson = await sourceContext.People
                .AsNoTracking()
                .Include(person => person.LabResults)
                .SingleOrDefaultAsync(
                    person => person.NationalCode == nationalCode,
                    cancellationToken);

            if (sourcePerson is null)
                return new PersonLookupResult(null, SourceUnavailable: false);

            var snapshot = await applicationContext.PersonSnapshots
                .Include(person => person.LabResults)
                .SingleOrDefaultAsync(
                    person => person.NationalCode == nationalCode,
                    cancellationToken);

            var now = DateTimeOffset.UtcNow;
            if (snapshot is null)
            {
                snapshot = new PersonSnapshot
                {
                    NationalCode = sourcePerson.NationalCode,
                    FullName = sourcePerson.FullName,
                    BirthDate = sourcePerson.BirthDate,
                    Gender = sourcePerson.Gender,
                    LastSyncedAtUtc = now,
                    SourceUpdatedAtUtc = sourcePerson.UpdatedAtUtc
                };
                applicationContext.PersonSnapshots.Add(snapshot);
            }
            else
            {
                snapshot.FullName = sourcePerson.FullName;
                snapshot.BirthDate = sourcePerson.BirthDate;
                snapshot.Gender = sourcePerson.Gender;
                snapshot.LastSyncedAtUtc = now;
                snapshot.SourceUpdatedAtUtc = sourcePerson.UpdatedAtUtc;
                applicationContext.LabResultSnapshots.RemoveRange(snapshot.LabResults);
                snapshot.LabResults.Clear();
            }

            foreach (var result in sourcePerson.LabResults)
            {
                snapshot.LabResults.Add(new LabResultSnapshot
                {
                    SourceRecordId = result.Id.ToString(),
                    TestName = result.TestName,
                    ResultValue = result.ResultValue,
                    Unit = result.Unit,
                    ReferenceRange = result.ReferenceRange,
                    PerformedAtUtc = result.PerformedAtUtc,
                    RetrievedAtUtc = now
                });
            }

            await applicationContext.SaveChangesAsync(cancellationToken);
            return new PersonLookupResult(Map(snapshot, isStale: false), SourceUnavailable: false);
        }
        catch (DbException exception)
        {
            logger.LogWarning(
                exception,
                "Source database lookup failed; trying the local snapshot.");
            return await FindFromCacheAsync(nationalCode, cancellationToken);
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(
                exception,
                "Source database lookup timed out; trying the local snapshot.");
            return await FindFromCacheAsync(nationalCode, cancellationToken);
        }
    }

    public async Task<PersonLookupResult> RefreshFromSourceAsync(
        string nationalCode,
        CancellationToken cancellationToken)
    {
        if (configuration.GetValue<bool>("DemoData:SimulateSourceUnavailable"))
        {
            logger.LogWarning("Demo mode is simulating an unavailable source database during refresh.");
            return await FindFromCacheAsync(nationalCode, cancellationToken);
        }

        try
        {
            var sourcePerson = await sourceContext.People
                .AsNoTracking()
                .Include(person => person.LabResults)
                .SingleOrDefaultAsync(
                    person => person.NationalCode == nationalCode,
                    cancellationToken);

            if (sourcePerson is null)
                return new PersonLookupResult(null, SourceUnavailable: false);

            var snapshot = await applicationContext.PersonSnapshots
                .Include(person => person.LabResults)
                .SingleOrDefaultAsync(
                    person => person.NationalCode == nationalCode,
                    cancellationToken) ?? new PersonSnapshot
                {
                    NationalCode = sourcePerson.NationalCode,
                    FullName = sourcePerson.FullName,
                    BirthDate = sourcePerson.BirthDate,
                    Gender = sourcePerson.Gender,
                    LastSyncedAtUtc = DateTimeOffset.UtcNow,
                    SourceUpdatedAtUtc = sourcePerson.UpdatedAtUtc
                };

            snapshot.FullName = sourcePerson.FullName;
            snapshot.BirthDate = sourcePerson.BirthDate;
            snapshot.Gender = sourcePerson.Gender;
            snapshot.SourceUpdatedAtUtc = sourcePerson.UpdatedAtUtc;
            snapshot.LastSyncedAtUtc = DateTimeOffset.UtcNow;

            if (snapshot.Id == Guid.Empty)
            {
                applicationContext.PersonSnapshots.Add(snapshot);
            }
            else
            {
                applicationContext.LabResultSnapshots.RemoveRange(snapshot.LabResults);
                snapshot.LabResults.Clear();
            }

            foreach (var result in sourcePerson.LabResults)
            {
                snapshot.LabResults.Add(new LabResultSnapshot
                {
                    SourceRecordId = result.Id.ToString(),
                    TestName = result.TestName,
                    ResultValue = result.ResultValue,
                    Unit = result.Unit,
                    ReferenceRange = result.ReferenceRange,
                    PerformedAtUtc = result.PerformedAtUtc,
                    RetrievedAtUtc = DateTimeOffset.UtcNow
                });
            }

            await applicationContext.SaveChangesAsync(cancellationToken);
            return new PersonLookupResult(Map(snapshot, isStale: false), SourceUnavailable: false);
        }
        catch (DbException exception)
        {
            logger.LogWarning(exception, "Source refresh failed while trying to reconnect to the source database.");
            return await FindFromCacheAsync(nationalCode, cancellationToken);
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Source refresh timed out while trying to reconnect to the source database.");
            return await FindFromCacheAsync(nationalCode, cancellationToken);
        }
    }

    private async Task<PersonLookupResult> FindFromCacheAsync(
        string nationalCode,
        CancellationToken cancellationToken)
    {
        var snapshot = await applicationContext.PersonSnapshots
            .AsNoTracking()
            .Include(person => person.LabResults)
            .SingleOrDefaultAsync(
                person => person.NationalCode == nationalCode,
                cancellationToken);

        return snapshot is null
            ? new PersonLookupResult(null, SourceUnavailable: true)
            : new PersonLookupResult(Map(snapshot, isStale: true), SourceUnavailable: true);
    }

    private static PersonLookupResponse Map(PersonSnapshot person, bool isStale)
    {
        return new PersonLookupResponse(
            person.NationalCode,
            person.FullName,
            person.BirthDate,
            person.Gender,
            person.LastSyncedAtUtc,
            isStale,
            isStale ? "cache" : "source",
            person.LabResults
                .OrderByDescending(result => result.PerformedAtUtc)
                .Select(result => new LabResultResponse(
                    result.TestName,
                    result.ResultValue,
                    result.Unit,
                    result.ReferenceRange,
                    result.PerformedAtUtc ?? result.RetrievedAtUtc))
                .ToArray());
    }
}
