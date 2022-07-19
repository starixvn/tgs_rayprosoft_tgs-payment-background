using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Tgs.MessageQueue;
using Tgs.Payment.Background.Protos;
using Tgs.WebApi.Toolkits;

namespace Tgs.Payment.Background.Controllers;

[Route("[controller]")]
[ApiController]
public class ScheduleController : ControllerBase
{
	[HttpPost(Name = "QueueProcessAllPaymentStatisticsJobs")]
	[SuccessStatusCode(202)]
	public void QueueProcessAllPaymentStatisticsJobs(
		[FromServices] IMessageSender messageSender)
	{
		var currentDate = DateOnly.FromDateTime(DateTimeOffset.Now
			.ToOffset(TimeSpan.FromHours(8D))
			.DateTime);

		var request = new ProcessRequest
		{
			ProcessDate = currentDate
		};

		_ = Task.Run(async () =>
		{
			await messageSender
				.SendAsync(
					"tgs.payment.background.agent",
					request.ToByteArray())
				.ConfigureAwait(false);

			await messageSender
				.SendAsync(
					"tgs.payment.background.brand",
					request.ToByteArray())
				.ConfigureAwait(false);
		});
	}

	[HttpPost("player/{processDate}", Name = "QueueProcessPlayerPaymentStatisticsJobWithProcessDate")]
	[SuccessStatusCode(202)]
	public void QueueProcessPlayerPaymentStatisticsJobWithProcessDate(
		[FromServices] IMessageSender messageSender,
		DateOnly processDate)
	{
		var request = new ProcessRequest
		{
			ProcessDate = processDate
		};

		_ = messageSender
			.SendAsync(
				"tgs.payment.background.player",
				request.ToByteArray())
			.AsTask();
	}

	[HttpPost("agent/{processDate}", Name = "QueueProcessAgentPaymentStatisticsJobWithProcessDate")]
	[SuccessStatusCode(202)]
	public void QueueProcessAgentPaymentStatisticsJobWithProcessDate(
		[FromServices] IMessageSender messageSender,
		DateOnly processDate)
	{
		var request = new ProcessRequest
		{
			ProcessDate = processDate
		};

		_ = messageSender
			.SendAsync(
				"tgs.payment.background.agent",
				request.ToByteArray())
			.AsTask();
	}

	[HttpPost("brand/{processDate}", Name = "QueueProcessBrandPaymentStatisticsJobWithProcessDate")]
	[SuccessStatusCode(202)]
	public void QueueProcessBrandPaymentStatisticsJobWithProcessDate(
		[FromServices] IMessageSender messageSender,
		DateOnly processDate)
	{
		var request = new ProcessRequest
		{
			ProcessDate = processDate
		};

		_ = messageSender
			.SendAsync(
				"tgs.payment.background.brand",
				request.ToByteArray())
			.AsTask();
	}
}
