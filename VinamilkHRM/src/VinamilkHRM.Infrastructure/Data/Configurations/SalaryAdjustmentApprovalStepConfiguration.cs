using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Salaries;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class SalaryAdjustmentApprovalStepConfiguration : IEntityTypeConfiguration<SalaryAdjustmentApprovalStep>
{
    public void Configure(EntityTypeBuilder<SalaryAdjustmentApprovalStep> builder)
    {
        builder.ToTable("SalaryAdjustmentApprovalSteps");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.RoleName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Comment).HasMaxLength(500);

        builder.HasOne(s => s.SalaryProposal)
            .WithMany(p => p.ApprovalSteps)
            .HasForeignKey(s => s.SalaryProposalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Approver)
            .WithMany()
            .HasForeignKey(s => s.ApproverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}