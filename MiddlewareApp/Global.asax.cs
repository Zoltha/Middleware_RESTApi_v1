using System;
using System.Web;
using System.Web.Http;
using Hangfire;
using Hangfire.SqlServer;
using MiddlewareApp.App_Start;
using MiddlewareApp.Background;
using MiddlewareApp.Services;

namespace MiddlewareApp
{
    /// <summary>
    /// Main application class that initializes Web API, Hangfire, and services
    /// </summary>
    public class WebApiApplication : HttpApplication
    {
        private BackgroundJobServer _hangfireServer;

        protected void Application_Start()
        {
            // Initialize logging service
            LoggingService.Initialize();

            // Configure Web API
            System.Web.Http.GlobalConfiguration.Configure(WebApiConfig.Register);

            // Configure Swagger
            SwaggerConfig.Register();

            // Configure Hangfire
            ConfigureHangfire();

            LoggingService.LogInformation("Application started successfully");
        }

        private void ConfigureHangfire()
        {
            try
            {
                var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["LogDatabase"].ConnectionString;

                // Configure Hangfire to use SQL Server
                Hangfire.GlobalConfiguration.Configuration
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.Zero,
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = true
                    });

                // Start Hangfire server
                _hangfireServer = new BackgroundJobServer();

                // Register recurring jobs
                HangfireTasks.RegisterRecurringJobs();

                LoggingService.LogInformation("Hangfire configured and started successfully");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to configure Hangfire", ex);
                throw;
            }
        }

        protected void Application_End()
        {
            _hangfireServer?.Dispose();
            LoggingService.LogInformation("Application stopped");
        }

        protected void Application_Error()
        {
            var exception = Server.GetLastError();
            if (exception != null)
            {
                LoggingService.LogError("Unhandled application error", exception);
            }
        }
    }

    /// <summary>
    /// Web API configuration
    /// </summary>
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Enable attribute routing
            config.MapHttpAttributeRoutes();

            // Configure default route
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Add authentication handler
            config.MessageHandlers.Add(new AuthHandler());

            // Configure JSON formatter
            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
            json.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;

            // Remove XML formatter
            config.Formatters.Remove(config.Formatters.XmlFormatter);
        }
    }
}
