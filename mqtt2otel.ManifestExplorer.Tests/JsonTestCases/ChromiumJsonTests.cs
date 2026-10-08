using mqtt2otel.ManifestExplorer.Tests.Helper;
using mqtt2otel.Shared;

namespace mqtt2otel.ManifestExplorer.Tests.JsonTestCases
{
    /// <summary>
    /// This class is responsible for testing the json test cases against the chromium browser.
    /// 
    /// The browsers are split up into different classes to be able to run in parallel.
    /// </summary>
    public class ChromiumJsonTests : JsonTestsBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonTestsBase"/> class.
        /// </summary>
        /// <param name="factory"></param>
        public ChromiumJsonTests(ManifestExplorerFactory factory) : base(factory) { }

        [Theory]
        [MemberData(nameof(TestCaseData.LoadAllAsMemberdataTestIds), MemberType = typeof(TestCaseData))]
        public async Task RunAllJsonTestsForChromium(string id)
        {
            var settings = new UITestSettings() { Browser = BrowserEngine.Chromium, TraceDir = Path.Combine("playwright", "traces", "errors") };
            await this.RunJsonTestCase(settings, id);
        }

        [Fact]
        public async Task RunAndCreateTraceForExampleMetricOnChromium()
        {
            var settings = new UITestSettings() { Browser = BrowserEngine.Chromium, CreateTraceOutput = ActionTrigger.Always, TraceDir = Path.Combine("playwright", "traces", "samples") };
            await this.RunJsonTestCase(settings, "metric-01");
        }

        [Fact]
        public async Task RunAndCreateTraceForExampleLogOnChromium()
        {
            var settings = new UITestSettings() { Browser = BrowserEngine.Chromium, CreateTraceOutput = ActionTrigger.Always, TraceDir = Path.Combine("playwright", "traces", "samples") };
            await this.RunJsonTestCase(settings, "log-01");
        }
    }
}
