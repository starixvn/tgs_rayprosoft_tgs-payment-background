WITH CTE 
(
	player_code, 
	"date", 
	deposit_amount, 
	deposit_count, 
	daily_first_deposit_on,
	withdraw_amount, 
	withdraw_count
)
AS
(
	SELECT
		COALESCE(D.player_code, W.user_id) AS player_code,
		COALESCE(D.date, W.date) AS "date",
		D.deposit_amount,
		D.deposit_count,
		D.daily_first_deposit_on,
		W.withdraw_amount,
		W.withdraw_count
	FROM
		(
			SELECT
				player_code,
				(reviewed_on at time zone 'Asia/Shanghai')::date AS date,
				Sum(COALESCE(actual_amount, amount)) AS deposit_amount,
				Min(reviewed_on) AS daily_first_deposit_on,
				Count(1) as deposit_count
			FROM
				deposit_forms
			WHERE
				status = 8000
				AND
				reviewed_on IS NOT NULL
			GROUP BY
				player_code,
				"date"
			ORDER BY
				"date"
		) AS D
		FULL OUTER JOIN
		(
			SELECT
				user_id,
				(paid_on at time zone 'Asia/Shanghai')::date AS date,
				Sum(COALESCE(actual_amount, amount)) AS withdraw_amount,
				Count(1) AS withdraw_count
			FROM
				withdraw_forms
			WHERE
				status = 9000
				AND
				user_type = 1
				AND
				paid_on IS NOT NULL
			GROUP BY
				user_id,
				date
			ORDER BY
				date
		) AS W ON D.player_code = W.user_id
			AND D.date = W.date
)
SELECT
	date AS report_date,
	player_code,
	deposit_amount AS daily_deposit_amount,
	deposit_count AS daily_deposit_count,
	Sum(deposit_amount) OVER w AS total_deposit_amount,
	Sum(deposit_count) OVER w AS total_deposit_count,
	withdraw_amount AS daily_withdraw_amount,
	withdraw_count AS daily_withdraw_count,
	Sum(withdraw_amount) OVER w AS total_withdraw_amount,
	Sum(withdraw_count) OVER w AS total_withdraw_count,
	Min(daily_first_deposit_on) OVER w AS first_deposit_on
FROM
	CTE
WINDOW w AS (PARTITION BY player_code ORDER BY "date")
ORDER BY
	report_date

	

	
