using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Tgs.WebApi.Toolkits;

namespace Tgs.Payment.Background.Controllers;

[Route("[controller]")]
[ApiController]
public class ScheduleController : ControllerBase
{
	[HttpPost(Name = "QueueProcessAllPaymentStatisticsJobs")]
	[SuccessStatusCode(202)]
	public void QueueProcessAllPaymentStatisticsJobs()
	{
		var currentDate = DateOnly.FromDateTime(DateTimeOffset.Now
			.ToOffset(TimeSpan.FromHours(8D))
			.DateTime);

		_ = BackgroundJob.Enqueue<PaymentBackgroundJob>(job => job.ExecuteAsync(
			currentDate,
			"tgs.payment.background.agent",
			"tgs.payment.background.brand"));
	}

	[HttpPost("player/{processDate}", Name = "QueueProcessPlayerPaymentStatisticsJobWithProcessDate")]
	[SuccessStatusCode(202)]
	public void QueueProcessPlayerPaymentStatisticsJobWithProcessDate(
		DateOnly processDate)
	{
		_ = BackgroundJob.Enqueue<PaymentBackgroundJob>(job => job.ExecuteAsync(
			processDate,
			"tgs.payment.background.player"));
	}

	[HttpPost("agent/{processDate}", Name = "QueueProcessAgentPaymentStatisticsJobWithProcessDate")]
	[SuccessStatusCode(202)]
	public void QueueProcessAgentPaymentStatisticsJobWithProcessDate(
		DateOnly processDate)
	{
		_ = BackgroundJob.Enqueue<PaymentBackgroundJob>(job => job.ExecuteAsync(
			processDate,
			"tgs.payment.background.agent"));
	}

	[HttpPost("brand/{processDate}", Name = "QueueProcessBrandPaymentStatisticsJobWithProcessDate")]
	[SuccessStatusCode(202)]
	public void QueueProcessBrandPaymentStatisticsJobWithProcessDate(
		DateOnly processDate)
	{
		_ = BackgroundJob.Enqueue<PaymentBackgroundJob>(job => job.ExecuteAsync(
			processDate,
			"tgs.payment.background.brand"));
	}
}
