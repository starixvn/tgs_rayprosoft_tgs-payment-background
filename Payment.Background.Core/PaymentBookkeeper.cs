using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Valhalla.Awaitables;

namespace Tgs.Payment.Background;

internal class PaymentBookkeeper(
	IPaymentOverviewUpdateEventSource paymentOverviewUpdateEventSource,
	IServiceProvider serviceProvider,
	ILogger<PaymentBookkeeper> logger) : BackgroundService
{
	private readonly IPaymentOverviewUpdateEventSource _paymentOverviewUpdateEventSource = paymentOverviewUpdateEventSource ?? throw new ArgumentNullException(nameof(paymentOverviewUpdateEventSource));
	private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	private readonly ILogger<PaymentBookkeeper> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		=> await Observable.Using(
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
							await TaskHelper.NewThread();

							_ = flags.TryRemove(playerCode, out _);

							return playerCode;
						}));
			})
			.Select(ob => ob
				.SelectMany(playerCode => Observable
					.FromAsync(async ct =>
					{
						await using var scope = _serviceProvider.CreateAsyncScope();

						var tgsRespository = scope.ServiceProvider.GetRequiredService<ITgsRepository>();

						await tgsRespository.UpdatePlayerPaymentOverviewAsync(
							playerCode,
							ct).ConfigureAwait(false);
					})
					.Catch((Exception ex) =>
					{
						_logger.LogError(ex, "Process player({playerCode}) payment overview occur error.", playerCode);

						return Observable.Empty<System.Reactive.Unit>();
					})))
			.Concat()
			.ToTask(stoppingToken)
			.ConfigureAwait(false);
}
