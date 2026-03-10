using ConversionReports.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace ConversionReports.UnitTests.Services;

public class ReportDefinitionTests
{
	[Fact]
	public void Validate_ShouldThrow_WhenPeriodEndIsNotGreaterThanStart()
	{
		var definition = new ReportDefinition(
			new DateTimeOffset(2026, 3, 8, 0, 0, 0, TimeSpan.Zero),
			new DateTimeOffset(2026, 3, 8, 0, 0, 0, TimeSpan.Zero),
			100,
			200);

		var action = definition.Validate;

		action.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void Validate_ShouldThrow_WhenProductIdIsNotPositive()
	{
		var definition = new ReportDefinition(
			new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
			new DateTimeOffset(2026, 3, 8, 0, 0, 0, TimeSpan.Zero),
			0,
			200);

		var action = definition.Validate;

		action.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void Validate_ShouldThrow_WhenDesignIdIsNotPositive()
	{
		var definition = new ReportDefinition(
			new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
			new DateTimeOffset(2026, 3, 8, 0, 0, 0, TimeSpan.Zero),
			100,
			0);

		var action = definition.Validate;

		action.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void Validate_ShouldNotThrow_WhenDefinitionIsValid()
	{
		var definition = new ReportDefinition(
			new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
			new DateTimeOffset(2026, 3, 8, 0, 0, 0, TimeSpan.Zero),
			100,
			200);

		var action = definition.Validate;

		action.Should().NotThrow();
	}
}
