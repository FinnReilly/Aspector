using Aspector.Core.Attributes.Validation;
using Aspector.Core.Models;
using Aspector.Core.Services;
using System.Reflection;

namespace Aspector.Core.Decorators.Validation
{
    public abstract class ValidateDecorator<TAspect, TParam> : ValidateDecorator<TAspect>
        where TAspect : ValidateAttribute<TParam>
    {
        protected ValidateDecorator(IDecoratorServices services, int layerIndex) 
            : base(services, layerIndex)
        {
        }

        protected override object?[] GetParametersToValidate(TAspect aspectParameter, DecorationContext context, object[]? allParameters)
        {
            if (aspectParameter.ParameterNamesProvided)
            {
                return aspectParameter.ParameterNames?
                    .Select(name => context.GetParameterByName<TParam>(name, allParameters!))
                    .Cast<object?>()
                    .ToArray() ?? [];
            }

            return [];
        }

        protected override Task ValidateUsageOrThrowAsync(
            IEnumerable<ParameterInfo> parameters,
            MethodInfo method,
            TAspect parameter,
            CancellationToken token)
        {
            if (parameter.ParameterNamesProvided)
            {
                foreach(var requestedTypedParam in parameter.ParameterNames ?? [])
                {
                    var matchingParameter = parameters.FirstOrDefault(
                        p => p.ParameterType == typeof(TParam)
                            && p.Name == requestedTypedParam);

                    if (matchingParameter == null)
                    {
                        throw new InvalidOperationException(
                            $"The {typeof(TAspect).FullName} requested validation of a {typeof(TParam).FullName} parameter with the name '{requestedTypedParam}', but none was found");
                    }
                }
            }

            throw new InvalidOperationException("No parameter names provided");
        }
    }
}
