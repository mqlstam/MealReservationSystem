using Microsoft.Data.SqlClient;

namespace WebApp.Middleware;

public class DatabaseErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DatabaseErrorHandlingMiddleware> _logger;

    public DatabaseErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<DatabaseErrorHandlingMiddleware> logger)
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
        catch (Exception ex) when (IsDatabaseTimeout(ex))
        {
            _logger.LogWarning(ex, "Database timeout occurred");
            
            // If it's an API call, return a 503 Service Unavailable
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new { 
                    error = "Database connection timeout. Please try again later." 
                });
                return;
            }

            // For web pages, show the custom error page
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            
            // Save the original path so we can display it
            context.Items["OriginalPath"] = context.Request.Path;
            
            // Render the custom error view
            context.Request.Path = "/Home/DatabaseError";
            try
            {
                await _next(context);
            }
            catch
            {
                // If rendering the error view fails, return a simple error message
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync(
                    "Database connection timeout. Please try again later.");
            }
        }
    }

    private bool IsDatabaseTimeout(Exception ex)
    {
        // Check for various SQL timeout scenarios
        if (ex is SqlException sqlEx)
        {
            // Common timeout error numbers
            int[] timeoutNumbers = { -2, 53, -1, 258, 245 };
            return timeoutNumbers.Contains(sqlEx.Number);
        }

        // Check if it's a timeout exception anywhere in the chain
        var current = ex;
        while (current != null)
        {
            if (current.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("timeout period elapsed", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            current = current.InnerException;
        }

        return false;
    }
}
