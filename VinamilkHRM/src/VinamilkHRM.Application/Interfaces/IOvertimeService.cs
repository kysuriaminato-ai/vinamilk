using System.Threading;
using System.Threading.Tasks;
using VinamilkHRM.Application.DTOs.Attendances;

namespace VinamilkHRM.Application.Interfaces;

public interface IOvertimeService
{
    Task<OvertimeRequestDto> CreateRequestAsync(CreateOvertimeRequest request, CancellationToken cancellationToken = default);
    Task<OvertimeRequestDto> ApproveByTeamLeaderAsync(int requestId, int leaderId, CancellationToken cancellationToken = default);
    Task<OvertimeRequestDto> ApproveByUnitManagerAsync(int requestId, int managerId, CancellationToken cancellationToken = default);
    Task<OvertimeRequestDto> RejectAsync(int requestId, string reason, CancellationToken cancellationToken = default);
}
