using Google.Protobuf;
using NSubstitute;
using Tgs.MessageQueue;
using Tgs.Payment.Background.MessageQueue;
using Tgs.Payment.Background.Protos;

namespace Tgs.Payment.Background.IntegrationTests;

[TestClass]
public class AgentDailyPaymentHandlerTests
{
	[TestMethod]
	public async Task Process_Request_By_Daily()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var sut = new AgentDailyPaymentHandler(fakeTgsRepository, fakeMessageSender);

		var request = new ProcessRequest
		{
			ProcessDate = DateOnly.Parse("2022-07-01")
		};

		await sut.HandleAsync(request.ToByteArray(), CancellationToken.None);

		_ = fakeTgsRepository.Received(1)
			.ProcessAgentDailyPaymentStatisticsAsync(Arg.Is((DateOnly)request.ProcessDate));
		_ = fakeTgsRepository.Received(1)
			.SetAgentDailyPaymentStatisticsLastProcessDateAsync(Arg.Is((DateOnly)request.ProcessDate));
		_ = fakeMessageSender.Received(1)
			.SendAsync(
				Arg.Is("tgs.payment.background.player"),
				Arg.Any<ReadOnlyMemory<byte>>(),
				Arg.Any<IEnumerable<MessageHeaderValue>>());
	}

	[TestMethod]
	public async Task Process_Request_By_Daily_Already_Has_ProcessDate()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var sut = new AgentDailyPaymentHandler(fakeTgsRepository, fakeMessageSender);

		var request = new ProcessRequest
		{
			ProcessDate = DateOnly.Parse("2022-07-01")
		};

		_ = fakeTgsRepository.GetAgentDailyPaymentStatisticsLastProcessDateAsync()
			.Returns(DateOnly.Parse("2022-06-30"));

		await sut.HandleAsync(request.ToByteArray(), CancellationToken.None);

		_ = fakeTgsRepository.Received(1)
			.ProcessAgentDailyPaymentStatisticsAsync(Arg.Is((DateOnly)request.ProcessDate));
		_ = fakeTgsRepository.Received(1)
			.SetAgentDailyPaymentStatisticsLastProcessDateAsync(Arg.Is((DateOnly)request.ProcessDate));
		_ = fakeMessageSender.Received(1)
			.SendAsync(
				Arg.Is("tgs.payment.background.player"),
				Arg.Any<ReadOnlyMemory<byte>>(),
				Arg.Any<IEnumerable<MessageHeaderValue>>());
	}

	[TestMethod]
	public async Task Reprocess_PlayerDailyPaymentStatisics_DataAsync()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var sut = new AgentDailyPaymentHandler(fakeTgsRepository, fakeMessageSender);

		var request = new ProcessRequest
		{
			ProcessDate = DateOnly.Parse("2022-06-27")
		};

		_ = fakeTgsRepository.GetAgentDailyPaymentStatisticsLastProcessDateAsync()
			.Returns(DateOnly.Parse("2022-06-30"));

		await sut.HandleAsync(request.ToByteArray(), CancellationToken.None);

		_ = fakeTgsRepository.Received(0)
			.SetAgentDailyPaymentStatisticsLastProcessDateAsync(Arg.Is((DateOnly)request.ProcessDate));
		_ = fakeMessageSender.Received(1)
			.SendAsync(
				Arg.Is("tgs.payment.background.player"),
				Arg.Any<ReadOnlyMemory<byte>>(),
				Arg.Any<IEnumerable<MessageHeaderValue>>());

		_ = fakeTgsRepository.Received(1)
			.ProcessAgentDailyPaymentStatisticsAsync(Arg.Is(DateOnly.Parse("2022-06-27")));
		_ = fakeTgsRepository.Received(1)
			.ProcessAgentDailyPaymentStatisticsAsync(Arg.Is(DateOnly.Parse("2022-06-28")));
		_ = fakeTgsRepository.Received(1)
			.ProcessAgentDailyPaymentStatisticsAsync(Arg.Is(DateOnly.Parse("2022-06-29")));
		_ = fakeTgsRepository.Received(1)
			.ProcessAgentDailyPaymentStatisticsAsync(Arg.Is(DateOnly.Parse("2022-06-30")));
	}
}
