using Aspector.Core.Attributes;
using Aspector.Core.Services;
using System.Reflection;

namespace Aspector.Core.Decorators
{
    public abstract class AsyncResultDecorator<TAspect, TFinalResult> : ResultDecorator<TAspect, Task<TFinalResult>>
        where TAspect : AspectAttribute
    {
        protected AsyncResultDecorator(IDecoratorServices services, int layerIndex) 
            : base(services, layerIndex)
        {
        }

        protected override Task ValidateUsageOrThrowAsync(IEnumerable<ParameterInfo> parameters, MethodInfo method, TAspect parameter, CancellationToken token)
        {
            var returnType = method.ReturnType;
            if (returnType.IsAssignableTo(typeof(Task))
                && returnType.IsGenericType
                && returnType.GenericTypeArguments.Length == 1
                && returnType.GenericTypeArguments[0].IsAssignableTo(typeof(TFinalResult)))
            {
                return Task.CompletedTask;
            }

            throw new InvalidOperationException(
                $"The {typeof(TAspect).FullName} can only be used on a method that returns a {typeof(Task<TFinalResult>).FullName} or derived type");
        }
    }
}
