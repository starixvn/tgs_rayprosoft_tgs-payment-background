using System.Threading.Tasks.Dataflow;

namespace Tgs.Payment.Background;

internal class PaymentOverviewUpdateEventSource : IPaymentOverviewUpdateSender, IPaymentOverviewUpdateEventSource
{
	private readonly BroadcastBlock<string> _playerUpdateEventSource = new(null);

	public IObservable<string> Observable => _playerUpdateEventSource.AsObservable();

	public void Post(string playerCode)
	{
		_ = _playerUpdateEventSource.Post(playerCode);
	}
}
