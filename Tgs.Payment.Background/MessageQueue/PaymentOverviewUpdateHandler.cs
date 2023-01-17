using Tgs.MessageQueue;
using Tgs.Payment.Background.Protos;

namespace Tgs.Payment.Background.MessageQueue;

public class PaymentOverviewUpdateHandler : IMessageHandler
{
	private readonly IPaymentOverviewUpdateSender _paymentOverviewUpdateSender;

	public PaymentOverviewUpdateHandler(IPaymentOverviewUpdateSender paymentOverviewUpdateSender)
	{
		_paymentOverviewUpdateSender = paymentOverviewUpdateSender ?? throw new ArgumentNullException(nameof(paymentOverviewUpdateSender));
	}

	public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = UpdatePaymentOverviewRequest.Parser.ParseFrom(data.Span);

		var playerCode = request.PlayerCode;

		_paymentOverviewUpdateSender.Post(playerCode);

		return ValueTask.CompletedTask;
	}
}
