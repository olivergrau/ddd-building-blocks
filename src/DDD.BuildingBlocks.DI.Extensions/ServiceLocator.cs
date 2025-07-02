using System;
using Microsoft.Extensions.DependencyInjection;
using DDD.BuildingBlocks.Core;

namespace DDD.BuildingBlocks.DI.Extensions
{
	/// <summary>
	///     Dependency Resolver abstraction for the default .NET Core built in service provider.
	/// </summary>
	public class ServiceLocator(IServiceProvider serviceProvider) : IDependencyResolver
    {
		private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        public T Resolve<T>() where T : notnull
		{
			return _serviceProvider.GetRequiredService<T>();
		}

		public object Resolve(string name, Type type)
		{
			throw new NotSupportedException();
		}

		public object Resolve(Type type)
		{
			return _serviceProvider.GetRequiredService(type);
		}
	}
}
