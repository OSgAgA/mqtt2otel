using mqtt2otel.ManifestExplorer.Tests.Helper;
using mqtt2otel.Shared;

namespace mqtt2otel.ManifestExplorer.Tests.JsonTestCases
{
    /// <summary>
    /// This class is responsible for testing the json test cases against the firefox browser.
    /// 
    /// The browsers are split up into different classes to be able to run in parallel.
    /// </summary>
    public class FirefoxJsonTests : JsonTestsBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonTestsBase"/> class.
        /// </summary>
        /// <param name="factory"></param>
        public FirefoxJsonTests(ManifestExplorerFactory factory) : base(factory) { }

        [Theory]
        [MemberData(nameof(TestCaseData.LoadAllAsMemberdataTestIds), MemberType = typeof(TestCaseData))]
        public async Task RunAllJsonTestsForFirefox(string id)
        {
            var settings = new UITestSettings() { Browser = BrowserEngine.Firefox, TraceDir = Path.Combine("playwright", "traces", "errors") };
            await this.RunJsonTestCase(settings, id);
        }

        [Fact]
        public async Task RunAndCreateTraceForExampleMetricOnFirefox()
        {
            var settings = new UITestSettings() { Browser = BrowserEngine.Firefox, CreateTraceOutput = ActionTrigger.Always, TraceDir = Path.Combine("playwright", "traces", "samples") };
            await this.RunJsonTestCase(settings, "metric-01");
        }

        [Fact]
        public async Task RunAndCreateTraceForExampleLogOnFirefox()
        {
            var settings = new UITestSettings() { Browser = BrowserEngine.Firefox, CreateTraceOutput = ActionTrigger.Always, TraceDir = Path.Combine("playwright", "traces", "samples") };
            await this.RunJsonTestCase(settings, "log-01");
        }
    }
}
