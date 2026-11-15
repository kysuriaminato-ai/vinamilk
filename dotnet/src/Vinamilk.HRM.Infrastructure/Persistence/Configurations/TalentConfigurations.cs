using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vinamilk.HRM.Domain.Entities.Talent;

namespace Vinamilk.HRM.Infrastructure.Persistence.Configurations;

public class RecruitmentRequestConfiguration : IEntityTypeConfiguration<RecruitmentRequest>
{
    public void Configure(EntityTypeBuilder<RecruitmentRequest> builder)
    {
        builder.ConfigureBaseAuditable<RecruitmentRequest, int>("RecruitmentRequests", "talent");

        builder.Property(x => x.RequestCode).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.EmploymentType).HasColumnType("varchar(30)").HasDefaultValue("FullTime");
        builder.Property(x => x.ReasonType).HasColumnType("varchar(30)").HasDefaultValue("NewHeadcount");
        builder.Property(x => x.BudgetMinSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.BudgetMaxSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Approved");

        builder.HasIndex(x => x.RequestCode)
            .IsUnique()
            .HasDatabaseName("UQ_RecruitmentRequests_RequestCode")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.OrganizationUnit)
            .WithMany()
            .HasForeignKey(x => x.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Position)
            .WithMany()
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class JobPostingConfiguration : IEntityTypeConfiguration<JobPosting>
{
    public void Configure(EntityTypeBuilder<JobPosting> builder)
    {
        builder.ConfigureBaseAuditable<JobPosting, int>("JobPostings", "talent");

        builder.Property(x => x.PostingCode).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.Title).HasMaxLength(250).IsRequired();
        builder.Property(x => x.SalaryRangeText).HasMaxLength(100);
        builder.Property(x => x.WorkLocation).HasMaxLength(200);
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Open");

        builder.HasIndex(x => x.PostingCode)
            .IsUnique()
            .HasDatabaseName("UQ_JobPostings_PostingCode")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.RecruitmentRequest)
            .WithMany(r => r.JobPostings)
            .HasForeignKey(x => x.RecruitmentRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OrganizationUnit)
            .WithMany()
            .HasForeignKey(x => x.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Position)
            .WithMany()
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.ConfigureBaseAuditable<Candidate, int>("Candidates", "talent");

