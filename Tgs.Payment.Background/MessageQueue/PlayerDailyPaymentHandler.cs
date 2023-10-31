using Tgs.Payment.Background.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.Background.MessageQueue;

public class PlayerDailyPaymentHandler : IMessageHandler
{
	private readonly ITgsRepository _tgsRepository;
	private readonly IPaymentOverviewUpdateSender _paymentOverviewUpdateSender;
	private readonly ILogger<PlayerDailyPaymentHandler> _logger;

	public PlayerDailyPaymentHandler(
		ITgsRepository tgsRepository,
		IPaymentOverviewUpdateSender paymentOverviewUpdateSender,
		ILogger<PlayerDailyPaymentHandler> logger)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_paymentOverviewUpdateSender = paymentOverviewUpdateSender ?? throw new ArgumentNullException(nameof(paymentOverviewUpdateSender));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

			_logger.LogDebug("1. Change players is {changePlayers}", string.Join(", ", changePlayers));

			foreach (var playerCode in changePlayers)
			{
				_logger.LogDebug("Enqueue payment overview update request {playerCode}", playerCode);
				_paymentOverviewUpdateSender.Post(playerCode);
			}
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

				_logger.LogDebug("2. Change players is {changePlayers}", string.Join(", ", changePlayers));

				foreach (var playerCode in changePlayers)
				{
					_logger.LogDebug("Enqueue payment overview update request {playerCode}", playerCode);
					_paymentOverviewUpdateSender.Post(playerCode);
				}
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

				_logger.LogDebug("3. Change players is {changePlayers}", string.Join(", ", changePlayers));

				foreach (var playerCode in changePlayers)
				{
					_logger.LogDebug("Enqueue payment overview update request {playerCode}", playerCode);
					_paymentOverviewUpdateSender.Post(playerCode);
				}
			}
		}
	}
}
