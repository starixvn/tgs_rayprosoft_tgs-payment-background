using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Tgs.Payment.Background;

internal class PaymentBookkeeper : BackgroundService
{
	private readonly IPaymentOverviewUpdateEventSource _paymentOverviewUpdateEventSource;
	private readonly IServiceProvider _serviceProvider;

	public PaymentBookkeeper(
		IPaymentOverviewUpdateEventSource paymentOverviewUpdateEventSource,
		IServiceProvider serviceProvider)
	{
		_paymentOverviewUpdateEventSource = paymentOverviewUpdateEventSource ?? throw new ArgumentNullException(nameof(paymentOverviewUpdateEventSource));
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_ = stoppingToken.Register(Observable.Using(
			() => Disposable.Empty,
			res =>
			{
				var flags = new ConcurrentDictionary<string, Guid>();

				return _paymentOverviewUpdateEventSource.Observable
					.Select(playerCode => (PlayerCode: playerCode, Nonce: Guid.NewGuid()))
					.Where(tup => flags.GetOrAdd(tup.PlayerCode, key => tup.Nonce) == tup.Nonce)
					.Select(tup => tup.PlayerCode)
					.Select(playerCode => Observable
						.FromAsync(async () =>
						{
							await Task.Yield();

							_ = flags.TryRemove(playerCode, out _);

							return playerCode;
						}));
			})
			.Select(ob => ob
				.SelectMany(playerCode => Observable.FromAsync(async ct =>
				{
					await using var scope = _serviceProvider.CreateAsyncScope();

					var tgsRespository = scope.ServiceProvider.GetRequiredService<ITgsRepository>();

					await tgsRespository.UpdatePlayerPaymentOverviewAsync(
						playerCode,
						ct).ConfigureAwait(false);
				})))
			.Concat()
			.Subscribe()
			.Dispose);

		return Task.CompletedTask;
	}
}
