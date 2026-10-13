using System.Threading;
using System.Threading.Tasks;
using VinamilkHRM.Application.DTOs.Attendances;

namespace VinamilkHRM.Application.Interfaces;

public interface IRosterService
{
    Task<int> GenerateMonthlyRosterAsync(GenerateRosterRequest request, CancellationToken cancellationToken = default);
    Task<RosterEntryDto> UpdateRosterEntryAsync(UpdateRosterEntryRequest request, CancellationToken cancellationToken = default);
    Task<int> PublishRosterAsync(PublishRosterRequest request, CancellationToken cancellationToken = default);
    Task<RosterGridResponse> GetRosterGridAsync(int organizationalUnitId, int year, int month, CancellationToken cancellationToken = default);

    Task<ShiftSwapRequestDto> ProposeShiftSwapAsync(CreateShiftSwapRequest request, CancellationToken cancellationToken = default);
    Task<ShiftSwapRequestDto> AcceptShiftSwapAsync(int swapRequestId, int targetEmployeeId, CancellationToken cancellationToken = default);
    Task<ShiftSwapRequestDto> ApproveShiftSwapAsync(int swapRequestId, int approverId, CancellationToken cancellationToken = default);
}
