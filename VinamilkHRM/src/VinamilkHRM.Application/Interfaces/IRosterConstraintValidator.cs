using System;
using System.Collections.Generic;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Shifts;

namespace VinamilkHRM.Application.Interfaces;

public interface IRosterConstraintValidator
{
    void ValidateRestBetweenShifts(
        DateOnly previousWorkDate,
        ShiftDefinition previousShift,
        DateOnly currentWorkDate,
        ShiftDefinition currentShift);

    void ValidateWeeklyContinuousRest(
        IReadOnlyList<(DateOnly WorkDate, ShiftDefinition? Shift, bool IsOff)> weekSchedule);

    void ValidateMaternalProtection(
        Employee employee,
        DateOnly workDate,
        ShiftDefinition? shift,
        decimal plannedOtHours = 0m);

    void ValidateOvertimeCap(
        decimal dailyOtHours,
        decimal currentMonthlyOtHours,
        decimal currentYearlyOtHours,
        decimal monthlyCap = 40m,
        decimal yearlyCap = 200m);
}
