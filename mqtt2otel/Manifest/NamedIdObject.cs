using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents objects that have an id, a name and a descriptin. Mainly used as a base class for other objects.
    /// </summary>
    public class NamedIdObject
    {
        /// <summary>
        /// A regular expression that checks whether a character is invalid for a otel collector name.
        /// </summary>
        private static readonly Regex InvalidCharsForSlug = new("[^a-z0-9_-]+", RegexOptions.Compiled);

        /// <summary>
        /// The backing field of the <see cref="Slug"/> property.
        /// </summary>
        private string slug = string.Empty;

        /// <summary>
        /// The backing field of the <see cref="Name"/> property.
        /// </summary>
        private string name = string.Empty;

        /// <summary>
        /// Gets a reprentation of the connection name, that is safe to be used inside an otel-collector
        /// or as an activity source name.
        /// </summary>
        public string Slug
        {
            get => this.slug;
        }

        /// <summary>
        /// Gets or sets the unique id of this setting. This is mainly used internally.
        /// </summary>
        public Guid InternalId { get; protected set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the human readable name of the setting.
        /// </summary>
        public string Name 
        {
            get => this.name;
            set
            {
                this.name = value;
                this.slug = this.CreateSlug(value);
            }
        }

        /// <summary>
        /// Gets or sets an (optional) description of the setting item.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a path to a yaml file from which a list of objects of the derived type can be created.
        /// If this is set, all other parameters are ignored.
        /// 
        /// The path may contain wildcard characters:
        /// 
        ///   - '*' matches any number of characters
        ///   - '$' matches exactly one character.
        /// 
        /// Set to null if nothing should be imported.
        /// </summary>
        public string? ImportFrom { get; set; } = null;

        /// <summary>
        /// Converts a string into a valid component name.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="maxLength">Maximum amount of allowed characters.</param>
        /// <returns>The created component name.</returns>
        private string CreateSlug(string input, int maxLength = 64)
        {
            // 1. normalize: lower case, replace every run of invalid characters with a single underscore
            var slug = InvalidCharsForSlug.Replace(input.Trim().ToLowerInvariant(), "_").Trim('_', '-');
            if (slug.Length == 0) slug = "x";

            // 2. add a short hash of the ORIGINAL string, so different inputs never collapse into the same name
            var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(input)))[..8];

            // 3. cut the slug so slug + "_" + hash fits
            var max = maxLength - hash.Length - 1;
            if (slug.Length > max) slug = slug[..max].TrimEnd('_', '-');

            return $"{slug}_{hash}";
        }
    }
}
