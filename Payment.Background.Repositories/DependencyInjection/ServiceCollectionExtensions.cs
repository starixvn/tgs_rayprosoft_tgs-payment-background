using Microsoft.EntityFrameworkCore;
using Tgs.Payment.Background;
using Tgs.Payment.Background.Repositories;
using Tgs.Payment.Background.Repositories.Entities;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPaymentBackgroundRepositories(
		this IServiceCollection services,
		Action<IServiceProvider, DbContextOptionsBuilder>? dbOptionsAction = null)
		=> services
			.AddSerialNumberGenerator()
			.AddDbContext<TgsContext>(dbOptionsAction)
			.AddTransient<ITgsRepository, TgsRepository>();
}
