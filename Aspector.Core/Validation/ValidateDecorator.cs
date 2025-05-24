using Aspector.Core.Attributes.Validation;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Aspector.Core.Validation
{
    public abstract class ValidateDecorator<TAspect> : VoidDecorator<TAspect>
        where TAspect : ValidateAttribute
    {
        protected ValidateDecorator(ILoggerFactory loggerFactory, int layerIndex) : base(loggerFactory, layerIndex)
        {
        }

        protected override sealed void Decorate(
            Action<object[]?> targetMethod,
            object[]? parameters,
            (ParameterInfo[] ParameterMetadata, MethodInfo DecoratedMethod, Type DecoratedType) decorationContext,
            IEnumerable<TAspect> aspectParameters)
        {
            var logger = GetLogger(decorationContext.DecoratedType);
            foreach (var aspectParameter in aspectParameters)
            {
                var retrievedParameters = GetParametersToValidate(aspectParameter, decorationContext.ParameterMetadata, parameters);

                foreach(var parameter in retrievedParameters)
                {
                    ValidateParameterOrThrow(parameter);
                }
            }

            targetMethod(parameters);
        }

        protected virtual object[] GetParametersToValidate(TAspect aspectParameter, ParameterInfo[] metadata, object[]? allParameters)
        {
            if (aspectParameter.ParameterNamesProvided)
            {
                return aspectParameter.ParameterNames!.Select(name => GetParameterByName(name, metadata, allParameters!)).ToArray();
            }

            return [];
        }

        protected abstract void ValidateParameterOrThrow(object parameterValue);
    }
}
