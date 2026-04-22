using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents objects that have an id, a name and a descriptin. Mainly used as a base class for other objects.
    /// </summary>
    public class NamedIdObject
    {
        /// <summary>
        /// Gets or sets the unique id of this setting. This is mainly used internally.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the human readable name of the setting.
        /// </summary>
        public string Name { get; set; } = Guid.NewGuid().ToString();

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
    }
}
