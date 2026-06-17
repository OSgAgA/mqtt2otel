using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Core;

namespace mqtt2otel.Metadata
{
    public class ErrorTestData
    {
        public string Message { get; set; } = string.Empty;

        public bool HasCoordinates
        {
            get => this.StartPosition != null && this.EndPosition != null;
        }

        public Position? StartPosition { get; set; } = null;

        public Position? EndPosition { get; set; } = null;

        public ErrorTestData() { }

        public ErrorTestData(string message, Position startPosition, Position endPosition)
        {
            Message = message;
            StartPosition = startPosition;
            EndPosition = endPosition;
        }

        public ErrorTestData(string message)
        {
            Message = message;
        }

        public override string ToString()
        {
            if (this.HasCoordinates)
            {
                return $"({this.StartPosition}) - ({this.EndPosition}): {this.Message}";
            }

            return this.Message;
        }
    }
}
