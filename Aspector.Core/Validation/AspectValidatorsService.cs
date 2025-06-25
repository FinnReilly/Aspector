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
                                onException: e => RecordUsageException(e, allExceptions),
                                cancellationToken);
                        }
                        catch (Exception e)
                        {
                            RecordUsageException(e, allExceptions);
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

        private void RecordUsageException(Exception exception, List<Exception> exceptionCollection)
        {
            _logger.LogError(
                exception,
                "An error was thrown while analysing aspect usage within your application : {ExceptionMessage}",
                exception.InnerException?.Message ?? exception.Message);

            exceptionCollection.Add(exception.InnerException ?? exception);
        }

        private Task ValidateForAttribute(
            Type implementedDecoratorType,
            IEnumerable<ParameterInfo> parameters,
            MethodInfo method,
            AspectAttributeLayer actualAspectAttributes,
            Action<Exception> onException,
            CancellationToken token)
        {
            var service = _serviceProvider.GetRequiredKeyedService(implementedDecoratorType, actualAspectAttributes.LayerIndex);

            var methodToCall = implementedDecoratorType.GetMethod(nameof(BaseDecorator<AspectAttribute>.ValidateUsagesAsync));

            return (Task)methodToCall!.Invoke(service, [parameters, method, actualAspectAttributes, onException, token])!;
        }
    }
}
