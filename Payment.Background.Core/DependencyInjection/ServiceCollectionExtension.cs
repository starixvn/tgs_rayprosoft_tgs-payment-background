using Tgs.Payment.Background;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtension
{
	public static IServiceCollection AddPaymentBackground(this IServiceCollection services)
		=> services
			.AddHostedService<PaymentBookkeeper>()
			.AddEventBus(config => config
				.RegisterEventSource<string>(SchedulerConsts.PaymentOverviewUpdateEventName));
}
