using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Defines the available strategies to apply when the retention time of a trace (or span) has exceeded.
    /// </summary>
    public enum SpanRetentionStrategy
    {
        /// <summary>
        /// Completes the span and keeps the current status.
        /// </summary>
        CompleteWithCurrentStatus,

        /// <summary>
        /// Completes the span and keeps the current status. Adds an event, that marks the span as retention exceeded.
        /// </summary>
        CompleteWithCurrentStatusAndEvent,

        /// <summary>
        /// Completes the span and sets the status to ok.
        /// </summary>
        CompleteOk,

        /// <summary>
        /// Completes the span and sets the status to ok. Adds an event, that marks the span as retention exceeded.
        /// </summary>
        CompleteOkWithEvent,

        /// <summary>
        /// Completes the span and sets the status to unset.
        /// </summary>
        CompleteUnset,

        /// <summary>
        /// Completes the span and sets the status to unset. Adds an event, that marks the span as retention exceeded.
        /// </summary>
        CompleteUnsetWithEvent,

        /// <summary>
        /// Completes the span and sets the status to error.
        /// </summary>
        CompleteError,

        /// <summary>
        /// Completes the span and sets the status to error. Adds an event, that marks the span as retention exceeded.
        /// </summary>
        CompleteErrorWithEvent,

        /// <summary>
        /// Ignores the span.
        /// </summary>
        Ignore,

        /// <summary>
        /// Throws an error.
        /// </summary>
        ThrowError
    }
}
