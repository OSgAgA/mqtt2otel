using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using YamlDotNet.Serialization;

namespace mqtt2otel
{
    /// <summary>
    /// Manages the internally used ids of the application.
    /// </summary>
    public class InternalIds
    {
        /// <summary>
        /// Gets the instance id.
        /// </summary>
        public Guid InstanceId { get; private set; } = Guid.NewGuid();

        /// <summary>
        /// Gets the cluster id.
        /// </summary>
        public Guid ClusterId { get; private set; } = Guid.NewGuid();

        /// <summary>
        /// Reads the ids from a file. If file is not existing it is created and new ids are provided.
        /// </summary>
        /// <param name="settings">The application settings.</param>
        /// <returns>The retrieved internal ids.</returns>
        public static InternalIds ReadOrCreate(ApplicationSettings settings)
        {
            string path = Path.Combine(settings.InternalDataPath, "identifier.txt");

            var result = new InternalIds();

            string directory = Path.GetDirectoryName(path) ?? string.Empty;
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            if (!Path.Exists(path)) result.CreateNewFile(path);

            if (!result.ReadFromFile(path)) result.CreateNewFile(path);

            return result;
        }

        /// <summary>
        /// Creates a new file with the given path.
        /// </summary>
        /// <param name="path">The path including the filename.</param>
        private void CreateNewFile(string path)
        {
            string content = $"{nameof(InstanceId)}: {this.InstanceId}\n";
            content += $"{nameof(ClusterId)}: {this.ClusterId}";

            File.WriteAllText(path, content);
        }

        /// <summary>
        /// Reads information from a file at the given path.
        /// </summary>
        /// <param name="path">The path including the filename.</param>
        /// <returns>A value indicating whether the file could be read successfully.</returns>
        private bool ReadFromFile(string path)
        {
            try
            {
                var lines = File.ReadLines(path);

                Dictionary<string, string> dict = new();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var split = line.Split(':');
                    if (split.Length != 2) return false;

                    dict[split[0]] = split[1];
                }

                if (!dict.ContainsKey(nameof(InstanceId))) return false;
                if (!dict.ContainsKey(nameof(ClusterId))) return false;

                this.InstanceId = Guid.Parse(dict[nameof(InstanceId)]);
                this.ClusterId = Guid.Parse(dict[nameof(ClusterId)]);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
