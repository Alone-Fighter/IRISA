using IRISA.Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IRISA.Backend.Data;

public static class DemoDataSeeder
{
    private static readonly DateTimeOffset SampleDate =
        new(2025, 10, 20, 8, 30, 0, TimeSpan.Zero);

    public static async Task SeedSourceAsync(
        SourceDataContext context,
        CancellationToken cancellationToken = default)
    {
        var people = new[]
        {
            new SourcePerson
            {
                Id = Guid.Parse("a1111111-1111-4111-8111-111111111111"),
                NationalCode = "1111111111",
                FullName = "سارا احمدی",
                BirthDate = new DateOnly(1375, 4, 12),
                Gender = "زن",
                UpdatedAtUtc = SampleDate,
                LabResults =
                [
                    new SourceLabResult
                    {
                        Id = Guid.Parse("b1111111-1111-4111-8111-111111111111"),
                        TestName = "قند خون ناشتا (FBS)",
                        ResultValue = "92",
                        Unit = "mg/dL",
                        ReferenceRange = "70 - 99",
                        PerformedAtUtc = SampleDate.AddDays(-12)
                    },
                    new SourceLabResult
                    {
                        Id = Guid.Parse("b1111111-1111-4111-8111-111111111112"),
                        TestName = "کلسترول کل (Total Cholesterol)",
                        ResultValue = "176",
                        Unit = "mg/dL",
                        ReferenceRange = "کمتر از 200",
                        PerformedAtUtc = SampleDate.AddDays(-12)
                    },
                    new SourceLabResult
                    {
                        Id = Guid.Parse("b1111111-1111-4111-8111-111111111113"),
                        TestName = "HDL",
                        ResultValue = "49",
                        Unit = "mg/dL",
                        ReferenceRange = "35 - 80",
                        PerformedAtUtc = SampleDate.AddDays(-12)
                    },
                    new SourceLabResult
                    {
                        Id = Guid.Parse("b1111111-1111-4111-8111-111111111114"),
                        TestName = "ALT",
                        ResultValue = "24",
                        Unit = "U/L",
                        ReferenceRange = "7 - 56",
                        PerformedAtUtc = SampleDate.AddDays(-8)
                    }
                ]
            },
            new SourcePerson
            {
                Id = Guid.Parse("a2222222-2222-4222-8222-222222222222"),
                NationalCode = "2222222222",
                FullName = "علی رضایی",
                BirthDate = new DateOnly(1368, 9, 3),
                Gender = "مرد",
                UpdatedAtUtc = SampleDate.AddDays(1),
                LabResults =
                [
                    new SourceLabResult
                    {
                        Id = Guid.Parse("b2222222-2222-4222-8222-222222222221"),
                        TestName = "هموگلوبین (Hemoglobin)",
                        ResultValue = "13.8",
                        Unit = "g/dL",
                        ReferenceRange = "12 - 16",
                        PerformedAtUtc = SampleDate.AddDays(-4)
                    },
                    new SourceLabResult
                    {
                        Id = Guid.Parse("b2222222-2222-4222-8222-222222222222"),
                        TestName = "HbA1c",
                        ResultValue = "6.1",
                        Unit = "%",
                        ReferenceRange = "< 6.5",
                        PerformedAtUtc = SampleDate.AddDays(-18)
                    },
                    new SourceLabResult
                    {
                        Id = Guid.Parse("b2222222-2222-4222-8222-222222222223"),
                        TestName = "کراتینین",
                        ResultValue = "0.9",
                        Unit = "mg/dL",
                        ReferenceRange = "0.7 - 1.2",
                        PerformedAtUtc = SampleDate.AddDays(-18)
                    }
                ]
            },
            new SourcePerson
            {
                Id = Guid.Parse("a3333333-3333-4333-8333-333333333333"),
                NationalCode = "3333333333",
                FullName = "مریم نوری",
                BirthDate = new DateOnly(1381, 2, 25),
                Gender = "زن",
                UpdatedAtUtc = SampleDate.AddDays(2),
                LabResults =
                [
                    new SourceLabResult
                    {
                        Id = Guid.Parse("b3333333-3333-4333-8333-333333333331"),
                        TestName = "TSH",
                        ResultValue = "2.4",
                        Unit = "mIU/L",
                        ReferenceRange = "0.5 - 4.5",
                        PerformedAtUtc = SampleDate.AddDays(-6)
                    },
                    new SourceLabResult
                    {
                        Id = Guid.Parse("b3333333-3333-4333-8333-333333333332"),
                        TestName = "ویتامین D",
                        ResultValue = "36",
                        Unit = "ng/mL",
                        ReferenceRange = "30 - 80",
                        PerformedAtUtc = SampleDate.AddDays(-14)
                    },
                    new SourceLabResult
                    {
                        Id = Guid.Parse("b3333333-3333-4333-8333-333333333333"),
                        TestName = "کمپلکس کامل خون (CBC)",
                        ResultValue = "در محدوده طبیعی",
                        Unit = "-",
                        ReferenceRange = "طبیعی",
                        PerformedAtUtc = SampleDate.AddDays(-21)
                    }
                ]
            }
        };

        var existingPeople = await context.People
            .Include(person => person.LabResults)
            .ToDictionaryAsync(person => person.NationalCode, cancellationToken);

        foreach (var seededPerson in people)
        {
            if (!existingPeople.TryGetValue(seededPerson.NationalCode, out var existingPerson))
            {
                context.People.Add(seededPerson);
                continue;
            }

            existingPerson.FullName = seededPerson.FullName;
            existingPerson.BirthDate = seededPerson.BirthDate;
            existingPerson.Gender = seededPerson.Gender;
            existingPerson.UpdatedAtUtc = seededPerson.UpdatedAtUtc;

            var seededResultIds = seededPerson.LabResults
                .Select(result => result.Id)
                .ToHashSet();
            context.LabResults.RemoveRange(
                existingPerson.LabResults.Where(result => !seededResultIds.Contains(result.Id)));

            var existingResults = existingPerson.LabResults.ToDictionary(result => result.Id);
            foreach (var seededResult in seededPerson.LabResults)
            {
                if (existingResults.TryGetValue(seededResult.Id, out var existingResult))
                {
                    existingResult.TestName = seededResult.TestName;
                    existingResult.ResultValue = seededResult.ResultValue;
                    existingResult.Unit = seededResult.Unit;
                    existingResult.ReferenceRange = seededResult.ReferenceRange;
                    existingResult.PerformedAtUtc = seededResult.PerformedAtUtc;
                }
                else
                {
                    existingPerson.LabResults.Add(seededResult);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public static async Task SeedDevelopmentUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        var email = configuration["DemoData:AdminEmail"];
        var password = configuration["DemoData:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Development demo user was not created because DemoData credentials are not configured.");
            return;
        }

        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException(
                $"Could not create the configured development demo user: {errors}");
        }

        logger.LogInformation("Created the configured development demo user.");
    }
}
