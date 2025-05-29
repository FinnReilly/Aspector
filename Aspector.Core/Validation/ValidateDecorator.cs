using Aspector.Core.Attributes.Validation;
using Aspector.Core.Models;
using Aspector.Core.Services;

namespace Aspector.Core.Validation
{
    public abstract class ValidateDecorator<TAspect> : VoidDecorator<TAspect>
        where TAspect : ValidateAttribute
    {
        protected ValidateDecorator(IDecoratorServices services, int layerIndex) : base(services, layerIndex)
        {
        }

        protected override sealed void Decorate(
            Action<object[]?> targetMethod,
            object[]? parameters,
            DecorationContext context,
            IEnumerable<TAspect> aspectParameters)
        {
            var logger = GetLogger(context);
            foreach (var aspectParameter in aspectParameters)
            {
                var retrievedParameters = GetParametersToValidate(aspectParameter, context, parameters);

                foreach(var parameter in retrievedParameters)
                {
                    ValidateParameterOrThrow(parameter);
                }
            }

            targetMethod(parameters);
        }

        protected virtual object[] GetParametersToValidate(TAspect aspectParameter, DecorationContext context, object[]? allParameters)
        {
            if (aspectParameter.ParameterNamesProvided)
            {
                return aspectParameter.ParameterNames!.Select(name => context.GetParameterByName(name, allParameters!)).ToArray();
            }

            return [];
        }

        protected abstract void ValidateParameterOrThrow(object parameterValue);
    }
}
