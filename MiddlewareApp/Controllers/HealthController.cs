using System;
using System.Data.Entity;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Hangfire;
using Hangfire.Storage;
using MiddlewareApp.Data;
using MiddlewareApp.Services;

namespace MiddlewareApp.Controllers
{
    /// <summary>
    /// Health check endpoint for monitoring system status
    /// </summary>
    [RoutePrefix("api/health")]
    public class HealthController : ApiController
    {
        /// <summary>
        /// Performs comprehensive health check on all system components
        /// </summary>
        /// <returns>Health status of database, CRM, and Hangfire scheduler</returns>
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetHealth()
        {
            var health = new HealthStatus
            {
                Timestamp = DateTime.UtcNow,
                Status = "Healthy"
            };

            try
            {
                // Check database connectivity
                health.Database = await CheckDatabaseHealthAsync();

                // Check CRM connectivity
                health.Crm = await CheckCrmHealthAsync();

                // Check Hangfire scheduler
                health.Hangfire = CheckHangfireHealth();

                // Determine overall status
                if (health.Database.Status == "Unhealthy" || 
                    health.Crm.Status == "Unhealthy" || 
                    health.Hangfire.Status == "Unhealthy")
                {
                    health.Status = "Unhealthy";
                    return Content(HttpStatusCode.ServiceUnavailable, health);
                }

                if (health.Database.Status == "Degraded" || 
                    health.Crm.Status == "Degraded" || 
                    health.Hangfire.Status == "Degraded")
                {
                    health.Status = "Degraded";
                }

                return Ok(health);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Health check failed", ex);
                health.Status = "Unhealthy";
                health.Error = ex.Message;
                return Content(HttpStatusCode.ServiceUnavailable, health);
            }
        }

        /// <summary>
        /// Check database health
        /// </summary>
        private async Task<ComponentHealth> CheckDatabaseHealthAsync()
        {
            var componentHealth = new ComponentHealth { Component = "Database" };
            
            try
            {
                using (var db = new SqlDbContext())
                {
                    // Test database connectivity with a simple query
                    var canConnect = await db.Database.ExecuteSqlCommandAsync("SELECT 1");
                    componentHealth.Status = "Healthy";
                    componentHealth.ResponseTimeMs = 0; // Could add timing here
                    componentHealth.Message = "Database connection successful";
                }
            }
            catch (Exception ex)
            {
                componentHealth.Status = "Unhealthy";
                componentHealth.Message = $"Database error: {ex.Message}";
                LoggingService.LogError("Database health check failed", ex);
            }

            return componentHealth;
        }

        /// <summary>
        /// Check CRM connectivity
        /// </summary>
        private async Task<ComponentHealth> CheckCrmHealthAsync()
        {
            var componentHealth = new ComponentHealth { Component = "CRM" };
            
            try
            {
                var crmService = new DynamicsCrmService();
                var startTime = DateTime.UtcNow;
                
                // Attempt to fetch a small set of accounts to verify connectivity
                var accounts = await crmService.GetAccountsAsync();
                
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                componentHealth.Status = "Healthy";
                componentHealth.ResponseTimeMs = (int)responseTime;
                componentHealth.Message = $"CRM connection successful. Retrieved {accounts.Count} accounts.";
            }
            catch (Exception ex)
            {
                // CRM connectivity issues might be configuration-related
                componentHealth.Status = "Degraded";
                componentHealth.Message = $"CRM connectivity issue: {ex.Message}";
                LoggingService.LogWarning($"Failed to store sync metadata: {ex}");
            }

            return componentHealth;
        }

        /// <summary>
        /// Check Hangfire scheduler status
        /// </summary>
        private ComponentHealth CheckHangfireHealth()
        {
            var componentHealth = new ComponentHealth { Component = "Hangfire" };
            
            try
            {
                using (var connection = JobStorage.Current.GetConnection())
                {
                    var monitoringApi = JobStorage.Current.GetMonitoringApi();
                    var servers = monitoringApi.Servers();

                    var recurringJobsCount = 0;
                    // If you need to count recurring jobs, you may need to use Hangfire.Storage.Monitoring.RecurringJobDto
                    // and access the storage directly, or remove this feature if not required.

                    componentHealth.Status = servers.Count > 0 ? "Healthy" : "Degraded";
                    componentHealth.Message = $"{servers.Count} server(s) running, {recurringJobsCount} recurring job(s) configured";
                }
            }
            catch (Exception ex)
            {
                componentHealth.Status = "Unhealthy";
                componentHealth.Message = $"Hangfire error: {ex.Message}";
                LoggingService.LogError("Hangfire health check failed", ex);
            }

            return componentHealth;
        }
    }

    /// <summary>
    /// Overall health status
    /// </summary>
    public class HealthStatus
    {
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
        public ComponentHealth Database { get; set; }
        public ComponentHealth Crm { get; set; }
        public ComponentHealth Hangfire { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// Health status for individual components
    /// </summary>
    public class ComponentHealth
    {
        public string Component { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public int ResponseTimeMs { get; set; }
    }
}
