namespace Aspector.Core.Attributes.Logging
{
    /// <summary>
    /// Add a property to logging context.
    /// <br/><br/>
    /// This may be a constant or the value of a parameter
    /// </summary>
    public class AddLogPropertyAttribute : AspectAttribute
    {
        public string LoggingContextKey { get; }
        public string? LoggableParameterName { get; }
        public object? ConstantValue { get; }
        public bool IsConstant { get; }

        /// <param name="contextKey">The key to use for the log property</param>
        /// <param name="loggableParameterName">The name of the parameter to use as a value for the log property</param>
        public AddLogPropertyAttribute(string contextKey, string loggableParameterName)
        {
            LoggingContextKey = contextKey;
            LoggableParameterName = loggableParameterName;
        }

        /// <param name="contextKey">The key to use for the log property</param>
        /// <param name="constantValue">A constant value to use for the log property</param>
        public AddLogPropertyAttribute(string contextKey, object? constantValue)
        {
            LoggingContextKey = contextKey;
            ConstantValue = constantValue;
            IsConstant = true;
        }
    }
}
