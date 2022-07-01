using Microsoft.EntityFrameworkCore;
using Tgs.Payment.Background.Repositories.Entities;

namespace Tgs.Payment.Background.Repositories;

internal class TgsRepository : ITgsRepository
{
	private readonly TgsContext _tgsContext;

	public TgsRepository(TgsContext tgsContext)
	{
		_tgsContext = tgsContext ?? throw new ArgumentNullException(nameof(tgsContext));
	}

	public async ValueTask ProcessPlayerDailyPaymentStatisticsAsync(DateOnly processDate, CancellationToken cancellationToken = default)
	{
		var (From, To) = (processDate.AddDays(-1), processDate);
		var queryTimeRange = (
			From: new DateTimeOffset(
				From
					.AddDays(-1)
					.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(16D)), DateTimeKind.Utc),
				TimeSpan.Zero),
			To: new DateTimeOffset(
				To
					.AddDays(-1)
					.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(16D)), DateTimeKind.Utc),
				TimeSpan.Zero));

		var processData = await _tgsContext.PlayerDailyPaymentStatistics
			.FromSqlInterpolated($@"
				SELECT
					COALESCE(D.player_code, W.player_code) AS player_code,
					COALESCE(D.date, W.date) AS report_date,
					D.deposit_amount AS daily_deposit_amount,
					D.deposit_count AS daily_deposit_count,
					D.daily_first_deposit_on AS first_deposit_on,
					D.total_deposit_count,
					D.total_deposit_amount,
					W.withdraw_amount AS daily_withdraw_amount,
					W.withdraw_count AS daily_withdraw_count,
					W.total_withdraw_count,
					W.total_withdraw_amount
				FROM
					(
						WITH depositStatistics
						(
							player_code,
							""date"",
							deposit_amount,
							daily_first_deposit_on,
							deposit_count
						)
						AS
						(
							SELECT
								player_code,
								(reviewed_on at time zone 'Asia/Shanghai')::date AS date,
								Sum(COALESCE(actual_amount, amount)) AS deposit_amount,
								Min(reviewed_on) AS daily_first_deposit_on,
								Count(1) as deposit_count
							FROM
								dbo.deposit_forms
							WHERE
								status = 8000
								AND
								reviewed_on >= {queryTimeRange.From}
								AND
								reviewed_on < {queryTimeRange.To}
							GROUP BY
								player_code,
								""date""
						)
						SELECT
							DS.player_code,
							DS.""date"",
							DS.deposit_amount,
							DS.deposit_count,
							COALESCE(TFD.first_deposit_on, DS.daily_first_deposit_on) AS daily_first_deposit_on,
							DS.deposit_count + COALESCE(TFD.total_deposit_count, 0) AS total_deposit_count,
							DS.deposit_amount + COALESCE(TFD.total_deposit_amount, 0) AS total_deposit_amount
						FROM
							depositStatistics AS DS
							LEFT JOIN
							(
								SELECT
									PS.player_code,
									Max(PS.report_date) AS report_date,
									Max(PS.total_deposit_count) AS total_deposit_count,
									Max(PS.total_deposit_amount) AS total_deposit_amount,
									Min(PS.first_deposit_on) AS first_deposit_on
								FROM
									dbo.player_daily_payment_statistics AS PS
									JOIN
									depositStatistics AS DS1 ON PS.player_code = DS1.player_code
								WHERE
									PS.report_date < {To}
								GROUP BY
									PS.player_code
							) AS TFD ON DS.player_code = TFD.player_code
					) AS D
					FULL OUTER JOIN
					(
						WITH withdrawStatistics
						(
							player_code,
							""date"",
							withdraw_amount,
							withdraw_count
						)
						AS
						(
							SELECT
								user_id,
								(paid_on at time zone 'Asia/Shanghai')::date AS date,
								Sum(COALESCE(actual_amount, amount)) AS withdraw_amount,
								Count(1) AS withdraw_count
							FROM
								dbo.withdraw_forms
							WHERE
								status = 9000
								AND
								user_type = 1
								AND
								paid_on >= {queryTimeRange.From}
								AND
								paid_on < {queryTimeRange.To}
							GROUP BY
								user_id,
								date
						)
						SELECT
							WS.player_code,
							WS.""date"",
							WS.withdraw_amount,
							WS.withdraw_count,
							WS.withdraw_count + COALESCE(TFW.total_withdraw_count, 0) AS total_withdraw_count,
							WS.withdraw_amount + COALESCE(TFw.total_withdraw_amount, 0) AS total_withdraw_amount
						FROM
							withdrawStatistics AS WS
							LEFT JOIN
							(
								SELECT
									PS.player_code,
									Max(PS.total_withdraw_count) AS total_withdraw_count,
									Max(PS.total_withdraw_amount) AS total_withdraw_amount
								FROM
									dbo.player_daily_payment_statistics AS PS
									JOIN
									withdrawStatistics AS WS1 ON PS.player_code = WS1.player_code
								WHERE
									PS.report_date < {To}
								GROUP BY
									PS.player_code
							) AS TFW ON WS.player_code = TFW.player_code
					) AS W ON D.player_code = W.player_code
						AND D.date = W.date")
			.AsNoTracking()
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var existsData = await _tgsContext.PlayerDailyPaymentStatistics
			.Where(row => row.ReportDate == To)
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var leftJoin = from input in processData
					   join db in existsData
						on new { input.PlayerCode, input.ReportDate } equals new { db.PlayerCode, db.ReportDate } into g
					   from db in g.DefaultIfEmpty()
					   select new
					   {
						   Input = input,
						   Db = db
					   };

		var rightJoin = from db in existsData
						join input in processData
							on new { db.PlayerCode, db.ReportDate } equals new { input.PlayerCode, input.ReportDate } into g
						from input in g.DefaultIfEmpty()
						where input is null
						select db;

		foreach (var p in leftJoin)
			if (p.Db is not null)
			{
				var modifyRow = p.Db;
				modifyRow.DailyDepositAmount = p.Input.DailyDepositAmount;
				modifyRow.DailyDepositCount = p.Input.DailyDepositCount;
				modifyRow.TotalDepositAmount = p.Input.TotalDepositAmount;
				modifyRow.TotalDepositCount = p.Input.TotalDepositCount;
				modifyRow.FirstDepositOn = p.Input.FirstDepositOn;
				modifyRow.DailyWithdrawAmount = p.Input.DailyWithdrawAmount;
				modifyRow.DailyWithdrawCount = p.Input.DailyWithdrawCount;
				modifyRow.TotalWithdrawAmount = p.Input.TotalWithdrawAmount;
				modifyRow.TotalWithdrawCount = p.Input.TotalWithdrawCount;
			}
			else
				_ = await _tgsContext.PlayerDailyPaymentStatistics
					.AddAsync(p.Input, cancellationToken)
					.ConfigureAwait(false);

		_tgsContext.RemoveRange(rightJoin);

		_ = await _tgsContext
			.SaveChangesAsync(cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask<DateOnly?> GetPlayerDailyPaymentStatisticsLastProcessDateAsync(CancellationToken cancellationToken = default)
	{
		var setting = await _tgsContext.BackgroundPaymentStatisticsSettings
			.FirstOrDefaultAsync(
				setting => setting.RecordKey == "PlayerDailyPayment",
				cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var value = setting is null
			? (DateTimeOffset?)null
			: new DateTimeOffset(setting.ProcessedTime).ToOffset(TimeSpan.FromHours(8D));

		return value is null
			? null
			: DateOnly.FromDateTime(value.Value.DateTime);
	}

	public async ValueTask SetPlayerDailyPaymentStatisticsLastProcessDateAsync(DateOnly date, CancellationToken cancellationToken = default)
	{
		var setting = await _tgsContext.BackgroundPaymentStatisticsSettings
			.FirstOrDefaultAsync(
				setting => setting.RecordKey == "PlayerDailyPayment",
				cancellationToken: cancellationToken)
			.ConfigureAwait(false)
			?? _tgsContext.BackgroundPaymentStatisticsSettings.Add(new BackgroundPaymentStatisticsSetting
			{
				RecordKey = "PlayerDailyPayment"
			}).Entity;

		setting.ProcessedTime = date
			.AddDays(-1)
			.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(16D)), DateTimeKind.Utc);

		_ = await _tgsContext
			.SaveChangesAsync(cancellationToken)
			.ConfigureAwait(false);
	}
}
