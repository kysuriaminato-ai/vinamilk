using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.OrganizationalUnits;

public record OrganizationalUnitTreeDto(
    int Id,
    string UnitCode,
    string UnitName,
    UnitType UnitType,
    string? Path,
    int? ParentUnitId,
    string? CostCenterCode,
    WorkPattern WorkPattern,
    MinimumWageRegion MinimumWageRegion,
    int PlannedHeadcount,
    int CurrentHeadcount,
    int? ManagerEmployeeId,
    string? ManagerEmployeeName,
    List<OrganizationalUnitTreeDto> Children);

public record MoveUnitRequest(int? NewParentUnitId);

public record AssignManagerRequest(
    int ManagerEmployeeId,
    string? AppointmentDecisionNumber = null,
    DateOnly? EffectiveFrom = null,
    string? Note = null);

public record ChangeWageRegionRequest(
    MinimumWageRegion NewRegion,
    DateOnly EffectiveFrom,
    string? Reason = null,
    string? LegalReference = null);

public record UnitManagerHistoryDto(
    int Id,
    int UnitId,
    int ManagerEmployeeId,
    string? ManagerName,
    DateOnly FromDate,
    DateOnly? ToDate,
    string? AppointmentDecisionNumber,
    string? Note);
