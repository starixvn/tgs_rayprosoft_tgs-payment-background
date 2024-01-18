using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;

namespace Payment.Background.Repositories.UnitTests;

internal class TestQueryCompilationContextFactory(QueryCompilationContextDependencies dependencies)
	: IQueryCompilationContextFactory
{
	public static readonly ServiceCollection Services = new();

	private readonly IServiceProvider _serviceProvider = Services.BuildServiceProvider();

	public QueryCompilationContext Create(bool async)
		=> new TestQueryCompilationContext(
			dependencies,
			async,
			_serviceProvider.GetRequiredKeyedService<CreateQueryExecutorWrapper>(dependencies.ContextOptions));
}
