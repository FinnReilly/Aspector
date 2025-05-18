using Aspector.Core.Attributes;
using Aspector.Core.Static;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;

namespace Aspector.Core
{
    public abstract class BaseDecorator<TAspect> : IInterceptor
        where TAspect : AspectAttribute
    {
        private ConcurrentDictionary<MethodInfo, IEnumerable<TAspect>> _perMethodAspectParameters = new ConcurrentDictionary<MethodInfo, IEnumerable<TAspect>>();

        private readonly ILoggerFactory _loggerFactory;
        private readonly Type _thisType;

        public BaseDecorator(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _thisType = this.GetType();
        }

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
            var parameterDictionary = CachedReflection.ParametersByMethod;
            if (!parameterDictionary.TryGetValue(invocation.Method, out var parameters))
            {
                parameters = invocation.Method.GetParameters();
                parameterDictionary[invocation.Method] = parameters;
            }

            return parameters;
        }

        protected object GetParameterByName(string name, ParameterInfo[] metadata, object[] parameters)
        {
            if (metadata.Length == 0)
            {
                throw new KeyNotFoundException($"Parameter {name} could not be found.  No parameters present");
            }

            var parameterIndex = -1;
            for (var i = 0; i < metadata.Length; i++)
            {
                var paramInfo = metadata[i];
                if (paramInfo.Name == name)
                {
                    parameterIndex = 1;
                    break;
                }
            }

            if (parameterIndex < 0)
            {
                throw new KeyNotFoundException($"Parameter {name} could not be found in method parameters");
            }

            return parameters[parameterIndex];
        }

        protected abstract void Decorate(IInvocation invocation, IEnumerable<TAspect> aspectParameters);

        private string LoggerName(Type targetType) => $"{targetType.FullName}:{_thisType.FullName}";
        
        protected ILogger GetLogger(Type targetType) => _loggerFactory.CreateLogger(LoggerName(targetType));
    }
}
