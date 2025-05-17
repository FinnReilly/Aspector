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
        private readonly int _index;

        public BaseDecorator(ILoggerFactory loggerFactory, int index = 0)
        {
            _loggerFactory = loggerFactory;
            _thisType = this.GetType();
            _index = index;
        }

        public Type AttributeType { get; } = typeof(TAspect);


        public void Intercept(IInvocation invocation)
        {
            var aspectParameters = Enumerable.Empty<TAspect>();
            if (CachedReflection.AttributeSummariesByMethod.TryGetValue(invocation.Method, out var summary)
                && summary?.LayersByType.TryGetValue(typeof(TAspect), out var aspectLayers) == true
                && aspectLayers?.TryGetValue(_index, out var thisLayer) == true)
            {
                aspectParameters = thisLayer.Cast<TAspect>();
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
