using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Tgs.Payment.Background.Protos;
using Valhalla.MessageQueue;
using Valhalla.Messages;

namespace Tgs.Payment.Background.Models;

public class BackgroundJobService(
	IEventBus eventBus,
	IMessageSender messageSender,
	ILogger<BackgroundJobService> logger) : BackgroundService
{
	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var fiveMinutesTask = eventBus.GetEventObserable<DateTime>(BackgroundMessageConsts.FiveMinutesBackgroundChannelName)!
			.Select(time => time.ToUniversalTime())
			.Select(time => Observable
				.FromAsync(async () =>
				{
					var request = new FiveMinuteProcessRequest
					{
						ProcessDate = Timestamp.FromDateTime(time)
					};

					await messageSender.SendAsync(
						"tgs.payment.background.5min",
						request,
						stoppingToken).ConfigureAwait(false);

					if (time.FloorFiveMinutesTime().Minute == 5)
						await messageSender.SendAsync(
							"tgs.payment.background.hour",
							request,
							stoppingToken).ConfigureAwait(false);
				})
				.Catch((Exception ex) =>
				{
					logger.LogError(ex, "Error occur.");

					return Observable.Empty<Unit>();
				}))
			.Merge()
			.ToTask(stoppingToken);

		var rerunFiveMinutesTask = eventBus.GetEventObserable<DateTime>(BackgroundMessageConsts.RerunFiveMinutesBackgroundChannelName)!
			.Select(time => time.ToUniversalTime())
			.Select(time => Observable
				.FromAsync(async () =>
				{
					var request = new FiveMinuteProcessRequest
					{
						ProcessDate = Timestamp.FromDateTime(time)
					};

					await messageSender.SendAsync(
						"tgs.payment.background.5min",
						request,
						stoppingToken).ConfigureAwait(false);
				})
				.Catch((Exception ex) =>
				{
					logger.LogError(ex, "Rerun 5min failed.");

					return Observable.Empty<Unit>();
				}))
			.Merge()
			.ToTask(stoppingToken);

		var rerunHourTask = eventBus.GetEventObserable<DateTime>(BackgroundMessageConsts.RerunHourBackgroundChannelName)!
			.Select(time => time.ToUniversalTime())
			.Select(time => Observable
				.FromAsync(async () =>
				{
					var request = new FiveMinuteProcessRequest
					{
						ProcessDate = Timestamp.FromDateTime(time)
					};

					await messageSender.SendAsync(
						"tgs.payment.background.hour",
						request,
						stoppingToken).ConfigureAwait(false);
				})
				.Catch((Exception ex) =>
				{
					logger.LogError(ex, "Rerun hour failed.");

					return Observable.Empty<Unit>();
				}))
			.Merge()
			.ToTask(stoppingToken);

		return Task.WhenAll(fiveMinutesTask, rerunFiveMinutesTask, rerunHourTask);
	}
}
