using Google.Protobuf.WellKnownTypes;
using Tgs.Payment.Background.MessageQueue;
using Tgs.Payment.Background.Protos;

namespace Tgs.Payment.Background.IntegrationTests;

[TestClass]
public class HourUserPaymentSummaryHandlerTests
{
	[TestMethod]
	public async Task HourUserPaymentSummaryHandler_當還沒有上次處理時間處理前一小時資料()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new HourUserPaymentSummaryHandler(fakeTgsRepository);

		var processDate = DateTime.ParseExact("2024-01-17T09:56:00.0000000Z", "o", null).ToUniversalTime();
		var request = new FiveMinuteProcessRequest
		{
			ProcessDate = Timestamp.FromDateTime(processDate)
		};

		await sut.HandleAsync("test", request, null);

		_ = fakeTgsRepository.Received(1)
			.ProcessHourPaymentStatisticsAsync(
				Arg.Is(DateTime.ParseExact("2024-01-17T09:00:00.0000000Z", "o", null).ToUniversalTime()));

		_ = fakeTgsRepository.SetUserHourLastProcessTimeAsync(
			Arg.Is(DateTime.ParseExact("2024-01-17T09:00:00.0000000Z", "o", null).ToUniversalTime()));
	}

	[TestMethod]
	public async Task HourUserPaymentSummaryHandler_取得上次處理時間_會依照處理區間處理每小時的資料()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new HourUserPaymentSummaryHandler(fakeTgsRepository);

		var processDate = DateTime.ParseExact("2024-01-17T09:56:00.0000000Z", "o", null).ToUniversalTime();
		var request = new FiveMinuteProcessRequest
		{
			ProcessDate = Timestamp.FromDateTime(processDate)
		};

		_ = fakeTgsRepository.GetUserHourLastProcessTimeAsync()
			.Returns(DateTime.ParseExact("2024-01-17T07:00:00.0000000Z", "o", null).ToUniversalTime());

		await sut.HandleAsync("test", request, null);

		_ = fakeTgsRepository.Received(1)
			.ProcessHourPaymentStatisticsAsync(
				Arg.Is(DateTime.ParseExact("2024-01-17T08:00:00.0000000Z", "o", null).ToUniversalTime()));

		_ = fakeTgsRepository.Received(1)
			.ProcessHourPaymentStatisticsAsync(
				Arg.Is(DateTime.ParseExact("2024-01-17T09:00:00.0000000Z", "o", null).ToUniversalTime()));

		_ = fakeTgsRepository.SetUserHourLastProcessTimeAsync(
			Arg.Is(DateTime.ParseExact("2024-01-17T09:00:00.0000000Z", "o", null).ToUniversalTime()));
	}

	[TestMethod]
	public async Task HourUserPaymentSummaryHandler_如果處理時間比上次處理時間還舊_代表要重做處理時間到上次處理時間的資料()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new HourUserPaymentSummaryHandler(fakeTgsRepository);

		var processDate = DateTime.ParseExact("2024-01-17T08:00:00.0000000Z", "o", null).ToUniversalTime();
		var request = new FiveMinuteProcessRequest
		{
			ProcessDate = Timestamp.FromDateTime(processDate)
		};

		_ = fakeTgsRepository.GetUserHourLastProcessTimeAsync()
			.Returns(DateTime.ParseExact("2024-01-17T09:00:00.0000000Z", "o", null).ToUniversalTime());

		await sut.HandleAsync("test", request, null);

		_ = fakeTgsRepository.Received(1)
			.ProcessHourPaymentStatisticsAsync(
				Arg.Is(DateTime.ParseExact("2024-01-17T09:00:00.0000000Z", "o", null).ToUniversalTime()));
	}
}
