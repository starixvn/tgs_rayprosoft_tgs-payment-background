namespace Tgs.Payment.Background.Repositories;

internal class DailyBrandDepositStatusSummary
{
	public Guid BrandId { get; set; }

	public int Approved { get; set; }

	public int Reject { get; set; }

	public int Total { get; set; }
}