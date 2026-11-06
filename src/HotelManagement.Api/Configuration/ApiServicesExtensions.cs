using System.Diagnostics;
using HotelManagement.Api.ErrorHandling;

namespace HotelManagement.Api.Configuration;

public static class ApiServicesExtensions
{
    public static IServiceCollection AddApiErrorHandling(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
                context.ProblemDetails.Extensions["traceId"] =
                    Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
