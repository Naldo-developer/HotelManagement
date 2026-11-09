using System.Diagnostics;
using HotelManagement.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Api.ErrorHandling;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            ResourceNotFoundException => (StatusCodes.Status404NotFound,
                "Recurso no encontrado", exception.Message),
            BusinessValidationException => (
                StatusCodes.Status400BadRequest,
                "Datos inválidos",
                exception.Message),
            BusinessConflictException => (StatusCodes.Status409Conflict,
                "Conflicto de negocio", exception.Message),
            _ => (StatusCodes.Status500InternalServerError,
                "Error interno del servidor", "Ocurrió un error inesperado. Inténtalo nuevamente.")
        };

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        if (status == StatusCodes.Status500InternalServerError)
        {
            // El mensaje, Data e InnerException pueden contener secretos del proveedor.
            // Registrar ubicación y tipo sin serializar la excepción ni datos de la petición.
            logger.LogError("Error inesperado. TraceId: {TraceId}. Tipo: {ExceptionType}. Pila: {StackTrace}",
                traceId, exception.GetType().FullName, exception.StackTrace);
        }

        httpContext.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };
        problem.Extensions["traceId"] = traceId;

        // Mantener application/problem+json incluso si Accept no admite JSON.
        if (!await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem
            }))
        {
            await httpContext.Response.WriteAsJsonAsync(problem,
                options: null, contentType: "application/problem+json",
                cancellationToken: cancellationToken);
        }

        return true;
    }
}
