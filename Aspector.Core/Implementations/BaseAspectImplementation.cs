using Aspector.Core.Attributes;
using Castle.DynamicProxy;
using System.Collections.Concurrent;
using System.Reflection;

namespace Aspector.Core.Implementations
{
    public abstract class BaseAspectImplementation<TAspect> : IInterceptor
        where TAspect : AspectAttribute
    {
        private ConcurrentDictionary<MethodInfo, IEnumerable<TAspect>> _perMethodAspectParameters = new ConcurrentDictionary<MethodInfo, IEnumerable<TAspect>>();

        public void Intercept(IInvocation invocation)
        {
            if (!_perMethodAspectParameters.TryGetValue(invocation.Method, out var aspectParameters))
            {
                var actualAttributes = invocation.Method.GetCustomAttributes<TAspect>();

                _perMethodAspectParameters.TryAdd(invocation.Method, actualAttributes);
                aspectParameters = actualAttributes;
            }

            if (aspectParameters == null)
            {
                invocation.Proceed();
                return;
            }

            Decorate(invocation, aspectParameters);
        }

        protected abstract void Decorate(IInvocation invocation, IEnumerable<TAspect> aspectParameters);
    }
}
