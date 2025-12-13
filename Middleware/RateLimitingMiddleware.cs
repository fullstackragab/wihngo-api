namespace Wihngo.Middleware
{
    using System.Collections.Concurrent;
    using System.Net;

    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        
        // Store: IP => (RequestCount, WindowStart)
        private static readonly ConcurrentDictionary<string, (int count, DateTime windowStart)> _loginAttempts = new();
        private static readonly ConcurrentDictionary<string, (int count, DateTime windowStart)> _apiRequests = new();
        
        // Configuration
        private const int MaxLoginAttemptsPerWindow = 5;
        private const int LoginWindowMinutes = 15;
        private const int MaxApiRequestsPerWindow = 100;
        private const int ApiWindowMinutes = 1;

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            
            // Cleanup old entries periodically
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5));
                    CleanupOldEntries();
                }
            });
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ipAddress = GetClientIpAddress(context);
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Apply rate limiting to login endpoints
            if (path.Contains("/api/auth/login") || path.Contains("/api/auth/register"))
            {
                if (!CheckLoginRateLimit(ipAddress))
                {
                    _logger.LogWarning("Rate limit exceeded for IP: {IpAddress} on path: {Path}", ipAddress, path);
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                    {
                        message = "Too many login attempts. Please try again later.",
                        code = "RATE_LIMIT_EXCEEDED",
                        retryAfter = LoginWindowMinutes * 60
                    }));
                    return;
                }
            }

            // Apply general API rate limiting
            if (path.StartsWith("/api/"))
            {
                if (!CheckApiRateLimit(ipAddress))
                {
                    _logger.LogWarning("API rate limit exceeded for IP: {IpAddress}", ipAddress);
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                    {
                        message = "Too many requests. Please slow down.",
                        code = "RATE_LIMIT_EXCEEDED",
                        retryAfter = ApiWindowMinutes * 60
                    }));
                    return;
                }
            }

            await _next(context);
        }

        private bool CheckLoginRateLimit(string ipAddress)
        {
            var now = DateTime.UtcNow;
            
            if (_loginAttempts.TryGetValue(ipAddress, out var existing))
            {
                var windowExpiry = existing.windowStart.AddMinutes(LoginWindowMinutes);
                
                if (now < windowExpiry)
                {
                    // Within the window
                    if (existing.count >= MaxLoginAttemptsPerWindow)
                    {
                        return false; // Rate limit exceeded
                    }
                    
                    _loginAttempts[ipAddress] = (existing.count + 1, existing.windowStart);
                }
                else
                {
                    // Window expired, start new window
                    _loginAttempts[ipAddress] = (1, now);
                }
            }
            else
            {
                // First request from this IP
                _loginAttempts[ipAddress] = (1, now);
            }
            
            return true;
        }

        private bool CheckApiRateLimit(string ipAddress)
        {
            var now = DateTime.UtcNow;
            
            if (_apiRequests.TryGetValue(ipAddress, out var existing))
            {
                var windowExpiry = existing.windowStart.AddMinutes(ApiWindowMinutes);
                
                if (now < windowExpiry)
                {
                    if (existing.count >= MaxApiRequestsPerWindow)
                    {
                        return false;
                    }
                    
                    _apiRequests[ipAddress] = (existing.count + 1, existing.windowStart);
                }
                else
                {
                    _apiRequests[ipAddress] = (1, now);
                }
            }
            else
            {
                _apiRequests[ipAddress] = (1, now);
            }
            
            return true;
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // Check for forwarded IP (when behind proxy/load balancer)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (ips.Length > 0)
                {
                    return ips[0].Trim();
                }
            }

            // Check X-Real-IP header
            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp.Trim();
            }

            // Fallback to remote IP
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private void CleanupOldEntries()
        {
            var now = DateTime.UtcNow;
            
            // Clean login attempts
            var loginExpiredKeys = _loginAttempts
                .Where(kvp => now > kvp.Value.windowStart.AddMinutes(LoginWindowMinutes * 2))
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in loginExpiredKeys)
            {
                _loginAttempts.TryRemove(key, out _);
            }
            
            // Clean API requests
            var apiExpiredKeys = _apiRequests
                .Where(kvp => now > kvp.Value.windowStart.AddMinutes(ApiWindowMinutes * 2))
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in apiExpiredKeys)
            {
                _apiRequests.TryRemove(key, out _);
            }
            
            if (loginExpiredKeys.Count > 0 || apiExpiredKeys.Count > 0)
            {
                _logger.LogDebug("Cleaned up {LoginCount} login and {ApiCount} API rate limit entries", 
                    loginExpiredKeys.Count, apiExpiredKeys.Count);
            }
        }
    }

    public static class RateLimitingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitingMiddleware>();
        }
    }
}
