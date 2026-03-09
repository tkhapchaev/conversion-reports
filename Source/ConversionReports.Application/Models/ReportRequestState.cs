namespace ConversionReports.Application.Models;

public enum ReportRequestState
{
	Pending = 1,
	Processing = 2,
	Completed = 3,
	Failed = 4,
	NotFound = 5
}
