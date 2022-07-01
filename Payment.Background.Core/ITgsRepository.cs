namespace Tgs.Payment.Background;

public interface ITgsRepository
{
	ValueTask<DateOnly?> GetPlayerDailyPaymentStatisticsLastProcessDateAsync(CancellationToken cancellationToken = default);

	ValueTask SetPlayerDailyPaymentStatisticsLastProcessDateAsync(DateOnly date, CancellationToken cancellationToken = default);

	ValueTask ProcessPlayerDailyPaymentStatisticsAsync(DateOnly processDate, CancellationToken cancellationToken = default);
}
