using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Tgs.WebApi.Toolkits;

namespace Tgs.Payment.Background.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OverviewController : ControllerBase
{
	[HttpPost(Name = "RefreshAllPlayerPaymentOverview")]
	[SuccessStatusCode(202)]
	public void RefreshAllPlayerPaymentOverview()
		=> _ = BackgroundJob.Enqueue<PaymentOverviewBackgroundJob>(job => job.ExecuteAsync());

	[HttpPost("{playerCode}", Name = "RefreshPlayerPaymentOverview")]
	public ValueTask RefreshPlayerPaymentOverviewAsync(
		[FromServices] ITgsRepository tgsRepository,
		string playerCode)
		=> tgsRepository.UpdatePlayerPaymentOverviewAsync(
			playerCode,
			HttpContext.RequestAborted);	
}
