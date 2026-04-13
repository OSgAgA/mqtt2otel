using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel
{
    /// <summary>
    /// Represents an object that can be identified via a key property.
    /// </summary>
    public interface IKeyObject
    {
        /// <summary>
        /// Gets the key of the object.
        /// </summary>
        public string Key { get; }
    }
}
