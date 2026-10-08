using mqtt2otel.ManifestExplorer.Tests.Helper;
using mqtt2otel.Shared;

namespace mqtt2otel.ManifestExplorer.Tests.JsonTestCases
{
    /// <summary>
    /// This class is responsible for testing the json test cases against the webkit browser.
    /// 
    /// The browsers are split up into different classes to be able to run in parallel.
    /// </summary>
    public class WebkitJsonTests : JsonTestsBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonTestsBase"/> class.
        /// </summary>
        /// <param name="factory"></param>
        public WebkitJsonTests(ManifestExplorerFactory factory) : base(factory) { }

        [Theory]
        [MemberData(nameof(TestCaseData.LoadAllAsMemberdataTestIds), MemberType = typeof(TestCaseData))]
        public async Task RunAllJsonTestsForWebkit(string id)
        {
            var settings = new UITestSettings() { Browser = BrowserEngine.Webkit, TraceDir = Path.Combine("playwright", "traces", "errors") };
            await this.RunJsonTestCase(settings, id);
        }

        [Fact]
        public async Task RunAndCreateTraceForExampleMetricOnWebkit()
        {
            var settings = new UITestSettings() { Browser = BrowserEngine.Webkit, CreateTraceOutput = ActionTrigger.Always, TraceDir = Path.Combine("playwright", "traces", "samples") };
            await this.RunJsonTestCase(settings, "metric-01");
        }

        [Fact]
        public async Task RunAndCreateTraceForExampleLogOnWebkit()
        {
            var settings = new UITestSettings() { Browser = BrowserEngine.Webkit, CreateTraceOutput = ActionTrigger.Always, TraceDir = Path.Combine("playwright", "traces", "samples") };
            await this.RunJsonTestCase(settings, "log-01");
        }        
    }
}
