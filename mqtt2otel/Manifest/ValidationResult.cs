using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Provides information about the validation of objects.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// A list of error found during validation.
        /// </summary>
        private List<string> errors = new();

        /// <summary>
        /// Gets or sets a value indicating wheter all validations have been successful.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Gets all errors found during validation.
        /// </summary>
        public IEnumerable<string> Errors { get => this.errors; }

        /// <summary>
        /// Adds an error the result list.
        /// </summary>
        /// <param name="error">The error message describing the issue.</param>
        /// <returns>This validation result.</returns>
        public ValidationResult AddError(string error)
        {
            this.errors.Add(error);
            this.Success = false;

            return this;
        }

        /// <summary>
        /// Creates log messages for the validation results.
        /// </summary>
        /// <param name="internalLogger">The internal logger used for logging the results.</param>
        public void LogOutput(ILogger internalLogger)
        {
            if (this.Success)
            {
                internalLogger.LogInformation("Validation of Manifest.yaml successful.");
            }
            else
            {
                this.errors.ForEach(error => internalLogger.LogError(error));
            }
        }
    }
}
