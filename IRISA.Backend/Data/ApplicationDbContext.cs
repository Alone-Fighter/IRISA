using IRISA.Backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IRISA.Backend.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<PersonSnapshot> PersonSnapshots => Set<PersonSnapshot>();

    public DbSet<LabResultSnapshot> LabResultSnapshots => Set<LabResultSnapshot>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<PersonSnapshot>(entity =>
        {
            entity.HasIndex(person => person.NationalCode)
                .IsUnique();

            entity.Property(person => person.NationalCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(person => person.FullName)
                .HasMaxLength(200)
                .IsRequired();
        });

        builder.Entity<LabResultSnapshot>(entity =>
        {
            entity.HasIndex(result => new { result.PersonSnapshotId, result.PerformedAtUtc });

            entity.Property(result => result.SourceRecordId)
                .HasMaxLength(100);

            entity.Property(result => result.TestName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(result => result.ResultValue)
                .HasMaxLength(2000);

            entity.Property(result => result.Unit)
                .HasMaxLength(50);

            entity.Property(result => result.ReferenceRange)
                .HasMaxLength(200);

            entity.HasOne(result => result.PersonSnapshot)
                .WithMany(person => person.LabResults)
                .HasForeignKey(result => result.PersonSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
