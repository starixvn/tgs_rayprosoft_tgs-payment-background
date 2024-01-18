using Tgs.Payment.Background.Models;
using Tgs.Payment.Background.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.Background.MessageQueue;

public class HourUserPaymentSummaryHandler(ITgsRepository tgsRepository) : IMessageHandler<FiveMinuteProcessRequest>
{
	public async ValueTask HandleAsync(
		string subject,
		FiveMinuteProcessRequest data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var processTime = data.ProcessDate.ToDateTime();

		var previousTime = await tgsRepository.GetUserHourLastProcessTimeAsync(
			cancellationToken).ConfigureAwait(false)
			?? processTime.AddHours(-2D).CeilingHourTime();

		if (processTime > previousTime)
		{
			var lastProcessTime = previousTime;
			foreach (var nextTimePoint in EachProcessTimePoints(previousTime, processTime, cancellationToken))
			{
				await tgsRepository.ProcessHourPaymentStatisticsAsync(
					nextTimePoint,
					cancellationToken).ConfigureAwait(false);

				lastProcessTime = nextTimePoint;
			}

			await tgsRepository.SetUserHourLastProcessTimeAsync(
				lastProcessTime,
				cancellationToken).ConfigureAwait(false);
		}
		else
		{
			var start = processTime.FloorFiveMinutesTime();

			foreach (var nextTimePoint in EachProcessTimePoints(start, previousTime, cancellationToken))
			{
				await tgsRepository.ProcessHourPaymentStatisticsAsync(
					nextTimePoint,
					cancellationToken).ConfigureAwait(false);

				start = nextTimePoint;
			}
		}
	}

	private static IEnumerable<DateTime> EachProcessTimePoints(DateTime start, DateTime end, CancellationToken cancellationToken)
	{
		var nextTimePoint = start;

		while (!cancellationToken.IsCancellationRequested)
		{
			nextTimePoint = nextTimePoint.AddHours(1D);

			if (nextTimePoint <= end)
				yield return nextTimePoint;
			else
				yield break;
		}

		cancellationToken.ThrowIfCancellationRequested();
	}
}
