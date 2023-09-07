namespace Tgs.Payment.Background.Repositories;

public class DailyBrandDepositStatusSummary
{
	public Guid BrandId { get; set; }

	public DateOnly ReportDate { get; set; }

	public int Approved { get; set; }

	public int Reject { get; set; }

	public int Total { get; set; }
}