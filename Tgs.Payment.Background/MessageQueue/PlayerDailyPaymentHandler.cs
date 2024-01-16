using Tgs.Payment.Background.Protos;
using Valhalla.MessageQueue;
using Valhalla.Messages;

namespace Tgs.Payment.Background.MessageQueue;

public class PlayerDailyPaymentHandler(
	ITgsRepository tgsRepository,
	IEventBus eventBus,
	ILogger<PlayerDailyPaymentHandler> logger)
	: IMessageHandler<ReadOnlyMemory<byte>>
{

	public async ValueTask HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var request = ProcessRequest.Parser.ParseFrom(data.Span);

		var requestDate = request.ProcessDate.ToDateOnly();
		var eventSender = eventBus.GetEventPublisher<string>(SchedulerConsts.PaymentOverviewUpdateEventName)!;

		if (requestDate is null)
			return;

		var lastProcessDate = await tgsRepository
			.GetPlayerDailyPaymentStatisticsLastProcessDateAsync(cancellationToken)
			.ConfigureAwait(false);

		if (lastProcessDate is null)
		{
			var changePlayers = await tgsRepository
				.ProcessPlayerDailyPaymentStatisticsAsync(requestDate.Value, cancellationToken)
				.ToArrayAsync(cancellationToken)
				.ConfigureAwait(false);

			await tgsRepository
				.SetPlayerDailyPaymentStatisticsLastProcessDateAsync(requestDate.Value, cancellationToken)
				.ConfigureAwait(false);

			logger.LogDebug("1. Change players is {changePlayers}", string.Join(", ", changePlayers));

			foreach (var playerCode in changePlayers)
			{
				logger.LogDebug("Enqueue payment overview update request {playerCode}", playerCode);
				_ = await eventSender.PublishAsync(
					playerCode,
					cancellationToken).ConfigureAwait(false);
			}
		}
		else if (requestDate > lastProcessDate)
		{
			var diffDays = (requestDate.Value.ToDateTime(TimeOnly.MinValue)
				- lastProcessDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays + 1;

			for (var i = 1; i < diffDays; i++)
			{
				var changePlayers = await tgsRepository
					.ProcessPlayerDailyPaymentStatisticsAsync(
						lastProcessDate.Value.AddDays(i),
						cancellationToken)
					.ToArrayAsync(cancellationToken)
					.ConfigureAwait(false);

				logger.LogDebug("2. Change players is {changePlayers}", string.Join(", ", changePlayers));

				foreach (var playerCode in changePlayers)
				{
					logger.LogDebug("Enqueue payment overview update request {playerCode}", playerCode);
					_ = await eventSender.PublishAsync(
						playerCode,
						cancellationToken).ConfigureAwait(false);
				}
			}

			await tgsRepository
				.SetPlayerDailyPaymentStatisticsLastProcessDateAsync(requestDate.Value, cancellationToken)
				.ConfigureAwait(false);
		}
		else
		{
			var diffDays = (lastProcessDate.Value.ToDateTime(TimeOnly.MinValue)
				- requestDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays + 1;

			for (var i = 0; i < diffDays; i++)
			{
				var changePlayers = await tgsRepository
					.ProcessPlayerDailyPaymentStatisticsAsync(
						requestDate.Value.AddDays(i),
						cancellationToken)
					.ToArrayAsync(cancellationToken)
					.ConfigureAwait(false);

				logger.LogDebug("3. Change players is {changePlayers}", string.Join(", ", changePlayers));

				foreach (var playerCode in changePlayers)
				{
					logger.LogDebug("Enqueue payment overview update request {playerCode}", playerCode);
					_ = await eventSender.PublishAsync(
						playerCode,
						cancellationToken).ConfigureAwait(false);
				}
			}
		}
	}
}
