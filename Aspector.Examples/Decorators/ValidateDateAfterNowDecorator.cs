using Aspector.Core.Decorators.Validation;
using Aspector.Core.Services;
using Aspector.Examples.Attributes;

namespace Aspector.Examples.Decorators
{
    public class ValidateDateAfterNowDecorator : ValidateDecorator<ValidateDateAfterNowAttribute>
    {
        public ValidateDateAfterNowDecorator(
            IDecoratorServices services,
            int layerIndex) : base(services, layerIndex)
        {
        }

        protected override void ValidateParameterOrThrow(object? parameterValue)
        {
            if (parameterValue == null
                || parameterValue is not DateTime dateTimeParameter
                || dateTimeParameter < DateTime.UtcNow)
            {
                throw new ArgumentException("Date time provided must be in future");
            }
        }
    }
}
