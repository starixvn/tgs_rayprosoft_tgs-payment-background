using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tgs.Payment.Background.Repositories.Entities;

namespace Tgs.Payment.Background.Repositories;

internal class TgsRepository : ITgsRepository
{
	private const string PlayerDailyPaymentKey = "PlayerDailyPayment";
	private const string AgentDailyPaymentKey = "AgentDailyPayment";
	private const string BrandDailyPaymentKey = "BrandDailyPayment";

	private readonly TgsContext _tgsContext;
	private readonly ILogger<TgsRepository> _logger;

	public TgsRepository(TgsContext tgsContext, ILogger<TgsRepository> logger)
	{
		_tgsContext = tgsContext ?? throw new ArgumentNullException(nameof(tgsContext));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public IAsyncEnumerable<string> ProcessPlayerDailyPaymentStatisticsAsync(DateOnly processDate, CancellationToken cancellationToken = default)
	{
		async Task<string[]> InnerProcessPlayerDailyPaymentStatisticsAsync()
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
				.FromSqlInterpolated($"""
					SELECT
						COALESCE(D.player_code, W.player_code) AS player_code,
						COALESCE(D.date, W.date) AS report_date,
						D.deposit_amount AS daily_deposit_amount,
						D.deposit_handling_fee AS daily_deposit_handling_fee,
						D.deposit_count AS daily_deposit_count,
						D.daily_first_deposit_on AS first_deposit_on,
						D.daily_first_deposit_amount AS first_deposit_amount,
						D.daily_first_deposit_handling_fee AS first_deposit_handling_fee,
						D.total_deposit_count,
						D.total_deposit_amount,
						D.total_deposit_handling_fee,
						W.withdraw_amount AS daily_withdraw_amount,
						W.withdraw_count AS daily_withdraw_count,
						W.withdraw_handling_fee AS daily_withdraw_handling_fee,
						W.total_withdraw_count,
						W.total_withdraw_amount,
						W.total_withdraw_handling_fee,
						W.daily_first_withdraw_on AS first_withdraw_on,
						W.daily_first_withdraw_amount AS first_withdraw_amount,
						W.daily_first_withdraw_handling_fee AS first_withdraw_handling_fee
					FROM
						(
							WITH depositStatistics
							(
								player_code,
								"date",
								deposit_amount,
								deposit_handling_fee,
								daily_first_deposit_on,
								daily_first_deposit_amount,
								daily_first_deposit_handling_fee,
								deposit_count
							)
							AS
							(
								SELECT
									player_code,
									date,
									Sum(deposit_amount) AS deposit_amount,
									Sum(handling_fee) AS deposit_handling_fee,
									Min(daily_first_deposit_on) AS daily_first_deposit_on,
									Min(daily_first_deposit_amount) AS daily_first_deposit_amount,
									Min(daily_first_deposit_handling_fee) AS daily_first_deposit_handling_fee,
									Count(1) AS deposit_count
								FROM
									(
										SELECT
											player_code,
											(updated_on at time zone 'Asia/Shanghai')::date AS date,
											COALESCE(actual_amount, amount) AS deposit_amount,
											handling_fee AS handling_fee,
											First_Value(updated_on) OVER w AS daily_first_deposit_on,
											First_Value(COALESCE(actual_amount, amount)) OVER w AS daily_first_deposit_amount,
											First_Value(handling_fee) OVER w AS daily_first_deposit_handling_fee
										FROM
											dbo.deposit_forms
										WHERE
											status = 8000
											AND
											updated_on >= {queryTimeRange.From}
											AND
											updated_on < {queryTimeRange.To}
										WINDOW w AS (PARTITION BY player_code ORDER BY updated_on ASC)
									) AS T
								GROUP BY
									player_code,
									"date"
							)
							SELECT
								DS.player_code,
								DS."date",
								DS.deposit_amount,
								DS.deposit_handling_fee,
								DS.deposit_count,
								COALESCE(TFD.first_deposit_on, DS.daily_first_deposit_on) AS daily_first_deposit_on,
								COALESCE(TFD.first_deposit_amount, DS.daily_first_deposit_amount) AS daily_first_deposit_amount,
								COALESCE(TFD.first_deposit_handling_fee, DS.daily_first_deposit_handling_fee) AS daily_first_deposit_handling_fee,
								DS.deposit_count + COALESCE(TFD.total_deposit_count, 0) AS total_deposit_count,
								DS.deposit_amount + COALESCE(TFD.total_deposit_amount, 0) AS total_deposit_amount,
								DS.deposit_handling_fee + COALESCE(TFD.total_deposit_handling_fee, 0) AS total_deposit_handling_fee
							FROM
								depositStatistics AS DS
								LEFT JOIN
								(
									SELECT
										PS.player_code,
										Max(PS.report_date) AS report_date,
										Max(PS.total_deposit_count) AS total_deposit_count,
										Max(PS.total_deposit_amount) AS total_deposit_amount,
										Max(PS.total_deposit_handling_fee) as total_deposit_handling_fee,
										Min(PS.first_deposit_on) AS first_deposit_on,
										Min(PS.first_deposit_amount) AS first_deposit_amount,
										Min(PS.first_deposit_handling_fee) AS first_deposit_handling_fee
									FROM
										dbo.player_daily_payment_statistics AS PS
										JOIN
										depositStatistics AS DS1 ON PS.player_code = DS1.player_code
									WHERE
										PS.report_date < {From}
									GROUP BY
										PS.player_code
								) AS TFD ON DS.player_code = TFD.player_code
						) AS D
						FULL OUTER JOIN
						(
							WITH withdrawStatistics
							(
								player_code,
								"date",
								withdraw_amount,
								withdraw_handling_fee,
								daily_first_withdraw_on,
								daily_first_withdraw_amount,
								daily_first_withdraw_handling_fee,
								withdraw_count
							)
							AS
							(
								SELECT
									user_id,
									(paid_on at time zone 'Asia/Shanghai')::date AS date,
									Sum(withdraw_amount) AS withdraw_amount,
									Sum(handling_fee) AS withdraw_handling_fee,
									Min(daily_first_withdraw_on) AS daily_first_withdraw_on,
									Min(daily_first_withdraw_amount) AS daily_first_withdraw_amount,
									Min(daily_first_withdraw_handling_fee) AS daily_first_withdraw_handling_fee,
									Count(1) AS withdraw_count
								FROM
									(
										SELECT
											user_id,
											paid_on,
											COALESCE(actual_amount, amount) AS withdraw_amount,
											handling_fee,
											First_Value(paid_on) OVER w AS daily_first_withdraw_on,
											First_Value(COALESCE(actual_amount, amount)) OVER w AS daily_first_withdraw_amount,
											First_Value(handling_fee) OVER w AS daily_first_withdraw_handling_fee
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
										WINDOW w AS (PARTITION BY user_id ORDER BY paid_on ASC)
									) AS T
								GROUP BY
									user_id,
									date
							)
							SELECT
								WS.player_code,
								WS."date",
								WS.withdraw_amount,
								WS.withdraw_count,
								WS.withdraw_handling_fee,
								WS.withdraw_count + COALESCE(TFW.total_withdraw_count, 0) AS total_withdraw_count,
								WS.withdraw_amount + COALESCE(TFw.total_withdraw_amount, 0) AS total_withdraw_amount,
								WS.withdraw_handling_fee + COALESCE(TFW.total_withdraw_handling_fee, 0) AS total_withdraw_handling_fee,
								COALESCE(TFW.first_withdraw_on, WS.daily_first_withdraw_on) AS daily_first_withdraw_on,
								COALESCE(TFW.first_withdraw_amount, WS.daily_first_withdraw_amount) AS daily_first_withdraw_amount,
								COALESCE(TFW.first_withdraw_handling_fee, WS.daily_first_withdraw_handling_fee) AS daily_first_withdraw_handling_fee
							FROM
								withdrawStatistics AS WS
								LEFT JOIN
								(
									SELECT
										PS.player_code,
										Max(PS.total_withdraw_count) AS total_withdraw_count,
										Max(PS.total_withdraw_amount) AS total_withdraw_amount,
										Max(PS.total_withdraw_handling_fee) AS total_withdraw_handling_fee,
										Min(PS.first_withdraw_on) AS first_withdraw_on,
										Min(PS.first_withdraw_amount) AS first_withdraw_amount,
										Min(PS.first_withdraw_handling_fee) AS first_withdraw_handling_fee
									FROM
										dbo.player_daily_payment_statistics AS PS
										JOIN
										withdrawStatistics AS WS1 ON PS.player_code = WS1.player_code
									WHERE
										PS.report_date < {From}
									GROUP BY
										PS.player_code
								) AS TFW ON WS.player_code = TFW.player_code
						) AS W ON D.player_code = W.player_code
							AND D.date = W.date
					""")
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
					modifyRow.DailyDepositHandlingFee = p.Input.DailyDepositHandlingFee;
					modifyRow.DailyDepositCount = p.Input.DailyDepositCount;
					modifyRow.TotalDepositAmount = p.Input.TotalDepositAmount;
					modifyRow.TotalDepositHandlingFee = p.Input.TotalDepositHandlingFee;
					modifyRow.TotalDepositCount = p.Input.TotalDepositCount;
					modifyRow.FirstDepositOn = p.Input.FirstDepositOn;
					modifyRow.FirstDepositAmount = p.Input.FirstDepositAmount;
					modifyRow.FirstDepositHandlingFee = p.Input.FirstDepositHandlingFee;
					modifyRow.DailyWithdrawAmount = p.Input.DailyWithdrawAmount;
					modifyRow.DailyWithdrawHandlingFee = p.Input.DailyWithdrawHandlingFee;
					modifyRow.DailyWithdrawCount = p.Input.DailyWithdrawCount;
					modifyRow.TotalWithdrawAmount = p.Input.TotalWithdrawAmount;
					modifyRow.TotalWithdrawHandlingFee = p.Input.TotalWithdrawHandlingFee;
					modifyRow.TotalWithdrawCount = p.Input.TotalWithdrawCount;
					modifyRow.FirstWithdrawOn = p.Input.FirstWithdrawOn;
					modifyRow.FirstWithdrawAmount = p.Input.FirstWithdrawAmount;
					modifyRow.FirstWithdrawHandlingFee = p.Input.FirstWithdrawHandlingFee;
				}
				else
					_ = await _tgsContext.PlayerDailyPaymentStatistics
						.AddAsync(p.Input, cancellationToken)
						.ConfigureAwait(false);

			_tgsContext.RemoveRange(rightJoin);

			_ = await _tgsContext
				.SaveChangesAsync(cancellationToken)
				.ConfigureAwait(false);

			return processData
				.Select(p => p.PlayerCode)
				.Distinct()
				.ToArray();
		}

