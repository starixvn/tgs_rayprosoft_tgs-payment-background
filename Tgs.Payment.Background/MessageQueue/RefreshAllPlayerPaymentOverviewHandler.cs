using Valhalla.MessageQueue;

namespace Tgs.Payment.Background.MessageQueue;

public class RefreshAllPlayerPaymentOverviewHandler(ITgsRepository tgsRepository) : IMessageHandler<ReadOnlyMemory<byte>>
{
	private readonly ITgsRepository _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));

	public ValueTask HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
		=> _tgsRepository.UpdateAllPlayerPaymentOverviewAsync(cancellationToken);
}
