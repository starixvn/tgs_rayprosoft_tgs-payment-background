using Google.Protobuf;
using Valhalla.MessageQueue;

namespace Tgs.Payment.Background;

public class PaymentOverviewBackgroundJob(IMessageSender messageSender)
{
	private readonly IMessageSender _messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));

	public async Task ExecuteAsync()
	{
		var request = new Google.Protobuf.WellKnownTypes.Empty();

		await _messageSender
			.SendAsync("tgs.payment.background.payment-overview.all", request.ToByteArray())
			.ConfigureAwait(false);
	}
}
