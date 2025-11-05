using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace WeatherImageApp.Middleware;

/// <summary>
/// Attribute to require API key authentication on Azure Functions
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ApiKeyAuthAttribute : Attribute
{
    public static async Task<HttpResponseData?> ValidateApiKeyAsync(
        HttpRequestData req,
        IConfiguration configuration,
        ILogger logger)
    {
        // Get API key from configuration
        // Azure Functions isolated worker reads from environment variables automatically
        var expectedApiKey = Environment.GetEnvironmentVariable("ApiKey");
        
        // Log for debugging what we found
        if (string.IsNullOrEmpty(expectedApiKey))
        {
            logger.LogWarning("⚠️ ApiKey not found in environment variables!");
            logger.LogWarning($"Available env keys: {string.Join(", ", Environment.GetEnvironmentVariables().Keys.Cast<string>().Where(k => k.Contains("Api") || k.Contains("KEY")).Take(5))}");
        }
        else
        {
            logger.LogInformation($"✓ API Key is configured (length: {expectedApiKey.Length})");
        }
        
        // IMPORTANT: In production, API key MUST be configured
        // For this implementation, we enforce authentication
        if (string.IsNullOrEmpty(expectedApiKey))
        {
            logger.LogError("❌ No API key configured in environment!");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "API authentication not configured" });
            return errorResponse;
        }

        // Check for API key in header
        if (!req.Headers.TryGetValues("X-API-Key", out var apiKeyValues))
        {
            logger.LogWarning("❌ Request missing X-API-Key header");
            var response = req.CreateResponse(HttpStatusCode.Unauthorized);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync("{\"error\":\"API key is required. Please provide X-API-Key header.\"}");
            return response;
        }

        var providedApiKey = apiKeyValues.FirstOrDefault();
        
        // Validate API key
        if (providedApiKey != expectedApiKey)
        {
            var maskedKey = providedApiKey?.Length > 5 ? providedApiKey.Substring(0, 5) + "..." : "***";
            logger.LogWarning($"❌ Invalid API key provided: {maskedKey}");
            var response = req.CreateResponse(HttpStatusCode.Forbidden);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync("{\"error\":\"Invalid API key\"}");
            return response;
        }

        // Authentication successful
        logger.LogDebug("API key validation successful");
        logger.LogInformation("API key validated successfully");
        return null;
    }
}
