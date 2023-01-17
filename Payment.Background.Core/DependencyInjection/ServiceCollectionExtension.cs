using Tgs.Payment.Background;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtension
{
	public static IServiceCollection AddPaymentBackground(this IServiceCollection services)
		=> services
			.AddSingleton<PaymentOverviewUpdateEventSource>()
			.AddTransient<IPaymentOverviewUpdateEventSource>(sp => sp.GetRequiredService<PaymentOverviewUpdateEventSource>())
			.AddTransient<IPaymentOverviewUpdateSender>(sp => sp.GetRequiredService<PaymentOverviewUpdateEventSource>())
			.AddHostedService<PaymentBookkeeper>();
}
