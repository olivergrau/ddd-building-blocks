namespace DDD.BuildingBlocks.Core
{
	public static class CoreErrors
	{
		public static readonly string NoCommandHandlerFound = "No command handler found for {0}";
		public static readonly string CommandHandlerCouldNotBeInstantiated = "Command handler could not be instantiated. Factory returned null.";
		public static readonly string CommandExecutionFailed = "Command execution failed";
		public static readonly string CannotCreateEventOfType = "Cannot create event of type {0}";
	}
}
