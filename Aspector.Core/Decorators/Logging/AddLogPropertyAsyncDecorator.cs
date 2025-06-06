using Aspector.Core.Attributes.Logging;
using Aspector.Core.Decorators;
using Aspector.Core.Models;
using Aspector.Core.Services;

namespace Aspector.Core.Decorators.Logging
{
    public class AddLogPropertyAsyncDecorator : AsyncDecorator<AddLogPropertyAsyncAttribute>
    {
        public AddLogPropertyAsyncDecorator(IDecoratorServices services, int layerIndex) : base(services, layerIndex)
        {
        }

        protected override async Task Decorate(
            Func<object[]?, Task> targetMethod,
            object[]? parameters,
            DecorationContext context,
            IEnumerable<AddLogPropertyAsyncAttribute> aspectParameters)
        {
            var logger = GetLogger(context);
            var logscopeDictionary = new Dictionary<string, object?>();

            foreach (var aspectParameter in aspectParameters)
            {
                if (aspectParameter.IsConstant)
                {
                    logscopeDictionary[aspectParameter.LoggingContextKey] = aspectParameter.ConstantValue;
                    continue;
                }

                logscopeDictionary[aspectParameter.LoggingContextKey] = context.GetParameterByName(aspectParameter.LoggableParameterName!, parameters!);
            }

            using var logScope = logger.BeginScope(logscopeDictionary);

            await targetMethod(parameters);
        }
    }
}
