using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks.Dataflow;

namespace Tgs.Payment.Background;

internal class PaymentOverviewUpdateEventSource : IPaymentOverviewUpdateSender, IPaymentOverviewUpdateEventSource
{
	private readonly Subject<string> _playerUpdateEventSource = new();

	public IObservable<string> Observable => _playerUpdateEventSource.AsObservable();

	public void Post(string playerCode)
	{
		_playerUpdateEventSource.OnNext(playerCode);
	}
}
