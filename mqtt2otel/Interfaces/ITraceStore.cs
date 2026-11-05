using mqtt2otel.Manifest;
using mqtt2otel.Stores;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Interfaces
{
    /// <summary>
    /// Must be implemented by all classes, that are able to store trace informations.
    /// </summary>
    public interface ITraceStore
    {
        /// <summary>
        /// Gets the span contexted with the provided internal id. If none is found a new context is created and stored.
        /// 
        /// This must be thread safe!
        /// </summary>
        /// <param name="id">The internal span id.</param>
        /// <returns>The span context.</returns>
        public OtelSpanContext GetOrCreateSpanContext(string id);

        /// <summary>
        /// Gets the span contexted with the provided internal id. If none is found a new context is created and stored.
        /// Explicitly set the retention time, retention strategy and retention exceeded event.
        /// 
        /// This must be thread safe!
        /// </summary>
        /// <param name="id">The internal span id.</param>
        /// <param name="retentionTimeInSeconds">The retention timeout in seconds.</param>
        /// <returns>The span context.</returns>

        public OtelSpanContext GetOrCreateSpanContext(string id, int retentionTimeInSeconds);

        /// <summary>
        /// Gets the span with the given id from the store. If none is existing a new one is created and stored.
        /// 
        /// This must be thread safe!
        /// </summary>
        /// <param name="id">The span id.</param>
        /// <returns>The span.</returns>
        public OtelSpan GetOrCreateSpan(string id);

        /// <summary>
        /// Stores a new span object.
        /// </summary>
        /// <param name="span">The span that should be stored.</param>
        public void StoreSpan(OtelSpan span, bool updateExisting = false);

        /// <summary>
        /// Adds an event to a span. If the span is not existing it is created with empty settings.
        /// </summary>
        /// <param name="evt">The event that should be added.</param>
        public void AddEvent(OtelSpanEvent evt);

        /// <summary>
        /// Clears the store. All stored values are deleted.
        /// </summary>
        public void Clear();

        /// <summary>
        /// Gets all spans, where the retention period has beed exceeded.
        /// </summary>
        public List<OtelSpan> GetRetentionExceededSpans();

        /// <summary>
        /// Removes the provided spans from the store.
        /// </summary>
        /// <param name="spans">The spans that should be removed.</param>
        public void RemoveSpans(List<OtelSpan> spans);

        /// <summary>
        /// Gets all completed spans or spans that exceeded their retention timout from the store.
        /// </summary>
        /// <returns>All available completed spans.</returns>
        public List<OtelSpan> GetCompletedOrTimeOutSpans();

        /// <summary>
        /// Removes all span contexts from the list, that have timed out.
        /// </summary>
        public void ClearTimedOutSpanContexts();

        /// <summary>
        /// Gets the full internal state of the application. This is meant for testing only and should not be used in production code.
        /// </summary>
        /// <returns>All available spans.</returns>
        public InternalState GetFullState();

        /// <summary>
        /// Initializes the store with the provided initial state.
        /// </summary>
        /// <param name="state">The state, that should be written into the store.</param>
        public void Initialize(InternalState state);
    }
}
