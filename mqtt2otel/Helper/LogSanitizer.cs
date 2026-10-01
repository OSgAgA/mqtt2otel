using System.Text;

namespace mqtt2otel.Helper
{
    /// <summary>
    /// Responsible for sanitizing messages before writing them to logs, to avoid log forgin (see CWE-117).
    /// </summary>
    public static class LogSanitizer
    {
        /// <summary>
        /// Neutralizes characters that could be used for log forging or terminal
        /// escape-sequence injection when the value is written to an unstructured sink.
        /// </summary>
        public static string SanitizeForLog(this string value, int maxLength = 1000)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            if (value.Length > maxLength)
            {
                value = value[..maxLength] + "...[truncated]";
            }

            var sb = new StringBuilder(value.Length);

            foreach (var c in value)
            {
                switch (c)
                {
                    case '\r': sb.Append("\\r"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        // Strip other C0/C1 control characters (includes ESC, used for
                        // ANSI/terminal escape sequence injection in console sinks).
                        if (char.IsControl(c))
                            sb.Append('.');
                        else
                            sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }
    }
}
