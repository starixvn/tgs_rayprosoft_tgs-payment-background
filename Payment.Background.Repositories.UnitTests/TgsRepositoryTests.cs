using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.Core;
using Tgs.Payment.Background;
using Tgs.Payment.Background.Repositories;
using Tgs.Payment.Background.Repositories.Entities;

namespace Payment.Background.Repositories.UnitTests;

public class TgsRepositoryTests
{
	[Fact]
	public async Task TgsRepository_取得前一個5分鐘處理時間()
	{
		// Arrange
		var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		var dbContext = new TgsContext(dbContextOptions);

		var sut = new TgsRepository(dbContext, NullLogger<TgsRepository>.Instance);

		var processTime = DateTime.ParseExact("2024-01-17T14:40:00.0000000Z", "o", null).ToUniversalTime();
		var settingRow = new BackgroundPaymentStatisticsSetting
		{
			RecordKey = "FiveMinutesUserPaymentSummary",
			ProcessedTime = processTime
		};

		_ = await dbContext.BackgroundPaymentStatisticsSettings
			.AddAsync(settingRow);

		_ = await dbContext.SaveChangesAsync();

		// Act
		var actual = await sut.GetUserFiveMinutesLastProcessTimeAsync();

		// Assert
		Assert.NotNull(actual);
		Assert.Equal(processTime, actual);
	}

	[Fact]
	public async Task TgsRepository_取得前一個小時處理時間()
	{
		// Arrange
		var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		var dbContext = new TgsContext(dbContextOptions);

		var sut = new TgsRepository(dbContext, NullLogger<TgsRepository>.Instance);

		var processTime = DateTime.ParseExact("2024-01-17T14:00:00.0000000Z", "o", null).ToUniversalTime();
		var settingRow = new BackgroundPaymentStatisticsSetting
		{
			RecordKey = "HourUserPaymentSummary",
			ProcessedTime = processTime
		};

		_ = await dbContext.BackgroundPaymentStatisticsSettings
			.AddAsync(settingRow);

		_ = await dbContext.SaveChangesAsync();

		// Act
		var actual = await sut.GetUserHourLastProcessTimeAsync();

		// Assert
		Assert.NotNull(actual);
		Assert.Equal(processTime, actual);
	}

	[Fact]
	public async Task TgsRepository_保存5分鐘資料匯總處理過的時間()
	{
		// Arrange
		var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		var dbContext = new TgsContext(dbContextOptions);

		var sut = new TgsRepository(dbContext, NullLogger<TgsRepository>.Instance);

		var processTime = DateTime.ParseExact("2024-01-17T14:40:00.0000000Z", "o", null).ToUniversalTime();

		// Act
		await sut.SetUserFiveMinutesLastProcessTimeAsync(processTime);
		var actualRow = await dbContext.BackgroundPaymentStatisticsSettings
			.FindAsync("FiveMinutesUserPaymentSummary");

		// Assert
		Assert.NotNull(actualRow);
		Assert.Equal(processTime, actualRow.ProcessedTime);
	}

	[Fact]
	public async Task TgsRepository_保存每小時資料匯總處理過的時間()
	{
		// Arrange
		var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		var dbContext = new TgsContext(dbContextOptions);

		var sut = new TgsRepository(dbContext, NullLogger<TgsRepository>.Instance);

		var processTime = DateTime.ParseExact("2024-01-17T14:00:00.0000000Z", "o", null).ToUniversalTime();

		// Act
		await sut.SetUserHourLastProcessTimeAsync(processTime);
		var actualRow = await dbContext.BackgroundPaymentStatisticsSettings
			.FindAsync("HourUserPaymentSummary");

		// Assert
		Assert.NotNull(actualRow);
		Assert.Equal(processTime, actualRow.ProcessedTime);
	}

