using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Salaries;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class SalaryProposalConfiguration : IEntityTypeConfiguration<SalaryProposal>
{
    public void Configure(EntityTypeBuilder<SalaryProposal> builder)
    {
        builder.ToTable("SalaryProposals", b => b.IsTemporal());

        builder.HasKey(s => s.Id);

        builder.Property(s => s.CurrentSalary)
            .HasPrecision(18, 2);

        builder.Property(s => s.ProposedSalary)
            .HasPrecision(18, 2);

        builder.Property(s => s.CurrentAllowance)
            .HasPrecision(18, 2);

        builder.Property(s => s.ProposedAllowance)
            .HasPrecision(18, 2);

        builder.Property(s => s.Reason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(s => s.DepartmentHeadNotes)
            .HasMaxLength(500);

        builder.Property(s => s.HRDirectorNotes)
            .HasMaxLength(500);

        builder.Property(s => s.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.UpdatedBy)
            .HasMaxLength(100);

        builder.HasOne(s => s.Employee)
            .WithMany(e => e.SalaryProposals)
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ProposedBy)
            .WithMany()
            .HasForeignKey(s => s.ProposedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
