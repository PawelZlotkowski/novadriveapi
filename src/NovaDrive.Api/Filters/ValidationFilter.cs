// Api/Filters/ValidationFilter.cs
namespace NovaDrive.Api.Filters;

using FluentValidation;

public static class ValidationFilter
{
    public static async Task<IResult> ValidateAsync<T>(T request, IValidator<T> validator)
    {
        var result = await validator.ValidateAsync(request);

        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return Results.UnprocessableEntity(new { message = "Validation failed", errors });
        }

        return null!; // null means validation passed
    }
}