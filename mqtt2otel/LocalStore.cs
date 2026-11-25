using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.Manifest;
using mqtt2otel.Metadata;
using mqtt2otel.Stores;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using YamlDotNet.Core.Tokens;

namespace mqtt2otel
{
    public class LocalStore : ITraceStore, IDynamicSubscriptionStore
    {
        /// <summary>
        /// Contains all stored span contexts.
        /// </summary>
        private Dictionary<string, OtelSpanContext> spanContexts = new();

        /// <summary>
        /// Contains all stored spans.
        /// </summary>
        private Dictionary<string, OtelSpan> spans = new();

        /// <summary>
        /// Ensures threat safety when changing the spanContexts store.
        /// </summary>
        private readonly Lock contextGate = new();

        /// <summary>
        /// Ensures threat safety when changing the span store.
        /// </summary>
        private readonly Lock spanGate = new();

        /// <summary>
        /// The provider used for generating trace and span ids.
        /// </summary>
        private ITraceIdProvider traceIdProvider;

        /// <summary>
        /// Stores data for dynamic mqtt subscriptions needed for response topics. The key is the response topic name.
        /// </summary>
        private Dictionary<string, DynamicSubscriptionData> dynamicSubscriptions = new();

        /// <summary>
        /// This event will be called, when a dynamic subscription has been removed from the store.
        /// </summary>
        public event EventHandler<DynamicSubscriptionData>? OnSubscriptionRemoved;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalStore"/> class.
        /// </summary>
        /// <param name="traceIdProvider">The provider used for generating trace and span ids.</param>
        public LocalStore(ITraceIdProvider traceIdProvider)
        {
            this.traceIdProvider = traceIdProvider; 
        }

        /// <summary>
        /// Gets the span contexted with the provided internal id. If none is found a new context is created and stored.
        /// 
        /// This must be thread safe!
        /// </summary>
        /// <param name="id">The internal span id.</param>
        /// <returns>The span context.</returns>
        public OtelSpanContext GetOrCreateSpanContext(string id)
        {
            lock (contextGate)
            {
                return GetOrCreateExistingSpanContextInternal(id);
            }
        }

        /// <summary>
        /// Gets the span contexted with the provided internal id. If none is found a new context is created and stored.
        /// Explicitly set the retention time, retention strategy and retention exceeded event.
        /// 
        /// This must be thread safe!
        /// </summary>
        /// <param name="id">The internal span id.</param>
        /// <param name="retentionTimeInSeconds">The retention timeout in seconds.</param>
        /// <returns>The span context.</returns>

        public OtelSpanContext GetOrCreateSpanContext(string id, int retentionTimeInSeconds)
        {
            OtelSpanContext result;

            lock (contextGate)
            {
                result = GetOrCreateExistingSpanContextInternal(id);

                result.RetentionInSeconds = retentionTimeInSeconds;

                this.spanContexts[id] = result;

                return result;
            }
        }

        /// <summary>
        /// Gets the span with the given id from the store. If none is existing a new one is created and stored.
        /// 
        /// This must be thread safe!
        /// </summary>
        /// <param name="id">The span id.</param>
        /// <returns>The span.</returns>
        public OtelSpan GetOrCreateSpan(string id)
        {
            lock (spanGate)
            {
                if (this.spans.TryGetValue(id, out var value))
                {
                    return value;
                }

                var result = new OtelSpan();
                this.spans[id] = result;

                return result;
            }
        }

