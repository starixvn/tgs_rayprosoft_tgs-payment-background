using Google.Protobuf.WellKnownTypes;
using Tgs.Payment.Background.MessageQueue;
using Tgs.Payment.Background.Protos;

namespace Tgs.Payment.Background.IntegrationTests;

[TestClass]
public class FiveMinutesUserPaymentSummaryHandlerTests
{
	[TestMethod]
	public async Task FiveMinutesUserPaymentSummaryHandler_當還沒有上次處理時間時會處理前5分鐘的資料()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var sut = new FiveMinutesUserPaymentSummaryHandler(fakeTgsRepository);

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
		_ = fakeTgsRepository.SetUserFiveMinutesLastProcessTimeAsync(
			Arg.Is(DateTime.ParseExact("2024-01-17T09:55:00.0000000Z", "o", null).ToUniversalTime()));
	}

	[TestMethod]
	public async Task FiveMinutesUserPaymentSummaryHandler_取得上次處理時間後_會依照處理時間區間處理所有的5分鐘資料()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var sut = new FiveMinutesUserPaymentSummaryHandler(fakeTgsRepository);

		var processDate = DateTime.ParseExact("2024-01-17T09:56:00.0000000Z", "o", null).ToUniversalTime();
		var request = new FiveMinuteProcessRequest
		{
			ProcessDate = Timestamp.FromDateTime(processDate)
		};

		_ = fakeTgsRepository.GetUserFiveMinutesLastProcessTimeAsync()
			.Returns(DateTime.ParseExact("2024-01-17T09:45:00.0000000Z", "o", null).ToUniversalTime());

		await sut.HandleAsync("test", request, null);

		_ = fakeTgsRepository.Received(1)
			.QueryUserPaymentSummaryStatisticsAsync(
				Arg.Is(DateTime.ParseExact("2024-01-17T09:45:00.0000000Z", "o", null).ToUniversalTime()),
				Arg.Is(DateTime.ParseExact("2024-01-17T09:50:00.0000000Z", "o", null).ToUniversalTime()));
		_ = fakeTgsRepository.Received(1)
			.QueryUserPaymentSummaryStatisticsAsync(
				Arg.Is(DateTime.ParseExact("2024-01-17T09:50:00.0000000Z", "o", null).ToUniversalTime()),
				Arg.Is(DateTime.ParseExact("2024-01-17T09:55:00.0000000Z", "o", null).ToUniversalTime()));
		_ = fakeTgsRepository.Received(2)
			.QueryUserPaymentSummaryStatisticsAsync(
				Arg.Any<DateTime>(),
				Arg.Any<DateTime>());
		_ = fakeTgsRepository.SetUserFiveMinutesLastProcessTimeAsync(
			Arg.Is(DateTime.ParseExact("2024-01-17T09:55:00.0000000Z", "o", null).ToUniversalTime()));
	}

	[TestMethod]
	public async Task FiveMinutesUserPaymentSummaryHandler_如果處理時間比上次處理時間還舊_代表要重做處理時間到上次處理時間的資料()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var sut = new FiveMinutesUserPaymentSummaryHandler(fakeTgsRepository);

		var processDate = DateTime.ParseExact("2024-01-17T09:45:00.0000000Z", "o", null).ToUniversalTime();
		var request = new FiveMinuteProcessRequest
		{
			ProcessDate = Timestamp.FromDateTime(processDate)
		};

		_ = fakeTgsRepository.GetUserFiveMinutesLastProcessTimeAsync()
			.Returns(DateTime.ParseExact("2024-01-17T09:55:00.0000000Z", "o", null).ToUniversalTime());

		await sut.HandleAsync("test", request, null);

		_ = fakeTgsRepository.Received(1)
			.QueryUserPaymentSummaryStatisticsAsync(
				Arg.Is(DateTime.ParseExact("2024-01-17T09:45:00.0000000Z", "o", null).ToUniversalTime()),
				Arg.Is(DateTime.ParseExact("2024-01-17T09:50:00.0000000Z", "o", null).ToUniversalTime()));
		_ = fakeTgsRepository.Received(1)
			.QueryUserPaymentSummaryStatisticsAsync(
				Arg.Is(DateTime.ParseExact("2024-01-17T09:50:00.0000000Z", "o", null).ToUniversalTime()),
				Arg.Is(DateTime.ParseExact("2024-01-17T09:55:00.0000000Z", "o", null).ToUniversalTime()));
		_ = fakeTgsRepository.Received(2)
			.QueryUserPaymentSummaryStatisticsAsync(
				Arg.Any<DateTime>(),
				Arg.Any<DateTime>());
	}
}
