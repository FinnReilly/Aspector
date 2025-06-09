using Aspector.Core.Attributes;
using Aspector.Core.Models;
using Aspector.Core.Services;
using Aspector.Core.Static;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;

namespace Aspector.Core.Decorators
{
    public abstract class BaseDecorator<TAspect> : IInterceptor
        where TAspect : AspectAttribute
    {
        private ConcurrentDictionary<MethodInfo, IEnumerable<TAspect>> _perMethodAspectParameters = new ConcurrentDictionary<MethodInfo, IEnumerable<TAspect>>();

        private readonly ILoggerFactory _loggerFactory;
        private readonly Type _thisType;
        private readonly int LayerIndex;
        protected readonly CancellationToken _globalCancellationToken;

        public BaseDecorator(IDecoratorServices services, int layerIndex)
        {
            _loggerFactory = services;
            _thisType = GetType();
            LayerIndex = layerIndex;
            _globalCancellationToken = services.GlobalToken;
        }

        public Type AttributeType { get; } = typeof(TAspect);

        public void Intercept(IInvocation invocation)
        {
            var aspectParameters = Enumerable.Empty<TAspect>();

            if (invocation.MethodInvocationTarget != null
                && !_perMethodAspectParameters.TryGetValue(invocation.MethodInvocationTarget, out aspectParameters)
                && CachedReflection.AttributeSummariesByClass.TryGetValue(invocation.TargetType!, out var summary)
                && summary.LayersByType.TryGetValue(invocation.MethodInvocationTarget!, out var aspectLayerMap)
                && aspectLayerMap.TryGetValue(AttributeType, out var attributeLayers)
                && attributeLayers.TryGetValue(LayerIndex, out var thisAttributeLayer))
            {
                var actualAttributes = thisAttributeLayer.Cast<TAspect>() ?? Enumerable.Empty<TAspect>();
                _perMethodAspectParameters.TryAdd(invocation.MethodInvocationTarget, actualAttributes);
                aspectParameters = actualAttributes;
            }

            if (aspectParameters?.Any() != true)
            {
                invocation.Proceed();
                return;
            }

            Decorate(invocation, aspectParameters);
        }

        protected abstract void Decorate(IInvocation invocation, IEnumerable<TAspect> aspectParameters);

        private string LoggerName(Type targetType) => $"{targetType.FullName}:{_thisType.FullName}";
        
        protected ILogger GetLogger(DecorationContext context) => _loggerFactory.CreateLogger(LoggerName(context.DecoratedType));

        public virtual Task ValidateUsageOrThrowAsync(IEnumerable<ParameterInfo> parameters, MethodInfo method, TAspect parameter, CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}
