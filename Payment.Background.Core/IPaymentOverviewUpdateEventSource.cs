namespace Tgs.Payment.Background;

internal interface IPaymentOverviewUpdateEventSource
{
	IObservable<string> Observable { get; }
}