		return InnerProcessPlayerDailyPaymentStatisticsAsync()
			.ToAsyncEnumerable()
			.SelectMany(arr => arr.ToAsyncEnumerable());
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
			.FromSqlInterpolated($"""
				WITH CTE(
					player_code,
					daily_deposit_amount,
					daily_deposit_handling_fee,
					daily_withdraw_amount,
					daily_withdraw_handling_fee,
					is_first_deposit,
					un_second_deposit
				)
				AS
				(
					SELECT
						COALESCE(FD.player_code, SD.player_code) AS player_code,
						FD.daily_deposit_amount,
						FD.daily_deposit_handling_fee,
						FD.daily_withdraw_amount,
						FD.daily_withdraw_handling_fee,
						FD.is_first_deposit,
						CASE
							WHEN SD.player_code IS NULL THEN 0
							ELSE 1
						END AS un_second_deposit
					FROM
						(
							SELECT
								PS.player_code,
								PS.daily_deposit_amount,
								PS.daily_deposit_handling_fee,
								PS.daily_withdraw_amount,
								PS.daily_withdraw_handling_fee,
								CASE
									WHEN PS.daily_deposit_amount IS NULL THEN NULL
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
					Sum(CTE.daily_deposit_amount) AS daily_deposit_amount,
					Sum(CTE.daily_deposit_handling_fee) AS daily_deposit_handling_fee,
					Sum(CTE.daily_withdraw_amount) AS daily_withdraw_amount,
					Sum(CTE.daily_withdraw_handling_fee) AS daily_withdraw_handling_fee,
					COALESCE(Sum(CTE.is_first_deposit), 0) AS first_deposit_players,
					COALESCE(Sum(CTE.un_second_deposit), 0) AS undeposit_again_players,
					Count(CTE.is_first_deposit) AS deposit_players
				FROM
					CTE
					JOIN
					dbo.players AS P ON CTE.player_code = P.player_code
				WHERE
					P.agent_id IS NOT NULL
				GROUP BY
					P.brand_id,
					P.agent_id
				""")
			.AsNoTracking()
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

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
				modifyRow.DailyDepositAmount = p.Input.DailyDepositAmount;
				modifyRow.DailyDepositHandlingFee = p.Input.DailyDepositHandlingFee;
				modifyRow.DailyWithdrawAmount = p.Input.DailyWithdrawAmount;
				modifyRow.DailyWithdrawHandlingFee = p.Input.DailyWithdrawHandlingFee;
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

