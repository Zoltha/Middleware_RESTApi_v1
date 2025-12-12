using System;
using System.Configuration;
using System.IO;
using System.Web.Hosting;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

namespace MiddlewareApp.Services
{
    /// <summary>
    /// Centralized logging service using Serilog with multiple sinks
    /// </summary>
    public static class LoggingService
    {
        private static ILogger _logger;
        private static bool _isInitialized = false;

        /// <summary>
        /// Initialize the logging service with configured sinks
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
                return;

            try
            {
                var config = LoadConfiguration();
                var loggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Is(ParseLogLevel(config.LogLevel))
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", "MiddlewareApp")
                    .Enrich.WithMachineName()
                    .Enrich.WithThreadId();

                // File logging
                if (config.EnableFileLogging)
                {
                    var logPath = GetLogFilePath();
                    loggerConfig.WriteTo.File(
                        logPath,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                    );
                }

                // SQL Server logging
                if (config.EnableSqlLogging)
                {
                    var connectionString = ConfigurationManager.ConnectionStrings["LogDatabase"].ConnectionString;
                    var columnOptions = new ColumnOptions();
                    columnOptions.Store.Remove(StandardColumn.Properties);
                    columnOptions.Store.Add(StandardColumn.LogEvent);

                    loggerConfig.WriteTo.MSSqlServer(
                        connectionString: connectionString,
                        sinkOptions: new MSSqlServerSinkOptions
                        {
                            TableName = "Logs",
                            SchemaName = "dbo",
                            AutoCreateSqlTable = true
                        },
                        columnOptions: columnOptions
                    );
                }

                // Azure Blob Storage logging (optional)
                if (config.EnableBlobLogging)
                {
                    try
                    {
                        var blobConnectionString = config.AzureBlobStorage;
                        if (!string.IsNullOrEmpty(blobConnectionString))
                        {
                            loggerConfig.WriteTo.AzureBlobStorage(
                                connectionString: blobConnectionString,
                                storageContainerName: "logs",
                                storageFileName: "middleware-{yyyy}-{MM}-{dd}.log"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        // Blob logging is optional, log to console if fails
                        Console.WriteLine($"Failed to configure Azure Blob logging: {ex.Message}");
                    }
                }

                _logger = loggerConfig.CreateLogger();
                _isInitialized = true;

                LogInformation("Logging service initialized successfully");
            }
            catch (Exception ex)
            {
                // Fallback to console if logging initialization fails
                Console.WriteLine($"Failed to initialize logging: {ex.Message}");
                _logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateLogger();
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Log an informational message
        /// </summary>
        public static void LogInformation(string message)
        {
            EnsureInitialized();
            _logger?.Information(message);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public static void LogWarning(string message)
        {
            EnsureInitialized();
            _logger?.Warning(message);
        }

        /// <summary>
        /// Log an error message with exception
        /// </summary>
        public static void LogError(string message, Exception exception = null)
        {
            EnsureInitialized();
            if (exception != null)
                _logger?.Error(exception, message);
            else
                _logger?.Error(message);
        }

        /// <summary>
        /// Log a debug message
        /// </summary>
        public static void LogDebug(string message)
        {
            EnsureInitialized();
            _logger?.Debug(message);
        }

        private static void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        private static string GetLogFilePath()
        {
            string logDirectory;
            
            if (HostingEnvironment.IsHosted)
            {
                logDirectory = HostingEnvironment.MapPath("~/Logs");
            }
            else
            {
                logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            }

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            return Path.Combine(logDirectory, "Middleware-.log");
        }

        private static LogEventLevel ParseLogLevel(string level)
        {
            if (Enum.TryParse<LogEventLevel>(level, true, out var result))
                return result;
            return LogEventLevel.Information;
        }

        private static LoggingConfiguration LoadConfiguration()
        {
            try
            {
                var appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(appSettingsPath))
                {
                    var json = File.ReadAllText(appSettingsPath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    return new LoggingConfiguration
                    {
                        LogLevel = settings?.Logging?.LogLevel ?? "Information",
                        EnableFileLogging = settings?.Logging?.EnableFileLogging ?? true,
                        EnableSqlLogging = settings?.Logging?.EnableSqlLogging ?? true,
                        EnableBlobLogging = settings?.Logging?.EnableBlobLogging ?? false,
                        AzureBlobStorage = settings?.ConnectionStrings?.AzureBlobStorage
                    };
                }
            }
            catch
            {
                // If configuration fails, use defaults
            }

            return new LoggingConfiguration
            {
                LogLevel = "Information",
                EnableFileLogging = true,
                EnableSqlLogging = true,
                EnableBlobLogging = false
            };
        }

        private class LoggingConfiguration
        {
            public string LogLevel { get; set; }
            public bool EnableFileLogging { get; set; }
            public bool EnableSqlLogging { get; set; }
            public bool EnableBlobLogging { get; set; }
            public string AzureBlobStorage { get; set; }
        }

        private class AppSettings
        {
            public ConnectionStrings ConnectionStrings { get; set; }
            public LoggingSettings Logging { get; set; }
        }

        private class ConnectionStrings
        {
            public string AzureBlobStorage { get; set; }
        }

        private class LoggingSettings
        {
            public string LogLevel { get; set; }
            public bool EnableFileLogging { get; set; }
            public bool EnableSqlLogging { get; set; }
            public bool EnableBlobLogging { get; set; }
        }
    }
}
