using Microsoft.Playwright;
using mqtt2otel.Shared;

namespace mqtt2otel.ManifestExplorer.Tests
{
    /// <summary>
    /// Tests all examples that are available to the explorer.
    /// </summary>
    public class ExampleDataTests : PageTestBase, IClassFixture<ManifestExplorerFactory>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleDataTests"/> class.
        /// </summary>
        /// <param name="factory"></param>
        public ExampleDataTests(ManifestExplorerFactory factory) : base(factory) { }

        /// <summary>
        /// For all available examples:
        /// 
        /// STEP 1:
        ///     If example is on example page: open home --ClickStartWithExample--> ExamplePage --ClickExampleLink--> Explorer
        ///                              else: open Explorer with example id.
        ///     
        /// STEP 2:
        ///     Tests if all setup information is set correctly in the UI.
        ///     
        /// STEP 3:
        ///     If example links are available, then tests if all the links appear with the right description and navigates to the links.
        ///     
        /// STEP 4:
        ///     Clicks the apply button.
        ///     
        /// STEP 5:
        ///     Tests if the expected results are shown on the UI.
        /// </summary>
        /// <param name="testCase">The case to be tested.</param>
        [Theory]
        [MemberData(nameof(TestCaseData.LoadAllAsMemberdataTestIds), MemberType = typeof(TestCaseData))]
        public async Task ShouldSuccessfullyExecuteExample(string exampleId)
        {
            // Arrange  
            var testCase = TestCaseData.GetById(exampleId);

            // Act and assert
            await this.NavigateToExplorerWithExample(testCase);

            await CheckExampleSetup(testCase);

            await CheckDocumentationLinks(testCase);

            await this.Page.GetByTestId("button-apply").ClickAsync();

            await CheckTestResults(testCase);
        }

        /// <summary>
        /// Tests if the expected results are shown on the UI.
        /// </summary>
        /// <param name="testCase">The current test case.</param>
        private async Task CheckTestResults(TestCaseData testCase)
        {
            // Check result headers
            var logResultsHeader = this.Page.GetByTestId("result-container").GetByRole(AriaRole.Tab).Nth(2);
            await Expect(logResultsHeader).ToHaveTextAsync($"Logs ({testCase.ExpectedResults[0].Logs.Count})");

            var metricsResultHeader = this.Page.GetByTestId("result-container").GetByRole(AriaRole.Tab).Nth(1);
            await Expect(metricsResultHeader).ToHaveTextAsync($"Metrics ({testCase.ExpectedResults[0].Metrics.Count})");

            var errorResultHeader = this.Page.GetByTestId("result-container").GetByRole(AriaRole.Tab).Nth(0);
            await Expect(errorResultHeader).ToHaveTextAsync($"Errors (0)");

            // Check test results
            if (testCase.ExpectedResults[0].Metrics.Count > 0)
            {
                await metricsResultHeader.ClickAsync();
                await CheckMetricResults(testCase, metricsResultHeader);
            }

            if (testCase.ExpectedResults[0].Logs.Count > 0)
            {
                await logResultsHeader.ClickAsync();
                await CheckLogResults(testCase);
            }
        }

