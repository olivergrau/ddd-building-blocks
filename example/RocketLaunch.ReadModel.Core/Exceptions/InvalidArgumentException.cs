using DDD.BuildingBlocks.Core.ErrorHandling;

namespace RocketLaunch.ReadModel.Core.Exceptions
{
	public class InvalidArgumentException : ClassifiedErrorException
	{
		public InvalidArgumentException(string message) :
			base(new ClassificationInfo(message,
			ErrorOrigin.ReadModel, ErrorClassification.ProgrammingError))
		{
		}

	}
}