        /// <summary>
        /// Stores a new span object.
        /// </summary>
        /// <param name="span">The span that should be stored.</param>
        /// <param name="updateExisting">A value indicating whether an existing one should be updated.</param>
        /// <param name="increaseCountBy">The delta by which the end count should be increased.</param>
        public void StoreSpan(OtelSpan span, bool updateExisting = false, int increaseCountBy = 0)
        {
            lock (spanGate)
            {
                if (this.spans.TryGetValue(span.Id, out var existingSpan))
                {
                    existingSpan.CombineWith(span, updateExisting);
                    existingSpan.EndCounter += increaseCountBy;
                    this.spans[existingSpan.Id] = existingSpan;
                    span = existingSpan;
                }
                else
                {
                    span.EndCounter += increaseCountBy;
                    this.spans[span.Id] = span;
                }

                OtelSpanContext context;
                OtelSpanContext? parent = null;

                if (span.Parent != null && span.Parent != span.Id)
                {
                    parent = this.GetOrCreateExistingSpanContextInternal(span.Parent);
                }

                context = this.GetOrCreateExistingSpanContextInternal(span.Id);
                span.Context = context;

                if (parent != null)
                {
                    context.TraceId = parent.TraceId;
                    span.ParentContext = parent.ToActivityContext();
                }
                else
                {
                    span.Parent = null;
                }

                foreach (var link in span.LinkTo)
                {
                    var linkContext = this.GetOrCreateExistingSpanContextInternal(link.Target);
                    link.Context = linkContext;
                    if (span.RetentionInSeconds != null)
                    {
                        linkContext.RetentionInSeconds = span.RetentionInSeconds.Value;
                    }
                }

                foreach (var evt in span.Events)
                {
                    if (evt.SpanId != span.Id) evt.SpanId = span.Id;
                    this.GetOrCreateExistingSpanContextInternal(evt.SpanId);
                }
            }
        }

        /// <summary>
        /// Adds an event to a span. If the span is not existing it is created with empty settings.
        /// </summary>
        /// <param name="evt">The event that should be added.</param>
        public void AddEvent(OtelSpanEvent evt)
        {
            lock (spanGate)
            {
                if (!this.spans.TryGetValue(evt.SpanId, out var span))
                {
                    span = new OtelSpan();
                    span.Id = evt.SpanId;
                    this.spans[evt.SpanId] = span;
                    var context = this.GetOrCreateExistingSpanContextInternal(span.Id);
                    this.spanContexts[span.Id] = context;
                }

                span.Events.Add(evt);
            }
        }

        /// <summary>
        /// Clears the store. All stored values are deleted.
        /// </summary>
        public void Clear()
        {
            lock (spanGate)
            {
                this.spans.Clear();
                this.spanContexts.Clear();
            }
        }