        /// <summary>
        /// Checks if all expected log results for the current test case are available on the UI.
        /// 
        /// The log result data must be visible on the UI for this step.
        /// </summary>
        /// <param name="testCase">The current test case.</param>
        private async Task CheckLogResults(TestCaseData testCase)
        {
            var logResultContainer = this.Page.GetByTestId("log-result-container");

            await Iterate(logResultContainer, testCase.ExpectedResults[0].Logs, "log-result-entry", async (log, locator) =>
            {
                await Expect(locator.GetByTestId("log-result-timestamp")).ToHaveTextAsync(log.Timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                await Expect(locator.GetByTestId("log-result-level")).ToHaveTextAsync(log.LogLevel.ToString());
                await Expect(locator.GetByTestId("log-result-body")).ToHaveTextAsync(log.Body ?? string.Empty);

                await this.Iterate(locator, log.Attributes.Where(tag => !tag.Key.StartsWith("otel_")), "attribute", async (tag, locator) =>
                {
                    await Expect(locator.GetByTestId("attribute-key")).ToHaveTextAsync(tag.Key);
                    await Expect(locator.GetByTestId("attribute-value")).ToHaveTextAsync(tag.Value?.ToString() ?? string.Empty);
                });
            });
        }

        /// <summary>
        /// If example links are available, then tests if all the links appear with the right description and navigates to the links.
        /// </summary>
        /// <param name="testCase">The current test case.</param>
        private async Task CheckDocumentationLinks(TestCaseData testCase)
        {
            var exampleLinksContainer = this.Page.GetByTestId("example-link-container");

            await Iterate(exampleLinksContainer, testCase.Links, "example-link-item", async (link, locator) =>
            {
                var linkLocator = locator.GetByTestId("example-link-entry");

                await Expect(linkLocator).ToHaveAttributeAsync("href", link.Value);
                await Expect(linkLocator).ToHaveTextAsync(link.Key);

                var url = await linkLocator.GetAttributeAsync("href") ?? "href returned <<null>>.";
                await this.IsLinkReachable(url);
            });
        }

        /// <summary>
        /// Tests if all setup information is set correctly in the UI.
        /// </summary>
        /// <param namee="testCase">The current test case.</param>
        private async Task CheckExampleSetup(TestCaseData testCase)
        {
            await Expect(this.Page.GetByTestId("input-topic")).ToHaveValueAsync(testCase.Setup.MqttData[0].Topic);

            await Expect(this.Page.GetByTestId("input-payload")).ToHaveValueAsync(testCase.Setup.MqttData[0].Payload.Replace("\r", ""));

            var userPropertiesContainer = this.Page.GetByTestId("user-properties-container");

            await Iterate(userPropertiesContainer, testCase.Setup.MqttData[0].UserProperties, "user-property-item-container", async (prop, propItem) =>
            {
                await Expect(propItem.GetByTestId("input-user-property-name")).ToHaveValueAsync(prop.Name);
                await Expect(propItem.GetByTestId("input-user-property-value")).ToHaveValueAsync(prop.Value);
            });

            await ExpectManifestEditor(testCase.Setup.Manifest);
        }

        /// <summary>
        /// If example is on example page: open home --ClickStartWithExample--> ExamplePage --ClickExampleLink--> Explorer
            //                       else: open Explorer with example id.
        /// </summary>
        /// <param name="testCase">The current test case.</param>
        private async Task NavigateToExplorerWithExample(TestCaseData testCase)
        {
            if (testCase.Setup.CreateExample)
            {
                await this.Page.GotoAsync(this.ServerAddress);
                await this.Page.GetByTestId("button-start-with-example").ClickAsync();

                var link = this.Page.Locator($"a[href$='exampleId={testCase.Setup.Id}']").First;
                await link.ClickAsync();
            }
            else
            {
                string uri = $"{this.ServerAddress}Explorer/?exampleId={testCase.Setup.Id}";
                await this.Page.GotoAsync(uri, new PageGotoOptions() {  Timeout = 60000 });
            }
        }

        /// <summary>
        /// Checks if all expected metric results for the current test case are available on the UI.
        /// 
        /// The metric data must be visible on the UI for this step.
        /// </summary>
        /// <param name="testCase">The current test case.</param>
        private async Task CheckMetricResults(TestCaseData testCase, ILocator metricsResultHeader)
        {
            var metricResultsContainer = this.Page.GetByTestId("metric-result-container");

            await Iterate(metricResultsContainer, testCase.ExpectedResults[0].Metrics, "metric-result-entry", async (metric, locator) =>
            {
                await Expect(locator.GetByTestId("metric-name")).ToHaveTextAsync(metric.Name);
                await Expect(locator.GetByTestId("metric-type")).ToHaveTextAsync(metric.MetricType.ToString());
                await Expect(locator.GetByTestId("metric-description")).ToHaveTextAsync(metric.Description);

                await Iterate(locator, metric.MetricPoints, "metric-point", async (point, locator) =>
                {
                    await Expect(locator.GetByTestId("metric-point-value")).ToHaveTextAsync(point.Value.ToString()!);
                    await Expect(locator.GetByTestId("metric-point-unit")).ToHaveTextAsync(metric.Unit.ToString()!);

                    await this.Iterate(locator, point.Tags, "attribute", async (tag, locator) =>
                    {
                        await Expect(locator.GetByTestId("attribute-key")).ToHaveTextAsync(tag.Key);
                        await Expect(locator.GetByTestId("attribute-value")).ToHaveTextAsync(tag.Value?.ToString() ?? string.Empty);
                    });
                });
            });
        }
    }
}