using Google.Protobuf;
using NSubstitute;
using Tgs.Payment.Background.MessageQueue;
using Tgs.Payment.Background.Protos;

namespace Tgs.Payment.Background.IntegrationTests;

[TestClass]
public class PlayerDailyPaymentHandlerTests
{
	[TestMethod]
	public async Task Process_Request_By_Daily()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new PlayerDailyPaymentHandler(fakeTgsRepository);

		var request = new ProcessRequest
		{
			ProcessDate = DateOnly.Parse("2022-07-01")
		};

		await sut.HandleAsync(request.ToByteArray(), CancellationToken.None);

		_ = fakeTgsRepository.Received(1)
			.ProcessPlayerDailyPaymentStatisticsAsync(Arg.Is((DateOnly)request.ProcessDate));
		_ = fakeTgsRepository.Received(1)
			.SetPlayerDailyPaymentStatisticsLastProcessDateAsync(Arg.Is((DateOnly)request.ProcessDate));
	}

	[TestMethod]
	public async Task Process_Request_By_Daily_Already_Has_ProcessDate()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new PlayerDailyPaymentHandler(fakeTgsRepository);

		var request = new ProcessRequest
		{
			ProcessDate = DateOnly.Parse("2022-07-01")
		};

		_ = fakeTgsRepository.GetPlayerDailyPaymentStatisticsLastProcessDateAsync()
			.Returns(DateOnly.Parse("2022-06-30"));

		await sut.HandleAsync(request.ToByteArray(), CancellationToken.None);

		_ = fakeTgsRepository.Received(1)
			.ProcessPlayerDailyPaymentStatisticsAsync(Arg.Is((DateOnly)request.ProcessDate));
		_ = fakeTgsRepository.Received(1)
			.SetPlayerDailyPaymentStatisticsLastProcessDateAsync(Arg.Is((DateOnly)request.ProcessDate));
	}

	[TestMethod]
	public async Task Reprocess_PlayerDailyPaymentStatisics_DataAsync()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new PlayerDailyPaymentHandler(fakeTgsRepository);

		var request = new ProcessRequest
		{
			ProcessDate = DateOnly.Parse("2022-06-27")
		};

		_ = fakeTgsRepository.GetPlayerDailyPaymentStatisticsLastProcessDateAsync()
			.Returns(DateOnly.Parse("2022-06-30"));

		await sut.HandleAsync(request.ToByteArray(), CancellationToken.None);

		_ = fakeTgsRepository.Received(0)
			.SetPlayerDailyPaymentStatisticsLastProcessDateAsync(Arg.Is((DateOnly)request.ProcessDate));

		_ = fakeTgsRepository.Received(1)
			.ProcessPlayerDailyPaymentStatisticsAsync(Arg.Is(DateOnly.Parse("2022-06-27")));
		_ = fakeTgsRepository.Received(1)
			.ProcessPlayerDailyPaymentStatisticsAsync(Arg.Is(DateOnly.Parse("2022-06-28")));
		_ = fakeTgsRepository.Received(1)
			.ProcessPlayerDailyPaymentStatisticsAsync(Arg.Is(DateOnly.Parse("2022-06-29")));
		_ = fakeTgsRepository.Received(1)
			.ProcessPlayerDailyPaymentStatisticsAsync(Arg.Is(DateOnly.Parse("2022-06-30")));
	}
}
