namespace Tgs.Payment.Background;

public interface ITgsRepository
{
	ValueTask<DateOnly?> GetPlayerDailyPaymentStatisticsLastProcessDateAsync(CancellationToken cancellationToken = default);

	ValueTask SetPlayerDailyPaymentStatisticsLastProcessDateAsync(DateOnly date, CancellationToken cancellationToken = default);

	IAsyncEnumerable<string> ProcessPlayerDailyPaymentStatisticsAsync(DateOnly processDate, CancellationToken cancellationToken = default);

	ValueTask<DateOnly?> GetAgentDailyPaymentStatisticsLastProcessDateAsync(CancellationToken cancellationToken = default);

	ValueTask SetAgentDailyPaymentStatisticsLastProcessDateAsync(DateOnly date, CancellationToken cancellationToken = default);

	ValueTask ProcessAgentDailyPaymentStatisticsAsync(DateOnly processDate, CancellationToken cancellationToken = default);

	ValueTask<DateOnly?> GetBrandDailyPaymentStatisticsLastProcessDateAsync(CancellationToken cancellationToken = default);

	ValueTask SetBrandDailyPaymentStatisticsLastProcessDateAsync(DateOnly date, CancellationToken cancellationToken = default);

	ValueTask ProcessBrandDailyPaymentStatisticsAsync(DateOnly processDate, CancellationToken cancellationToken = default);

	ValueTask UpdatePlayerPaymentOverviewAsync(string playerCode, CancellationToken cancellationToken = default);

	ValueTask UpdateAllPlayerPaymentOverviewAsync(CancellationToken cancellationToken = default);

	ValueTask<DateTime?> GetUserFiveMinutesLastProcessTimeAsync(CancellationToken cancellationToken = default);

	ValueTask SetUserFiveMinutesLastProcessTimeAsync(DateTime newProcessTime, CancellationToken cancellationToken = default);

	IAsyncEnumerable<UserPaymentSummaryStatistics> QueryUserPaymentSummaryStatisticsAsync(
		DateTime begin,
		DateTime end,
		CancellationToken cancellationToken = default);

	ValueTask UpsertFiveMinutesUserPaymentSummaryStatisticsAsync(
		DateTime timeRange,
		IEnumerable<UserPaymentSummaryStatistics> userPaymentSummaries,
		CancellationToken cancellationToken = default);
}
