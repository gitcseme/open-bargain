using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace OpenBargain.SharedKernel.Endpoints;

// Runs IValidator<T> on the endpoint's T argument and returns a 400 ValidationProblem on failure.
// Opt in per endpoint: .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
public sealed class ValidationFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "The request body is missing.");
        }

        // Resolved per request: validators are scoped.
        var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<T>>();
        var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        if (result.IsValid)
        {
            return await next(context);
        }

        var errors = result.Errors
            .GroupBy(e => JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        return TypedResults.ValidationProblem(errors);
    }
}
