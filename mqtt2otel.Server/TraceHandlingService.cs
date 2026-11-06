using mqtt2otel.Interfaces;

namespace mqtt2otel.Server
{
    /// <summary>
    /// This service handles the creation and cancelation of traces/spans.
    /// </summary>
    /// <param name="coordinator">The otel coordinator to create new spans.</param>
    /// <param name="traceStore">The trace store to identify the current trace state.</param>
    public class TraceHandlingService(IOtelCoordinator coordinator, ITraceStore traceStore) : IHostedService
    {
        /// <summary>
        /// The otel coordinator to create new spans.
        /// </summary>
        private IOtelCoordinator coordinator = coordinator;

        /// <summary>
        /// The trace store to identify the current trace state.
        /// </summary>
        private ITraceStore traceStore = traceStore;

        /// <summary>
        /// A value indicating whether the service is currently active.
        /// </summary>
        private bool isActive = true;

        /// <summary>
        /// Starts the service. Runs a periodic timer to check the current trace state.
        /// </summary>
        /// <param name="cancellationToken">The cancelation token to cancel the service.</param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.isActive = true;

            using PeriodicTimer periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

            while(isActive && !cancellationToken.IsCancellationRequested)
            {

                var completedSpans = this.traceStore.GetCompletedOrTimeOutSpans();

                if (completedSpans.Count > 0)
                {
                    this.coordinator.CreateSpans(completedSpans);
                    this.traceStore.RemoveSpans(completedSpans);
                }

                this.traceStore.ClearTimedOutSpanContexts();

                await periodicTimer.WaitForNextTickAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Stops the service. 
        /// </summary>
        /// <param name="cancellationToken">The cancelation token to cancel the service.</param>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            this.isActive = false;
        }
    }
}
