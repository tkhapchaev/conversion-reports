namespace ConversionReports.Application.Extensions;

internal static class EnumerableAsyncExtensions
{
	public static Task<List<T>> ToListAsyncSafe<T>(this IEnumerable<T> source, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(source);

		cancellationToken.ThrowIfCancellationRequested();

		return Task.FromResult(source.ToList());
	}
}