        /// <summary>
        /// Gets all spans, where the retention period has beed exceeded.
        /// </summary>
        public List<OtelSpan> GetRetentionExceededSpans()
        {
            var result = new List<OtelSpan>();

            lock (spanGate)
            {
                foreach (var context in this.spanContexts)
                {
                    if (context.Value.IsExpired && this.spans.TryGetValue(context.Key, out var span))
                    {
                        result.Add(span);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Removes the provided spans from the store.
        /// </summary>
        /// <param name="spans">The spans that should be removed.</param>
        public void RemoveSpans(List<OtelSpan> spans)
        {
            foreach (var span in spans)
            {
                this.spans.Remove(span.Id);
                if (this.spanContexts.ContainsKey(span.Id)) this.spanContexts.Remove(span.Id);
            }
        }

        /// <summary>
        /// Gets all completed spans or spans that exceeded their retention timout from the store.
        /// </summary>
        /// <returns>All available completed spans.</returns>
        public List<OtelSpan> GetCompletedOrTimeOutSpans()
        {
            var result = new List<OtelSpan>();

            lock (spanGate)
            {
                foreach (var span in this.spans)
                {
                    if (span.Value.IsCompleted || span.Value.IsRetentionExceeded)
                    {
                        span.Value.ApplyDefaultValues();
                        span.Value.Context = this.GetOrCreateExistingSpanContextInternal(span.Value.Id);

                        foreach (var link in span.Value.LinkTo)
                        {
                            link.Context = this.GetOrCreateExistingSpanContextInternal(link.Target);
                        }
                        result.Add(span.Value);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Removes all span contexts from the list, that have timed out.
        /// </summary>
        public void ClearTimedOutSpanContexts()
        {
            lock (spanGate)
            {
                foreach (var context in this.spanContexts)
                {
                    if (context.Value.IsExpired) this.spanContexts.Remove(context.Key);
                }
            }
        }

        /// <summary>
        /// Gets the full internal state of the application. This is meant for testing only and should not be used in production code.
        /// </summary>
        /// <returns>All available spans.</returns>
        public InternalState GetFullState()
        {
            lock (spanGate)
            {
                var result = new InternalState(this.spans.Values.ToList(), this.spanContexts.Values.ToList(), this.dynamicSubscriptions.Values.ToList());
                result.TraceIdProviderState = this.traceIdProvider.GetState();

                return result;
            }
        }

        /// <summary>
        /// Initializes the store with the provided values.
        /// </summary>
        /// <param name="state">The state, that should be written into the store.</param>
        public void Initialize(InternalState state)
        {
            lock (spanGate)
            {
                foreach (var span in state.Spans)
                {
                    this.spans[span.Id] = span;
                }

                foreach (var context in state.SpanContexts)
                {
                    this.spanContexts[context.Id] = context;
                }

                foreach (var sub in state.DynamicSubscriptions)
                {
                    this.dynamicSubscriptions[sub.Topic] = sub;
                }
            }
        }

        /// <summary>
        /// Gets an exising span, or creates a new one with the given span id.
        /// </summary>
        /// <param name="spanId">The span id.</param>
        /// <returns>The created, or existing span.</returns>
        private OtelSpanContext GetOrCreateExistingSpanContextInternal(string id)
        {
            OtelSpanContext result;

            if (this.spanContexts.TryGetValue(id, out var value))
            {
                result = value;
            }
            else
            {
                result = new OtelSpanContext();
                result.Id = id;
                result.SpanId = this.traceIdProvider.CreateSpanId();
                result.TraceId = this.traceIdProvider.CreateTraceId();
                this.spanContexts[id] = result;
            }

            return result;
        }

        /// <summary>
        /// Stores or updates a response subscription.
        /// 
        /// An existing entry will only update values that are null. All other values will not be changed.
        /// </summary>
        /// <param name="topic">The topic to identify the subscription.</param>
        /// <param name="subscription">The internal subscription.</param>
        /// <param name="processor">The internal processor.</param>
        public void UpsertDynamicSubscription(string topic, MqttSubscription subscription, Processor processor)
        {
            if (this.dynamicSubscriptions.ContainsKey(topic))
            {
                var entry = this.dynamicSubscriptions[topic];
                entry.Processor = processor;
                entry.Subscription = subscription;
            }
            else
            {
                var data = new DynamicSubscriptionData(topic, subscription, processor);

                this.dynamicSubscriptions[topic] = data;
            }
        }

        /// <summary>
        /// Gets an exiting response subscription or creates a new one if none exists.
        /// </summary>
        /// <param name="topic">The topic name for receiving the subscription.</param>
        /// <returns>The existing or newly created response subscription.</returns>
        public DynamicSubscriptionData GetOrCreateDynamicSubscriptionData(string topic)
        {
            if (this.dynamicSubscriptions.ContainsKey(topic))
            {
                return this.dynamicSubscriptions[topic];
            }

            var newEntry = new DynamicSubscriptionData(topic);
            this.dynamicSubscriptions[topic] = newEntry;

            return newEntry;
        }


        /// <summary>
        /// Deletes all not needed subscriptions.
        /// </summary>
        public void CleanupDynamicSubscriptions()
        {
            foreach (var sub in this.dynamicSubscriptions)
            {
                if (sub.Value.CanBeDeleted)
                {
                    this.dynamicSubscriptions.Remove(sub.Key);
                    if (this.OnSubscriptionRemoved != null)
                    {
                        this.OnSubscriptionRemoved(this, sub.Value);
                    }
                }
            }
        }
    }
}
