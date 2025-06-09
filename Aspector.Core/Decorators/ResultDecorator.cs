using Aspector.Core.Attributes;
using Aspector.Core.Models;
using Aspector.Core.Services;
using Castle.DynamicProxy;
using System.Reflection;

namespace Aspector.Core.Decorators
{
    public abstract class ResultDecorator<TAspect, TResult> : BaseDecorator<TAspect>
        where TAspect : AspectAttribute
    {
        protected ResultDecorator(IDecoratorServices services, int layerIndex) 
            : base(services, layerIndex)
        {
        }

        protected override sealed void Decorate(IInvocation invocation, IEnumerable<TAspect> aspectParameters)
        {
            Func<object[]?, TResult> targetMethodAsAction = (args) =>
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
                return (TResult)invocation.ReturnValue!;
            };

            var decoratorResult = Decorate(
                targetMethodAsAction,
                invocation.Arguments!,
                DecorationContext.FromInvocation(invocation, _globalCancellationToken),
                aspectParameters);

            invocation.ReturnValue = decoratorResult;
        }

        protected abstract TResult Decorate(
            Func<object[]?, TResult> targetMethod,
            object[]? parameters,
            DecorationContext context,
            IEnumerable<TAspect> aspectParameters);

        public override Task ValidateUsageOrThrowAsync(
            IEnumerable<ParameterInfo> parameters,
            MethodInfo method,
            TAspect parameter,
            CancellationToken token)
        {
            if (method.ReturnType == typeof(void) || !method.ReturnType.IsAssignableTo(typeof(TResult)))
            {
                throw new InvalidOperationException(
                    $"A {typeof(TAspect).FullName} can only be used on a method that returns a {typeof(TResult).FullName} or derived type");
            }

            return base.ValidateUsageOrThrowAsync(parameters, method, parameter, token);
        }
    }
}
