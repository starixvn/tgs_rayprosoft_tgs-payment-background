using Tgs.MessageQueue;
using Tgs.Payment.Background.Protos;

namespace Tgs.Payment.Background.MessageQueue;

public class PlayerDailyPaymentHandler : IMessageHandler
{
	private readonly ITgsRepository _tgsRepository;
	private readonly IPaymentOverviewUpdateSender _paymentOverviewUpdateSender;

	public PlayerDailyPaymentHandler(
		ITgsRepository tgsRepository,
		IPaymentOverviewUpdateSender paymentOverviewUpdateSender)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_paymentOverviewUpdateSender = paymentOverviewUpdateSender ?? throw new ArgumentNullException(nameof(paymentOverviewUpdateSender));
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

		if (lastProcessDate is null)
		{
			var changePlayers = await _tgsRepository
				.ProcessPlayerDailyPaymentStatisticsAsync(requestDate.Value, cancellationToken)
				.ToArrayAsync(cancellationToken)
				.ConfigureAwait(false);

			await _tgsRepository
				.SetPlayerDailyPaymentStatisticsLastProcessDateAsync(requestDate.Value, cancellationToken)
				.ConfigureAwait(false);

			foreach (var playerCode in changePlayers)
				_paymentOverviewUpdateSender.Post(playerCode);
		}
		else if (requestDate > lastProcessDate)
		{
			var diffDays = (requestDate.Value.ToDateTime(TimeOnly.MinValue)
				- lastProcessDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays + 1;

			for (var i = 1; i < diffDays; i++)
			{
				var changePlayers = await _tgsRepository
					.ProcessPlayerDailyPaymentStatisticsAsync(
						lastProcessDate.Value.AddDays(i),
						cancellationToken)
					.ToArrayAsync(cancellationToken)
					.ConfigureAwait(false);

				foreach (var playerCode in changePlayers)
					_paymentOverviewUpdateSender.Post(playerCode);
			}

			await _tgsRepository
				.SetPlayerDailyPaymentStatisticsLastProcessDateAsync(requestDate.Value, cancellationToken)
				.ConfigureAwait(false);
		}
		else
		{
			var diffDays = (lastProcessDate.Value.ToDateTime(TimeOnly.MinValue)
				- requestDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays + 1;

			for (var i = 0; i < diffDays; i++)
			{
				var changePlayers = await _tgsRepository
					.ProcessPlayerDailyPaymentStatisticsAsync(
						requestDate.Value.AddDays(i),
						cancellationToken)
					.ToArrayAsync(cancellationToken)
					.ConfigureAwait(false);

				foreach (var playerCode in changePlayers)
					_paymentOverviewUpdateSender.Post(playerCode);
			}
		}
	}
}