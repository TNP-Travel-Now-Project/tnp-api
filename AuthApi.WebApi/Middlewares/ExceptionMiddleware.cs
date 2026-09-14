using System.Net;
using AuthApi.Application.Common;
using AuthApi.Application.Common.Security;
using AuthApi.Domain.Exceptions;
using FluentValidation;

namespace AuthApi.WebApi.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

            var response = new ApiErrorResponse
            {
                Code = ErrorCodes.Unauthorized,
                Message = ex.Message
            };

            await context.Response.WriteAsJsonAsync(response);
        }
        catch (ForbiddenException ex)
        {
            _logger.LogWarning(ex, "Forbidden access attempt");
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;

            var response = new ApiErrorResponse
            {
                Code = ErrorCodes.Forbidden,
                Message = ex.Message
            };

            await context.Response.WriteAsJsonAsync(response);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed");
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => new FieldError(e.ErrorCode, e.ErrorMessage)).ToArray()
                );

            var response = new ApiErrorResponse
            {
                Code = ErrorCodes.ValidationError,
                Message = ValidationMessages.ValidationFailed,
                Errors = errors
            };

            await context.Response.WriteAsJsonAsync(response);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;

            var response = new ApiErrorResponse
            {
                Code = ErrorCodes.NotFound,
                Message = ex.Message
            };

            await context.Response.WriteAsJsonAsync(response);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated");
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var response = new ApiErrorResponse
            {
                Code = ErrorCodes.ValidationError,
                Message = ex.Message
            };

            await context.Response.WriteAsJsonAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new ApiErrorResponse
            {
                Code = ErrorCodes.InternalError,
                Message = ValidationMessages.InternalServerError
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
