using mqtt2otel.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace mqtt2otel.ManifestExplorer.DTOs
{
    /// <summary>
    /// Represents example data that can be used inside the manifest explorer.
    /// </summary>
    public class ExampleData
    {
        /// <summary>
        /// Maps an example id to the example data.
        /// </summary>
        private static Dictionary<string, ExampleData> cache = new Dictionary<string, ExampleData>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleData"/> class.
        /// 
        /// This is for serialization purposes only and should not be used directly.
        /// </summary>
        public ExampleData() { }


        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleData"/> class.
        /// </summary>
        /// <param name="id">The example id.</param>
        /// <param name="name">The name of the example</param>
        /// <param name="description">A description.</param>
        /// <param name="topic">The mqtt topic.</param>
        /// <param name="payload">The mqtt payload</param>
        /// <param name="manifest">The manifest.</param>
        public ExampleData(string id, string name, string description, string topic, string payload, string manifest)
        {
            this.Id = id;
            this.Name = name;
            this.Description = description;
            this.Topic = topic;
            this.Payload = payload;
            this.Manifest = manifest;
        }

        /// <summary>
        /// Creates all example data from the json files.
        /// </summary>
        private static void Create()
        {
            if (cache != null && cache.Count > 0) return;

            cache = new Dictionary<string, ExampleData>();

            foreach (var example in TestCase.LoadAllExamples())
            {
                cache[example.Key] = example.Value;
            }
        }

        /// <summary>
        /// Gets an example via the given id.
        /// </summary>
        /// <param name="id">The example id.</param>
        /// <returns>The example with the provided id.</returns>
        /// <exception cref="Exception">Thrown if id cannot be found.</exception>
        public static ExampleData GetExampleById(string id)
        {
            ExampleData.Create();
            if (cache.ContainsKey(id)) return cache[id];

            return cache.First().Value;
        } 

        /// <summary>
        /// Gets all examples.
        /// </summary>
        /// <returns>All available examples.</returns>
        public static List<ExampleData> GetAll()
        {
            var result = new List<ExampleData>();

            Create();
            foreach (var item in cache)
            {
                result.Add(item.Value);
            }

            return result;
        }

        /// <summary>
        /// Gets or sets the id of the example.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the example name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the example description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the example topic.
        /// </summary>
        public string Topic { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the example payload.
        /// </summary>
        public string Payload { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the example manifest.
        /// </summary>
        public string Manifest { get; set; } = string.Empty;


    }
}
