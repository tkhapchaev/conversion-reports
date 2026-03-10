using ConversionReports.Application.Models;

namespace ConversionReports.Application.Interfaces;

public interface IReportResultCache
{
	bool TryGet(Guid requestId, out ReportRequestStatusDto status);

	void Set(ReportRequestStatusDto status);

	void Remove(Guid requestId);
}
