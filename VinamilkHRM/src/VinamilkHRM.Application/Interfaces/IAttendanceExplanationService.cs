using System.Threading;
using System.Threading.Tasks;
using VinamilkHRM.Application.DTOs.Attendances;

namespace VinamilkHRM.Application.Interfaces;

public interface IAttendanceExplanationService
{
    Task<AttendanceExplanationDto> SubmitExplanationAsync(CreateExplanationRequest request, CancellationToken cancellationToken = default);
    Task<AttendanceExplanationDto> ApproveExplanationAsync(int explanationId, int approverId, CancellationToken cancellationToken = default);
    Task<AttendanceExplanationDto> RejectExplanationAsync(int explanationId, string reason, CancellationToken cancellationToken = default);
}
