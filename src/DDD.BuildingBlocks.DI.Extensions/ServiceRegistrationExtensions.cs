using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using DDD.BuildingBlocks.DI.Extensions.Constants;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace DDD.BuildingBlocks.DI.Extensions
{
	public static class ServiceRegistrationExtensions
	{
		public static void RegisterTransient<T>(
			this IServiceCollection services, Func<IServiceProvider, T> factory, T defaultInstance) where T : class
		{
			services.RegisterTransient(factory, _ => defaultInstance);
		}

		public static void RegisterScoped<T>(
			this IServiceCollection services, Func<IServiceProvider, T> factory, T defaultInstance) where T : class
		{
			services.RegisterScoped(factory, _ => defaultInstance);
		}

		public static void RegisterSingleton<T>(
			this IServiceCollection services, Func<IServiceProvider, T> factory, T defaultInstance) where T : class
		{
			services.RegisterSingleton(factory, _ => defaultInstance);
		}

		public static void RegisterScoped<T>(
			this IServiceCollection services, Func<IServiceProvider, T>? factory, Func<IServiceProvider, T> defaultImplementation) where T : class
		{
			if (factory == null)
			{
				services.AddScoped(defaultImplementation);

				Debug.WriteLine(UtilsDiLogMessages.LoadingDefaultTypeForInterface,
					typeof(T).FullName);
			}
			else
			{
				services.AddScoped(c =>
				{
					var customType = factory.Invoke(c);

					Debug.WriteLine(UtilsDiLogMessages.LoadingCustomTypeForInterface, customType.GetType().FullName,
						typeof(T).FullName);

					return customType;
				});
			}
		}

		public static void RegisterSingleton<T>(
			this IServiceCollection services, Func<IServiceProvider, T>? factory, Func<IServiceProvider, T> defaultImplementation) where T : class
		{
			if (factory == null)
			{
				services.AddSingleton(defaultImplementation);

				Debug.WriteLine(UtilsDiLogMessages.LoadingDefaultTypeForInterface,
					typeof(T).FullName);
			}
			else
			{
				services.AddSingleton(c =>
				{
					var customType = factory.Invoke(c);

					Debug.WriteLine(UtilsDiLogMessages.LoadingCustomTypeForInterface, customType.GetType().FullName,
						typeof(T).FullName);

					return customType;
				});
			}
		}

		public static void RegisterTransient<T>(
			this IServiceCollection services, Func<IServiceProvider, T>? factory, Func<IServiceProvider, T> defaultImplementation) where T : class
		{
			if (factory == null)
			{
				services.AddTransient(defaultImplementation);

				Debug.WriteLine(UtilsDiLogMessages.LoadingDefaultTypeForInterface,
					typeof(T).FullName);
			}
			else
			{
				services.AddTransient(c =>
				{
					var customType = factory.Invoke(c);

					Debug.WriteLine(UtilsDiLogMessages.LoadingCustomTypeForInterface, customType.GetType().FullName,
						typeof(T).FullName);

					return customType;
				});
			}
		}
	}
}
