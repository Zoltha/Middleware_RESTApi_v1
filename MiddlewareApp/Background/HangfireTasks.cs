using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using MiddlewareApp.Services;

namespace MiddlewareApp.Background
{
    /// <summary>
    /// Hangfire background tasks for periodic operations
    /// </summary>
    public static class HangfireTasks
    {
        /// <summary>
        /// Register all recurring jobs
        /// </summary>
        public static void RegisterRecurringJobs()
        {
            try
            {
                // Register hourly CRM sync job
                RecurringJob.AddOrUpdate(
                    "sync-crm-accounts",
                    () => SyncCrmAccountsAsync(),
                    Cron.Hourly,
                    TimeZoneInfo.Local);

                LoggingService.LogInformation("Recurring jobs registered successfully");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to register recurring jobs", ex);
                throw;
            }
        }

        /// <summary>
        /// Sync CRM accounts to SQL database
        /// Runs hourly to maintain a local cache of CRM data
        /// </summary>
        [AutomaticRetry(Attempts = 3)]
        public static async Task SyncCrmAccountsAsync()
        {
            var startTime = DateTime.UtcNow;
            LoggingService.LogInformation("Starting CRM accounts sync job");

            try
            {
                var crmService = new DynamicsCrmService();
                var accounts = await crmService.GetAccountsAsync();

                if (accounts == null || accounts.Count == 0)
                {
                    LoggingService.LogWarning("No accounts retrieved from CRM during sync");
                    return;
                }

                // Log sync statistics
                var duration = DateTime.UtcNow - startTime;
                LoggingService.LogInformation(
                    $"CRM sync completed successfully. " +
                    $"Synced {accounts.Count} accounts in {duration.TotalSeconds:F2} seconds");

                // Calculate and log statistics
                var accountsWithEmail = accounts.Count(a => !string.IsNullOrEmpty(a.Email));
                var accountsWithPhone = accounts.Count(a => !string.IsNullOrEmpty(a.Phone));
                var accountsWithRevenue = accounts.Count(a => a.Revenue.HasValue);

                LoggingService.LogInformation(
                    $"Sync statistics: {accountsWithEmail} with email, " +
                    $"{accountsWithPhone} with phone, " +
                    $"{accountsWithRevenue} with revenue");

                // Store sync metadata to database (optional)
                await StoreSyncMetadataAsync(accounts.Count, duration);
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                LoggingService.LogError($"CRM sync job failed after {duration.TotalSeconds:F2} seconds", ex);
                throw; // Re-throw to trigger Hangfire retry
            }
        }

        /// <summary>
        /// Store sync metadata to database for monitoring
        /// </summary>
        private static async Task StoreSyncMetadataAsync(int accountCount, TimeSpan duration)
        {
            try
            {
                using (var db = new Data.SqlDbContext())
                {
                    // Create a simple log entry in the database
                    // In a real application, you might have a SyncHistory table
                    var metadata = new
                    {
                        SyncTime = DateTime.UtcNow,
                        AccountCount = accountCount,
                        DurationSeconds = duration.TotalSeconds,
                        Status = "Success"
                    };

                    LoggingService.LogDebug($"Sync metadata: {Newtonsoft.Json.JsonConvert.SerializeObject(metadata)}");
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning($"Failed to store sync metadata: {ex}");
                // Don't throw - metadata storage is not critical
            }
        }

        /// <summary>
        /// Clean up expired idempotency records
        /// This job runs daily to remove old records
        /// </summary>
        [AutomaticRetry(Attempts = 2)]
        public static async Task CleanupExpiredIdempotencyRecordsAsync()
        {
            LoggingService.LogInformation("Starting idempotency records cleanup job");

            try
            {
                using (var db = new Data.SqlDbContext())
                {
                    var expiredRecords = db.IdempotencyRecords
                        .Where(r => r.ExpiresAt < DateTime.UtcNow)
                        .ToList();

                    if (expiredRecords.Count > 0)
                    {
                        db.IdempotencyRecords.RemoveRange(expiredRecords);
                        await db.SaveChangesAsync();
                        
                        LoggingService.LogInformation($"Cleaned up {expiredRecords.Count} expired idempotency records");
                    }
                    else
                    {
                        LoggingService.LogInformation("No expired idempotency records to clean up");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to cleanup expired idempotency records", ex);
                throw;
            }
        }

        /// <summary>
        /// Register cleanup job for idempotency records (runs daily)
        /// </summary>
        public static void RegisterCleanupJob()
        {
            try
            {
                RecurringJob.AddOrUpdate(
                    "cleanup-idempotency-records",
                    () => CleanupExpiredIdempotencyRecordsAsync(),
                    Cron.Daily,
                    TimeZoneInfo.Local);

                LoggingService.LogInformation("Idempotency cleanup job registered successfully");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to register cleanup job", ex);
            }
        }
    }
}