        builder.Property(x => x.CandidateCode).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Gender).HasMaxLength(10);
        builder.Property(x => x.PhoneNumber).HasColumnType("varchar(20)").IsRequired();
        builder.Property(x => x.Email).HasColumnType("varchar(150)").IsRequired();
        builder.Property(x => x.Major).HasMaxLength(150);
        builder.Property(x => x.YearsOfExperience).HasColumnType("decimal(4,1)");
        builder.Property(x => x.ExpectedSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SourceChannel).HasColumnType("varchar(50)");
        builder.Property(x => x.ResumeUrl).HasMaxLength(300);
        builder.Property(x => x.Stage).HasColumnType("varchar(30)").HasDefaultValue("Applied");
        builder.Property(x => x.OverallScore).HasColumnType("decimal(5,2)");

        builder.HasIndex(x => x.CandidateCode)
            .IsUnique()
            .HasDatabaseName("UQ_Candidates_CandidateCode")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.JobPostingId, x.Stage })
            .HasDatabaseName("IX_Candidates_Posting_Stage");

        builder.HasOne(x => x.JobPosting)
            .WithMany(p => p.Candidates)
            .HasForeignKey(x => x.JobPostingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.EducationLevel)
            .WithMany()
            .HasForeignKey(x => x.EducationLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ConvertedEmployee)
            .WithMany()
            .HasForeignKey(x => x.ConvertedEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class InterviewConfiguration : IEntityTypeConfiguration<Interview>
{
    public void Configure(EntityTypeBuilder<Interview> builder)
    {
        builder.ConfigureBaseAuditable<Interview, int>("Interviews", "talent");

        builder.Property(x => x.RoundTitle).HasMaxLength(150).IsRequired();
        builder.Property(x => x.InterviewMode).HasColumnType("varchar(30)").HasDefaultValue("InPerson");
        builder.Property(x => x.LocationOrLink).HasMaxLength(300);
        builder.Property(x => x.TechnicalScore).HasColumnType("decimal(5,2)");
        builder.Property(x => x.CultureFitScore).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalScore).HasColumnType("decimal(5,2)");
        builder.Property(x => x.EvaluationNotes).HasMaxLength(1000);
        builder.Property(x => x.Result).HasColumnType("varchar(30)").HasDefaultValue("Pending");

        builder.HasIndex(x => x.CandidateId).HasDatabaseName("IX_Interviews_CandidateId");

        builder.HasOne(x => x.Candidate)
            .WithMany(c => c.Interviews)
            .HasForeignKey(x => x.CandidateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Interviewer)
            .WithMany()
            .HasForeignKey(x => x.InterviewerEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class JobOfferConfiguration : IEntityTypeConfiguration<JobOffer>
{
    public void Configure(EntityTypeBuilder<JobOffer> builder)
    {
        builder.ConfigureBaseAuditable<JobOffer, int>("JobOffers", "talent");

        builder.Property(x => x.OfferCode).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.OfferedProbationSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.OfferedOfficialSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Sent");
        builder.Property(x => x.CandidateFeedback).HasMaxLength(500);

        builder.HasIndex(x => x.OfferCode)
            .IsUnique()
            .HasDatabaseName("UQ_JobOffers_OfferCode")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.Candidate)
            .WithMany(c => c.Offers)
            .HasForeignKey(x => x.CandidateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProposedPosition)
            .WithMany()
            .HasForeignKey(x => x.ProposedPositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProposedUnit)
            .WithMany()
            .HasForeignKey(x => x.ProposedUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class OnboardingTemplateConfiguration : IEntityTypeConfiguration<OnboardingTemplate>
{
    public void Configure(EntityTypeBuilder<OnboardingTemplate> builder)
    {
        builder.ConfigureBaseAuditable<OnboardingTemplate, int>("OnboardingTemplates", "talent");

        builder.Property(x => x.TemplateCode).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.TaskName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ProcessType).HasColumnType("varchar(20)").HasDefaultValue("Onboarding");
        builder.Property(x => x.AssignedDepartmentRole).HasColumnType("varchar(30)").HasDefaultValue("HR");
    }
}

public class EmployeeOnboardingTaskConfiguration : IEntityTypeConfiguration<EmployeeOnboardingTask>
{
    public void Configure(EntityTypeBuilder<EmployeeOnboardingTask> builder)
    {
        builder.ConfigureBaseAuditable<EmployeeOnboardingTask, int>("EmployeeOnboardingTasks", "talent");

        builder.Property(x => x.ProcessType).HasColumnType("varchar(20)").HasDefaultValue("Onboarding");
        builder.Property(x => x.TaskName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => new { x.EmployeeId, x.ProcessType })
            .HasDatabaseName("IX_EmployeeOnboardingTasks_Emp_Type");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OnboardingTemplate)
            .WithMany()
            .HasForeignKey(x => x.OnboardingTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ConfigureBaseAuditable<Asset, int>("Assets", "talent");

        builder.Property(x => x.AssetCode).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.AssetName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AssetCategory).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.SerialNumber).HasColumnType("varchar(100)");
        builder.Property(x => x.PurchaseValue).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Available");

        builder.HasIndex(x => x.AssetCode)
            .IsUnique()
            .HasDatabaseName("UQ_Assets_AssetCode")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class EmployeeAssetAssignmentConfiguration : IEntityTypeConfiguration<EmployeeAssetAssignment>
{
    public void Configure(EntityTypeBuilder<EmployeeAssetAssignment> builder)
    {
        builder.ConfigureBaseAuditable<EmployeeAssetAssignment, int>("EmployeeAssetAssignments", "talent");

        builder.Property(x => x.ConditionOnAssign).HasMaxLength(200);
        builder.Property(x => x.ConditionOnReturn).HasMaxLength(200);
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Active");

        builder.HasIndex(x => new { x.EmployeeId, x.Status })
            .HasDatabaseName("IX_EmployeeAssetAssignments_Emp_Status");

        builder.HasOne(x => x.Asset)
            .WithMany(a => a.Assignments)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TrainingCourseConfiguration : IEntityTypeConfiguration<TrainingCourse>
{
    public void Configure(EntityTypeBuilder<TrainingCourse> builder)
    {
        builder.ConfigureBaseAuditable<TrainingCourse, int>("TrainingCourses", "talent");

        builder.Property(x => x.CourseCode).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.CourseName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CourseCategory).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.StandardCostPerPerson).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Description).HasMaxLength(1000);

        builder.HasIndex(x => x.CourseCode)
            .IsUnique()
            .HasDatabaseName("UQ_TrainingCourses_CourseCode")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class TrainingClassConfiguration : IEntityTypeConfiguration<TrainingClass>
{
    public void Configure(EntityTypeBuilder<TrainingClass> builder)
    {
        builder.ConfigureBaseAuditable<TrainingClass, int>("TrainingClasses", "talent");

        builder.Property(x => x.ClassCode).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.ClassName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.InstructorName).HasMaxLength(150);
        builder.Property(x => x.Location).HasMaxLength(200);
        builder.Property(x => x.TotalActualCost).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Completed");

        builder.HasIndex(x => x.ClassCode)
            .IsUnique()
            .HasDatabaseName("UQ_TrainingClasses_ClassCode")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.TrainingCourse)
            .WithMany(c => c.Classes)
            .HasForeignKey(x => x.TrainingCourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TrainingEnrollmentConfiguration : IEntityTypeConfiguration<TrainingEnrollment>
{
    public void Configure(EntityTypeBuilder<TrainingEnrollment> builder)
    {
        builder.ConfigureBaseAuditable<TrainingEnrollment, int>("TrainingEnrollments", "talent");

        builder.Property(x => x.AttendanceRatePercent).HasColumnType("decimal(5,2)");
        builder.Property(x => x.FinalScore).HasColumnType("decimal(5,2)");
        builder.Property(x => x.Result).HasColumnType("varchar(30)").HasDefaultValue("Passed");
        builder.Property(x => x.CertificateNumber).HasMaxLength(100);
        builder.Property(x => x.FeedbackComment).HasMaxLength(500);

        builder.HasIndex(x => new { x.TrainingClassId, x.EmployeeId })
            .IsUnique()
            .HasDatabaseName("UQ_TrainingEnrollments_Class_Employee")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.TrainingClass)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(x => x.TrainingClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EvaluationCycleConfiguration : IEntityTypeConfiguration<EvaluationCycle>
{
    public void Configure(EntityTypeBuilder<EvaluationCycle> builder)
    {
        builder.ConfigureBaseAuditable<EvaluationCycle, int>("EvaluationCycles", "talent");

        builder.Property(x => x.CycleCode).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.CycleName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CycleType).HasColumnType("varchar(20)").IsRequired();
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Closed");

        builder.HasIndex(x => x.CycleCode)
            .IsUnique()
            .HasDatabaseName("UQ_EvaluationCycles_CycleCode")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class EmployeeKpiEvaluationConfiguration : IEntityTypeConfiguration<EmployeeKpiEvaluation>
{
    public void Configure(EntityTypeBuilder<EmployeeKpiEvaluation> builder)
    {
        builder.ConfigureBaseAuditable<EmployeeKpiEvaluation, int>("EmployeeKpiEvaluations", "talent");

        builder.Property(x => x.TotalWeight).HasColumnType("decimal(5,2)");
        builder.Property(x => x.SelfTotalScore).HasColumnType("decimal(5,2)");
        builder.Property(x => x.ManagerTotalScore).HasColumnType("decimal(5,2)");
        builder.Property(x => x.FinalScore).HasColumnType("decimal(5,2)");
        builder.Property(x => x.RatingGrade).HasColumnType("varchar(10)").IsRequired();
        builder.Property(x => x.BonusMultiplier).HasColumnType("decimal(5,2)");
        builder.Property(x => x.EmployeeComment).HasMaxLength(1000);
        builder.Property(x => x.ReviewerComment).HasMaxLength(1000);
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Approved");

        builder.HasIndex(x => new { x.EvaluationCycleId, x.EmployeeId })
            .IsUnique()
            .HasDatabaseName("UQ_EmployeeKpiEvaluations_Cycle_Emp")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.EvaluationCycle)
            .WithMany(c => c.Evaluations)
            .HasForeignKey(x => x.EvaluationCycleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Employee)
            .WithMany(e => e.KpiEvaluations)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Reviewer)
            .WithMany()
            .HasForeignKey(x => x.ReviewerEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeKpiItemConfiguration : IEntityTypeConfiguration<EmployeeKpiItem>
{
    public void Configure(EntityTypeBuilder<EmployeeKpiItem> builder)
    {
        builder.ConfigureBaseAuditable<EmployeeKpiItem, int>("EmployeeKpiItems", "talent");

        builder.Property(x => x.GoalTitle).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Perspective).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.MeasureUnit).HasMaxLength(100);
        builder.Property(x => x.WeightPercent).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TargetValue).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ActualValue).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CompletionRatePercent).HasColumnType("decimal(5,2)");
        builder.Property(x => x.SelfScore).HasColumnType("decimal(5,2)");
        builder.Property(x => x.ManagerScore).HasColumnType("decimal(5,2)");
        builder.Property(x => x.EvidenceNote).HasMaxLength(500);

        builder.HasIndex(x => x.EmployeeKpiEvaluationId)
            .HasDatabaseName("IX_EmployeeKpiItems_EvaluationId");

        builder.HasOne(x => x.Evaluation)
            .WithMany(e => e.Items)
            .HasForeignKey(x => x.EmployeeKpiEvaluationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

