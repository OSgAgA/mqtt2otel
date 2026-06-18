using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Core;

namespace mqtt2otel.Metadata
{
    /// <summary>
    /// Represents an error inside a test expectation.
    /// </summary>
    public class ErrorTestData
    {
        /// <summary>
        /// Gets or sets the expected error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the error contains coordinates that will locate the error 
        /// inside a manifest file.
        /// </summary>
        public bool HasCoordinates
        {
            get => this.StartPosition != null && this.EndPosition != null;
        }

        /// <summary>
        /// Gets or sets the start position of the part of the manifest file that caused the error.
        /// </summary>
        public Position? StartPosition { get; set; } = null;

        /// <summary>
        /// Gets or sets the end position of the part of the manifest file that caused the error.
        /// </summary>

        public Position? EndPosition { get; set; } = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorTestData"/> class.
        /// 
        /// This constructor is for serialization purposes only and should not be used directly.
        /// </summary>
        public ErrorTestData() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorTestData"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="startPosition">The start position inside a manifest file.</param>
        /// <param name="endPosition">The end position inside a manifest file.</param>
        public ErrorTestData(string message, Position startPosition, Position endPosition)
        {
            Message = message;
            StartPosition = startPosition;
            EndPosition = endPosition;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorTestData"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ErrorTestData(string message)
        {
            Message = message;
        }

        /// <inheritdoc/>
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
