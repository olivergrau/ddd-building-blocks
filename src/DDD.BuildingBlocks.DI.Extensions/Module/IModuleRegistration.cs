namespace DDD.BuildingBlocks.DI.Extensions.Module
{
	/// <summary>
	///     Represents a type which can be used for service container setup.
	/// </summary>
	public interface IModuleRegistration
	{
		string Name { get; }
	}
}