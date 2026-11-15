using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;
using Vinamilk.HRM.Application.Common.Security;

#nullable disable

namespace Vinamilk.HRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncRbacRolePermissionsMatrix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (!ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("IF EXISTS (SELECT 1 FROM [identity].[Roles] WHERE [IsDeleted] = 0)");
            sb.AppendLine("BEGIN");
            sb.AppendLine("    DECLARE @NowUtc DATETIME2 = SYSUTCDATETIME();");
            sb.AppendLine("    DECLARE @AddedPerms INT = 0;");
            sb.AppendLine("    DECLARE @RemovedRolePerms INT = 0;");
            sb.AppendLine("    DECLARE @AddedRolePerms INT = 0;");
            sb.AppendLine();

            foreach (var perm in RbacPolicyMatrix.BuildPermissionCatalog())
            {
                var code = EscapeSql(perm.PermissionCode);
                var mod = EscapeSql(perm.ModuleName);
                var act = EscapeSql(perm.ActionName);
                var disp = EscapeSql(perm.DisplayName);
                sb.AppendLine($@"    IF NOT EXISTS (SELECT 1 FROM [identity].[Permissions] WHERE [PermissionCode] = '{code}' AND [IsDeleted] = 0)
    BEGIN
        INSERT INTO [identity].[Permissions] ([PermissionCode], [ModuleName], [ActionName], [DisplayName], [SortOrder], [CreatedAt], [CreatedBy], [IsDeleted])
        VALUES ('{code}', '{mod}', '{act}', N'{disp}', {perm.SortOrder}, @NowUtc, 'EF_MIGRATION_RBAC', 0);
        SET @AddedPerms += 1;
    END");
            }

            sb.AppendLine();
            sb.AppendLine("    CREATE TABLE #ExpectedRolePerm ([RoleCode] varchar(50) NOT NULL, [PermissionCode] varchar(100) NOT NULL);");

            foreach (var role in RbacPolicyMatrix.RoleDefinitions)
            {
                var rCode = EscapeSql(role.RoleCode);
                var dScope = EscapeSql(role.DefaultDataScope);
                sb.AppendLine($@"    UPDATE [identity].[Roles]
    SET [DefaultDataScope] = '{dScope}', [UpdatedAt] = @NowUtc, [UpdatedBy] = 'EF_MIGRATION_RBAC'
    WHERE [RoleCode] = '{rCode}' AND [IsDeleted] = 0 AND [DefaultDataScope] <> '{dScope}';");

                foreach (var pCode in role.PermissionCodes)
                {
                    sb.AppendLine($"    INSERT INTO #ExpectedRolePerm ([RoleCode], [PermissionCode]) VALUES ('{rCode}', '{EscapeSql(pCode)}');");
                }
            }

            sb.AppendLine();
            sb.AppendLine(@"    UPDATE rp
    SET rp.[IsDeleted] = 1, rp.[UpdatedAt] = @NowUtc, rp.[UpdatedBy] = 'EF_MIGRATION_RBAC'
    FROM [identity].[RolePermissions] rp
    INNER JOIN [identity].[Roles] r ON rp.[RoleId] = r.[Id] AND r.[IsDeleted] = 0
    INNER JOIN [identity].[Permissions] p ON rp.[PermissionId] = p.[Id] AND p.[IsDeleted] = 0
    WHERE rp.[IsDeleted] = 0
      AND r.[RoleCode] IN ('SUPER_ADMIN','HR_MANAGER','HR_STAFF','ACCOUNTANT','LINE_MANAGER','EMPLOYEE')
      AND NOT EXISTS (
          SELECT 1 FROM #ExpectedRolePerm e
          WHERE e.[RoleCode] = r.[RoleCode] AND e.[PermissionCode] = p.[PermissionCode]
      );
    SET @RemovedRolePerms = @@ROWCOUNT;

    INSERT INTO [identity].[RolePermissions] ([RoleId], [PermissionId], [CreatedAt], [CreatedBy], [IsDeleted])
    SELECT r.[Id], p.[Id], @NowUtc, 'EF_MIGRATION_RBAC', 0
    FROM #ExpectedRolePerm e
    INNER JOIN [identity].[Roles] r ON r.[RoleCode] = e.[RoleCode] AND r.[IsDeleted] = 0
    INNER JOIN [identity].[Permissions] p ON p.[PermissionCode] = e.[PermissionCode] AND p.[IsDeleted] = 0
    WHERE NOT EXISTS (
        SELECT 1 FROM [identity].[RolePermissions] rp
        WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id] AND rp.[IsDeleted] = 0
    );
    SET @AddedRolePerms = @@ROWCOUNT;

    DROP TABLE #ExpectedRolePerm;

    IF (@AddedPerms > 0 OR @RemovedRolePerms > 0 OR @AddedRolePerms > 0)
    BEGIN
        INSERT INTO [system].[AuditLogs] ([Username], [ModuleName], [ActionType], [EntityName], [EntityId], [NewValuesJson], [IpAddress], [UserAgent], [CreatedAt], [CreatedBy], [IsDeleted])
        VALUES (
            'system_job',
            'System',
            'SyncRbacMatrix',
            'RolePermission',
            'SyncRbacRolePermissionsMatrix',
            CONCAT(N'{""AddedPermissions"":', @AddedPerms, N',""SoftDeletedRolePermissions"":', @RemovedRolePerms, N',""AddedRolePermissions"":', @AddedRolePerms, N'}'),
            '127.0.0.1',
            'EF Core Migration 20261115220301_SyncRbacRolePermissionsMatrix',
            @NowUtc,
            'EF_MIGRATION_RBAC',
            0
        );
    END
END");

            migrationBuilder.Sql(sb.ToString());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data migration không hoàn nguyên quyền bảo mật lỗi thời.
        }

        private static string EscapeSql(string value) => value.Replace("'", "''");
    }
}
