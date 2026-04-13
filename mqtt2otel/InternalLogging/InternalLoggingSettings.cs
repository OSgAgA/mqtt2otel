using Microsoft.Extensions.Logging;
using mqtt2otel.Configuration;
using OpenTelemetry.Exporter;
using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;

namespace mqtt2otel.InternalLogging
{
    /// <summary>
    /// Provides the available settings for internal logging.
    /// </summary>
    public class InternalLoggingSettings
    {
        /// <summary>
        /// Gets or sets a value indicating, wether the application should log to the console.
        /// </summary>
        public bool LogToConsole { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating, whether the application should log to a file.
        /// </summary>
        public bool LogToFile { get; set; } = false;

        /// <summary>
        /// Gets or sets the path to which the file logger will write its files. Will be ignroed if <see cref="LogToFile"/> is false.
        /// </summary>
        public string LogFilePath { get; set; } = "logs";

        /// <summary>
        /// Gets or sets the maximum numbers of files that will be kept by the file logger. Will be ignroed if <see cref="LogToFile"/> is false.
        /// </summary>
        public int LogFileKeepMax { get; set; } = 5;

        /// <summary>
        /// Gets or sets a value indicating, whether the application should log to an open telemetry endpoint.
        /// </summary>
        public bool LogToOtel { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the minimum log level that will be logged.
        /// </summary>
        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Gets or sets the otel server settings. Will be ignored if <see cref="LogToOtel"/> is false.
        /// </summary>
        public OtelServerSettings Otel { get; set; }  = new();
    }
}
