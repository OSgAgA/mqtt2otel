using mqtt2otel.Stores;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel
{
    /// <summary>
    /// Containes all data stores used in the application. A data store is a way to store data that will be needed by 
    /// other parts of the application at another point in time. The primary application for stores is the communication
    /// between the mqtt coordinator and the otel coordinator. 
    /// </summary>
    public class DataStores
    {
        /// <summary>
        /// Gets or sets the signal store. The signal store contains metrics (signals) created from a mqtt subscription,
        /// that will later be used by the otel coordinator to send them to an otel endpoint.
        /// </summary>
        public SignalStore SignalStore { get; private set; }

        /// <summary>
        /// Gets or sets the logger store. The logger store contains all the loggers created by the otel coordinator. These
        /// loggers will be used by the mqtt coordinator for logging messages based on recevied subscription messages.
        /// </summary>
        public LoggerStore LoggerStore { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataStores"/> class.
        /// </summary>
        /// <param name="signalStore">The signal store.</param>
        /// <param name="loggerStore">The logger store.</param>
        public DataStores(SignalStore signalStore, LoggerStore loggerStore)
        {
            SignalStore = signalStore;
            LoggerStore = loggerStore;
        }
    }
}
