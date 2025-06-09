using Aspector.Core.Attributes;
using Aspector.Core.Decorators;
using Aspector.Core.Models.Registration;
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
            // it feels more helpful to enumerate ALL usage mistakes early on than to fail one at a time
            var allExceptions = new List<Exception>();

            foreach (var decoratedClass in CachedReflection.AttributeSummariesByClass)
            {
                var decoratedType = decoratedClass.Key;
                var aspectUsageSummary = decoratedClass.Value;

                foreach (var decoratedMethod in aspectUsageSummary.LayersFromInnermostByMethod)
                {
                    var method = decoratedMethod.Key;
                    var parameters = method.GetParameters();
                    foreach (var layer in decoratedMethod.Value)
                    {
                        var matchingDecoratorImplementation = CachedReflection.DecoratorTypesByAspectAttribute[layer.AspectType];

                        try
                        {
                            await ValidateForAttribute(
                                matchingDecoratorImplementation,
                                parameters,
                                method,
                                layer,
                                cancellationToken);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(
                                e,
                                "An error was thrown while analysing aspect usage within your application : {ExceptionMessage}",
                                e.InnerException?.Message ?? e.Message);

                            allExceptions.Add(e.InnerException ?? e);
                        }
                    }
                }
            }

            if (allExceptions.Count > 0)
            {
                // stop host
                throw new AggregateException(allExceptions);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private Task ValidateForAttribute(
            Type implementedDecoratorType,
            IEnumerable<ParameterInfo> parameters,
            MethodInfo method,
            AspectAttributeLayer actualAspectAttributes,
            CancellationToken token)
        {
            var service = _serviceProvider.GetRequiredKeyedService(implementedDecoratorType, actualAspectAttributes.LayerIndex);

            var methodToCall = implementedDecoratorType.GetMethod(nameof(BaseDecorator<AspectAttribute>.ValidateUsageOrThrowAsync));
            var allMethodCalls = actualAspectAttributes.Select(
                attr =>
                    (Task)methodToCall!.Invoke(service, [parameters, method, attr, token])!);

            return Task.WhenAll(allMethodCalls);
        }
    }
}
