namespace Aspector.Core.Attributes.Logging
{
    public class AddLogPropertyAsyncAttribute : AddLogPropertyAttribute
    {
        public AddLogPropertyAsyncAttribute(string contextKey, string loggableParameterName)
            : base(contextKey, loggableParameterName)
        {
        }

        public AddLogPropertyAsyncAttribute(string contextKey, object? constantValue) 
            : base(contextKey)
        {
        }
    }
}
