using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
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
}
