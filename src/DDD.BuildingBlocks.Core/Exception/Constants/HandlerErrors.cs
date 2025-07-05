namespace DDD.BuildingBlocks.Core.Exception.Constants
{
	public static class HandlerErrors
	{
		/// <summary>
		/// Aggregate id is empty.
		/// </summary>
		public static readonly string AggregateIdIsEmpty = "Aggregate id is empty.";
		/// <summary>
		/// Aggregate Id is not a valid id: {0}
		/// </summary>
		public static readonly string AggregateIdIsNotAValidId = "Aggregate Id is not a valid id: {0}";

		/// <summary>
		/// Value cannot be null or whitespace: {0}
		/// </summary>
		public static readonly string ValueCannotBeNullOrWhitespace = "Value cannot be null or whitespace: {0}";
		/// <summary>
		/// Error reading from repository
		/// </summary>
		public static readonly string ErrorReadingFromRepository = "Error reading from repository";
		/// <summary>
		/// Error writing to repository
		/// </summary>
		public static readonly string ErrorWritingFromRepository = "Error writing to repository";
        /// <summary>
		/// Aggregate sourcing error.
		/// </summary>
		public static readonly string AggregateSourcingError = "Aggregate sourcing error.";
        /// <summary>
        /// Application processing error
        /// </summary>
        public static readonly string ApplicationProcessingError = "Application processing error";
		/// <summary>
		/// Error reading user data from read model.
		/// </summary>
		public static readonly string ErrorReadingDataFromReadModel =
			"Error reading data from read model.";
        /// <summary>
		/// A unique constraint has been violated. [Name]
		/// </summary>
		public static readonly string AUniqueConstraintHasBeenViolatedName =
			"A unique constraint has been violated. [Name]";
	}
}
