using IRISA.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace IRISA.Backend.Data;

public sealed class SourceDataContext(DbContextOptions<SourceDataContext> options)
    : DbContext(options)
{
    public DbSet<SourcePerson> People => Set<SourcePerson>();

    public DbSet<SourceLabResult> LabResults => Set<SourceLabResult>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<SourcePerson>(entity =>
        {
            entity.HasIndex(person => person.NationalCode).IsUnique();
            entity.Property(person => person.NationalCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsRequired();
            entity.Property(person => person.FullName)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(person => person.Gender)
                .HasMaxLength(20);
        });

        builder.Entity<SourceLabResult>(entity =>
        {
            entity.Property(result => result.TestName).HasMaxLength(200).IsRequired();
            entity.Property(result => result.ResultValue).HasMaxLength(2000);
            entity.Property(result => result.Unit).HasMaxLength(50);
            entity.Property(result => result.ReferenceRange).HasMaxLength(200);
            entity.HasOne(result => result.SourcePerson)
                .WithMany(person => person.LabResults)
                .HasForeignKey(result => result.SourcePersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
