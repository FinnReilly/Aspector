namespace Aspector.Core.Attributes.Validation
{
    /// <summary>
    /// Perform a validation action on all named parameters.
    /// 
    /// If none are named then no validation will happen.  If a named parameter does not exist then throw.
    /// </summary>
    public class ValidateAttribute : AspectAttribute
    {
        public List<string>? ParameterNames { get; }
        public bool ParameterNamesProvided => ParameterNames?.Any() == true;

        /// <param name="parameterNames">A list of named parameters to validate</param>
        public ValidateAttribute(params string[] parameterNames)
            : base()
        {
            ParameterNames = parameterNames.ToList();
        }
    }
}
