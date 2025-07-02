using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DDD.BuildingBlocks.DI.Extensions.Constants;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace DDD.BuildingBlocks.DI.Extensions.Module
{
    using System.Globalization;

    /// <summary>
	///     Very simple Plugin (Module) System.
	/// </summary>
	public static class ModuleRegistrations
	{
		private static readonly List<Action<IServiceCollection, IHostEnvironment>> Registrations = [];

		private static readonly List<Action<IServiceCollection, IHostEnvironment?, IConfiguration?>> RegistrationsWithGlobalConfigurations = [];

		/// <summary>
		///     Adds a registration.
		/// </summary>
		/// <param name="registration"></param>
		public static void Add(Action<IServiceCollection, IHostEnvironment?> registration)
		{
			Registrations.Add(registration);
		}

		/// <summary>
		///     Adds a registration.
		/// </summary>
		/// <param name="registration"></param>
		public static void Add(Action<IServiceCollection, IHostEnvironment?, IConfiguration?> registration)
		{
			RegistrationsWithGlobalConfigurations.Add(registration);
		}

		/// <summary>
		///     Discovers IModuleServiceConfiguration types and activates them.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="startingAssembly"></param>
		/// <param name="environment"></param>
		/// <param name="configuration"></param>
		/// <param name="scanPrefixes"></param>
		/// <returns></returns>
		public static void LoadModuleRegistrations(this IServiceCollection services,
			Assembly? startingAssembly, IHostEnvironment? environment, IConfiguration? configuration,
			string[]? scanPrefixes)
		{
			var codeBase = startingAssembly?.Location ?? Assembly.GetEntryAssembly()?.Location;
            var path = Path.GetDirectoryName(codeBase)!;

			Debug.WriteLine(UtilsDiLogMessages.DetectedCodeBase, path);

			var scanDirectories = new List<string>();

			if (scanPrefixes == null || !scanPrefixes.Any())
			{
				scanDirectories.Add("*.dll");
			}
			else
			{
				foreach (var prefix in scanPrefixes)
				{
					scanDirectories.AddRange(Directory.GetFiles(path, prefix));
				}
			}

			foreach (var assembly in scanDirectories)
			{
				var loadedAssembly = Assembly.LoadFrom(assembly);

				foreach (var module in GetModuleRegistrationTypes<IModuleRegistration>(loadedAssembly).ToList())
				{
				 	Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, UtilsDiLogMessages.ModuleFound, module.GetType().FullName));
				}
			}

			foreach (var registration in Registrations)
			{
				Debug.Assert(environment != null, nameof(environment) + " != null");
				registration.Invoke(services, environment);
			}

			foreach (var registration in RegistrationsWithGlobalConfigurations)
			{
				registration.Invoke(services, environment, configuration);
			}
		}

		private static IEnumerable<T> GetModuleRegistrationTypes<T>(Assembly assembly)
		{
#pragma warning disable CS8604
			return from ti in assembly.DefinedTypes
				   where ti.ImplementedInterfaces.Contains(typeof(T))
				   select (T) assembly!.CreateInstance(ti?.FullName)!;
#pragma warning restore CS8604
		}
	}
}
