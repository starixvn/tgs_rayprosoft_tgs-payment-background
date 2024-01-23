namespace Tgs.Payment.Background.ViewModels;

public class PaymentStatisticsViewModel
{
	public required DateTime TimePoint { get; set; }

	public required string UserId { get; set; }

	public required UserType UserType { get; set; }

	public required Guid BrandId { get; set; }

	public decimal? DepositAmount { get; set; }

	public decimal? DepositHandlingFee { get; set; }

	public int? DepositCount { get; set; }

	public decimal? FirstDepositAmount { get; set; }

	public decimal? FirstDepositHandlingFee { get; set; }

	public DateTime? FirstDepositOn { get; set; }

	public decimal? WithdrawAmount { get; set; }

	public decimal? WithdrawHandlingFee { get; set; }

	public int? WithdrawCount { get; set; }

	public decimal? FirstWithdrawAmount { get; set; }

	public decimal? FirstWithdrawHandlingFee { get; set; }

	public DateTime? FirstWithdrawOn { get; set; }
}
