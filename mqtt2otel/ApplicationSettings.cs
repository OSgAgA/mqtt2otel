using Microsoft.Extensions.Logging;
using mqtt2otel.Manifest;
using mqtt2otel.InternalLogging;
using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;

namespace mqtt2otel
{
    /// <summary>
    /// Represents the general settings of the application.
    /// </summary>
    public class ApplicationSettings
    {
        /// <summary>
        /// Read settings from a yaml file.
        /// </summary>
        /// <param name="path">The path to the yaml file.</param>
        /// <returns>The parsed settings.</returns>
        public static ApplicationSettings ReadFromYaml(string path = "ApplicationSettings.yaml")
        {
            if (!Path.Exists(path))
            {
                // Look directly in the application directory, useful for development
                var developmentPath = "./ApplicationSettings.yaml";

                if (Path.Exists(developmentPath))
                {
                    path = developmentPath;
                }
                else
                {
                    try
                    {
                        // try creating a new application file.
                        var settings = new ApplicationSettings();
                        var serializer = new SerializerBuilder().Build();

                        var serialized = serializer.Serialize(settings);
                        File.WriteAllText(path, serialized);

                        return settings;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Cannot create application settings file at {path}. The following error occured: {ex}");

                        return new ApplicationSettings();
                    }
                }
            }

            // read settings from file.
            var yaml = File.ReadAllText(path);
            var deserializer = new DeserializerBuilder().Build();

            return deserializer.Deserialize<ApplicationSettings>(yaml);
        }

        /// <summary>
        /// Gets or sets the logging settings used for internal logging.
        /// </summary>
        public InternalLoggingSettings Logging { get; set; } = new();

        /// <summary>
        /// Gets or sets the delay intervall that will be used for <see cref="AutoUpdateMode.PollManifestFile"/>.
        /// </summary>
        public int PollIntervallInSeconds { get; set; } = 5;

        /// <summary>
        /// Gets or sets the path to the manifest file.
        /// </summary>
        public string ManifestPath { get; set; } = "./Manifest.yaml";
    }
}
