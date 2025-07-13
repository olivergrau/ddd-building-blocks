using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.ErrorHandling;
using Microsoft.AspNetCore.Http;

namespace RocketLaunch.Api.Handler;

internal static class RequestHandlerExtensions
{
    internal static IResult ToApiResult(this ICommandExecutionResult execResult)
    {
        if (execResult.IsSuccess)
            return Results.Ok();

        if (execResult.ResultException is ClassifiedErrorException ce)
        {
            var payload = new { ce.ErrorInfo.Message, ce.ErrorInfo.Origin, ce.ErrorInfo.Classification };
            return ce.ErrorInfo.Classification switch
            {
                ErrorClassification.NotFound => Results.NotFound(payload),
                ErrorClassification.InputDataError => Results.BadRequest(payload),
                ErrorClassification.ProcessingError => Results.UnprocessableEntity(payload),
                ErrorClassification.Infrastructure => Results.StatusCode(StatusCodes.Status503ServiceUnavailable),
                ErrorClassification.ProgrammingError => Results.StatusCode(StatusCodes.Status500InternalServerError),
                _ => Results.BadRequest(payload)
            };
        }

        return Results.BadRequest(new { Message = execResult.FailReason });
    }
}
