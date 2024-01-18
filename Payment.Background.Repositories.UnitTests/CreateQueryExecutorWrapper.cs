using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Payment.Background.Repositories.UnitTests;

public abstract class CreateQueryExecutorWrapper
{
	public virtual Func<QueryContext, TResult> CreateQueryExecutor<TResult>(
		Expression query,
		Func<Expression, Func<QueryContext, TResult>> baseFunc)
		=> baseFunc(query);
}
