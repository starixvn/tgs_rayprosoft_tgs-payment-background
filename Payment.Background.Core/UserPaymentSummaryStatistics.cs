namespace Tgs.Payment.Background;
public record UserPaymentSummaryStatistics(
	string UserId,
	UserType UserType,
	Guid BrandId,
	decimal? DepositAmount,
	decimal? DepositHandlingFee,
	int? DepositCount,
	decimal? FirstDepositAmount,
	decimal? FirstDepositHandlingFee,
	DateTime? FirstDepositOn,
	decimal? WithdrawAmount,
	decimal? WithdrawHandlingFee,
	int? WithdrawCount,
	decimal? FirstWithdrawAmount,
	decimal? FirstWithdrawHandlingFee,
	DateTime? FirstWithdrawOn);