	public async ValueTask<DateOnly?> GetBrandDailyPaymentStatisticsLastProcessDateAsync(CancellationToken cancellationToken = default)
	{
		var setting = await _tgsContext.BackgroundPaymentStatisticsSettings
			.FirstOrDefaultAsync(
				setting => setting.RecordKey == BrandDailyPaymentKey,
				cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var value = setting is null
			? (DateTimeOffset?)null
			: new DateTimeOffset(setting.ProcessedTime).ToOffset(TimeSpan.FromHours(8D));

		return value is null
			? null
			: DateOnly.FromDateTime(value.Value.DateTime);
	}

	public async ValueTask SetBrandDailyPaymentStatisticsLastProcessDateAsync(DateOnly date, CancellationToken cancellationToken = default)
	{
		var setting = await _tgsContext.BackgroundPaymentStatisticsSettings
			.FirstOrDefaultAsync(
				setting => setting.RecordKey == BrandDailyPaymentKey,
				cancellationToken: cancellationToken)
			.ConfigureAwait(false)
			?? _tgsContext.BackgroundPaymentStatisticsSettings.Add(new BackgroundPaymentStatisticsSetting
			{
				RecordKey = BrandDailyPaymentKey
			}).Entity;

		setting.ProcessedTime = date
			.AddDays(-1)
			.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(16D)), DateTimeKind.Utc);

