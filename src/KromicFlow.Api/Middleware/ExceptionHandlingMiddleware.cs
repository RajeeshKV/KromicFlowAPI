using System.Net;
using FluentValidation;
using KromicFlow.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace KromicFlow.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Validation failed", exception.Errors.FirstOrDefault()?.ErrorMessage ?? exception.Message);
        }
        catch (MetaApiException exception)
        {
            logger.LogError(exception, "Meta API error for {Method} {Path} - {Message}", context.Request.Method, context.Request.Path, exception.Message);
            await WriteProblemAsync(context, StatusCodes.Status502BadGateway, "Meta API Error", "Failed to communicate with Meta API. Please try again later.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, (int)HttpStatusCode.InternalServerError, "Unexpected error", "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = statusCode, Title = title, Detail = detail, Instance = context.Request.Path });
    }
}
