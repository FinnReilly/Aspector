using Aspector.Core.Attributes.Validation;

namespace Aspector.Examples.Attributes
{
    public class ValidateDateAfterNowAttribute : ValidateAttribute<DateTime>
    {
        public ValidateDateAfterNowAttribute(params string[] parameterNames) : base(parameterNames)
        {
        }
    }
}