	[Fact]
	public async Task TgsRepository_查詢5分鐘匯總資料()
	{
		// Arrange
		var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.ReplaceService<IQueryCompilationContextFactory, TestQueryCompilationContextFactory>()
			.Options;
		var fakeCreateQueryExecutorWrapper = Substitute.For<CreateQueryExecutorWrapper>();

		_ = TestQueryCompilationContextFactory.Services
			.AddKeyedScoped(
				dbContextOptions,
				(sp, _) => fakeCreateQueryExecutorWrapper);

		var dbContext = new TgsContext(dbContextOptions);
		var sut = new TgsRepository(dbContext, NullLogger<TgsRepository>.Instance);

		var start = DateTime.ParseExact("2024-01-17T14:40:00.0000000Z", "o", null).ToUniversalTime();
		var end = start.AddMinutes(5D);

		_ = fakeCreateQueryExecutorWrapper.CreateQueryExecutor(
			Arg.Any<Expression>(),
			Arg.Any<Func<Expression, Func<QueryContext, IEnumerable<UserPaymentSummaryStatistics>>>>())
			.Returns((callInfo) =>
			{
				var queryExp = callInfo.Arg<Expression>();
				var baseFunc = callInfo.Arg<Func<Expression, Func<QueryContext, IEnumerable<UserPaymentSummaryStatistics>>>>();

				if (queryExp is MethodCallExpression methodCall
					&& methodCall.Method.Name == nameof(EntityFrameworkQueryableExtensions.AsNoTracking)
					&& methodCall.Arguments[0] is FromSqlQueryRootExpression sqlQueryExp)
				{
					static IEnumerable<UserPaymentSummaryStatistics> GetData(QueryContext queryContext)
					{
						return [
							new UserPaymentSummaryStatistics(
								"test1",
								UserType.Player,
								Guid.NewGuid(),
								100M,
								0M,
								1,
								100M,
								0M,
								DateTime.UtcNow,
								null,
								null,
								null,
								null,
								null,
								null)];
					}

					return GetData;
				}

				return baseFunc(queryExp);
			});

		// Act
		var actual = await sut.QueryUserPaymentSummaryStatisticsAsync(start, end).ToArrayAsync();

		// Assert
		Assert.NotNull(actual);
		_ = Assert.Single(actual);
	}

	[Fact]
	public async Task TgsRepository_更新5分鐘匯總資料()
	{
		// Arrange
		var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		var dbContext = new TgsContext(dbContextOptions);

		var sut = new TgsRepository(dbContext, NullLogger<TgsRepository>.Instance);

		var timeRange = DateTime.ParseExact("2024-01-18T14:25:00.0000000Z", "o", null).ToUniversalTime();
		var summaries = new[]
		{
			new UserPaymentSummaryStatistics(
				"test1",
				UserType.Player,
				Guid.NewGuid(),
				100M,
				0M,
				1,
				100M,
				0M,
				DateTime.UtcNow,
				null,
				null,
				null,
				null,
				null,
				null)
		};

		// Act
		await sut.UpsertFiveMinutesUserPaymentSummaryStatisticsAsync(timeRange, summaries);
		var actual = await dbContext.FiveMinutesPaymentStatistics.ToArrayAsync();

		// Assert
		Assert.NotNull(actual);
		_ = Assert.Single(actual);
		Assert.Equal(timeRange, actual[0].TimePoint);
	}

	[Fact]
	public async Task TgsRepository_更新5分鐘匯總資料_重複的會蓋成新資料()
	{
		// Arrange
		var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		using var dbContext = new TgsContext(dbContextOptions);

		var sut = new TgsRepository(dbContext, NullLogger<TgsRepository>.Instance);

		var timeRange = DateTime.ParseExact("2024-01-18T14:25:00.0000000Z", "o", null).ToUniversalTime();
		var summaries = new[]
		{
			new UserPaymentSummaryStatistics(
				"test1",
				UserType.Player,
				Guid.NewGuid(),
				100M,
				0M,
				1,
				100M,
				0M,
				DateTime.UtcNow,
				null,
				null,
				null,
				null,
				null,
				null)
		};

		_ = await dbContext.FiveMinutesPaymentStatistics.AddAsync(new FiveMinutesPaymentStatistic
		{
			TimePoint = timeRange,
			UserId = summaries[0].UserId,
			UserType = (int)summaries[0].UserType,
			BrandId = summaries[0].BrandId,
			DepositAmount = 200M,
			DepositCount = summaries[0].DepositCount,
			DepositHandlingFee = summaries[0].DepositHandlingFee,
			FirstDepositAmount = summaries[0].FirstDepositAmount,
			FirstDepositHandlingFee = summaries[0].FirstDepositHandlingFee,
			FirstDepositOn = summaries[0].FirstDepositOn,
			FirstWithdrawAmount = summaries[0].FirstWithdrawAmount,
			FirstWithdrawHandlingFee = summaries[0].FirstWithdrawHandlingFee,
			FirstWithdrawOn = summaries[0].FirstWithdrawOn,
			WithdrawAmount = summaries[0].WithdrawAmount,
			WithdrawCount = summaries[0].WithdrawCount,
			WithdrawHandlingFee = summaries[0].WithdrawHandlingFee
		});
		_ = await dbContext.SaveChangesAsync();

		// Act
		await sut.UpsertFiveMinutesUserPaymentSummaryStatisticsAsync(timeRange, summaries);
		var actual = await dbContext.FiveMinutesPaymentStatistics.ToArrayAsync();

		// Assert
		Assert.NotNull(actual);
		_ = Assert.Single(actual);
		Assert.Equal(100M, actual[0].DepositAmount);
	}

