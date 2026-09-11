using mqtt2otel.ManifestExplorer.Helper;
using mqtt2otel.ManifestExplorer.Meters;

namespace mqtt2otel.ManifestExplorer.Services
{
    /// <summary>
    /// This class is responsible for executing real user monitoring tasks.
    /// </summary>
    public class RealUserMonitoring(UsageMeter usageMeter)
    {
        /// <summary>
        /// The meter to track the data.
        /// </summary>
        private UsageMeter meter { get; set; } = usageMeter;

        /// <summary>
        /// Records a page load.
        /// </summary>
        /// <param name="browserInfo">Information gathered about the browser.</param>
        /// <param name="pageName">The page name.</param>
        public void RecordPageLoad(ClientInfo browserInfo, string pageName)
        {
            var parser = UAParser.Parser.GetDefault();
            var clientInfo = parser.ParseUserAgent(browserInfo.UserAgent);
            var os = parser.ParseOS(browserInfo.UserAgent);

            var pageTags = new System.Diagnostics.TagList()
            {
                { "Page", pageName},
                { "OS_Family", os.Family },
                { "OS", os.ToString() },
                { "Client", clientInfo.Family},
                { "Window.Width", (int)(browserInfo.WindowWidth / 100) * 100 },
                { "Window.Height", (int) (browserInfo.WindowHeight / 100) * 100 },
                { "Language", browserInfo.Language }
            };

            this.meter.PageRequestedCounter.Add(1, pageTags);
        }

        /// <summary>
        /// Records the event when an apply button is clicked on an example.
        /// </summary>
        /// <param name="exampleId">The example id</param>
        public void RecordExampleApplied(string? exampleId)
        {
            var tags = new System.Diagnostics.TagList()
            {
                {  "ExampleId", exampleId ?? "custom" }
            };

            this.meter.ExampleAppliedCounter.Add(1);
        }

        /// <summary>
        /// Records the event when an example is requested.
        /// </summary>
        /// <param name="exampleId">The id of the example that is requested.</param>
        public void RecordExampleRequested(string exampleId)
        {
            var exampleTags = new System.Diagnostics.TagList()
                {
                    { "ExampleId", exampleId}
                };

            this.meter.ExampleRequestedCounter.Add(1, exampleTags);
        }
    }
}
