using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.Payment.Background.MessageQueue;

var builder = WebApplication.CreateBuilder(args);

builder.Logging
	.AddOpenTelemetry(options =>
	{
		options.IncludeFormattedMessage = true;
		options.ParseStateValues = true;
		_ = options.AddFluentdForwardExporter(fluentOptions =>
		{
			var nodeIp = builder.Configuration.GetValue<string>("NODE_IP");
			fluentOptions.Host = nodeIp;
			fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME");
			fluentOptions.UseMessagePack();
		});
	});

builder.Services
	.AddRouting(options => options.LowercaseUrls = true)
	.AddControllers()
	.AddMvcOptions(mvcOptions =>
	{
		mvcOptions.AddWebApiToolkitFilters();
		_ = mvcOptions.UseDateOnlyTimeOnlyStringConverters();
	})
	.AddJsonOptions(jsonOpt => jsonOpt
		.UseDateOnlyTimeOnlyStringConverters());

builder.Services
	.AddPaymentBackgroundRepositories((_, dbOptions) => dbOptions
		.UseNpgsql("name=Tgs"));

builder.Services
	.AddMessageQueue()
	.AddNatsMessageQueue(config => config
		.ConfigQueueOptions((options, sp) =>
		{
			var config = sp.GetRequiredService<IConfiguration>();

			options.Url = config.GetConnectionString("Nats");
		})
		.AddReplyHandler<PlayerDailyPaymentHandler>("tgs.payment.background.player", "tgs.payment.background.player"))
	.AddNatsGlobPatternExchange("*");

builder.Services
	.AddSwaggerGen(c =>
	{
		c.SwaggerDoc("v1", new OpenApiInfo { Title = "PaymentBackgroundService", Version = "v1" });
		c.ApplyWebApiToolkitSupport();
		c.UseDateOnlyTimeOnlyStringConverters();
	});

builder.Services
	.AddHealthChecks();

builder.Services
	.AddSingleton<IConfigureOptions<OtlpExporterOptions>>(
		sp => new ConfigureNamedOptions<OtlpExporterOptions>(
			Options.DefaultName,
			exporterOptions =>
			{
				var config = sp.GetRequiredService<IConfiguration>();
				var nodeIp = config.GetValue<string>("NODE_IP");

				if (!string.IsNullOrWhiteSpace(nodeIp))
					exporterOptions.Endpoint = new Uri($"http://{nodeIp}:4317");

				config.GetSection("Opentelemetry:Tracing").Bind(exporterOptions);
			}))
	.AddOpenTelemetryTracing(
		tracingBuilder => tracingBuilder
			.SetResourceBuilder(ResourceBuilder.CreateDefault()
				.AddEnvironmentVariableDetector())
			.AddSource("Tgs.*")
			.AddSource("TGS.*")
			.AddAspNetCoreInstrumentation()
			.AddNpgsql()
			.AddProfileViewExporter()
			.AddOtlpExporter())
	.Configure<AspNetCoreInstrumentationOptions>(
		options => options.Filter = httpContext => !httpContext.Request.Path.HasValue
			|| !httpContext.Request.Path.StartsWithSegments("/swagger")
			&& !httpContext.Request.Path.StartsWithSegments("/healthz")
			&& !httpContext.Request.Path.StartsWithSegments("/metrics")
			&& !httpContext.Request.Path.StartsWithSegments("/profiler/")
			&& !new[] { "html", "ico", "png", "jpg" }.Any(
				ext => httpContext.Request.Path.Value.EndsWith($".{ext}")))
	.AddProfileViewer();

var app = builder.Build();

app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PaymentService v1"));

app.MapControllers();
app.MapSwagger();
app.MapHealthChecks("/healthz");
app.MapProfileViewer();

app.Run();
