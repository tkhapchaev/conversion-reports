using ConversionReports.Application.Models;

namespace ConversionReports.Application.Interfaces;

public interface IReportRequestReadRepository
{
	Task<ReportRequestStatusDto?> GetStatusAsync(Guid requestId, CancellationToken cancellationToken);
}
