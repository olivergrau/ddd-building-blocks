namespace DDD.BuildingBlocks.Hosting.Background.Constants
{
	public static class HostingBackgroundLogMessages
	{
		/// <summary>
		/// Queued Hosted Service is starting.
		/// </summary>
		public static readonly string QueuedHostingServiceStarting = "Queued Hosted Service is starting.";
		/// <summary>
		/// Queued Hosted Service is stopping.
		/// </summary>
		public static readonly string QueuedHostingServiceStopping = "Queued Hosted Service is stopping.";
		/// <summary>
		/// Error occurred executing {0}.
		/// </summary>
		public static readonly string ErrorOccuredExecuting = "Error occurred executing {0}.";
		/// <summary>
		/// Global trigger timeout {timeout} applied to: {type}
		/// </summary>
		public static readonly string GlobalTriggerTimeoutApplied = "Global trigger timeout {timeout} applied to: {type}";
		/// <summary>
		/// Inspecting: {type} with value {value}
		/// </summary>
		public static readonly string InspectingTypeWithValue = "Inspecting: {type} with value {value}";
		/// <summary>
		/// Local trigger timeout {timeout} (overwrites global) applied to: {type}
		/// </summary>
		public static readonly string LocalTriggerTimeoutApplied = "Local trigger timeout {timeout} (overwrites global) applied to: {type}";
		/// <summary>
		/// Timed Background Service [{0}] is starting.
		/// </summary>
		public static readonly string TimedBackgroundServiceStartingWithFullName = "Timed Background Service [{0}] is starting.";
		/// <summary>
		/// Error executing worker: {EXCEPTION}
		/// </summary>
		public static readonly string ErrorExecutingWorker = "Error executing worker: {EXCEPTION}";
		/// <summary>
		/// Timed Background Service is stopping.
		/// </summary>
		public static readonly string TimedBackgroundServiceStopping = "Timed Background Service is stopping.";
		/// <summary>
		/// Timed Background Service: Time has been stopped.
		/// </summary>
		public static readonly string TimedBackgroundServiceStopped = "Timed Background Service: Timer stopped.";
		/// <summary>
		/// Timed Background Service is starting."
		/// </summary>
		public static readonly string TimedBackgroundServiceStarting = "Timed Background Service is starting.";
	}
}
