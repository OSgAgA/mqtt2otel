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
        /// the current page object, that can be used by the tests.
        /// </summary>
        private IPage? testPage = null;

        /// <summary>
        /// The factory used for creating an instance of the manifest explorer server.
        /// </summary>
        private readonly ManifestExplorerFactory _factory;

        /// <summary>
        /// This context is used by the playwright browser, e.g. to create traces.
        /// </summary>
        private IBrowserContext? browserContext;

        /// <summary>
        /// The browser object used for testing.
        /// </summary>
        private IBrowser? browser;

        /// <summary>
        /// The playwright driver that is created for each test.
        /// </summary>
        private IPlaywright playwrightDriver;

        /// <summary>
        /// The server address of the manifest explorer.
        /// </summary>
        public string ServerAddress { get; private set; }

        /// <summary>
        /// Gets or sets the test display name. This is used to create filenames for videos and traces.
        /// </summary>
        public string TestDisplayName { get; set; } = "Unknown test";

        /// <summary>
        /// Gets or sets a value that defines, when playwright trace output should be created.
        /// </summary>
        public ActionTrigger CreateTraceOutput { get; set; } = ActionTrigger.OnFailure;

        /// <summary>
        /// Gets or sets a value that defines, when playwright trace output should be created.
        /// </summary>
        public ActionTrigger CreateVideoOutput { get; set; } = ActionTrigger.Never;

        /// <summary>
        /// Gets the current page object, that can be used by the tests.
        /// </summary>
        public IPage TestPage
        {
            get
            {
                if (this.testPage == null) throw new Exception("Internal error: Property TestPage is not set.");

                return this.testPage;
            }
        }

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
            this.playwrightDriver = await Microsoft.Playwright.Playwright.CreateAsync();
            var launchOptions = new BrowserTypeLaunchOptions()
            {
                Headless = true,
            };

            this.browser = await this.playwrightDriver.Chromium.LaunchAsync(launchOptions);

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

            this.browserContext = await this.browser.NewContextAsync(options);

            await browserContext.Tracing.StartAsync(new()
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true,
            });

            this.testPage = await this.browserContext.NewPageAsync();
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
                if (this.browserContext == null || this.TestPage == null) return;

                var failed = TestContext.Current.TestState?.Result == Xunit.TestResult.Failed;

                string videoPath = this.TestPage.Video != null ? await this.TestPage.Video!.PathAsync() : string.Empty;

                var videoPathDirectory = Path.GetDirectoryName(videoPath) ?? string.Empty;
                var videoFilename = $"{DateTime.UtcNow:yyyy-MM-dd HHmmss} {this.TestDisplayName}{Path.GetExtension(videoPath)}";
                var traceFilename = $"{DateTime.UtcNow:yyyy-MM-dd HHmmss} Playwright {this.TestDisplayName}.zip";

                if (this.TestActionTrigger(failed, this.CreateTraceOutput))
                {
                    await browserContext.Tracing.StopAsync(new()
                    {
                        Path = Path.Combine("playwright", "traces", traceFilename)
                    });
                }

                await this.browserContext.CloseAsync();

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
                if (this.browser != null)
                    await browser.CloseAsync();

                this.playwrightDriver?.Dispose();
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
            await this.StartTraceGroupAsync("Test content of manifest editor)");

            await Expect(this.TestPage!.Locator(".monaco-editor")).ToBeVisibleAsync();
            await this.TestPage.WaitForFunctionAsync("() => monaco?.editor?.getModels()?.length > 0");

            // Inject hidden dump element
            await this.TestPage!.EvaluateAsync(@"() => {
                const container = document.querySelector('#manifest-editor-container');

                let div = document.createElement('div');
                div.id = 'monaco-text-dump';
                div.style.display = 'block';

                container.insertBefore(div, container.children[1]);
            }");

            // Dump Monaco text into DOM
            await this.TestPage.EvaluateAsync(@"() => {
                const text = monaco.editor.getModels()[0].getValue();
                document.querySelector('#monaco-text-dump').innerText = text.replace('\n', '');
            }");

            // Removes \n from expectation, as sometimes the plain text is sometimes delivered without \n from 
            // monacco control.
            var cleaned = expectation.Replace("\n", "");

            // Expectation that shows up in traces
            await Expect(this.TestPage.Locator("#monaco-text-dump"))
                .ToContainTextAsync(cleaned);

            // Inject hidden dump element
            await this.TestPage.EvaluateAsync(@"() => {
                document.getElementById('monaco-text-dump').remove();
            }");

            await this.EndTraceGroupAsync();
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
        /// Starts a new tracing group.
        /// </summary>
        /// <param name="title">The title of the tracing group.</param>
        /// <exception cref="Exception">Thrown if <see cref="browserContext"/> is not set.</exception>
        public async Task StartTraceGroupAsync(string title)
        {
            if (this.browserContext == null) throw new Exception("Cannot create a tracing group on null context.");

            await this.browserContext.Tracing.GroupAsync(title);
        }

        /// <summary>
        /// Ends the current active tracing group.
        /// </summary>
        /// <exception cref="Exception">Thrown if <see cref="browserContext"/> is not set.</exception>
        public async Task EndTraceGroupAsync()
        {
            if (this.browserContext == null) throw new Exception("Cannot end a tracing group on null context.");

            await this.browserContext.Tracing.GroupEndAsync();
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