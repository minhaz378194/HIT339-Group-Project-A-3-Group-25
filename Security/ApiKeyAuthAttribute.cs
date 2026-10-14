using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LibraTrack.Security
{
    /// <summary>
    /// Action filter attribute verifying that the caller provides a valid 'X-Api-Key' header.
    /// Returns 401 Unauthorized with descriptive JSON error if missing or invalid.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAuthAttribute : Attribute, IAsyncActionFilter
    {
        public const string HeaderName = "X-Api-Key";

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var expectedKey = configuration.GetValue<string>("ApiKey") ?? "libra-track-secret-api-key-2026";

            if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var extractedKey) ||
                string.IsNullOrWhiteSpace(extractedKey))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    status = 401,
                    error = "Unauthorized",
                    message = $"Missing required '{HeaderName}' header in request."
                });
                return;
            }

            if (!string.Equals(extractedKey.ToString().Trim(), expectedKey.Trim(), StringComparison.Ordinal))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    status = 401,
                    error = "Unauthorized",
                    message = $"Invalid API key supplied in '{HeaderName}' header."
                });
                return;
            }

            await next();
        }
    }
}
