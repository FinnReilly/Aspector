using Microsoft.Extensions.Logging;

namespace Aspector.Core.Attributes.Logging
{
    /// <summary>
    /// Logs a message as specified by attribute parameters
    /// </summary>
    public class LogAttribute : AspectAttribute
    {
        public string LogString { get; }
        public LogLevel Level { get; }
        public List<string> ParametersForLogging { get; } = new List<string>();

        /// <param name="logString">A string to log</param>
        /// <param name="level">The log level to use.  Default is <see cref="LogLevel.Information"/></param>
        public LogAttribute(string logString, LogLevel level = LogLevel.Information)
        {
            LogString = logString;
            Level = level;
        }

        /// <param name="logString">A format string to log</param>
        /// <param name="level">The log level to use.  Default is <see cref="LogLevel.Information"/></param>
        /// <param name="parametersForLogging">The name of the parameters whose values will be used in the format string</param>
        public LogAttribute(string logString, LogLevel level, params string[] parametersForLogging) : this(logString, level)
        {
            ParametersForLogging = parametersForLogging.ToList();
        }
    }
}
