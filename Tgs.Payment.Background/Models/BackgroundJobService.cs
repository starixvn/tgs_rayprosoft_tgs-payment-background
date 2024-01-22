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
		=> eventBus.GetEventObserable<DateTime>(BackgroundMessageConsts.FiveMinutesBackgroundChannelName)!
			.Select(time => time.ToUniversalTime().FloorFiveMinutesTime())
			.Select(time => Observable.FromAsync(async () =>
			{
				try
				{
					var request = new FiveMinuteProcessRequest
					{
						ProcessDate = Timestamp.FromDateTime(time)
					};

					await messageSender.SendAsync(
						"tgs.payment.background.5min",
						request,
						stoppingToken).ConfigureAwait(false);

					if (time.Minute == 5)
						await messageSender.SendAsync(
							"tgs.payment.background.hour",
							request,
							stoppingToken).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Error occur.");
				}
			}))
			.Merge()
			.ToTask(stoppingToken);
}
