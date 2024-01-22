using Microsoft.Extensions.DependencyInjection;
using Valhalla.Messages;

namespace Tgs.Payment.Background.IntegrationTests;

[TestClass]
public class PaymentBookkeeperTests
{
	[TestMethod, Timeout(1000)]
	public async Task PaymentBookkeeper會處理玩家金流總覽更新要求()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		await using var web = TestPaymentBackgroundApplication.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureServices(services => services
					.AddSingleton(fakeTgsRepository)));

		var sut = web.Services.GetRequiredService<IEventBus>()
			.GetEventPublisher<string>(SchedulerConsts.PaymentOverviewUpdateEventName)!;

		var playerCode = "test1";

		var tcs = new TaskCompletionSource();

		fakeTgsRepository
			.When(fake => fake.UpdatePlayerPaymentOverviewAsync(
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>()))
			.Do(_ => tcs.SetResult());

		_ = await sut.PublishAsync(playerCode);

		await tcs.Task;
	}

	[TestMethod, Timeout(1000)]
	public async Task PaymentBookkeeper收到兩次玩家金流總覽更新要求就會執行兩次更新()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		await using var web = TestPaymentBackgroundApplication.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureServices(services => services
					.AddSingleton(fakeTgsRepository)));

		var sut = web.Services.GetRequiredService<IEventBus>()
					.GetEventPublisher<string>(SchedulerConsts.PaymentOverviewUpdateEventName)!;

		var playerCode1 = "test1";
		var playerCode2 = "test2";

		var tcs1 = new TaskCompletionSource();
		var tcs2 = new TaskCompletionSource();

		fakeTgsRepository
			.When(fake => fake.UpdatePlayerPaymentOverviewAsync(
				Arg.Is(playerCode1),
				Arg.Any<CancellationToken>()))
			.Do(_ => tcs1.SetResult());
		fakeTgsRepository
			.When(fake => fake.UpdatePlayerPaymentOverviewAsync(
				Arg.Is(playerCode2),
				Arg.Any<CancellationToken>()))
			.Do(_ => tcs2.SetResult());

		_ = await sut.PublishAsync(playerCode1);
		_ = await sut.PublishAsync(playerCode2);

		await tcs1.Task;
		await tcs2.Task;
	}

	[TestMethod]
	public async Task PaymentBookkeeper收到三個玩家金流總覽更新要求就會執行三次更新()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		await using var web = TestPaymentBackgroundApplication.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureServices(services => services
					.AddSingleton(fakeTgsRepository)));

		var sut = web.Services.GetRequiredService<IEventBus>()
			.GetEventPublisher<string>(SchedulerConsts.PaymentOverviewUpdateEventName)!;

		var playerCode1 = "test1";
		var playerCode2 = "test2";
		var playerCode3 = "test3";

		var tcs1 = new TaskCompletionSource();
		var tcs2 = new TaskCompletionSource();
		var tcs3 = new TaskCompletionSource();

		fakeTgsRepository
			.When(fake => fake.UpdatePlayerPaymentOverviewAsync(
				Arg.Is(playerCode1),
				Arg.Any<CancellationToken>()))
			.Do(_ => tcs1.SetResult());
		fakeTgsRepository
			.When(fake => fake.UpdatePlayerPaymentOverviewAsync(
				Arg.Is(playerCode2),
				Arg.Any<CancellationToken>()))
			.Do(_ => tcs2.SetResult());
		fakeTgsRepository
			.When(fake => fake.UpdatePlayerPaymentOverviewAsync(
				Arg.Is(playerCode3),
				Arg.Any<CancellationToken>()))
			.Do(_ => tcs3.SetResult());

		Assert.IsTrue(await sut.PublishAsync(playerCode1));
		Assert.IsTrue(await sut.PublishAsync(playerCode2));
		Assert.IsTrue(await sut.PublishAsync(playerCode3));

		await tcs1.Task;
		await tcs2.Task;
		await tcs3.Task;
	}

	[TestMethod]
	public async Task PaymentBookkeeper接到連續要求會在處理中忽略同樣的要求_後面的只會處理一個()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		await using var web = TestPaymentBackgroundApplication.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureServices(services => services
					.AddSingleton(fakeTgsRepository)));

		var sut = web.Services.GetRequiredService<IEventBus>()
			.GetEventPublisher<string>(SchedulerConsts.PaymentOverviewUpdateEventName)!;

		var playerCode = "test1";

		var tcs1 = new TaskCompletionSource();
		var tcs2 = new TaskCompletionSource();

		var queue = new Queue<TaskCompletionSource>(new[] { tcs1, tcs2 });

		fakeTgsRepository
			.When(fake => fake.UpdatePlayerPaymentOverviewAsync(
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>()))
			.Do(_ =>
			{
				Thread.Sleep(500);

				queue.Dequeue().SetResult();
			});

		for (var i = 0; i < 5; i++)
			_ = sut.PublishAsync(playerCode);
		Thread.Sleep(50);
		_ = sut.PublishAsync(playerCode);

		await tcs1.Task.WaitAsync(TimeSpan.FromSeconds(1D));
		await tcs2.Task.WaitAsync(TimeSpan.FromSeconds(1D));

		_ = fakeTgsRepository.Received(2)
			.UpdatePlayerPaymentOverviewAsync(
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>());
	}
}
