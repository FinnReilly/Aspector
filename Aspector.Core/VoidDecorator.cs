using Aspector.Core.Attributes;
using Aspector.Core.Models;
using Aspector.Core.Services;
using Castle.DynamicProxy;

namespace Aspector.Core
{
    public abstract class VoidDecorator<TAspect> : BaseDecorator<TAspect>
        where TAspect : AspectAttribute
    {
        protected VoidDecorator(IDecoratorServices services, int layerIndex) 
            : base(services, layerIndex)
        {
        }

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
                DecorationContext.FromInvocation(invocation, _globalCancellationToken),
                aspectParameters);
        }

        protected abstract void Decorate(
            Action<object[]?> targetMethod,
            object[]? parameters,
            DecorationContext context,
            IEnumerable<TAspect> aspectParameters);
    }
}
