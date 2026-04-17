using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;
using mqtt2otel.Interfaces;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Describes an endpoint used to connect to the <see cref="IMqttCoordinator"/> or the <see cref="IOtelCoordinator"/>.
    /// </summary>
    public class Endpoint
    {
        /// <summary>
        /// Gets or sets the address of the endpoint without a protocoll or a port.
        /// </summary>
        public string? Address { get; set; } = null;

        /// <summary>
        /// Gets or sets the port of the endpoint.
        /// </summary>
        public int Port { get; set; } = 0;

        /// <summary>
        /// Gets or sets the protocoll used by the endpoint. Can include or ommit the trailing "://".
        /// </summary>
        public string Protocol { get; set; } = string.Empty;

        /// <summary>
        /// Gets the full addres consisting of the protocoll, the adress and the port of the endpoint.
        /// </summary>
        public string FullAddress
        {
            get
            {
                string protocol = this.Protocol;

                if (protocol.Length > 2 && protocol.EndsWith("://")) protocol = protocol.Remove(protocol.Length - 3, 3);

                string address = protocol + "://" + this.Address + ":" + this.Port;

                return address;
            }
        }

        /// <summary>
        /// Gets the <see cref="FullAddress"/> as an <see cref="Uri"/>.
        /// </summary>
        public Uri Uri
        {
            get => new Uri(this.FullAddress);
        }

        /// <summary>
        /// Validates the data of the endpoint.
        /// </summary>
        /// <param name="id">The id of the container, that holds the endpoint.</param>
        /// <param name="result">The validation result.</param>
        public void Validate(string id, ValidationResult result)
        {
            if (this.Port <= 0) result.AddError($"Provided {id} endpoint port ({this.Port}) needs to be > 0.");

            if (string.IsNullOrWhiteSpace(this.Address))
            {
                result.AddError($"{id} endpoint Address is empty, but must be set.");
            }
            else
            {
                Uri? test;
                UriCreationOptions options = new();
                if (!Uri.TryCreate(this.FullAddress, in options, out test)) result.AddError($"Provided {id} endpoint address ({this.FullAddress}) is not a valid URI.");
            }
        }
    }
}
