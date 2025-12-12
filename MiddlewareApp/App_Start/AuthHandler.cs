using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MiddlewareApp.Services;

namespace MiddlewareApp.App_Start
{
    /// <summary>
    /// HTTP Message Handler for basic authentication and authorization
    /// In production, this should be replaced with proper OAuth/JWT validation
    /// </summary>
    public class AuthHandler : DelegatingHandler
    {
        private const string AuthScheme = "Basic";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Skip authentication for health check and Swagger endpoints
            if (IsPublicEndpoint(request.RequestUri.PathAndQuery))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            try
            {
                // Check for Authorization header
                if (!request.Headers.Contains("Authorization"))
                {
                    return CreateUnauthorizedResponse("Missing Authorization header");
                }

                var authHeader = request.Headers.GetValues("Authorization").FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader))
                {
                    return CreateUnauthorizedResponse("Empty Authorization header");
                }

                // Validate authentication scheme
                if (!authHeader.StartsWith(AuthScheme, StringComparison.OrdinalIgnoreCase))
                {
                    return CreateUnauthorizedResponse($"Invalid authentication scheme. Expected {AuthScheme}");
                }

                // Extract and validate credentials
                var encodedCredentials = authHeader.Substring(AuthScheme.Length).Trim();
                var credentials = ExtractCredentials(encodedCredentials);

                if (credentials == null)
                {
                    return CreateUnauthorizedResponse("Invalid credentials format");
                }

                // Validate user credentials
                if (!ValidateCredentials(credentials.Username, credentials.Password))
                {
                    LoggingService.LogWarning($"Authentication failed for user: {credentials.Username}");
                    return CreateUnauthorizedResponse("Invalid username or password");
                }

                // Set user principal for the request
                var identity = new GenericIdentity(credentials.Username, AuthScheme);
                var principal = new GenericPrincipal(identity, new[] { "User" });
                HttpContext.Current.User = principal;
                Thread.CurrentPrincipal = principal;

                LoggingService.LogDebug($"User {credentials.Username} authenticated successfully");

                // Continue with the request
                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Authentication error", ex);
                return CreateUnauthorizedResponse("Authentication error occurred");
            }
        }

        /// <summary>
        /// Check if the endpoint is public and doesn't require authentication
        /// </summary>
        private bool IsPublicEndpoint(string path)
        {
            var publicPaths = new[]
            {
                "/api/health",
                "/swagger",
                "/favicon.ico"
            };

            return publicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Extract username and password from Base64 encoded credentials
        /// </summary>
        private Credentials ExtractCredentials(string encodedCredentials)
        {
            try
            {
                var credentialBytes = Convert.FromBase64String(encodedCredentials);
                var credentialString = Encoding.UTF8.GetString(credentialBytes);
                var parts = credentialString.Split(':');

                if (parts.Length != 2)
                    return null;

                return new Credentials
                {
                    Username = parts[0],
                    Password = parts[1]
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Validate user credentials
        /// In production, this should validate against a secure user store (database, Azure AD, etc.)
        /// </summary>
        private bool ValidateCredentials(string username, string password)
        {
            // For demonstration purposes, accept any non-empty credentials
            // In production, implement proper credential validation:
            // - Check against database
            // - Validate against Azure AD
            // - Implement proper password hashing (bcrypt, PBKDF2, etc.)
            // - Implement account lockout policies
            // - Add rate limiting

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return false;

            // Demo credentials (replace with actual validation)
            // In production: query database, validate hash, check Azure AD, etc.
            return username.Length >= 3 && password.Length >= 6;
        }

        /// <summary>
        /// Create an unauthorized HTTP response
        /// </summary>
        private HttpResponseMessage CreateUnauthorizedResponse(string message)
        {
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent($"{{\"error\": \"{message}\"}}", Encoding.UTF8, "application/json")
            };
            response.Headers.Add("WWW-Authenticate", AuthScheme);
            return response;
        }

        private class Credentials
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
    }
}
