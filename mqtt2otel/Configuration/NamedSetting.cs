using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Configuration
{
    /// <summary>
    /// Represents settings that have an id, a name and a descriptin. Mainly used as a base class for other settings.
    /// </summary>
    public class NamedSetting
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
    }
}
