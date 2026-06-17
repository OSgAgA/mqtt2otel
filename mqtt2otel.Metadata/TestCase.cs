using mqtt2otel.ManifestExplorer.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Metadata
{
    public class TestCase
    {
        public ExampleData Setup { get; set; } = new();

        public ExpectedTestResult ExpectedResult { get; set; } = new();
    }
}
