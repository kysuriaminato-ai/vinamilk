using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vinamilk.HRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceDataSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "EditReason",
                schema: "time",
                table: "DailyAttendances",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LocationAddress",
                schema: "time",
                table: "AttendanceLogs",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientDeviceFingerprint",
                schema: "time",
                table: "AttendanceLogs",
                type: "varchar(120)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DistanceMeters",
                schema: "time",
                table: "AttendanceLogs",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EnteredByUserId",
                schema: "time",
                table: "AttendanceLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnteredByUsername",
                schema: "time",
                table: "AttendanceLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalLogKey",
                schema: "time",
                table: "AttendanceLogs",
                type: "varchar(140)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeofenceLocationCode",
                schema: "time",
                table: "AttendanceLogs",
                type: "varchar(40)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeofenceStatus",
                schema: "time",
                table: "AttendanceLogs",
                type: "varchar(40)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                schema: "time",
                table: "AttendanceLogs",
                type: "varchar(64)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsManualOverride",
                schema: "time",
                table: "AttendanceLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ManualReason",
                schema: "time",
                table: "AttendanceLogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                schema: "time",
                table: "AttendanceLogs",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttendanceDevices",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ConnectorType = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "FileExport"),
                    LocationName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    EndpointOrFolderPath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SyncIntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSyncSummary = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDeviceBindings",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "varchar(120)", nullable: false),
                    DeviceLabel = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    LastIpAddress = table.Column<string>(type: "varchar(64)", nullable: true),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDeviceBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDeviceBindings_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDeviceMappings",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    DeviceCode = table.Column<string>(type: "varchar(50)", nullable: true),
                    DeviceUserCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDeviceMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDeviceMappings_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GeofenceLocations",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationCode = table.Column<string>(type: "varchar(40)", nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", nullable: false),
                    AllowedRadiusMeters = table.Column<double>(type: "float", nullable: false),
                    OutsideGeofenceAction = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "PendingApproval"),
                    QrRotationSeconds = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeofenceLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnmappedAttendanceLogs",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    DeviceUserCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    PunchTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PunchType = table.Column<string>(type: "varchar(20)", nullable: false),
                    RawPayload = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "PendingMapping"),
                    ResolvedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnmappedAttendanceLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_AttendanceLogs_ExternalLogKey",
                schema: "time",
                table: "AttendanceLogs",
                column: "ExternalLogKey",
                unique: true,
                filter: "[ExternalLogKey] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_AttendanceDevices_Code",
                schema: "time",
                table: "AttendanceDevices",
                column: "DeviceCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDeviceBindings_Device",
                schema: "time",
                table: "EmployeeDeviceBindings",
                columns: new[] { "DeviceFingerprint", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDeviceBindings_EmployeeId",
                schema: "time",
                table: "EmployeeDeviceBindings",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDeviceMappings_DeviceUser",
                schema: "time",
                table: "EmployeeDeviceMappings",
                columns: new[] { "DeviceCode", "DeviceUserCode" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDeviceMappings_EmployeeId",
                schema: "time",
                table: "EmployeeDeviceMappings",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "UQ_GeofenceLocations_Code",
                schema: "time",
                table: "GeofenceLocations",
                column: "LocationCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UnmappedAttendanceLogs_Dedup",
                schema: "time",
                table: "UnmappedAttendanceLogs",
                columns: new[] { "DeviceCode", "DeviceUserCode", "PunchTime", "PunchType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceDevices",
                schema: "time");

            migrationBuilder.DropTable(
                name: "EmployeeDeviceBindings",
                schema: "time");

            migrationBuilder.DropTable(
                name: "EmployeeDeviceMappings",
                schema: "time");

            migrationBuilder.DropTable(
                name: "GeofenceLocations",
                schema: "time");

            migrationBuilder.DropTable(
                name: "UnmappedAttendanceLogs",
                schema: "time");

            migrationBuilder.DropIndex(
                name: "UQ_AttendanceLogs_ExternalLogKey",
                schema: "time",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "ClientDeviceFingerprint",
                schema: "time",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "DistanceMeters",
                schema: "time",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "EnteredByUserId",
                schema: "time",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "EnteredByUsername",
                schema: "time",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "ExternalLogKey",
                schema: "time",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "GeofenceLocationCode",
                schema: "time",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "GeofenceStatus",
                schema: "time",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                schema: "time",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "IsManualOverride",
                schema: "time",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "ManualReason",
                schema: "time",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                schema: "time",
                table: "AttendanceLogs");

            migrationBuilder.AlterColumn<string>(
                name: "EditReason",
                schema: "time",
                table: "DailyAttendances",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LocationAddress",
                schema: "time",
                table: "AttendanceLogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);
        }
    }
}
