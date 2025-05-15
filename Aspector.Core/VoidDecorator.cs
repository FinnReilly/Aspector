using Aspector.Core.Attributes;
using Castle.DynamicProxy;
using System.Reflection;

namespace Aspector.Core
{
    public abstract class VoidDecorator<TAspect> : BaseAspectImplementation<TAspect>
        where TAspect : AspectAttribute
    {
        protected override sealed void Decorate(IInvocation invocation, IEnumerable<TAspect> aspectParameters)
        {
            Action<object[]?> targetMethodAsAction = (args) =>
            {
                args = args ?? Array.Empty<object>(); 
                for (var i = 0; i < invocation.Arguments.Length; i++)
                {
                    if (args[i] != invocation.Arguments[i])
                    {
                        invocation.SetArgumentValue(i, args[i]);
                    }
                }
                invocation.Proceed();
            };

            Decorate(
                targetMethodAsAction,
                invocation.Arguments!,
                (GetMethodParameterMetadata(invocation), invocation.Method, invocation.TargetType!),
                aspectParameters);
        }

        protected abstract void Decorate(
            Action<object[]?> targetMethod,
            object[]? parameters,
            (IEnumerable<ParameterInfo> ParameterMetadata, MethodInfo DecoratedMethod, Type DecoratedType) decorationContext,
            IEnumerable<TAspect> aspectParameters);
    }
}
