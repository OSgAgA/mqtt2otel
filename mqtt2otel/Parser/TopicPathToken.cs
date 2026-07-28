using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Parser
{
    /// <summary>
    /// Represents a segment of a topic path pattern.
    /// </summary>
    internal class TopicPathToken
    {
        /// <summary>
        /// Gets the path name, if this is a path segment, or empty string otherwise.
        /// </summary>
        internal string PathSegment { get; init; }

        /// <summary>
        /// Gets the skip count if this is not a path segment, or 0 otherwise.
        /// </summary>
        internal int SkipCount { get; init; }

        /// <summary>
        /// Gets a value indicating if this is a path segment.
        /// </summary>
        internal bool IsPathSegment { get; init; }

        /// <summary>
        /// Initializes a new <see cref="TopicPathToken"/> as a path segment.
        /// </summary>
        /// <param name="pathSegment">The segment name that should be matched by this token.</param>
        public TopicPathToken(string pathSegment)
        {
            this.PathSegment = pathSegment;
            this.SkipCount = 0;
            this.IsPathSegment = true;
        }

        /// <summary>
        /// Initializes a new <see cref="TopicPathToken"/> as a skip segment.
        /// </summary>
        /// <param name="skipCount">The number off segments that should be skipped.</param>
        public TopicPathToken(int skipCount)
        {
            this.PathSegment = string.Empty;
            this.SkipCount = skipCount;
            this.IsPathSegment = false;
        }
    }
}
