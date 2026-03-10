using ConversionReports.Application.Interfaces;

namespace ConversionReports.Infrastructure.Providers;

public class SystemClock : IClock
{
	public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
