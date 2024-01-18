using Tgs.Payment.Background.Models;
using Tgs.Payment.Background.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.Background.MessageQueue;

internal class FiveMinutesUserPaymentSummaryHandler(
	ITgsRepository tgsRepository)
	: IMessageHandler<FiveMinuteProcessRequest>
{
	public async ValueTask HandleAsync(
		string subject,
		FiveMinuteProcessRequest data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var processTime = data.ProcessDate.ToDateTimeOffset().UtcDateTime;

		var previousTime = await tgsRepository.GetUserFiveMinutesLastProcessTimeAsync(
			cancellationToken).ConfigureAwait(false)
			?? processTime.AddMinutes(-10D).CeilingFiveMinutesTime();

		if (processTime > previousTime)
		{
			var lastProcessTime = previousTime;
			foreach (var nextTimePoint in EachProcessTimePoints(previousTime, processTime, cancellationToken))
			{
				var summaries = await tgsRepository.QueryUserPaymentSummaryStatisticsAsync(
					lastProcessTime,
					nextTimePoint,
					cancellationToken)
					.ToArrayAsync(cancellationToken)
					.ConfigureAwait(false);

				await tgsRepository.UpsertFiveMinutesUserPaymentSummaryStatisticsAsync(
					nextTimePoint,
					summaries,
					cancellationToken).ConfigureAwait(false);

				lastProcessTime = nextTimePoint;
			}

			await tgsRepository.SetUserFiveMinutesLastProcessTimeAsync(
				lastProcessTime,
				cancellationToken).ConfigureAwait(false);
		}
		else
		{
			var start = processTime.FloorFiveMinutesTime();

			foreach (var nextTimePoint in EachProcessTimePoints(start, previousTime, cancellationToken))
			{
				var summaries = await tgsRepository.QueryUserPaymentSummaryStatisticsAsync(
					start,
					nextTimePoint,
					cancellationToken)
					.ToArrayAsync(cancellationToken)
					.ConfigureAwait(false);

				await tgsRepository.UpsertFiveMinutesUserPaymentSummaryStatisticsAsync(
					nextTimePoint,
					summaries,
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
			nextTimePoint = nextTimePoint.AddMinutes(5D);

			if (nextTimePoint <= end)
				yield return nextTimePoint;
			else
				yield break;
		}

		cancellationToken.ThrowIfCancellationRequested();
	}
}
