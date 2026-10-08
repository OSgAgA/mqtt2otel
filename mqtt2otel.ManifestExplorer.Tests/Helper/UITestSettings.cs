using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.ManifestExplorer.Tests.Helper
{
    /// <summary>
    /// Represents the settings used for creating playwright ui tests.
    /// </summary>
    public class UITestSettings
    {
        /// <summary>
        /// Gets or sets a value that defines, when playwright trace output should be created.
        /// </summary>
        public ActionTrigger CreateTraceOutput { get; set; } = ActionTrigger.OnFailure;

        /// <summary>
        /// Gets or sets a value that defines, when playwright trace output should be created.
        /// </summary>
        public ActionTrigger CreateVideoOutput { get; set; } = ActionTrigger.Never;

        /// <summary>
        /// Gets or sets the screen width and viewport width used for recording videos and traces.
        /// </summary>
        public int ScreenWidth { get; set; } = 1600;

        /// <summary>
        /// Gets or sets the screedn and viewport height used for recording videos and traces.
        /// </summary>
        public int ScreenHeight { get; set; } = 1200;

        /// <summary>
        /// Gets or sets a value that slows down test execution by the given amount of milliseconds per step.
        /// 
        /// Leave at null, to not slow down execution.
        /// </summary>
        public float? SloMo { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the test should be executed without opening a browser window (=headless).
        /// </summary>
        public bool Headless { get; set; } = true;

        /// <summary>
        /// Gets or sets the directory for storing video recordings.
        /// </summary>
        public string VideoDir { get; set; } = Path.Combine("playwright", "videos");

        /// <summary>
        /// Gets or sets the directory for storing trace recordings.
        /// </summary>
        public string TraceDir { get; set; } = Path.Combine("playwright", "traces");

        /// <summary>
        /// Gets or sets the browser engine, that will be used for test execution.
        /// </summary>
        public BrowserEngine Browser { get; set; } = BrowserEngine.Chromium;

        /// <summary>
        /// Gets or sets a value indicating, wether videos should be recoreded while executing the test.
        /// </summary>
        public ActionTrigger RecordVideos { get; set; } = ActionTrigger.Never;

        /// <summary>
        /// Gets or sets a value indicating, wether traces should be recoreded while executing the test.
        /// </summary>

        public ActionTrigger RecordTraces { get; set; } = ActionTrigger.OnFailure;
    }
}
