using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VinamilkHRM.Application.DTOs.Attendances;

namespace VinamilkHRM.Application.Interfaces;

public interface ITimeEventService
{
    Task<BatchTimeEventsResponse> BatchCreateAsync(BatchTimeEventsRequest request, CancellationToken cancellationToken = default);
    Task<TimeEventDto> MobilePunchAsync(MobilePunchRequest request, CancellationToken cancellationToken = default);
    Task<BatchTimeEventsResponse> ImportFromExcelAsync(Stream fileStream, CancellationToken cancellationToken = default);
}
