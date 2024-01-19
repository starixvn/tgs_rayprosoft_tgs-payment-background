using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Valhalla.Awaitables;
using Valhalla.Messages;

namespace Tgs.Payment.Background;

internal class PaymentBookkeeper(
	IEventBus eventBus,
	IServiceProvider serviceProvider,
	ILogger<PaymentBookkeeper> logger)
	: BackgroundService
{

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		=> await Observable.Using(
			() => Disposable.Empty,
			res =>
			{
				var flags = new ConcurrentDictionary<string, Guid>();

				return eventBus.GetEventObserable<string>(SchedulerConsts.PaymentOverviewUpdateEventName)!
					.Select(playerCode => (PlayerCode: playerCode, Nonce: Guid.NewGuid()))
					.Where(tup => flags.GetOrAdd(tup.PlayerCode, key => tup.Nonce) == tup.Nonce)
					.Select(tup => tup.PlayerCode)
					.Select(playerCode => Observable
						.FromAsync(async () =>
						{
							await TaskHelper.NewThread();

							_ = flags.TryRemove(playerCode, out _);

							return playerCode;
						}))
					.Concat();
			})
			.Select(playerCode => Observable
				.FromAsync(async ct =>
				{
					await using var scope = serviceProvider.CreateAsyncScope();

					var tgsRespository = scope.ServiceProvider.GetRequiredService<ITgsRepository>();

					await tgsRespository.UpdatePlayerPaymentOverviewAsync(
						playerCode,
						ct).ConfigureAwait(false);
				})
				.Catch((Exception ex) =>
				{
					logger.LogError(ex, "Process player({playerCode}) payment overview occur error.", playerCode);

					return Observable.Empty<System.Reactive.Unit>();
				}))
			.Merge()
			.ToTask(stoppingToken)
			.ConfigureAwait(false);
}
