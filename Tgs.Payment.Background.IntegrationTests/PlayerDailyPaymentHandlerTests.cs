using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using Tgs.Payment.Background.MessageQueue;
using Tgs.Payment.Background.Protos;
using Valhalla.Messages;

namespace Tgs.Payment.Background.IntegrationTests;

[TestClass]
public class PlayerDailyPaymentHandlerTests
{
	[TestMethod]
	public async Task 初次執行沒有記錄之前的處理時間時也要能發出更新玩家總覽要求()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeEventSender = Substitute.For<IEventPublisher<string>>();
		var fakeEventBus = Substitute.For<IEventBus>();

		_ = fakeEventBus.GetEventPublisher<string>(Arg.Is(SchedulerConsts.PaymentOverviewUpdateEventName))
			.Returns(fakeEventSender);

		var sut = new PlayerDailyPaymentHandler(
			fakeTgsRepository,
			fakeEventBus,
			NullLogger<PlayerDailyPaymentHandler>.Instance);

		var request = new ProcessRequest
		{
			ProcessDate = DateOnly.Parse("2022-07-01")
		};

		_ = fakeTgsRepository
			.ProcessPlayerDailyPaymentStatisticsAsync(Arg.Is((DateOnly)request.ProcessDate))
			.Returns(new[] { "player1" }.ToAsyncEnumerable());

		await sut.HandleAsync("test", request.ToByteArray(), null);

