using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// This attribute should be set to properties with a class hierarchy, that will inhert its value from 
    /// the parent, if not explicitly set.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class InheritedPropertyAttribute : Attribute 
    {
        /// <summary>
        /// Gets or sets the name that should be used for identifying other matching properties in the hierarchy.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Initializeas a new instance of the <see cref="InheritedPropertyAttribute"/> class.
        /// </summary>
        /// <param name="name">The name that should be used for identifying other matching properties in the hierarchy.</param>
        public InheritedPropertyAttribute(string? name = null) 
        {
            this.Name = name;
        }
    }
}
