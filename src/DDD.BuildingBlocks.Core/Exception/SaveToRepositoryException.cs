using DDD.BuildingBlocks.Core.ErrorHandling;
using DDD.BuildingBlocks.Core.Exception.Constants;

namespace DDD.BuildingBlocks.Core.Exception
{
	public class SaveToRepositoryException : ClassifiedErrorException
	{
		public SaveToRepositoryException(System.Exception inner) : base(
			new ClassificationInfo(ExceptionMessages.ErrorWritingToRepository, ErrorOrigin.Repository,
				ErrorClassification.Infrastructure), inner)
		{
		}

		public SaveToRepositoryException(string errorMessage, System.Exception inner) : base(
			new ClassificationInfo(errorMessage, ErrorOrigin.Repository,
				ErrorClassification.Infrastructure), inner)
		{
		}
	}
}