		_ = fakeEventSender.Received(1)
			.PublishAsync(Arg.Is("player1"));
	}

	[TestMethod]
	public async Task Process_Request_By_Daily()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeEventSender = Substitute.For<IEventPublisher<string>>();
		var fakeEventBus = Substitute.For<IEventBus>();

		_ = fakeEventBus.GetEventPublisher<string>(Arg.Is(SchedulerConsts.PaymentOverviewUpdateEventName))
			.Returns(fakeEventSender);

		var sut = new PlayerDailyPaymentHandler(
			fakeTgsRepository,
			fakeEventBus,
			NullLogger<PlayerDailyPaymentHandler>.Instance);

		var request = new ProcessRequest
		{
			ProcessDate = DateOnly.Parse("2022-07-01")
		};

		await sut.HandleAsync("test", request.ToByteArray(), null);

		_ = fakeTgsRepository.Received(1)
			.ProcessPlayerDailyPaymentStatisticsAsync(Arg.Is((DateOnly)request.ProcessDate));
		_ = fakeTgsRepository.Received(1)
			.SetPlayerDailyPaymentStatisticsLastProcessDateAsync(Arg.Is((DateOnly)request.ProcessDate));
	}

	[TestMethod]
	public async Task 處理玩家統計資料正常單天處理應該發送更新玩家總覽資訊要求()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeEventSender = Substitute.For<IEventPublisher<string>>();
		var fakeEventBus = Substitute.For<IEventBus>();

		_ = fakeEventBus.GetEventPublisher<string>(Arg.Is(SchedulerConsts.PaymentOverviewUpdateEventName))
			.Returns(fakeEventSender);

		var sut = new PlayerDailyPaymentHandler(
			fakeTgsRepository,
			fakeEventBus,
			NullLogger<PlayerDailyPaymentHandler>.Instance);

		var request = new ProcessRequest
		{
			ProcessDate = DateOnly.Parse("2022-07-01")
		};

		_ = fakeTgsRepository.GetPlayerDailyPaymentStatisticsLastProcessDateAsync()
			.Returns(DateOnly.Parse("2022-06-30"));

		_ = fakeTgsRepository.ProcessPlayerDailyPaymentStatisticsAsync(
			Arg.Is((DateOnly)request.ProcessDate),
			Arg.Any<CancellationToken>())
			.Returns(new[] { "player1" }.ToAsyncEnumerable());

		await sut.HandleAsync("test", request.ToByteArray(), null);

		_ = fakeEventSender.Received(1)
			.PublishAsync(Arg.Is("player1"));
	}

	[TestMethod]
	public async Task Process_Request_By_Daily_Already_Has_ProcessDate()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeEventSender = Substitute.For<IEventPublisher<string>>();
		var fakeEventBus = Substitute.For<IEventBus>();

		_ = fakeEventBus.GetEventPublisher<string>(Arg.Is(SchedulerConsts.PaymentOverviewUpdateEventName))
			.Returns(fakeEventSender);

		var sut = new PlayerDailyPaymentHandler(
			fakeTgsRepository,
			fakeEventBus,
			NullLogger<PlayerDailyPaymentHandler>.Instance);

		var request = new ProcessRequest
		{
			ProcessDate = DateOnly.Parse("2022-07-01")
		};

		_ = fakeTgsRepository.GetPlayerDailyPaymentStatisticsLastProcessDateAsync()
			.Returns(DateOnly.Parse("2022-06-30"));

		await sut.HandleAsync("test", request.ToByteArray(), null);

		_ = fakeTgsRepository.Received(1)
			.ProcessPlayerDailyPaymentStatisticsAsync(Arg.Is((DateOnly)request.ProcessDate));
		_ = fakeTgsRepository.Received(1)
			.SetPlayerDailyPaymentStatisticsLastProcessDateAsync(Arg.Is((DateOnly)request.ProcessDate));
	}

	[TestMethod]
	public async Task 在多天處理的情境下也能夠送出更新玩家總覽要求()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeEventSender = Substitute.For<IEventPublisher<string>>();
		var fakeEventBus = Substitute.For<IEventBus>();

		_ = fakeEventBus.GetEventPublisher<string>(Arg.Is(SchedulerConsts.PaymentOverviewUpdateEventName))
			.Returns(fakeEventSender);

		var sut = new PlayerDailyPaymentHandler(
			fakeTgsRepository,
			fakeEventBus,
			NullLogger<PlayerDailyPaymentHandler>.Instance);

		var request = new ProcessRequest
		{
			ProcessDate = DateOnly.Parse("2022-07-01")
		};

		_ = fakeTgsRepository.GetPlayerDailyPaymentStatisticsLastProcessDateAsync()
			.Returns(DateOnly.Parse("2022-06-28"));

		_ = fakeTgsRepository
			.ProcessPlayerDailyPaymentStatisticsAsync(Arg.Is(DateOnly.Parse("2022-6-29")))
			.Returns(new[] { "player1" }.ToAsyncEnumerable());
		_ = fakeTgsRepository
			.ProcessPlayerDailyPaymentStatisticsAsync(Arg.Is(DateOnly.Parse("2022-6-30")))
			.Returns(new[] { "player2" }.ToAsyncEnumerable());
		_ = fakeTgsRepository
			.ProcessPlayerDailyPaymentStatisticsAsync(Arg.Is(DateOnly.Parse("2022-7-1")))
			.Returns(new[] { "player3" }.ToAsyncEnumerable());

		await sut.HandleAsync("test", request.ToByteArray(), null);

		_ = fakeEventSender.Received(1)
			.PublishAsync(Arg.Is("player1"));
		_ = fakeEventSender.Received(1)
			.PublishAsync(Arg.Is("player2"));
		_ = fakeEventSender.Received(1)
			.PublishAsync(Arg.Is("player3"));
	}

	[TestMethod]
	public async Task Process_Request_RequestDate_Grant_Then_ProcessedDate_More()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeEventSender = Substitute.For<IEventPublisher<string>>();
		var fakeEventBus = Substitute.For<IEventBus>();

		_ = fakeEventBus.GetEventPublisher<string>(Arg.Is(SchedulerConsts.PaymentOverviewUpdateEventName))
			.Returns(fakeEventSender);

		var sut = new PlayerDailyPaymentHandler(
			fakeTgsRepository,
			fakeEventBus,
			NullLogger<PlayerDailyPaymentHandler>.Instance);

		var request = new ProcessRequest
		{
			ProcessDate = DateOnly.Parse("2022-07-01")
		};

		_ = fakeTgsRepository.GetPlayerDailyPaymentStatisticsLastProcessDateAsync()
			.Returns(DateOnly.Parse("2022-06-28"));

		await sut.HandleAsync("test", request.ToByteArray(), null);

		_ = fakeTgsRepository.Received(0)
			.ProcessPlayerDailyPaymentStatisticsAsync(Arg.Is(DateOnly.Parse("2022-6-28")));
		_ = fakeTgsRepository.Received(1)
			.ProcessPlayerDailyPaymentStatisticsAsync(Arg.Is(DateOnly.Parse("2022-6-29")));
		_ = fakeTgsRepository.Received(1)
			.ProcessPlayerDailyPaymentStatisticsAsync(Arg.Is(DateOnly.Parse("2022-6-30")));
		_ = fakeTgsRepository.Received(1)
			.ProcessPlayerDailyPaymentStatisticsAsync(Arg.Is(DateOnly.Parse("2022-7-1")));
		_ = fakeTgsRepository.Received(1)
			.SetPlayerDailyPaymentStatisticsLastProcessDateAsync(Arg.Is((DateOnly)request.ProcessDate));
	}

	[TestMethod]
	public async Task 重跑的情節也能送出更新玩家總覽要求()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeEventSender = Substitute.For<IEventPublisher<string>>();
		var fakeEventBus = Substitute.For<IEventBus>();

		_ = fakeEventBus.GetEventPublisher<string>(Arg.Is(SchedulerConsts.PaymentOverviewUpdateEventName))
			.Returns(fakeEventSender);

		var sut = new PlayerDailyPaymentHandler(
			fakeTgsRepository,
			fakeEventBus,
			NullLogger<PlayerDailyPaymentHandler>.Instance);

		var request = new ProcessRequest
		{
			ProcessDate = DateOnly.Parse("2022-06-27")
		};

		_ = fakeTgsRepository.GetPlayerDailyPaymentStatisticsLastProcessDateAsync()
			.Returns(DateOnly.Parse("2022-06-30"));
		_ = fakeTgsRepository
			.ProcessPlayerDailyPaymentStatisticsAsync(Arg.Is(DateOnly.Parse("2022-6-27")))
			.Returns(new[] { "player1" }.ToAsyncEnumerable());
		_ = fakeTgsRepository
			.ProcessPlayerDailyPaymentStatisticsAsync(Arg.Is(DateOnly.Parse("2022-6-28")))
			.Returns(new[] { "player2" }.ToAsyncEnumerable());
		_ = fakeTgsRepository
			.ProcessPlayerDailyPaymentStatisticsAsync(Arg.Is(DateOnly.Parse("2022-6-29")))
			.Returns(new[] { "player3" }.ToAsyncEnumerable());
		_ = fakeTgsRepository
			.ProcessPlayerDailyPaymentStatisticsAsync(Arg.Is(DateOnly.Parse("2022-6-30")))
			.Returns(new[] { "player4" }.ToAsyncEnumerable());

		await sut.HandleAsync("test", request.ToByteArray(), null);

		_ = fakeEventSender.Received(1)
			.PublishAsync(Arg.Is("player1"));
		_ = fakeEventSender.Received(1)
			.PublishAsync(Arg.Is("player2"));
		_ = fakeEventSender.Received(1)
			.PublishAsync(Arg.Is("player3"));
		_ = fakeEventSender.Received(1)
			.PublishAsync(Arg.Is("player4"));
	}

	[TestMethod]
	public async Task Reprocess_PlayerDailyPaymentStatistics_Data()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeEventSender = Substitute.For<IEventPublisher<string>>();
		var fakeEventBus = Substitute.For<IEventBus>();

		_ = fakeEventBus.GetEventPublisher<string>(Arg.Is(SchedulerConsts.PaymentOverviewUpdateEventName))
			.Returns(fakeEventSender);

		var sut = new PlayerDailyPaymentHandler(
			fakeTgsRepository,
			fakeEventBus,
			NullLogger<PlayerDailyPaymentHandler>.Instance);

		var request = new ProcessRequest
		{
			ProcessDate = DateOnly.Parse("2022-06-27")
		};

		_ = fakeTgsRepository.GetPlayerDailyPaymentStatisticsLastProcessDateAsync()
			.Returns(DateOnly.Parse("2022-06-30"));

		await sut.HandleAsync("test", request.ToByteArray(), null);

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
