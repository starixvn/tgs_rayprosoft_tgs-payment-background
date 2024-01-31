using Google.Protobuf.WellKnownTypes;
using Tgs.Payment.Background.MessageQueue;
using Tgs.Payment.Background.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.Background.IntegrationTests;

[TestClass]
public class FiveMinutesUserPaymentSummaryHandlerTests
{
	[TestMethod]
	public async Task FiveMinutesUserPaymentSummaryHandler_當還沒有上次處理時間時會處理前5分鐘的資料()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var sut = new FiveMinutesUserPaymentSummaryHandler(fakeTgsRepository, fakeMessageSender);

		var processDate = DateTime.ParseExact("2024-01-17T09:56:00.0000000Z", "o", null).ToUniversalTime();
		var request = new FiveMinuteProcessRequest
		{
			ProcessDate = Timestamp.FromDateTime(processDate)
		};

		await sut.HandleAsync("test", request, null);

		_ = fakeTgsRepository.Received(1)
			.QueryUserPaymentSummaryStatisticsAsync(
				Arg.Is(DateTime.ParseExact("2024-01-17T09:50:00.0000000Z", "o", null).ToUniversalTime()),
				Arg.Is(DateTime.ParseExact("2024-01-17T09:55:00.0000000Z", "o", null).ToUniversalTime()));
		_ = fakeTgsRepository.Received(1)
			.QueryUserPaymentSummaryStatisticsAsync(
				Arg.Any<DateTime>(),
				Arg.Any<DateTime>());
		_ = fakeTgsRepository.Received(1)
			.UpsertFiveMinutesUserPaymentSummaryStatisticsAsync(
				Arg.Any<DateTime>(),
				Arg.Any<IEnumerable<UserPaymentSummaryStatistics>>());
		_ = fakeTgsRepository.Received(1)
			.UpsertFiveMinutesUserPaymentSummaryStatisticsAsync(
				Arg.Is(DateTime.ParseExact("2024-01-17T09:50:00.0000000Z", "o", null).ToUniversalTime()),
				Arg.Any<IEnumerable<UserPaymentSummaryStatistics>>());
		_ = fakeTgsRepository.SetUserFiveMinutesLastProcessTimeAsync(
			Arg.Is(DateTime.ParseExact("2024-01-17T09:55:00.0000000Z", "o", null).ToUniversalTime()));
	}

	[TestMethod]
	public async Task FiveMinutesUserPaymentSummaryHandler_取得上次處理時間後_會依照處理時間區間處理所有的5分鐘資料()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var sut = new FiveMinutesUserPaymentSummaryHandler(fakeTgsRepository, fakeMessageSender);

		var processDate = DateTime.ParseExact("2024-01-17T09:56:00.0000000Z", "o", null).ToUniversalTime();
		var request = new FiveMinuteProcessRequest
		{
			ProcessDate = Timestamp.FromDateTime(processDate)
		};

		_ = fakeTgsRepository.GetUserFiveMinutesLastProcessTimeAsync()
			.Returns(DateTime.ParseExact("2024-01-17T09:45:00.0000000Z", "o", null).ToUniversalTime());

		await sut.HandleAsync("test", request, null);

		_ = fakeTgsRepository.Received(2)
			.QueryUserPaymentSummaryStatisticsAsync(
				Arg.Any<DateTime>(),
				Arg.Any<DateTime>());
		_ = fakeTgsRepository.Received(1)
			.QueryUserPaymentSummaryStatisticsAsync(
				Arg.Is(DateTime.ParseExact("2024-01-17T09:45:00.0000000Z", "o", null).ToUniversalTime()),
				Arg.Is(DateTime.ParseExact("2024-01-17T09:50:00.0000000Z", "o", null).ToUniversalTime()));
		_ = fakeTgsRepository.Received(1)
			.QueryUserPaymentSummaryStatisticsAsync(
				Arg.Is(DateTime.ParseExact("2024-01-17T09:50:00.0000000Z", "o", null).ToUniversalTime()),
				Arg.Is(DateTime.ParseExact("2024-01-17T09:55:00.0000000Z", "o", null).ToUniversalTime()));
		_ = fakeTgsRepository.Received(2)
			.UpsertFiveMinutesUserPaymentSummaryStatisticsAsync(
				Arg.Any<DateTime>(),
				Arg.Any<IEnumerable<UserPaymentSummaryStatistics>>());
		_ = fakeTgsRepository.Received(1)
			.UpsertFiveMinutesUserPaymentSummaryStatisticsAsync(
				Arg.Is(DateTime.ParseExact("2024-01-17T09:45:00.0000000Z", "o", null).ToUniversalTime()),
				Arg.Any<IEnumerable<UserPaymentSummaryStatistics>>());
		_ = fakeTgsRepository.Received(1)
			.UpsertFiveMinutesUserPaymentSummaryStatisticsAsync(
				Arg.Is(DateTime.ParseExact("2024-01-17T09:50:00.0000000Z", "o", null).ToUniversalTime()),
				Arg.Any<IEnumerable<UserPaymentSummaryStatistics>>());
		_ = fakeTgsRepository.SetUserFiveMinutesLastProcessTimeAsync(
			Arg.Is(DateTime.ParseExact("2024-01-17T09:55:00.0000000Z", "o", null).ToUniversalTime()));
	}

	[TestMethod]
	public async Task FiveMinutesUserPaymentSummaryHandler_如果處理時間比上次處理時間還舊_代表要重做處理時間到上次處理時間的資料()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var sut = new FiveMinutesUserPaymentSummaryHandler(fakeTgsRepository, fakeMessageSender);

		var processDate = DateTime.ParseExact("2024-01-17T09:45:00.0000000Z", "o", null).ToUniversalTime();
		var request = new FiveMinuteProcessRequest
		{
			ProcessDate = Timestamp.FromDateTime(processDate)
		};

		_ = fakeTgsRepository.GetUserFiveMinutesLastProcessTimeAsync()
			.Returns(DateTime.ParseExact("2024-01-17T09:55:00.0000000Z", "o", null).ToUniversalTime());

		await sut.HandleAsync("test", request, null);

		_ = fakeTgsRepository.Received(2)
			.QueryUserPaymentSummaryStatisticsAsync(
				Arg.Any<DateTime>(),
				Arg.Any<DateTime>());
		_ = fakeTgsRepository.Received(1)
			.QueryUserPaymentSummaryStatisticsAsync(
				Arg.Is(DateTime.ParseExact("2024-01-17T09:45:00.0000000Z", "o", null).ToUniversalTime()),
				Arg.Is(DateTime.ParseExact("2024-01-17T09:50:00.0000000Z", "o", null).ToUniversalTime()));
		_ = fakeTgsRepository.Received(1)
			.QueryUserPaymentSummaryStatisticsAsync(
				Arg.Is(DateTime.ParseExact("2024-01-17T09:50:00.0000000Z", "o", null).ToUniversalTime()),
				Arg.Is(DateTime.ParseExact("2024-01-17T09:55:00.0000000Z", "o", null).ToUniversalTime()));
		_ = fakeTgsRepository.Received(2)
			.UpsertFiveMinutesUserPaymentSummaryStatisticsAsync(
				Arg.Any<DateTime>(),
				Arg.Any<IEnumerable<UserPaymentSummaryStatistics>>());
		_ = fakeTgsRepository.Received(1)
			.UpsertFiveMinutesUserPaymentSummaryStatisticsAsync(
				Arg.Is(DateTime.ParseExact("2024-01-17T09:45:00.0000000Z", "o", null).ToUniversalTime()),
				Arg.Any<IEnumerable<UserPaymentSummaryStatistics>>());
		_ = fakeTgsRepository.Received(1)
			.UpsertFiveMinutesUserPaymentSummaryStatisticsAsync(
				Arg.Is(DateTime.ParseExact("2024-01-17T09:50:00.0000000Z", "o", null).ToUniversalTime()),
				Arg.Any<IEnumerable<UserPaymentSummaryStatistics>>());
	}

	[TestMethod]
	public async Task FiveMinutesUserPaymentSummaryHandler_當還沒有上次處理時間時會處理前5分鐘的資料_處理時間剛好在5分的刻度上()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var sut = new FiveMinutesUserPaymentSummaryHandler(fakeTgsRepository, fakeMessageSender);

		var processDate = DateTime.ParseExact("2024-01-18T16:00:00.0000000Z", "o", null).ToUniversalTime();
		var request = new FiveMinuteProcessRequest
		{
			ProcessDate = Timestamp.FromDateTime(processDate)
		};

		await sut.HandleAsync("test", request, null);

		_ = fakeTgsRepository.Received(1)
			.QueryUserPaymentSummaryStatisticsAsync(
				Arg.Is(DateTime.ParseExact("2024-01-18T15:50:00.0000000Z", "o", null).ToUniversalTime()),
				Arg.Is(DateTime.ParseExact("2024-01-18T15:55:00.0000000Z", "o", null).ToUniversalTime()));
		_ = fakeTgsRepository.Received(1)
			.UpsertFiveMinutesUserPaymentSummaryStatisticsAsync(
				Arg.Is(DateTime.ParseExact("2024-01-18T15:50:00.0000000Z", "o", null).ToUniversalTime()),
				Arg.Any<IEnumerable<UserPaymentSummaryStatistics>>());
		_ = fakeTgsRepository.Received(1)
			.QueryUserPaymentSummaryStatisticsAsync(
				Arg.Any<DateTime>(),
				Arg.Any<DateTime>());
		_ = fakeTgsRepository.Received(1)
			.SetUserFiveMinutesLastProcessTimeAsync(
				Arg.Is(DateTime.ParseExact("2024-01-18T15:55:00.0000000Z", "o", null).ToUniversalTime()));
	}

	[TestMethod]
	public async Task FiveMinutesUserPaymentSummaryHandler_不是重做模式時_會發出已完成的時間刻度事件()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var sut = new FiveMinutesUserPaymentSummaryHandler(fakeTgsRepository, fakeMessageSender);

		var processDate = DateTime.ParseExact("2024-01-17T09:51:00.0000000Z", "o", null).ToUniversalTime();
		var request = new FiveMinuteProcessRequest
		{
			ProcessDate = Timestamp.FromDateTime(processDate)
		};

		_ = fakeTgsRepository.GetUserFiveMinutesLastProcessTimeAsync()
			.Returns(DateTime.ParseExact("2024-01-17T09:45:00.0000000Z", "o", null).ToUniversalTime());

		await sut.HandleAsync("test", request, null);

		_ = fakeMessageSender.Received(1)
			.PublishAsync(
				Arg.Is("tgs.payment.background.event.5min"),
				Arg.Any<FiveMinuteProcessedEvent>(),
				Arg.Any<CancellationToken>());
	}
}
