namespace Tgs.Payment.Background;

public interface ITgsRepository
{
	ValueTask<DateOnly?> GetPlayerDailyPaymentStatisticsLastProcessDateAsync(CancellationToken cancellationToken = default);

	ValueTask SetPlayerDailyPaymentStatisticsLastProcessDateAsync(DateOnly date, CancellationToken cancellationToken = default);

	ValueTask ProcessPlayerDailyPaymentStatisticsAsync(DateOnly processDate, CancellationToken cancellationToken = default);

	ValueTask<DateOnly?> GetAgentDailyPaymentStatisticsLastProcessDateAsync(CancellationToken cancellationToken = default);

	ValueTask SetAgentDailyPaymentStatisticsLastProcessDateAsync(DateOnly date, CancellationToken cancellationToken = default);

	ValueTask ProcessAgentDailyPaymentStatisticsAsync(DateOnly processDate, CancellationToken cancellationToken = default);

	ValueTask<DateOnly?> GetBrandDailyPaymentStatisticsLastProcessDateAsync(CancellationToken cancellationToken = default);

	ValueTask SetBrandDailyPaymentStatisticsLastProcessDateAsync(DateOnly date, CancellationToken cancellationToken = default);

	ValueTask ProcessBrandDailyPaymentStatisticsAsync(DateOnly processDate, CancellationToken cancellationToken = default);
}
