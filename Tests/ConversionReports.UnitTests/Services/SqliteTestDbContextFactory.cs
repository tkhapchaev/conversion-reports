using ConversionReports.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ConversionReports.UnitTests.Services;

public class SqliteTestDbContextFactory : IAsyncDisposable
{
	private readonly SqliteConnection _connection;
	private readonly DbContextOptions<AppDbContext> _options;

	public SqliteTestDbContextFactory()
	{
		_connection = new SqliteConnection("Data Source=:memory:");
		_connection.Open();

		_options = new DbContextOptionsBuilder<AppDbContext>()
			.UseSqlite(_connection)
			.Options;
	}

	public async Task InitializeAsync()
	{
		await using var dbContext = CreateDbContext();
		await dbContext.Database.EnsureCreatedAsync();
	}

	public AppDbContext CreateDbContext()
	{
		return new AppDbContext(_options);
	}

	public async ValueTask DisposeAsync()
	{
		await _connection.DisposeAsync();
	}
}
