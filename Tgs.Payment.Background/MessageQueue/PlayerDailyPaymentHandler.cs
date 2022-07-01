using Tgs.MessageQueue;
using Tgs.Payment.Background.Protos;

namespace Tgs.Payment.Background.MessageQueue;

public class PlayerDailyPaymentHandler : IMessageHandler
{
	private readonly ITgsRepository _tgsRepository;

	public PlayerDailyPaymentHandler(ITgsRepository tgsRepository)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	}

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
	{
		var request = ProcessRequest.Parser.ParseFrom(data.Span);

		var requestDate = request.ProcessDate.ToDateOnly();

		if (requestDate is null)
			return;

		var lastProcessDate = await _tgsRepository
			.GetPlayerDailyPaymentStatisticsLastProcessDateAsync(cancellationToken)
			.ConfigureAwait(false);

		if (lastProcessDate is null || requestDate > lastProcessDate)
		{
			await _tgsRepository
				.ProcessPlayerDailyPaymentStatisticsAsync(requestDate.Value, cancellationToken)
				.ConfigureAwait(false);
			await _tgsRepository
				.SetPlayerDailyPaymentStatisticsLastProcessDateAsync(requestDate.Value, cancellationToken)
				.ConfigureAwait(false);
		}
		else
		{
			var diffDays = (lastProcessDate.Value.ToDateTime(TimeOnly.MinValue)
				- requestDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays + 1;

			for (int i = 0; i < diffDays; i++)
				await _tgsRepository
					.ProcessPlayerDailyPaymentStatisticsAsync(
						requestDate.Value.AddDays(i),
						cancellationToken)
					.ConfigureAwait(false);
		}
	}
}
