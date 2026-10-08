using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace mqtt2otel.ManifestExplorer.Tests.Helper
{
    /// <summary>
    /// Represents a single variant used for a set of browser test cases. 
    /// </summary>
    /// <param name="settings">The settings of the current variant.</param>
    /// <param name="value">The value under test.</param>
    public class BrowserTestCaseVariant<T>( UITestSettings settings, T value)
    {
        /// <summary>
        /// The default resolutions, that will be used, if no explicit resolutions are provided.
        /// </summary>
        private static readonly List<Tuple<int, int>> defaultResolutions = new List<Tuple<int, int>>() { new Tuple<int, int>(1600, 1200), new Tuple<int, int>(800, 600) };

        /// <summary>
        /// Gets or sets the settings of the current variant.
        /// </summary>
        public UITestSettings Settings { get; set; } = settings;

        /// <summary>
        /// Gets or sets the value under test.
        /// </summary>
        public T Value { get; set; } = value;

        /// <summary>
        /// Creates all variants for a singe value under test.
        /// </summary>
        /// <param name="value">The value under test.</param>
        /// <param name="resolutions">The screen resolution variants.</param>
        /// <returns>All test variants.</returns>
        public static IEnumerable<BrowserTestCaseVariant<T>> CreateAllVariants(T value, List<Tuple<int, int>>? resolutions = null)
        {
            if (resolutions == null) resolutions = defaultResolutions;

            foreach (var browser in Enum.GetValues<BrowserEngine>())
            {
                foreach (var resolution in resolutions)
                {
                    var settings = new UITestSettings()
                    {
                        Browser = browser,
                        ScreenWidth = resolution.Item1,
                        ScreenHeight = resolution.Item2,
                    };
                 
                    yield return new BrowserTestCaseVariant<T>(settings, value);
                }
            }
        }

        /// <summary>
        /// Creates all variants for a singe value under test.
        /// </summary>
        /// <param name="value">The values under test.</param>
        /// <param name="resolutions">The screen resolution variants.</param>
        /// <returns>All test variants.</returns>
        public static IEnumerable<object[]> CreateAllVariantsFromList(IEnumerable<T> values, List<Tuple<int, int>>? resolutions = null)
        {
            foreach (var value in values)
            {
                foreach (var variant in CreateAllVariants(value, resolutions))
                {
                    yield return new object[] { variant };
                }
            }
        }
    }
}
