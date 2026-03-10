using ConversionReports.Application.Models;
using ConversionReports.Application.Services;
using ConversionReports.Infrastructure.DependencyInjection;
using ConversionReports.Infrastructure.Persistence;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HostOptions>(options =>
{
	options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

builder.WebHost.ConfigureKestrel(options =>
{
	options.ListenAnyIP(8080, listenOptions => listenOptions.Protocols = HttpProtocols.Http1AndHttp2);
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ReportStatusQueryService>();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

	await dbContext.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapGrpcService<ConversionReports.Api.Grpc.ReportingGrpcService>();

app.MapGet(
	"/api/report-requests/{requestId:guid}",
	async (Guid requestId, ReportStatusQueryService service, CancellationToken cancellationToken) =>
	{
		var result = await service.GetAsync(requestId, cancellationToken);

		if (result.State == ReportRequestState.NotFound)
		{

			return Results.NotFound(result);
		}

		return Results.Ok(result);
	})
	.WithName("GetReportRequestStatus")
	.WithOpenApi();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
