using ConversionReports.Application.Models;
using ConversionReports.Application.Services;
using Grpc.Core;

namespace ConversionReports.Api.Grpc;

public class ReportingGrpcService : Reporting.ReportingBase
{
	private readonly ReportStatusQueryService _service;

	public ReportingGrpcService(ReportStatusQueryService service)
	{
		_service = service ?? throw new ArgumentNullException(nameof(service));
	}

	public override async Task<GetReportResultReply> GetReportResult(GetReportResultRequest request, ServerCallContext context)
	{
		ArgumentNullException.ThrowIfNull(request);

		if (!Guid.TryParse(request.RequestId, out var requestId))
		{
			throw new RpcException(new Status(StatusCode.InvalidArgument, "request_id must be a valid GUID"));
		}

		var result = await _service.GetAsync(requestId, context.CancellationToken);

		return new GetReportResultReply
		{
			RequestId = result.RequestId.ToString(),
			UserId = result.UserId,
			State = MapState(result.State),
			ConversionRatio = result.ConversionRatio.HasValue ? (double)result.ConversionRatio.Value : 0,
			PaymentsCount = result.PaymentsCount ?? 0,
			Error = result.Error ?? string.Empty,
			CreatedAtUtc = result.CreatedAtUtc == DateTimeOffset.MinValue ? string.Empty : result.CreatedAtUtc.ToString("O"),
			CompletedAtUtc = result.CompletedAtUtc?.ToString("O") ?? string.Empty
		};
	}

	private static ReportState MapState(ReportRequestState state)
	{
		return state switch
		{
			ReportRequestState.Pending => ReportState.Pending,
			ReportRequestState.Processing => ReportState.Processing,
			ReportRequestState.Completed => ReportState.Completed,
			ReportRequestState.Failed => ReportState.Failed,
			_ => ReportState.NotFound
		};
	}
}
