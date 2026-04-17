namespace mqtt2otel.Interfaces
{
    /// <summary>
    /// Represents the main class for communicating with the mqtt broker.
    /// </summary>
    public interface IMqttCoordinator
    {
        /// <summary>
        /// Connects to the server and subscribes to all topics as defined in the provided settings.
        /// </summary>
        /// <param name="manifest">The manifest containing information about connection details and subscriptions.</param>
        /// <exception cref="Exception">Thrown if client is unable to connect to server or if manifest contains an error.</exception>
        Task ConnectAndSubscribe(Manifest.Manifest manifest);

        /// <summary>
        /// Disconnects all brokers.
        /// </summary>
        Task DisconnectAllBrokers();
    }
}