	[Fact]
	public async Task TgsRepository_更新5分鐘匯總資料_會刪除多出來的資料()
	{
		// Arrange
		var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		using var dbContext = new TgsContext(dbContextOptions);

		var sut = new TgsRepository(dbContext, NullLogger<TgsRepository>.Instance);

		var timeRange = DateTime.ParseExact("2024-01-18T14:25:00.0000000Z", "o", null).ToUniversalTime();
		var summaries = new[]
		{
			new UserPaymentSummaryStatistics(
				"test1",
				UserType.Player,
				Guid.NewGuid(),
				100M,
				0M,
				1,
				100M,
				0M,
				DateTime.UtcNow,
				null,
				null,
				null,
				null,
				null,
				null)
		};

		_ = await dbContext.FiveMinutesPaymentStatistics.AddAsync(new FiveMinutesPaymentStatistic
		{
			TimePoint = timeRange,
			UserId = "test2",
			UserType = (int)UserType.Player,
			BrandId = Guid.NewGuid(),
			DepositAmount = 200M,
			DepositCount = 1,
			DepositHandlingFee = 0M,
			FirstDepositAmount = 200M,
			FirstDepositHandlingFee = 0M,
			FirstDepositOn = DateTime.UtcNow
		});
		_ = await dbContext.SaveChangesAsync();

		// Act
		await sut.UpsertFiveMinutesUserPaymentSummaryStatisticsAsync(timeRange, summaries);
		var actual = await dbContext.FiveMinutesPaymentStatistics.ToArrayAsync();

		// Assert
		Assert.NotNull(actual);
		_ = Assert.Single(actual);
		Assert.Equal("test1", actual[0].UserId);
	}

	[Fact]
	public async Task TgsRepository_更新1小時匯總資料()
	{
		// Arrange
		var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.ReplaceService<IQueryCompilationContextFactory, TestQueryCompilationContextFactory>()
			.Options;
		var fakeCreateQueryExecutorWrapper = Substitute.For<CreateQueryExecutorWrapper>();

		_ = TestQueryCompilationContextFactory.Services
			.AddKeyedScoped(
				dbContextOptions,
				(sp, _) => fakeCreateQueryExecutorWrapper);

		using var dbContext = new TgsContext(dbContextOptions);
		var sut = new TgsRepository(dbContext, NullLogger<TgsRepository>.Instance);

		var processTime = DateTime.ParseExact("2024-01-17T14:00:00.0000000Z", "o", null).ToUniversalTime();

		_ = fakeCreateQueryExecutorWrapper.CreateQueryExecutor(
			Arg.Any<Expression>(),
			Arg.Any<Func<Expression, Func<QueryContext, IAsyncEnumerable<UserPaymentSummaryStatistics>>>>())
			.Returns((CallInfo callInfo) =>
			{
				var queryExp = callInfo.Arg<Expression>();
				var baseFunc = callInfo.Arg<Func<Expression, Func<QueryContext, IAsyncEnumerable<UserPaymentSummaryStatistics>>>>();

				return queryExp is MethodCallExpression methodCall
					&& methodCall.Method.Name == nameof(EntityFrameworkQueryableExtensions.AsNoTracking)
					&& methodCall.Arguments[0] is FromSqlQueryRootExpression sqlQueryExp
					? new Func<QueryContext, IAsyncEnumerable<UserPaymentSummaryStatistics>>(
						queryContext => new[] {
							new UserPaymentSummaryStatistics(
								"test1",
								UserType.Player,
								Guid.NewGuid(),
								100M,
								0M,
								1,
								100M,
								0M,
								DateTime.UtcNow,
								null,
								null,
								null,
								null,
								null,
								null)
						}.ToAsyncEnumerable())
					: baseFunc(queryExp);
			});

		_ = fakeCreateQueryExecutorWrapper.CreateQueryExecutor(
			Arg.Any<Expression>(),
			Arg.Any<Func<Expression, Func<QueryContext, IAsyncEnumerable<HourPaymentStatistic>>>>())
			.Returns((CallInfo callInfo) =>
			{
				var queryExp = callInfo.Arg<Expression>();
				var baseFunc = callInfo.Arg<Func<Expression, Func<QueryContext, IAsyncEnumerable<HourPaymentStatistic>>>>();

				return baseFunc(queryExp);
			});

		// Act
		await sut.ProcessHourPaymentStatisticsAsync(processTime);
		var actual = await dbContext.HourPaymentStatistics.ToArrayAsync();

		// Assert
		Assert.NotNull(actual);
		_ = Assert.Single(actual);
		Assert.Equal(processTime, actual[0].TimePoint);
	}

