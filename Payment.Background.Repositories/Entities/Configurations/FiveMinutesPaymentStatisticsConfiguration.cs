using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tgs.Payment.Background.Repositories.Entities.Models;

namespace Tgs.Payment.Background.Repositories.Entities.Configurations;
public class FiveMinutesPaymentStatisticsConfiguration : IEntityTypeConfiguration<FiveMinutesPaymentStatistics>
{
	public void Configure(EntityTypeBuilder<FiveMinutesPaymentStatistics> entity)
	{
		entity.HasKey(e => new { e.TimePoint, e.UserId, e.UserType });

		entity.ToTable("five_minutes_payment_statistics", "dbo");

		entity.Property(e => e.BrandId);
		entity.Property(e => e.DepositCount);
		entity.Property(e => e.DepositAmount);
		entity.Property(e => e.DepositHandlingFee);
		entity.Property(e => e.FirstDepositAmount);
		entity.Property(e => e.FirstDepositHandlingFee);
		entity.Property(e => e.FirstDepositOn);
		entity.Property(e => e.WithdrawCount);
		entity.Property(e => e.WithdrawAmount);
		entity.Property(e => e.WithdrawHandlingFee);
		entity.Property(e => e.FirstWithdrawAmount);
		entity.Property(e => e.FirstWithdrawHandlingFee);
		entity.Property(e => e.FirstWithdrawOn);
	}
}
