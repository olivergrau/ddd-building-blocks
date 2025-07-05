using System.Globalization;
using DDD.BuildingBlocks.Core.ErrorHandling;
using DDD.BuildingBlocks.Core.Exception.Constants;

namespace DDD.BuildingBlocks.Core.Exception
{
    public class InvalidCommandDataException(string message) : ClassifiedErrorException(new ClassificationInfo(
        string.Format(CultureInfo.InvariantCulture, ExceptionMessages.InvalidInputData, message),
        ErrorOrigin.ApplicationLevel,
        ErrorClassification.InputDataError));
}
