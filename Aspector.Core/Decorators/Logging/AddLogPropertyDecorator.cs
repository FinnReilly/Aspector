using Aspector.Core.Attributes.Logging;
using Aspector.Core.Decorators;
using Aspector.Core.Models;
using Aspector.Core.Services;

namespace Aspector.Core.Decorators.Logging
{
    public class AddLogPropertyDecorator : VoidDecorator<AddLogPropertyAttribute>
    {
        public AddLogPropertyDecorator(IDecoratorServices services, int layerIndex) 
            : base(services, layerIndex)
        {
        }

        protected override void Decorate(
            Action<object[]?> targetMethod,
            object[]? parameters,
            DecorationContext context,
            IEnumerable<AddLogPropertyAttribute> aspectParameters)
        {
            var logger = GetLogger(context);
            var logscopeDictionary = new Dictionary<string, object?>();

            foreach(var aspectParameter in aspectParameters)
            {
                if (aspectParameter.IsConstant)
                {
                    logscopeDictionary[aspectParameter.LoggingContextKey] = aspectParameter.ConstantValue;
                    continue;
                }

                logscopeDictionary[aspectParameter.LoggingContextKey] = context.GetParameterByName(aspectParameter.LoggableParameterName!, parameters!);
            }

            using var logScope = logger.BeginScope(logscopeDictionary);

            targetMethod(parameters);
        }
    }
}
