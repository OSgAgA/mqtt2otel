using Microsoft.Extensions.Logging;
using mqtt2otel.Interfaces;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using ZLogger;
using ZLogger.Providers;

namespace mqtt2otel.InternalLogging
{
    /// <summary>
    /// Represents a log factory class, for creating internal loggers.
    /// </summary>
    public class InternalLogFactory
    {
        /// <summary>
        /// Defines the main activity source used by the project.
        /// </summary>
        public static ActivitySource MainActivitySource = new("mqtt2otel-internal-activity-source");

        /// <summary>
        /// The tracer provider.
        /// </summary>
        private static TracerProvider? tracerProvider;

        /// <summary>
        /// Creates a new factory.
        /// </summary>
        /// <param name="settings">The application settings for logging.</param>
        /// <returns>The created logger factory.</returns>
        public static ILoggerFactory Create(InternalLoggingSettings settings)
        {
            return LoggerFactory.Create(logging =>
            {
                logging.SetMinimumLevel(settings.MinimumLogLevel);

                if (settings.LogToConsole) AddConsoleLogger(logging, settings);
                if (settings.LogToOtel) AddOtelLogger(logging, settings);
                if (settings.LogToFile) AddFileLogger(logging, settings);
            });
        }

        /// <summary>
        /// Adds an otel logger to a logging builder.
        /// </summary>
        /// <param name="loggingBuilder">The logging builder.</param>
        /// <param name="settings">The logging settings that should be applied.</param>
        /// <exception cref="Exception">Thrown if no otel server endpoint address is set.</exception>
        private static void AddOtelLogger(ILoggingBuilder loggingBuilder, InternalLoggingSettings settings)
        {
            loggingBuilder.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                    .AddService(settings.Otel.ServiceName, serviceNamespace: settings.Otel.ServiceNamespace))
                    .IncludeScopes = true;

                options.AddOtlpExporter(otlpOptions =>
                {
                    if (settings.Otel.Endpoint.Address == null) throw new Exception("Address of Otel server endpoint must be set!");

                    otlpOptions.Endpoint = settings.Otel.Endpoint.Uri;
                    otlpOptions.Protocol = settings.Otel.OtlpExportProtocol;
                    otlpOptions.ExportProcessorType = settings.Otel.ExportProcessorType;
                });
            });

            InternalLogFactory.tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddSource(InternalLogFactory.MainActivitySource.Name)
                .SetResourceBuilder(
                    OpenTelemetry.Resources.ResourceBuilder.CreateDefault()
                        .AddService(settings.Otel.ServiceName, serviceNamespace: settings.Otel.ServiceNamespace))
                .AddOtlpExporter(otlpOptions =>
                {
                    if (settings.Otel.Endpoint.Address == null) throw new Exception("Address of Otel server endpoint must be set!");

                    otlpOptions.Endpoint = settings.Otel.Endpoint.Uri;
                    otlpOptions.Protocol = settings.Otel.OtlpExportProtocol;
                    otlpOptions.ExportProcessorType = settings.Otel.ExportProcessorType;
                })
                .Build();
        }

        /// <summary>
        /// Adds a console logger to the logging builder.
        /// </summary>
        /// <param name="loggingBuilder">The logging builder.</param>
        /// <param name="settings">The logging settings that should be applied.</param>
        private static void AddConsoleLogger(ILoggingBuilder loggingBuilder, InternalLoggingSettings settings)
        {
            var options = new ZLoggerConsoleOptions();

            options.UsePlainTextFormatter(formatter =>
            {
                formatter.SetPrefixFormatter($"{0:yyyy-MM-ddTHH:mm:ssZ} {1}  ", (in MessageTemplate template, in LogInfo info) => template.Format(info.Timestamp.Utc, info.LogLevel.ToString().PadLeft(11).ToUpper()));
            });

            var loggerProvider = new ZLoggerConsoleLoggerProvider(options);

            loggingBuilder.AddProvider(new ActivityLoggerProvider(loggerProvider, InternalLogFactory.MainActivitySource));
        }

        /// <summary>
        /// Adds a file logger to the logging builder.
        /// </summary>
        /// <param name="loggingBuilder">The logging builder.</param>
        /// <param name="settings">The logging settings that should be applied.</param>
        private static void AddFileLogger(ILoggingBuilder loggingBuilder, InternalLoggingSettings settings)
        {
            var dir = new DirectoryInfo(settings.LogFilePath);

            if (!dir.Exists) dir.Create();

            var options = new ZLoggerRollingFileOptions();

            options.RollingInterval = RollingInterval.Day;
            options.FilePathSelector = (timestamp, sequenceNumber) => FilePathSelector(timestamp, sequenceNumber, settings);

            options.UsePlainTextFormatter(formatter =>
            {
                formatter.SetPrefixFormatter($"{0:yyyy-MM-ddTHH:mm:ssZ} {1}  ", (in MessageTemplate template, in LogInfo info) => template.Format(info.Timestamp.Utc, info.LogLevel.ToString().PadLeft(11).ToUpper()));
            });

            var loggerProvider = new ZLoggerRollingFileLoggerProvider(options);

            loggingBuilder.AddProvider(new ActivityLoggerProvider(loggerProvider, InternalLogFactory.MainActivitySource));
        }

        /// <summary>
        /// Called by the rolling file logger to determine a file path for new log files. Old Files will be deleted. <see cref="ApplicationSettings.Logging.LogFileKeepMax"/>.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="sequenceNumber">The current sequence number.</param>
        /// <param name="settings">The internal log settings.</param>
        /// <returns>The created file path.</returns>
        private static string FilePathSelector(DateTimeOffset timestamp, int sequenceNumber, InternalLoggingSettings settings)
        {
            var dir = new DirectoryInfo(settings.LogFilePath);

            var files = dir.GetFiles("*.log")
                           .OrderByDescending(f => f.CreationTimeUtc)
                           .Skip(settings.LogFileKeepMax)
                           .ToList();

            foreach (var file in files)
            {
                try { file.Delete(); }
                catch { }
            }

            return $"{settings.LogFilePath}/{timestamp.ToUniversalTime():yyyy-MM-dd}_mqtt2otel_{sequenceNumber:000}.log";
        }
    }
}
