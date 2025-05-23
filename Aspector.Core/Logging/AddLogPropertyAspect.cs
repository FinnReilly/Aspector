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
    public class AddLogPropertyAspect : VoidDecorator<AddLogPropertyAttribute>
    {
        public AddLogPropertyAspect(ILoggerFactory loggerFactory, int layerIndex) 
            : base(loggerFactory, layerIndex)
        {
        }

        protected override void Decorate(
            Action<object[]?> targetMethod,
            object[]? parameters,
            (ParameterInfo[] ParameterMetadata, MethodInfo DecoratedMethod, Type DecoratedType) decorationContext,
            IEnumerable<AddLogPropertyAttribute> aspectParameters)
        {
            var logger = GetLogger(decorationContext.DecoratedType);
            var logscopeDictionary = new Dictionary<string, object?>();

            foreach(var aspectParameter in aspectParameters)
            {
                if (aspectParameter.IsConstant)
                {
                    logscopeDictionary[aspectParameter.LoggingContextKey] = aspectParameter.ConstantValue;
                    continue;
                }

                logscopeDictionary[aspectParameter.LoggingContextKey] = GetParameterByName(aspectParameter.LoggableParameterName!, decorationContext.ParameterMetadata, parameters!);
            }

            using var logScope = logger.BeginScope(logscopeDictionary);

            targetMethod(parameters);
        }
    }
}
