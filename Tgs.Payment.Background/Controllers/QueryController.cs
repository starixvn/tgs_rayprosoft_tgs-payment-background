using Microsoft.AspNetCore.Mvc;

namespace Tgs.Payment.Background.Controllers;

[Route("[controller]")]
[ApiController]
public class QueryController : ControllerBase
{
	[HttpGet]
	public void Query5minPaymentStatistics()
	{

	}
}
