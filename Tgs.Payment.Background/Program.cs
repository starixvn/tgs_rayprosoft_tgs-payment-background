using FluentdForward.OpenTelemetry.MessagePack;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NATS.Client.Core;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.MessageQueue.Nats.Serializers;
using Tgs.Payment.Background;
using Tgs.Payment.Background.MessageQueue;

var builder = WebApplication.CreateBuilder(args);
{
	_ = builder.Logging
		.AddOpenTelemetry(options =>
		{
			options.IncludeFormattedMessage = true;
			_ = options.AddFluentdForwardExporter(fluentOptions =>
			{
				var nodeIp = builder.Configuration.GetValue<string>("NODE_IP")!;
				fluentOptions.Host = nodeIp;
				fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
				fluentOptions.UseMessagePack(LogRecordFormatterResolver.GetResolverInstanceWithExtendInfo(
					"MachineInfo",
					() => new
					{
						Environment.MachineName
					}));
			});
		});

	_ = builder.Services
		.AddRouting(options => options.LowercaseUrls = true)
		.AddControllers()
		.AddMvcOptions(mvcOptions => mvcOptions.AddWebApiToolkitFilters());

	_ = builder.Services
		.AddPaymentBackground()
		.AddPaymentBackgroundRepositories((_, dbOptions) => dbOptions
			.UseNpgsql("name=Tgs"));

	_ = builder.Services
		.AddKeyedSingleton(
			"Nats",
			(sp, _) => new NatsConnection(NatsOpts.Default with
			{
				Url = builder.Configuration.GetConnectionString("Nats")!,
				SerializerRegistry = TgsNatsSerializerRegistration.Default
			}))
		.AddMessageQueue()
		.AddNatsMessageQueue(config => config
			.ConfigureResolveConnection(sp => sp.GetRequiredKeyedService<NatsConnection>("Nats"))
			.AddReplyHandler<PlayerDailyPaymentHandler>("tgs.payment.background.player", "tgs.payment.background.player")
			.AddReplyHandler<AgentDailyPaymentHandler>("tgs.payment.background.agent", "tgs.payment.background.agent")
			.AddReplyHandler<BrandDailyPaymentHandler>("tgs.payment.background.brand", "tgs.payment.background.brand")
			.AddHandler<RefreshAllPlayerPaymentOverviewHandler>(
				"tgs.payment.background.payment-overview.all",
				"tgs.payment.background.payment-overview.all")
			.AddReplyHandler<FiveMinutesUserPaymentSummaryHandler>("tgs.payment.background.5min", "tgs.payment.background.5min"))
		.AddNatsGlobPatternExchange("*");

	_ = builder.Services
		.AddTransient<PaymentBackgroundJob>()
		.AddHangfire((_, configuration) => configuration
			.UseMemoryStorage())
		.AddHangfireServer();

	_ = builder.Services
		.AddSwaggerGen(c =>
		{
			c.SwaggerDoc("v1", new OpenApiInfo { Title = "PaymentBackgroundService", Version = "v1" });
			c.ApplyWebApiToolkitSupport();
			c.UseDateOnlyTimeOnlyStringConverters();
		});

	var excludeExtensions = new[] { "html", "ico", "png", "jpg" };
	_ = builder.Services
		.AddOpenTelemetry()
		.WithTracing(tracingBuilder => tracingBuilder
			.SetResourceBuilder(ResourceBuilder.CreateDefault()
				.AddEnvironmentVariableDetector())
			.AddSource("Tgs.*")
			.AddSource("TGS.*")
			.AddSource("Valhalla.*")
			.AddAspNetCoreInstrumentation(options => options.Filter =
				httpContext => !httpContext.Request.Path.HasValue
					|| !httpContext.Request.Path.StartsWithSegments("/swagger")
					&& !httpContext.Request.Path.StartsWithSegments("/healthz")
					&& !httpContext.Request.Path.StartsWithSegments("/metrics")
					&& !httpContext.Request.Path.StartsWithSegments("/profiler/")
					&& !excludeExtensions.Any(
						ext => httpContext.Request.Path.Value.EndsWith($".{ext}")))
			.AddNpgsql()
			.AddProfileViewExporter()
			.AddOtlpExporter())
		.WithMetrics(meterBuilder => meterBuilder
			.SetResourceBuilder(ResourceBuilder.CreateDefault()
				.AddEnvironmentVariableDetector())
			.AddAspNetCoreInstrumentation()
			.AddRuntimeInstrumentation()
			.AddOtlpExporter())
		.Services
		.AddProfileViewer()
		.AddHealthChecks();
}

var app = builder.Build();
{
	_ = app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PaymentService v1"));

	_ = app.MapControllers();
	_ = app.MapSwagger();
	_ = app.MapHealthChecks("/healthz");
	app.MapProfileViewer();
}

app.Run();
