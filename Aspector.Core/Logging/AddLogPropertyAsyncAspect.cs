using Aspector.Core.Attributes.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Aspector.Core.Logging
{
    public class AddLogPropertyAsyncAspect : AsyncDecorator<AddLogPropertyAsyncAttribute>
    {
        public AddLogPropertyAsyncAspect(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
        }

        protected override async Task Decorate(
            Func<object[]?, Task> targetMethod,
            object[]? parameters,
            (ParameterInfo[] ParameterMetadata, MethodInfo DecoratedMethod, Type DecoratedType) decorationContext,
            IEnumerable<AddLogPropertyAsyncAttribute> aspectParameters)
        {
            var logger = GetLogger(decorationContext.DecoratedType);
            var logscopeDictionary = new Dictionary<string, object?>();

            foreach (var aspectParameter in aspectParameters)
            {
                if (aspectParameter.IsConstant)
                {
                    logscopeDictionary[aspectParameter.LoggingContextKey] = aspectParameter.ConstantValue;
                    continue;
                }

                logscopeDictionary[aspectParameter.LoggingContextKey] = GetParameterByName(aspectParameter.LoggableParameterName!, decorationContext.ParameterMetadata, parameters!);
            }

            using var logScope = logger.BeginScope(logscopeDictionary);

            await targetMethod(parameters);
        }
    }
}
