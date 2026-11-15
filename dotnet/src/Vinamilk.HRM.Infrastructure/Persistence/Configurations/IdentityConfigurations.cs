using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vinamilk.HRM.Domain.Entities.Identity;

namespace Vinamilk.HRM.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ConfigureBaseAuditable<User, int>("Users", "identity");

        builder.Property(x => x.Username).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.Email).HasMaxLength(150).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.TwoFactorSecret).HasMaxLength(256);

        builder.HasIndex(x => x.Username)
            .IsUnique()
            .HasDatabaseName("UQ_Users_Username")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName("UQ_Users_Email")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.EmployeeId)
            .IsUnique()
            .HasDatabaseName("UQ_Users_EmployeeId")
            .HasFilter("[EmployeeId] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasOne(x => x.Employee)
            .WithOne(e => e.UserAccount)
            .HasForeignKey<User>(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ConfigureBaseAuditable<Role, int>("Roles", "identity");

        builder.Property(x => x.RoleCode).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.RoleName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.DefaultDataScope).HasColumnType("varchar(30)").HasDefaultValue("SelfOnly");

        builder.HasIndex(x => x.RoleCode)
            .IsUnique()
            .HasDatabaseName("UQ_Roles_RoleCode")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ConfigureBaseAuditable<Permission, int>("Permissions", "identity");

        builder.Property(x => x.PermissionCode).HasColumnType("varchar(100)").IsRequired();
        builder.Property(x => x.ModuleName).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.ActionName).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();

        builder.HasIndex(x => x.PermissionCode)
            .IsUnique()
            .HasDatabaseName("UQ_Permissions_PermissionCode")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ConfigureBaseAuditable<UserRole, int>("UserRoles", "identity");

        builder.HasIndex(x => new { x.UserId, x.RoleId })
            .IsUnique()
            .HasDatabaseName("UQ_UserRoles_User_Role")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ConfigureBaseAuditable<RolePermission, int>("RolePermissions", "identity");

        builder.HasIndex(x => new { x.RoleId, x.PermissionId })
            .IsUnique()
            .HasDatabaseName("UQ_RolePermissions_Role_Perm")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class UserDataScopeConfiguration : IEntityTypeConfiguration<UserDataScope>
{
    public void Configure(EntityTypeBuilder<UserDataScope> builder)
    {
        builder.ConfigureBaseAuditable<UserDataScope, int>("UserDataScopes", "identity");

        builder.HasIndex(x => new { x.UserId, x.OrganizationUnitId })
            .IsUnique()
            .HasDatabaseName("UQ_UserDataScopes_User_Unit")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.User)
            .WithMany(u => u.DataScopes)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OrganizationUnit)
            .WithMany()
            .HasForeignKey(x => x.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ConfigureBaseAuditable<UserSession, long>("UserSessions", "identity");

        builder.Property(x => x.RefreshTokenHash).HasColumnType("varchar(500)").IsRequired();
        builder.Property(x => x.IpAddress).HasColumnType("varchar(64)");
        builder.Property(x => x.UserAgent).HasMaxLength(500);

        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_UserSessions_UserId");

        builder.HasOne(x => x.User)
            .WithMany(u => u.Sessions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DataPrivacyConsentConfiguration : IEntityTypeConfiguration<DataPrivacyConsent>
{
    public void Configure(EntityTypeBuilder<DataPrivacyConsent> builder)
    {
        builder.ConfigureBaseAuditable<DataPrivacyConsent, int>("DataPrivacyConsents", "identity");

        builder.Property(x => x.PolicyVersion).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.IpAddress).HasColumnType("varchar(64)");
        builder.Property(x => x.ConsentNote).HasMaxLength(500);

        builder.HasIndex(x => new { x.EmployeeId, x.PolicyVersion })
            .IsUnique()
            .HasDatabaseName("UQ_DataPrivacyConsents_Emp_Version")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