	[Fact]
	public async Task TgsRepository_更新1小時匯總資料_重複的會蓋成新資料()
	{
		// Arrange
		var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.ReplaceService<IQueryCompilationContextFactory, TestQueryCompilationContextFactory>()
			.EnableSensitiveDataLogging()
			.Options;
		var fakeCreateQueryExecutorWrapper = Substitute.For<CreateQueryExecutorWrapper>();

		_ = TestQueryCompilationContextFactory.Services
			.AddKeyedSingleton(
				dbContextOptions,
				(sp, _) => fakeCreateQueryExecutorWrapper);

		var dbContext = new TgsContext(dbContextOptions);
		var sut = new TgsRepository(dbContext, NullLogger<TgsRepository>.Instance);

		var processTime = DateTime.ParseExact("2024-01-17T14:00:00.0000000Z", "o", null).ToUniversalTime();
		var brandId = Guid.NewGuid();

		var queryData = new[] {
			new UserPaymentSummaryStatistics(
				"test1",
				UserType.Player,
				brandId,
				300M,
				0M,
				1,
				100M,
				0M,
				DateTime.UtcNow,
				null,
				null,
				null,
				null,
				null,
				null)
		};

		_ = fakeCreateQueryExecutorWrapper.CreateQueryExecutor(
			Arg.Any<Expression>(),
			Arg.Any<Func<Expression, Func<QueryContext, IAsyncEnumerable<UserPaymentSummaryStatistics>>>>())
			.Returns((CallInfo callInfo) =>
			{
				var queryExp = callInfo.Arg<Expression>();
				var baseFunc = callInfo.Arg<Func<Expression, Func<QueryContext, IAsyncEnumerable<UserPaymentSummaryStatistics>>>>();

				return queryExp is MethodCallExpression methodCall
					&& methodCall.Method.Name == nameof(EntityFrameworkQueryableExtensions.AsNoTracking)
					&& methodCall.Arguments[0] is FromSqlQueryRootExpression sqlQueryExp
					? new Func<QueryContext, IAsyncEnumerable<UserPaymentSummaryStatistics>>(
						queryContext => queryData.ToAsyncEnumerable())
					: baseFunc(queryExp);
			});

		_ = fakeCreateQueryExecutorWrapper.CreateQueryExecutor(
			Arg.Any<Expression>(),
			Arg.Any<Func<Expression, Func<QueryContext, IAsyncEnumerable<HourPaymentStatistic>>>>())
			.Returns((CallInfo callInfo) =>
			{
				var queryExp = callInfo.Arg<Expression>();
				var baseFunc = callInfo.Arg<Func<Expression, Func<QueryContext, IAsyncEnumerable<HourPaymentStatistic>>>>();

				return baseFunc(queryExp);
			});

		var entity = await dbContext.HourPaymentStatistics.AddAsync(new HourPaymentStatistic
		{
			TimePoint = processTime,
			UserId = queryData[0].UserId,
			UserType = (int)queryData[0].UserType,
			BrandId = brandId,
			DepositAmount = 200M,
			DepositCount = queryData[0].DepositCount,
			DepositHandlingFee = queryData[0].DepositHandlingFee,
			FirstDepositAmount = queryData[0].FirstDepositAmount,
			FirstDepositHandlingFee = queryData[0].FirstDepositHandlingFee,
			FirstDepositOn = queryData[0].FirstDepositOn,
			FirstWithdrawAmount = queryData[0].FirstWithdrawAmount,
			FirstWithdrawHandlingFee = queryData[0].FirstWithdrawHandlingFee,
			FirstWithdrawOn = queryData[0].FirstWithdrawOn,
			WithdrawAmount = queryData[0].WithdrawAmount,
			WithdrawCount = queryData[0].WithdrawCount,
			WithdrawHandlingFee = queryData[0].WithdrawHandlingFee
		});
		_ = await dbContext.SaveChangesAsync();

		// Act
		await sut.ProcessHourPaymentStatisticsAsync(processTime);
		var actual = await dbContext.HourPaymentStatistics.ToArrayAsync();

		// Assert
		Assert.NotNull(actual);
		_ = Assert.Single(actual);
		Assert.Equal(processTime, actual[0].TimePoint);
		Assert.Equal(300M, actual[0].DepositAmount);
	}

