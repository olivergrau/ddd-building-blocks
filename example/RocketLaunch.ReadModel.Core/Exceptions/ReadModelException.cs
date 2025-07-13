using DDD.BuildingBlocks.Core.ErrorHandling;

namespace RocketLaunch.ReadModel.Core.Exceptions
{
	public class ReadModelException(string message, ErrorClassification classification = ErrorClassification.NotSpecified) : ClassifiedErrorException(new ClassificationInfo(message,
		ErrorOrigin.ReadModel, classification));
}
