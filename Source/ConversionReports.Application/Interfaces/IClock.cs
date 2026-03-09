namespace ConversionReports.Application.Interfaces;

public interface IClock
{
	DateTimeOffset UtcNow { get; }
}
