using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tgs.Payment.Background.Repositories.Entities.Models;
public class FiveMinutesPaymentStatistics
{
	public string UserId { get; set; } = null!;
	public int UserType { get; set; }
	public Guid BrandId { get; set; }

	public DateTime TimePoint { get; set; }

	public decimal? DepositAmount { get; set; }

	public int? DepositCount { get; set; }

	public decimal? WithdrawAmount { get; set; }

	public int? WithdrawCount { get; set; }

	public DateTime? FirstDepositOn { get; set; }

	public decimal? FirstDepositAmount { get; set; }

	public decimal? DepositHandlingFee { get; set; }

	public decimal? WithdrawHandlingFee { get; set; }

	public decimal? FirstDepositHandlingFee { get; set; }

	public decimal? FirstWithdrawAmount { get; set; }

	public decimal? FirstWithdrawHandlingFee { get; set; }

	public DateTime? FirstWithdrawOn { get; set; }
}
