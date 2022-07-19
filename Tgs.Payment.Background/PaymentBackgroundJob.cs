using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Payment.Background.Protos;

namespace Tgs.Payment.Background;

public class PaymentBackgroundJob
{
	private readonly IMessageSender _messageSender;

	public PaymentBackgroundJob(IMessageSender messageSender)
	{
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

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
