namespace Aspector.Core.Attributes.Logging
{
    /// <summary>
    /// Add a property to logging context for an async method.
    /// <br/><br/>
    /// This may be a constant or the value of a parameter
    /// </summary>
    public class AddLogPropertyAsyncAttribute : AddLogPropertyAttribute
    {
        /// <inheritdoc/>
        public AddLogPropertyAsyncAttribute(string contextKey, string loggableParameterName)
            : base(contextKey, loggableParameterName)
        {
        }

        /// <inheritdoc/>
        public AddLogPropertyAsyncAttribute(string contextKey, object? constantValue) 
            : base(contextKey, constantValue)
        {
        }
    }
}
