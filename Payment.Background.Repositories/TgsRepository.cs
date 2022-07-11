using Microsoft.EntityFrameworkCore;
using Tgs.Payment.Background.Repositories.Entities;

namespace Tgs.Payment.Background.Repositories;

internal class TgsRepository : ITgsRepository
{
	private const string PlayerDailyPaymentKey = "PlayerDailyPayment";
	private const string AgentDailyPaymentKey = "AgentDailyPayment";
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
			.Where(row => row.ReportDate == From)
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
				setting => setting.RecordKey == PlayerDailyPaymentKey,
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
				setting => setting.RecordKey == PlayerDailyPaymentKey,
				cancellationToken: cancellationToken)
			.ConfigureAwait(false)
			?? _tgsContext.BackgroundPaymentStatisticsSettings.Add(new BackgroundPaymentStatisticsSetting
			{
				RecordKey = PlayerDailyPaymentKey
			}).Entity;

		setting.ProcessedTime = date
			.AddDays(-1)
			.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(16D)), DateTimeKind.Utc);

		_ = await _tgsContext
			.SaveChangesAsync(cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask ProcessAgentDailyPaymentStatisticsAsync(DateOnly processDate, CancellationToken cancellationToken = default)
	{
		var queryDate = processDate.AddDays(-1);
		var undepositAgainCheckDate = processDate.AddDays(-3);

		var processData = await _tgsContext.AgentDailyPaymentStatistics
			.FromSqlInterpolated($@"
				WITH CTE(
					player_code,
					is_first_deposit,
					un_second_deposit
				)
				AS
				(
					SELECT
						COALESCE(FD.player_code, SD.player_code) AS player_code,
						FD.is_first_deposit,
						CASE
							WHEN SD.player_code IS NULL THEN 0
							ELSE 1
						END AS un_second_deposit
					FROM
						(
							SELECT
								PS.player_code,
								CASE
									WHEN PS.report_date = (PS.first_deposit_on AT TIME ZONE 'Asia/Shanghai')::date THEN 1
									ELSE 0
								END AS is_first_deposit
							FROM
								dbo.player_daily_payment_statistics AS PS
							WHERE
								PS.report_date = {queryDate}
						) AS FD
						FULL OUTER JOIN
						(
							SELECT
								player_code
							FROM
								dbo.player_daily_payment_statistics AS PS
							WHERE
								report_date <= {undepositAgainCheckDate}
							GROUP BY
								player_code
							HAVING
								Max(total_deposit_count) = 1
						) AS SD ON FD.player_code = SD.player_code
				)
				SELECT
					P.agent_id,
					P.brand_id,
					{queryDate} AS report_date,
					COALESCE(Sum(CTE.is_first_deposit), 0) AS first_deposit_players,
					COALESCE(Sum(CTE.un_second_deposit), 0) AS undeposit_again_players,
					Count(CTE.player_code) AS deposit_players
				FROM
					CTE
					JOIN
					dbo.players AS P ON CTE.player_code = P.player_code
				WHERE
					P.agent_id IS NOT NULL
				GROUP BY
					P.brand_id,
					P.agent_id")
			.AsNoTracking()
			.ToArrayAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

		var existsData = await _tgsContext.AgentDailyPaymentStatistics
			.Where(row => row.ReportDate == queryDate)
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var leftJoin = from input in processData
					   join db in existsData
						on new { input.AgentId, input.BrandId, input.ReportDate }
							equals new { db.AgentId, db.BrandId, db.ReportDate } into g
					   from db in g.DefaultIfEmpty()
					   select new
					   {
						   Input = input,
						   Db = db
					   };

		var rightJoin = from db in existsData
						join input in processData
							on new { db.AgentId, db.BrandId, db.ReportDate }
								equals new { input.AgentId, input.BrandId, input.ReportDate } into g
						from input in g.DefaultIfEmpty()
						where input is null
						select db;

		foreach (var p in leftJoin)
			if (p.Db is not null)
			{
				var modifyRow = p.Db;
				modifyRow.FirstDepositPlayers = p.Input.FirstDepositPlayers;
				modifyRow.UndepositAgainPlayers = p.Input.UndepositAgainPlayers;
				modifyRow.DepositPlayers = p.Input.DepositPlayers;
			}
			else
				_ = await _tgsContext.AgentDailyPaymentStatistics
					.AddAsync(p.Input, cancellationToken)
					.ConfigureAwait(false);

		_tgsContext.RemoveRange(rightJoin);

		_ = await _tgsContext
			.SaveChangesAsync(cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask<DateOnly?> GetAgentDailyPaymentStatisticsLastProcessDateAsync(CancellationToken cancellationToken = default)
	{
		var setting = await _tgsContext.BackgroundPaymentStatisticsSettings
			.FirstOrDefaultAsync(
				setting => setting.RecordKey == AgentDailyPaymentKey,
				cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var value = setting is null
			? (DateTimeOffset?)null
			: new DateTimeOffset(setting.ProcessedTime).ToOffset(TimeSpan.FromHours(8D));

		return value is null
			? null
			: DateOnly.FromDateTime(value.Value.DateTime);
	}

	public async ValueTask SetAgentDailyPaymentStatisticsLastProcessDateAsync(DateOnly date, CancellationToken cancellationToken = default)
	{
		var setting = await _tgsContext.BackgroundPaymentStatisticsSettings
			.FirstOrDefaultAsync(
				setting => setting.RecordKey == AgentDailyPaymentKey,
				cancellationToken: cancellationToken)
			.ConfigureAwait(false)
			?? _tgsContext.BackgroundPaymentStatisticsSettings.Add(new BackgroundPaymentStatisticsSetting
			{
				RecordKey = AgentDailyPaymentKey
			}).Entity;

		setting.ProcessedTime = date
			.AddDays(-1)
			.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(16D)), DateTimeKind.Utc);

		_ = await _tgsContext
			.SaveChangesAsync(cancellationToken)
			.ConfigureAwait(false);
	}
}
