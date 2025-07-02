using System;

namespace DDD.BuildingBlocks.Core.ErrorHandling
{
    public class ClassificationInfo
    {
        public ClassificationInfo(string message, ErrorOrigin origin, ErrorClassification classification)
        {
            if (origin == default(ErrorOrigin))
            {
                throw new System.Exception("Invalid value for error origin.");
            }

            if (classification == default(ErrorClassification))
            {
                throw new System.Exception("Invalid value for error origin.");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(message));
            }

            Message = message;
            Origin = origin;
            Classification = classification;
        }

        public string Message { get; }
        public ErrorOrigin Origin { get; }
        public ErrorClassification Classification { get; }
    }
}
