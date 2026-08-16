namespace DDD.BuildingBlocks.Core.ErrorHandling
{
    public enum ErrorClassification
    {
        NotSpecified,
        Validation,
        DomainRejection,
        StreamNotFound,
        StreamAlreadyExists,
        ConcurrencyConflict,
        UnknownEventType,
        UnsupportedSchemaVersion,
        SerializationFailure,
        TransientProviderFailure,
        PermanentProviderFailure,
        Cancellation,
        InputDataError,
        NotFound,
        ProcessingError,
        Infrastructure,
        ProgrammingError,
        InvalidState,
        TransientFailure
    }
}
