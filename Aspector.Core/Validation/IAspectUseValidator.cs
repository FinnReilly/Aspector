using Aspector.Core.Attributes;
using Aspector.Core.Decorators;
using System.Reflection;

namespace Aspector.Core.Validation
{
    public interface IAspectUseValidator<TAspectAttribute, TDecorator>
        where TAspectAttribute : AspectAttribute
        where TDecorator : BaseDecorator<TAspectAttribute>
    {
        Task ValidateOrThrowAsync(
            IEnumerable<ParameterInfo> parameters,
            MethodInfo method,
            TAspectAttribute attributeParameter,
            CancellationToken token);
    }
}
