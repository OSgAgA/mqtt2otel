using Microsoft.Extensions.Logging;
using mqtt2otel.Manifest;
using mqtt2otel.Shared.Helper;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace mqtt2otel
{
    /// <summary>
    /// A helper class for communicating with the internal otel collector.
    /// </summary>
    public class OtelCollector
    {
        /// <summary>
        /// The internal logger.
        /// </summary>
        private ILogger<OtelCollector>? internalLogger;

        /// <summary>
        /// Handles access to the otel collector process.
        /// </summary>
        private Process otelCollectorProcess = new Process();

        /// <summary>
        /// Gets a value indicating, whether the collector is active. An inactive collector will not create
        /// any config files or start a process.
        /// </summary>
        private bool isActive = true;

        /// <summary>
        /// Gets the path to the open telemetry collector config file.
        /// </summary>
        private string configFilePath
        {
            get => Path.Join("/", "etc", "otelcol", "config.yaml");
        }

        /// <summary>
        /// Gets the path to the open telemetry collector executable.
        /// </summary>
        private string executableFilePath
        {
            get => Path.Join("/", "usr", "local", "bin", this.executableFileName);
        }

        /// <summary>
        /// Gets the filename of the collector executable.
        /// </summary>
        private string executableFileName
        {
            get => "otelcol-contrib";
        }

        public OtelCollector(ILogger<OtelCollector>? internalLogger = null, bool isActive = true)
        {
            this.internalLogger = internalLogger;
            this.KillExisting();

            this.otelCollectorProcess.StartInfo.FileName = this.executableFilePath;
            this.otelCollectorProcess.StartInfo.Arguments = $"--config=file:{configFilePath}";
            this.otelCollectorProcess.ErrorDataReceived += collectorError;
            this.otelCollectorProcess.StartInfo.UseShellExecute = false;
            this.otelCollectorProcess.StartInfo.RedirectStandardError = true;
            this.isActive = isActive;
        }


        /// <summary>
        /// Create and store the collector config.
        /// </summary>
        /// <param name="connections">The available connections.</param>
        public void CreateConfig(List<OtelServerConnection> connections)
        {
            if (!this.isActive) return;

            string config = OtelCollectorConfig.CreateConfig(connections);

            Directory.CreateDirectory(Path.GetDirectoryName(this.configFilePath) ?? string.Empty);
            File.WriteAllText(Path.Join(this.configFilePath), config);

            this.StartCollector();
        }

        /// <summary>
        /// Stops the otel collector.
        /// </summary>
        public void Stop()
        {
            this.otelCollectorProcess.Close();
        }

        /// <summary>
        /// (Re)starts the open telemetry collector process.
        /// </summary>
        private void StartCollector()
        {
            this.otelCollectorProcess.Close();

            var result = this.otelCollectorProcess.Start();
            this.otelCollectorProcess.BeginErrorReadLine();
        }


        /// <summary>
        /// The otel collector writes its output to the error queue.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args.</param>
        private void collectorError(object sender, DataReceivedEventArgs e)
        {
            this.internalLogger?.LogInformation($"Otel-Collector: {e.Data}");
        }

        /// <summary>
        /// Kills all existing otel collector processes, that may be still active from a crash, or hard kill of the application.
        /// </summary>
        private void KillExisting()
        {
            string processName = this.executableFileName.Substring(0, 15); // process names are limited to 15 characters!

            foreach (var p in Process.GetProcessesByName(processName))
            {
                try
                {
                    this.internalLogger?.LogWarning("An instance of the otel collector is still running. Killing collector with id: {ProcessId}", p.Id);
                    p.Kill(entireProcessTree: true);
                    p.WaitForExit(TimeSpan.FromSeconds(1));
                }
                catch (InvalidOperationException) { }              // already exited
                finally
                {
                    p.Dispose();
                }
            }
        }
    }
}
