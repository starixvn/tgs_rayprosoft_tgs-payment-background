namespace Tgs.Payment.Background;

public interface IPaymentOverviewUpdateSender
{
	void Post(string playerCode);
}
