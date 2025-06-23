namespace Aspector.Core.Attributes.Logging
{
    /// <summary>
    /// Add a property to logging context.
    /// <br/><br/>
    /// This may be a constant or the value of a parameter
    /// </summary>
    public class AddLogPropertyAttribute : AspectAttribute
    {
        private object? _constantValue;
        private string? _parameterName;

        public string LoggingContextKey { get; }

        /// <summary>
        /// The name of the parameter to use as a value for the log property
        /// <br/><br/>
        /// Attention - setting this will override any constant value previously set in this attribute instance
        /// </summary>
        public string? LoggableParameterName 
        { 
            get => _parameterName;
            set
            {
                _parameterName = value;
                IsConstant = true;
            }
        }

        /// <summary>
        /// A constant value to use for the log property
        /// <br/><br/>
        /// Attention - setting this will override any parameter name configured in this attribute instance
        /// </summary>
        public object? ConstantValue 
        {
            get => _constantValue;  
            set
            {
                _constantValue = value;
                IsConstant = true;
            }
        }

        public bool IsConstant { get; private set; }

        /// <param name="contextKey">The key to use for the log property</param>
        /// <param name="loggableParameterName">The name of the parameter to use as a value for the log property</param>
        public AddLogPropertyAttribute(string contextKey, string loggableParameterName)
        {
            LoggingContextKey = contextKey;
            LoggableParameterName = loggableParameterName;
        }

        /// <param name="contextKey">The key to use for the log property</param>
        public AddLogPropertyAttribute(string contextKey)
        {
            LoggingContextKey = contextKey;
        }
    }
}
