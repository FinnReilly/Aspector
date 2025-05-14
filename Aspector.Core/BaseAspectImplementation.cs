using Aspector.Core.Attributes;
using Castle.DynamicProxy;
using System.Collections.Concurrent;
using System.Reflection;

namespace Aspector.Core
{
    public abstract class BaseAspectImplementation<TAspect> : IInterceptor
        where TAspect : AspectAttribute
    {
        private ConcurrentDictionary<MethodInfo, IEnumerable<TAspect>> _perMethodAspectParameters = new ConcurrentDictionary<MethodInfo, IEnumerable<TAspect>>();
        private ConcurrentDictionary<MethodInfo, ParameterInfo[]> _perMethodParameters = new ConcurrentDictionary<MethodInfo, ParameterInfo[]>();

        public Type AttributeType { get; } = typeof(TAspect);

        public void Intercept(IInvocation invocation)
        {
            var aspectParameters = Enumerable.Empty<TAspect>();
            if (invocation.MethodInvocationTarget != null && !_perMethodAspectParameters.TryGetValue(invocation.MethodInvocationTarget, out aspectParameters))
            {
                var actualAttributes = invocation.MethodInvocationTarget.GetCustomAttributes<TAspect>();

                _perMethodAspectParameters.TryAdd(invocation.Method, actualAttributes);
                aspectParameters = actualAttributes;
            }

            if (!aspectParameters.Any())
            {
                invocation.Proceed();
                return;
            }

            Decorate(invocation, aspectParameters);
        }

        protected ParameterInfo[] GetMethodParameterMetadata(IInvocation invocation)
        {
            if (!_perMethodParameters.TryGetValue(invocation.Method, out var parameters))
            {
                parameters = invocation.Method.GetParameters();
                _perMethodParameters[invocation.Method] = parameters;
            }

            return parameters;
        }

        protected abstract void Decorate(IInvocation invocation, IEnumerable<TAspect> aspectParameters);
    }
}
