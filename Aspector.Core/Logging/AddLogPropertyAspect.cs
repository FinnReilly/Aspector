using Aspector.Core.Attributes.Logging;
using Aspector.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aspector.Core.Logging
{
    public class AddLogPropertyAspect : VoidDecorator<AddLogPropertyAttribute>
    {
        public AddLogPropertyAspect(ILoggerFactory loggerFactory, int layerIndex) 
            : base(loggerFactory, layerIndex)
        {
        }

        protected override void Decorate(
            Action<object[]?> targetMethod,
            object[]? parameters,
            DecorationContext context,
            IEnumerable<AddLogPropertyAttribute> aspectParameters)
        {
            var logger = GetLogger(context.DecoratedType);
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