		_ = await _tgsContext
			.SaveChangesAsync(cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask ProcessBrandDailyPaymentStatisticsAsync(DateOnly processDate, CancellationToken cancellationToken = default)
	{
		var queryDate = processDate.AddDays(-1);
		var undepositAgainCheckDate = processDate.AddDays(-3);

		var processData = await _tgsContext.BrandDailyPaymentStatistics
			.FromSqlInterpolated($@"
				WITH CTE(
					player_code,
					daily_deposit_amount,
					daily_deposit_handling_fee,
					daily_withdraw_amount,
					daily_withdraw_handling_fee,
					is_first_deposit,
					un_second_deposit
				)
				AS
				(
					SELECT
						COALESCE(FD.player_code, SD.player_code) AS player_code,
						FD.daily_deposit_amount,
						FD.daily_deposit_handling_fee,
						FD.daily_withdraw_amount,
						FD.daily_withdraw_handling_fee,
						FD.is_first_deposit,
						CASE
							WHEN SD.player_code IS NULL THEN 0
							ELSE 1
						END AS un_second_deposit
					FROM
						(
							SELECT
								PS.player_code,
								PS.daily_deposit_amount,
								PS.daily_deposit_handling_fee,
								PS.daily_withdraw_amount,
								PS.daily_withdraw_handling_fee,
								CASE
									WHEN PS.daily_deposit_amount IS NULL THEN NULL
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
					P.brand_id,
					{queryDate} AS report_date,
					Sum(CTE.daily_deposit_amount) as daily_deposit_amount,
					Sum(CTE.daily_deposit_handling_fee) as daily_deposit_handling_fee,
					Sum(CTE.daily_withdraw_amount) as daily_withdraw_amount,
					Sum(CTE.daily_withdraw_handling_fee) as daily_withdraw_handling_fee,
					COALESCE(Sum(CTE.is_first_deposit), 0) AS first_deposit_players,
					COALESCE(Sum(CTE.un_second_deposit), 0) AS undeposit_again_players,
					Count(CTE.is_first_deposit) AS deposit_players
				FROM
					CTE
					JOIN
					dbo.players AS P ON CTE.player_code = P.player_code
				GROUP BY
					P.brand_id")
			.AsNoTracking()
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var existsData = await _tgsContext.BrandDailyPaymentStatistics
			.Where(row => row.ReportDate == queryDate)
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var leftJoin = from input in processData
					   join db in existsData
						on new { input.BrandId, input.ReportDate }
							equals new { db.BrandId, db.ReportDate } into g
					   from db in g.DefaultIfEmpty()
					   select new
					   {
						   Input = input,
						   Db = db
					   };

		var rightJoin = from db in existsData
						join input in processData
							on new { db.BrandId, db.ReportDate }
								equals new { input.BrandId, input.ReportDate } into g
						from input in g.DefaultIfEmpty()
						where input is null
						select db;

		foreach (var p in leftJoin)
			if (p.Db is not null)
			{
				var modifyRow = p.Db;
				modifyRow.DailyDepositAmount = p.Input.DailyDepositAmount;
				modifyRow.DailyDepositHandlingFee = p.Input.DailyDepositHandlingFee;
				modifyRow.DailyWithdrawAmount = p.Input.DailyWithdrawAmount;
				modifyRow.DailyWithdrawHandlingFee = p.Input.DailyWithdrawHandlingFee;
				modifyRow.FirstDepositPlayers = p.Input.FirstDepositPlayers;
				modifyRow.UndepositAgainPlayers = p.Input.UndepositAgainPlayers;
				modifyRow.DepositPlayers = p.Input.DepositPlayers;
			}
			else
				_ = await _tgsContext.BrandDailyPaymentStatistics
					.AddAsync(p.Input, cancellationToken)
					.ConfigureAwait(false);

		_tgsContext.RemoveRange(rightJoin);

		_ = await _tgsContext
			.SaveChangesAsync(cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask UpdatePlayerPaymentOverviewAsync(string playerCode, CancellationToken cancellationToken = default)
	{
		_logger.LogDebug("UpdatePlayerPaymentOverview {playerCode}", playerCode);

		var playerLastestRow = await _tgsContext.PlayerDailyPaymentStatistics
			.Where(row => row.PlayerCode == playerCode)
			.OrderByDescending(row => row.ReportDate)
			.FirstOrDefaultAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		if (playerLastestRow is null)
		{
			_logger.LogWarning("Player ({playerCode}) has not found statistics data.", playerCode);
			return;
		}

		var reportDate = playerLastestRow.ReportDate;

		var updateStatistics = await _tgsContext.PlayerPaymentOverviews
			.FromSqlInterpolated($"""
				WITH CTE (
				    player_code,
				    report_date,
				    total_deposit_amount,
				    total_withdraw_amount,
				    first_deposit_amount,
				    first_deposit_handling_fee,
				    first_deposit_on,
				    first_withdraw_amount,
				    first_withdraw_handling_fee,
				    first_withdraw_on
				) AS
				(
				    SELECT
				        player_code,
				        report_date,
				        max(total_deposit_amount) OVER w AS "total_deposit_amount",
				        max(total_withdraw_amount) OVER w AS "total_withdraw_amount",
				        min(first_deposit_amount) OVER w AS "first_deposit_amount",
				        min(first_deposit_handling_fee) OVER w AS "first_deposit_handling_fee",
				        min(first_deposit_on) OVER w AS "first_deposit_on",
				        min(first_withdraw_amount) OVER w AS "first_withdraw_amount",
				        min(first_withdraw_handling_fee) OVER w AS "first_withdraw_handling_fee",
				        min(first_withdraw_on) OVER w AS "first_withdraw_on"
				    FROM
				        dbo.player_daily_payment_statistics
				    WHERE
				        player_code = {playerCode}
				    WINDOW w AS (PARTITION BY  player_code ORDER BY report_date)
				)
				SELECT
				    player_code,
				    COALESCE(total_deposit_amount, 0) AS "total_deposit_amount",
				    COALESCE(total_withdraw_amount, 0) AS "total_withdraw_amount",
				    first_deposit_amount,
				    first_deposit_handling_fee,
				    first_deposit_on,
				    first_withdraw_amount,
				    first_withdraw_handling_fee,
				    first_withdraw_on,
				    report_date AS "recorded_by"
				FROM
				    CTE
				WHERE
				    report_date = {reportDate}
				""")
			.AsNoTracking()
			.FirstAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var updateOverview = await _tgsContext.PlayerPaymentOverviews.FindAsync(
			new object?[] { updateStatistics.PlayerCode },
			cancellationToken: cancellationToken).ConfigureAwait(false)
			?? (await _tgsContext.PlayerPaymentOverviews.AddAsync(
				updateStatistics,
				cancellationToken).ConfigureAwait(false)).Entity;

		updateOverview.TotalDepositAmount = updateStatistics.TotalDepositAmount;
		updateOverview.TotalWithdrawAmount = updateStatistics.TotalWithdrawAmount;
		updateOverview.FirstDepositAmount = updateStatistics.FirstDepositAmount;
		updateOverview.FirstDepositHandlingFee = updateStatistics.FirstDepositHandlingFee;
		updateOverview.FirstDepositOn = updateStatistics.FirstDepositOn;
		updateOverview.FirstWithdrawAmount = updateStatistics.FirstWithdrawAmount;
		updateOverview.FirstWithdrawHandlingFee = updateStatistics.FirstWithdrawHandlingFee;
		updateOverview.FirstWithdrawOn = updateStatistics.FirstWithdrawOn;
		updateOverview.RecordedBy = updateStatistics.RecordedBy;

		_ = await _tgsContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask UpdateAllPlayerPaymentOverviewAsync(CancellationToken cancellationToken = default)
	{
		var updateStatistics = _tgsContext.PlayerPaymentOverviews
			.FromSqlInterpolated($"""
				WITH CTE (
				    player_code,
				    report_date,
				    max_report_date,
				    total_deposit_amount,
				    total_withdraw_amount,
				    first_deposit_amount,
				    first_deposit_handling_fee,
				    first_deposit_on,
				    first_withdraw_amount,
				    first_withdraw_handling_fee,
				    first_withdraw_on
				) AS
				(
				    SELECT
				        player_code,
				        report_date,
				        max(report_date) OVER w AS "max_report_date",
				        max(total_deposit_amount) OVER w AS "total_deposit_amount",
				        max(total_withdraw_amount) OVER w AS "total_withdraw_amount",
				        min(first_deposit_amount) OVER w AS "first_deposit_amount",
				        min(first_deposit_handling_fee) OVER w AS "first_deposit_handling_fee",
				        min(first_deposit_on) OVER w AS "first_deposit_on",
				        min(first_withdraw_amount) OVER w AS "first_withdraw_amount",
				        min(first_withdraw_handling_fee) OVER w AS "first_withdraw_handling_fee",
				        min(first_withdraw_on) OVER w AS "first_withdraw_on"
				    FROM
				        dbo.player_daily_payment_statistics
				    WINDOW w AS (PARTITION BY  player_code)
				)
				SELECT
				    player_code,
				    report_date,
				    COALESCE(total_deposit_amount, 0) AS "total_deposit_amount",
				    COALESCE(total_withdraw_amount, 0) AS "total_withdraw_amount",
				    first_deposit_amount,
				    first_deposit_handling_fee,
				    first_deposit_on,
				    first_withdraw_amount,
				    first_withdraw_handling_fee,
				    first_withdraw_on,
				    report_date AS "recorded_by"
				FROM
				    CTE
				WHERE
				    report_date = max_report_date
				""")
			.AsNoTracking();

		var query = await (from s in updateStatistics
						   join row in _tgsContext.PlayerPaymentOverviews
							   on s.PlayerCode equals row.PlayerCode
							   into g
						   from row in g.DefaultIfEmpty()
						   select new
						   {
							   Statistics = s,
							   Exists = row
						   })
						   .ToArrayAsync(cancellationToken: cancellationToken)
						   .ConfigureAwait(false);

		foreach (var updateInfo in query)
		{
			if (updateInfo.Exists is null)
				(await _tgsContext.PlayerPaymentOverviews
					.AddAsync(updateInfo.Statistics, cancellationToken)
					.ConfigureAwait(false)).State = EntityState.Added;
			else
			{
				_tgsContext.PlayerPaymentOverviews.Attach(updateInfo.Exists).State = EntityState.Modified;

				updateInfo.Exists.TotalDepositAmount = updateInfo.Statistics.TotalDepositAmount;
				updateInfo.Exists.TotalWithdrawAmount = updateInfo.Statistics.TotalWithdrawAmount;
				updateInfo.Exists.FirstDepositAmount = updateInfo.Statistics.FirstDepositAmount;
				updateInfo.Exists.FirstDepositHandlingFee = updateInfo.Statistics.FirstDepositHandlingFee;
				updateInfo.Exists.FirstDepositOn = updateInfo.Statistics.FirstDepositOn;
				updateInfo.Exists.FirstWithdrawAmount = updateInfo.Statistics.FirstWithdrawAmount;
				updateInfo.Exists.FirstWithdrawHandlingFee = updateInfo.Statistics.FirstWithdrawHandlingFee;
				updateInfo.Exists.FirstWithdrawOn = updateInfo.Statistics.FirstWithdrawOn;
				updateInfo.Exists.RecordedBy = updateInfo.Statistics.RecordedBy;
			}

			_ = await _tgsContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
		}
	}
}
