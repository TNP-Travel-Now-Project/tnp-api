using System.Net;
using AuthApi.Application.Common;
using FluentValidation;

namespace AuthApi.WebApi.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                var errors = ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );

                var response = new ApiErrorResponse
                {
                    Code = ErrorCodes.ValidationError,
                    Message = ValidationMessages.ValidationFailed,
                    Errors = errors
                };

                await context.Response.WriteAsJsonAsync(response);
            }
            catch (Exception)
            {
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
}
