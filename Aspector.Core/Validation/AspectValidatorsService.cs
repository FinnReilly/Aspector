using Aspector.Core.Attributes;
using Aspector.Core.Decorators;
using Aspector.Core.Static;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Aspector.Core.Validation
{
    public class AspectValidatorsService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AspectValidatorsService> _logger;

        public AspectValidatorsService(
            IServiceProvider serviceProvider,
            ILogger<AspectValidatorsService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var decoratorsByAttributeType = new Dictionary<Type, Type>();

            var allExceptions = new List<Exception>();

            foreach (var methodMap in CachedReflection.ParametersByMethod)
            {
                var method = methodMap.Key;
                var parameters = methodMap.Value;
                var decoratedType = method.DeclaringType;

                var aspectUsageSummary = CachedReflection.AttributeSummariesByClass[decoratedType!];

                if (!aspectUsageSummary.LayersFromInnermostByMethod.TryGetValue(method, out var layers))
                {
                    continue;
                }

                foreach (var layer in layers) 
                {
                    var matchingDecoratorImplementation = CachedReflection.DecoratorTypesByAspectAttribute[layer.AspectType];

                    try
                    {

                        await ValidateForAttribute(
                            layer.AspectType,
                            matchingDecoratorImplementation,
                            parameters,
                            method,
                            layer,
                            cancellationToken);
                    }
                    catch(Exception e)
                    {
                        _logger.LogError(
                            e,
                            "An error was thrown while analysing aspect usage within your application : {ExceptionMessage}",
                            e.Message);

                        allExceptions.Add(e);
                    }
                }
            }

            if (allExceptions.Count > 0)
            {
                throw new AggregateException(allExceptions);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private Task ValidateForAttribute(
            Type attributeType,
            Type implementedDecoratorType,
            IEnumerable<ParameterInfo> parameters,
            MethodInfo method,
            IEnumerable<object> actualAspectAttributes,
            CancellationToken token)
        {
            var service = _serviceProvider.GetRequiredService(implementedDecoratorType);

            var methodToCall = implementedDecoratorType.GetMethod(nameof(BaseDecorator<AspectAttribute>.ValidateUsageOrThrowAsync));
            var allMethodCalls = actualAspectAttributes.Select(
                attr =>
                    (Task)methodToCall!.Invoke(service, [parameters, method, attr, token])!);

            return Task.WhenAll(allMethodCalls);
        }
    }
}
