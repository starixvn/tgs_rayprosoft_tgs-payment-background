using Tgs.Payment.Background.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.Background.MessageQueue;

public class AgentDailyPaymentHandler : IMessageHandler
{
	private readonly ITgsRepository _tgsRepository;
	private readonly IMessageSender _messageSender;

	public AgentDailyPaymentHandler(
		ITgsRepository tgsRepository,
		IMessageSender messageSender)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = ProcessRequest.Parser.ParseFrom(data.Span);

		var requestDate = request.ProcessDate.ToDateOnly();

		if (requestDate is null)
			return;

		var playerStatisticsLastProcessDate = await _tgsRepository
			.GetPlayerDailyPaymentStatisticsLastProcessDateAsync(cancellationToken)
			.ConfigureAwait(false);

		if (playerStatisticsLastProcessDate is null || requestDate > playerStatisticsLastProcessDate)
			await _messageSender
				.SendAsync("tgs.payment.background.player", data, cancellationToken: cancellationToken)
				.ConfigureAwait(false);

		var lastProcessDate = await _tgsRepository
			.GetAgentDailyPaymentStatisticsLastProcessDateAsync(cancellationToken)
			.ConfigureAwait(false);

		if (lastProcessDate is null)
		{
			await _tgsRepository
				.ProcessAgentDailyPaymentStatisticsAsync(requestDate.Value, cancellationToken)
				.ConfigureAwait(false);
			await _tgsRepository
				.SetAgentDailyPaymentStatisticsLastProcessDateAsync(requestDate.Value, cancellationToken)
				.ConfigureAwait(false);
		}
		else if (requestDate > lastProcessDate)
		{
			var diffDays = (requestDate.Value.ToDateTime(TimeOnly.MinValue)
				- lastProcessDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays + 1;

			for (int i = 1; i < diffDays; i++)
				await _tgsRepository
					.ProcessAgentDailyPaymentStatisticsAsync(
						lastProcessDate.Value.AddDays(i),
						cancellationToken)
					.ConfigureAwait(false);

			await _tgsRepository
				.SetAgentDailyPaymentStatisticsLastProcessDateAsync(requestDate.Value, cancellationToken)
				.ConfigureAwait(false);
		}
		else
		{
			var diffDays = (lastProcessDate.Value.ToDateTime(TimeOnly.MinValue)
				- requestDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays + 1;

			for (int i = 0; i < diffDays; i++)
				await _tgsRepository
					.ProcessAgentDailyPaymentStatisticsAsync(
						requestDate.Value.AddDays(i),
						cancellationToken)
					.ConfigureAwait(false);
		}
	}
}
