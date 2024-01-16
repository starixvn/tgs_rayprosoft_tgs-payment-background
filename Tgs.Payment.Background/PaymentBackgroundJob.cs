using Google.Protobuf;
using Tgs.Payment.Background.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.Background;

public class PaymentBackgroundJob(IMessageSender messageSender)
{
	private readonly IMessageSender _messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));

	public async Task ExecuteAsync(DateOnly processDate, params string[] subjects)
	{
		var request = new ProcessRequest
		{
			ProcessDate = processDate
		};

		var sendData = request.ToByteArray();

		foreach (var subject in subjects)
			await _messageSender
				.SendAsync(subject, sendData)
				.ConfigureAwait(false);
	}
}
