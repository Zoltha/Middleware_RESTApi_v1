using System;
using System.Web.Http;
using Swashbuckle.Application;

namespace MiddlewareApp.App_Start
{
    /// <summary>
    /// Configuration for Swagger/OpenAPI documentation
    /// </summary>
    public static class SwaggerConfig
    {
        /// <summary>
        /// Register Swagger configuration
        /// </summary>
        public static void Register()
        {
            var thisAssembly = typeof(SwaggerConfig).Assembly;

            GlobalConfiguration.Configuration
                .EnableSwagger(c =>
                {
                    // API info
                    c.SingleApiVersion("v1", "Middleware REST API")
                        .Description("ASP.NET Web API middleware providing secure gateway to Microsoft Dynamics 365 CRM")
                        .Contact(cc => cc
                            .Name("Middleware Team")
                            .Email("support@middleware.com"))
                        .License(lic => lic
                            .Name("MIT License")
                            .Url("https://opensource.org/licenses/MIT"));

                    // Include XML comments if available
                    var xmlPath = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "bin",
                        "MiddlewareApp.xml");
                    
                    if (System.IO.File.Exists(xmlPath))
                    {
                        c.IncludeXmlComments(xmlPath);
                    }

                    // Security definitions
                    c.BasicAuth("basic")
                        .Description("Basic HTTP authentication");

                    // Operation filters
                    c.OperationFilter<AddAuthorizationHeaderParameter>();

                    // Describe all enums as strings
                    c.DescribeAllEnumsAsStrings();
                })
                .EnableSwaggerUi(c =>
                {
                    // Enable OAuth2 implicit flow for testing
                    c.EnableDiscoveryUrlSelector();
                    c.EnableApiKeySupport("Authorization", "header");
                    c.DocExpansion(DocExpansion.List);
                    c.EnableValidator();
                });
        }
    }

    /// <summary>
    /// Adds Authorization header parameter to Swagger operations
    /// </summary>
    public class AddAuthorizationHeaderParameter : Swashbuckle.Swagger.IOperationFilter
    {
        public void Apply(
            Swashbuckle.Swagger.Operation operation,
            Swashbuckle.Swagger.SchemaRegistry schemaRegistry,
            System.Web.Http.Description.ApiDescription apiDescription)
        {
            // Skip public endpoints
            var publicEndpoints = new[] { "/api/health" };
            var isPublic = false;
            foreach (var endpoint in publicEndpoints)
            {
                if (apiDescription.RelativePath.StartsWith(endpoint.TrimStart('/'), StringComparison.OrdinalIgnoreCase))
                {
                    isPublic = true;
                    break;
                }
            }

            if (isPublic)
                return;

            if (operation.parameters == null)
                operation.parameters = new System.Collections.Generic.List<Swashbuckle.Swagger.Parameter>();

            operation.parameters.Add(new Swashbuckle.Swagger.Parameter
            {
                name = "Authorization",
                @in = "header",
                description = "Basic authentication header (e.g., 'Basic dXNlcm5hbWU6cGFzc3dvcmQ=')",
                required = true,
                type = "string",
                @default = "Basic "
            });

            operation.parameters.Add(new Swashbuckle.Swagger.Parameter
            {
                name = "Idempotency-Key",
                @in = "header",
                description = "Unique key for idempotent POST requests (optional, recommended for POST operations)",
                required = false,
                type = "string"
            });
        }
    }
}
