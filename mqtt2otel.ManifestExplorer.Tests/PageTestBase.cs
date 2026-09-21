using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using mqtt2otel.Shared;
using NCalc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace mqtt2otel.ManifestExplorer.Tests
{
    /// <summary>
    /// This is the base class, that should be used for all playwright page tests. The responsibility of this class is to
    /// 
    ///     * Make the mqtt2otel manifest explorer available for testing
    ///     * Setup playwright with the configured values.
    ///     * Cleanup after each test and ensure that all objects are disposed correctly
    ///     
    /// </summary>
    public class PageTestBase : PageTest, IAsyncLifetime
    {
        /// <summary>
        /// The factory used for creating an instance of the manifest explorer server.
        /// </summary>
        private readonly ManifestExplorerFactory _factory;

        /// <summary>
        /// The server address of the manifest explorer.
        /// </summary>
        public string ServerAddress { get; private set; }

        /// <summary>
        /// Gets or sets a value that defines, when playwright trace output should be created.
        /// </summary>
        public ActionTrigger CreateTraceOutput { get; set; } = ActionTrigger.OnFailure;

        /// <summary>
        /// Gets or sets a value that defines, when playwright trace output should be created.
        /// </summary>
        public ActionTrigger CreateVideoOutput { get; set; } = ActionTrigger.Never;

        /// <summary>
        /// Initializes a new instance of the <see cref="PageTestBase"/> class.
        /// </summary>
        /// <param name="factory">The server factory.</param>
        public PageTestBase(ManifestExplorerFactory factory)
        {
            _factory = factory;
            ServerAddress = factory.ServerAddress;
        }

        /// <summary>
        /// Called before each test. Creates the <see cref="TestPage"/> object including all contexts and browsers.
        /// </summary>
        public async override ValueTask InitializeAsync()
        {
            await base.InitializeAsync();

            if (this.CreateTraceOutput != ActionTrigger.Never)
            {
                await this.Context.Tracing.StartAsync(new()
                {
                    Screenshots = true,
                    Snapshots = true,
                    Sources = true,
                });
            }
        }

        
        /// <summary>
        /// Provides the browser context options, setting e.g. the screen and viewport size. Called before each test.
        /// </summary>
        /// <returns>The created context options.</returns>
        public override BrowserNewContextOptions ContextOptions()
        {
            int width = 2500;
            int height = 1300;

            var options = new BrowserNewContextOptions()
            {

                ScreenSize = new ScreenSize() { Width = width, Height = height },
                ViewportSize = new ViewportSize() { Width = width, Height = height },
            };

            if (this.CreateVideoOutput != ActionTrigger.Never)
            {
                options.RecordVideoDir = Path.Combine("playwright", "videos");
                options.RecordVideoSize = new RecordVideoSize() { Width = width, Height = height };
            }

            return options;
        }

        /// <summary>
        /// Provides the browser launch options. Called before each test.
        /// </summary>
        /// <returns>The created options.</returns>
        public override Task<BrowserTypeLaunchOptions?> LaunchOptionsAsync()
        {
            return Task.FromResult<BrowserTypeLaunchOptions?>(new BrowserTypeLaunchOptions()
            {
                Headless = true,
            });
        }


        /// <summary>
        /// Disposes all objects created by the base class after each test.
        /// 
        /// Creates videos and trace outputs.
        /// </summary>
        public async override ValueTask DisposeAsync()
        {
            try
            {
                var displayName = TestContext.Current.TestCase?.TestCaseDisplayName ?? "NoMethod()";

                var split = displayName.Split("(");
                if (split.Length == 2)
                {
                    displayName = (TestContext.Current.TestCase?.TestMethodName ?? "NoMethod") + "(" + split[1];
                }

                displayName = displayName.Replace("\"", "");
                displayName = displayName.Replace("(", "( ");
                displayName = displayName.Replace(")", " )");
                displayName = displayName.Replace(": ", "=");
                displayName = displayName.Replace(":", "=");

                var failed = TestContext.Current.TestState?.Result == Xunit.TestResult.Failed;

                string videoPath = this.Page.Video != null ? await this.Page.Video!.PathAsync() : string.Empty;

                var videoPathDirectory = Path.GetDirectoryName(videoPath) ?? string.Empty;
                var videoFilename = $"{DateTime.UtcNow:yyyy-MM-ddTHHmmssZ} {displayName}{Path.GetExtension(videoPath)}";
                var traceFilename = $"{DateTime.UtcNow:yyyy-MM-ddTHHmmssZ} {displayName}.zip";

                if (this.TestActionTrigger(failed, this.CreateTraceOutput))
                {
                    await Context.Tracing.StopAsync(new()
                    {
                        Path = Path.Combine("playwright", "traces", traceFilename)
                    });
                }
                else
                {
                    await Context.Tracing.StopAsync();
                }

                await this.Context.CloseAsync();

                if (this.TestActionTrigger(failed, this.CreateVideoOutput))
                {

                    File.Move(videoPath, Path.Combine(videoPathDirectory, videoFilename));
                }
                else
                {
                    if (File.Exists(videoPath)) File.Delete(videoPath);
                }
            }
            finally
            {
                await base.DisposeAsync();
            }
        }

        /// <summary>
        /// Tests, whether the manifest editor contains a given expectation string.
        /// 
        /// This is done in a way, that playwright creates traces for this step.
        /// </summary>
        /// <param name="expectation"></param>
        /// <returns></returns>
        public async Task ExpectManifestEditor(string expectation)
        {
            await this.Context.Tracing.GroupAsync("Test content of manifest editor)");

            await Expect(this.Page!.Locator(".monaco-editor")).ToBeVisibleAsync();
            await this.Page.WaitForFunctionAsync("() => monaco?.editor?.getModels()?.length > 0");

            // Inject hidden dump element
            await this.Page!.EvaluateAsync(@"() => {
                const container = document.querySelector('#manifest-editor-container');

                let div = document.createElement('div');
                div.id = 'monaco-text-dump';
                div.style.display = 'block';

                container.insertBefore(div, container.children[1]);
            }");

            // Dump Monaco text into DOM
            await this.Page.EvaluateAsync(@"() => {
                const text = monaco.editor.getModels()[0].getValue();
                document.querySelector('#monaco-text-dump').innerText = text.replace('\n', '');
            }");

            // Removes \n from expectation, as sometimes the plain text is sometimes delivered without \n from 
            // monacco control.
            var cleaned = expectation.Replace("\n", "");

            // Expectation that shows up in traces
            await Expect(this.Page.Locator("#monaco-text-dump"))
                .ToContainTextAsync(cleaned);

            // Inject hidden dump element
            await this.Page.EvaluateAsync(@"() => {
                document.getElementById('monaco-text-dump').remove();
            }");

            await this.Context.Tracing.GroupEndAsync();
        }

        /// <summary>
        /// Iterates through items and calls the execute function on each of them.
        /// </summary>
        /// <typeparam name="T">The type of an item.</typeparam>
        /// <param name="parent">The locator of the parent, that contains the other locators.</param>
        /// <param name="items">The items to iterate through</param>
        /// <param name="itemTestId">The testid that is set on each item inside the locator.</param>
        /// <param name="execute">The function that is called, with the expected item and the locator representing this item.</param>
        public async Task Iterate<T>(ILocator parent, IEnumerable<T> items, string itemTestId, Func<T, ILocator, Task> execute)
        {
            int index = 0;
            foreach (var item in items)
            {
                var itemLocator = parent.GetByTestId(itemTestId).Nth(index++);
                await execute(item, itemLocator);
            }
        }

        /// <summary>
        /// Tests whether a link is reachable (sending repsonse in [200,300). Uses maxAttempts retries. 
        /// </summary>
        /// <param name="url">The url under test.</param>
        /// <param name="maxAttempts">The maximum number of attempts.</param>
        /// <returns></returns>
        public async Task<bool> IsLinkReachable(string url, int maxAttempts = 3)
        {
            await this.Context.Tracing.GroupAsync($"Test reachability of: {url}");

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var response = await this.Page.Context.APIRequest.GetAsync(url, new()
                    {
                        MaxRedirects = 10,
                        Timeout = 5000,
                        IgnoreHTTPSErrors = true
                    });


                    await this.Context.Tracing.GroupEndAsync();
                    return response.Ok;
                }
                catch (PlaywrightException) when (attempt < maxAttempts)
                {
                    await Task.Delay(1000 * attempt);
                }
            }

            await this.Context.Tracing.GroupEndAsync();

            return false;
        }

        /// <summary>
        /// Tests whethe an action should be executed based on an action trigger.
        /// </summary>
        /// <param name="testFailed">A value indicating, whether the current test has failed.</param>
        /// <param name="trigger">The trigger to be tested against.</param>
        /// <returns>True, if the action should be executed, false otherwise.</returns>
        private bool TestActionTrigger(bool testFailed, ActionTrigger trigger)
        {
            bool result =
                (
                    trigger == ActionTrigger.Always ||
                    (trigger == ActionTrigger.OnFailure && testFailed) ||
                    (trigger == ActionTrigger.OnSuccess && !testFailed)
                );

            return result;
        }
    }
}