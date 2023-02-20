using Tgs.MessageQueue;

namespace Tgs.Payment.Background.MessageQueue;

public class RefreshAllPlayerPaymentOverviewHandler : IMessageHandler
{
	private readonly ITgsRepository _tgsRepository;

	public RefreshAllPlayerPaymentOverviewHandler(ITgsRepository tgsRepository)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	}

	public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
		=> _tgsRepository.UpdateAllPlayerPaymentOverviewAsync(cancellationToken);
}
