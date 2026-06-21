using mqtt2otel.ManifestExplorer.DTOs;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;

namespace mqtt2otel.Metadata
{
    /// <summary>
    /// Represents a test case, including the needed setup and the expected outcome.
    /// </summary>
    public class TestCase
    {
        /// <summary>
        /// Gets or sets the setup , that is needed to execute the test successfully.
        /// </summary>
        public ExampleData Setup { get; set; } = new();

        /// <summary>
        /// Gets or sets the expected test result.
        /// </summary>
        public ExpectedTestResult ExpectedResult { get; set; } = new();

        /// <summary>
        /// Loads all tests as member data that can be used in a unit test.
        /// </summary>
        /// <returns>All available test cases.</returns>
        /// <exception cref="Exception">Thrown if directory with json files describing the test cases is not found.</exception>
        public static IEnumerable<object[]> LoadAllAsMemberdata()
        {
            foreach (var testCase in TestCase.LoadAll())
            {
                yield return new object[] { testCase };
            }
        }

        /// <summary>
        /// Loads all test cases.
        /// </summary>
        /// <returns>All available test cases.</returns>
        /// <exception cref="Exception">Thrown if directory with json files describing the test cases is not found.</exception>
        public static List<TestCase> LoadAll()
        {
            var results = new List<TestCase>();

            var directories = Directory.GetDirectories("./", "TestCases", SearchOption.AllDirectories);

            if (directories?.Length == 1)
            {
                var dir = directories[0];

                foreach (var file in Directory.GetFiles(dir, "*.json"))
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var result = JsonSerializer.Deserialize<TestCase>(json);

                        if (result != null)
                        {
                            results.Add(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Could not parse json file {Path.Combine(dir, file)}. See inner exception for details.", ex);
                    }
                }
            }
            else
            {
                throw new Exception($"Searching for TestCase directory returned {directories?.Length} results. Exactly one result has been expected.");
            }

            return results;
        }

        /// <summary>
        /// Loads all test cases as a dictionary.
        /// </summary>
        /// <returns>All available test cases.</returns>
        /// <exception cref="Exception">Thrown if directory with json files describing the test cases is not found.</exception>
        public static Dictionary<string, ExampleData> LoadAllExamples()
        {
            Dictionary<string, ExampleData> result = new Dictionary<string, ExampleData>();

            foreach (var testCase in TestCase.LoadAll())
            {
                result[testCase.Setup.Id] = testCase.Setup;
            }

            return result;
        }
    }
}