	[Fact]
	public async Task TgsRepository_更新1小時匯總資料_會刪除多出來的資料()
	{
		// Arrange
		var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.ReplaceService<IQueryCompilationContextFactory, TestQueryCompilationContextFactory>()
			.Options;
		var fakeCreateQueryExecutorWrapper = Substitute.For<CreateQueryExecutorWrapper>();

		_ = TestQueryCompilationContextFactory.Services
			.AddKeyedScoped(
				dbContextOptions,
				(sp, _) => fakeCreateQueryExecutorWrapper);

		var dbContext = new TgsContext(dbContextOptions);
		var sut = new TgsRepository(dbContext, NullLogger<TgsRepository>.Instance);

		var processTime = DateTime.ParseExact("2024-01-17T14:00:00.0000000Z", "o", null).ToUniversalTime();
		var brandId = Guid.NewGuid();

		var queryData = new[] {
			new UserPaymentSummaryStatistics(
				"test1",
				UserType.Player,
				brandId,
				100M,
				0M,
				1,
				100M,
				0M,
				DateTime.UtcNow,
				null,
				null,
				null,
				null,
				null,
				null)
		};

		_ = fakeCreateQueryExecutorWrapper.CreateQueryExecutor(
			Arg.Any<Expression>(),
			Arg.Any<Func<Expression, Func<QueryContext, IAsyncEnumerable<UserPaymentSummaryStatistics>>>>())
			.Returns((CallInfo callInfo) =>
			{
				var queryExp = callInfo.Arg<Expression>();
				var baseFunc = callInfo.Arg<Func<Expression, Func<QueryContext, IAsyncEnumerable<UserPaymentSummaryStatistics>>>>();

				return queryExp is MethodCallExpression methodCall
					&& methodCall.Method.Name == nameof(EntityFrameworkQueryableExtensions.AsNoTracking)
					&& methodCall.Arguments[0] is FromSqlQueryRootExpression sqlQueryExp
					? new Func<QueryContext, IAsyncEnumerable<UserPaymentSummaryStatistics>>(
						queryContext => queryData.ToAsyncEnumerable())
					: baseFunc(queryExp);
			});

		_ = fakeCreateQueryExecutorWrapper.CreateQueryExecutor(
			Arg.Any<Expression>(),
			Arg.Any<Func<Expression, Func<QueryContext, IAsyncEnumerable<HourPaymentStatistic>>>>())
			.Returns((CallInfo callInfo) =>
			{
				var queryExp = callInfo.Arg<Expression>();
				var baseFunc = callInfo.Arg<Func<Expression, Func<QueryContext, IAsyncEnumerable<HourPaymentStatistic>>>>();

				return baseFunc(queryExp);
			});

		_ = await dbContext.HourPaymentStatistics.AddAsync(new HourPaymentStatistic
		{
			TimePoint = processTime,
			UserId = "test2",
			UserType = (int)UserType.Player,
			BrandId = brandId,
			DepositAmount = 200M,
			DepositCount = 1,
			DepositHandlingFee = 0M,
			FirstDepositAmount = 200M,
			FirstDepositHandlingFee = 0M,
			FirstDepositOn = DateTime.UtcNow
		});
		_ = await dbContext.SaveChangesAsync();

		// Act
		await sut.ProcessHourPaymentStatisticsAsync(processTime);
		var actual = await dbContext.HourPaymentStatistics.ToArrayAsync();

		// Assert
		Assert.NotNull(actual);
		_ = Assert.Single(actual);
		Assert.Equal(processTime, actual[0].TimePoint);
		Assert.Equal("test1", actual[0].UserId);
	}
}
