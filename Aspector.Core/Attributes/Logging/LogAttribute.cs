using Microsoft.Extensions.Logging;

namespace Aspector.Core.Attributes.Logging
{
    public class LogAttribute : AspectAttribute
    {
        public string LogString { get; }
        public LogLevel Level { get; }
        public List<string> ParametersForLogging { get; } = new List<string>();

        public LogAttribute(string logString, LogLevel level = LogLevel.Information)
        {
            LogString = logString;
        }

        public LogAttribute(string logString, LogLevel level, List<string> parametersForLogging) : this(logString, level)
        {
            ParametersForLogging = parametersForLogging;
        }
    }
}
